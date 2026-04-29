#!/usr/bin/env python3
"""Python AI worker entrypoint."""

from __future__ import annotations

import argparse
import importlib
import json
import sys
from pathlib import Path

from shared import AiObservation, AiTransition, BaseAiBrain

PROTOCOL_VERSION = 1


def emit(message: dict) -> None:
    sys.stdout.write(json.dumps(message) + "\n")
    sys.stdout.flush()


def load_brain(ai_type: str, rollout_path: Path) -> BaseAiBrain:
    checkpoint_path = rollout_path.with_name(f"{ai_type}_latest.pt")
    post_exploration_checkpoint_path = rollout_path.with_name(f"{ai_type}_after_exploration.pt")
    module = importlib.import_module(f"{ai_type}.brain")
    return module.create_brain(rollout_path, checkpoint_path, post_exploration_checkpoint_path)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--rollout-path", required=True)
    args = parser.parse_args()

    rollout_path = Path(args.rollout_path)
    rollout_path.parent.mkdir(parents=True, exist_ok=True)
    sys.path.insert(0, str(Path(__file__).resolve().parent))

    brains: dict[str, BaseAiBrain] = {}

    with rollout_path.open("a", encoding="utf-8") as rollout_file:
        emit({"type": "ready", "protocolVersion": PROTOCOL_VERSION})

        for raw_line in sys.stdin:
            line = raw_line.strip()
            if not line:
                continue

            message = json.loads(line)
            ai_type = str(message.get("observation", {}).get("aiType", "")).strip()
            if not ai_type:
                continue

            if ai_type not in brains:
                brains[ai_type] = load_brain(ai_type, rollout_path)

            brain = brains[ai_type]

            if message["type"] == "decide":
                observation = AiObservation.from_dict(message["observation"])
                action = brain.choose_action(observation)
                emit(
                    {
                        "type": "action",
                        "protocolVersion": PROTOCOL_VERSION,
                        "shipId": message["shipId"],
                        "sequence": int(message["sequence"]),
                        "throttle": action.throttle,
                        "turn": action.turn,
                        "fireLeft": action.fireLeft,
                        "fireRight": action.fireRight,
                        "debugState": action.debugState,
                    }
                )
                continue

            if message["type"] == "transition":
                brain.record_transition(AiTransition.from_message(message), rollout_file)

    for brain in brains.values():
        if brain.checkpoint_path is not None:
            brain.save_checkpoint(brain.checkpoint_path)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
