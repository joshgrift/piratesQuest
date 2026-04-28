#!/usr/bin/env python3
"""Local-dev Python AI worker for waypoint navigation experiments.

Protocol:
- reads newline-delimited JSON from stdin
- writes newline-delimited JSON to stdout
- appends `transition` messages to one rollout JSONL file

This v1 worker does not train. It returns a simple heuristic action so the
Godot side can validate the bridge, rollout logging, and shared-worker design.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

PROTOCOL_VERSION = 1


def clamp(value: float, minimum: float, maximum: float) -> float:
    return max(minimum, min(maximum, value))


def emit(message: dict) -> None:
    sys.stdout.write(json.dumps(message) + "\n")
    sys.stdout.flush()


def choose_action(observation: dict) -> tuple[float, float, str]:
    goal_x = float(observation.get("goalLocalX", 0.0))
    goal_z = float(observation.get("goalLocalZ", 0.0))
    distance_to_goal = float(observation.get("distanceToGoal", 0.0))
    forward = float(observation.get("forwardDistanceFraction", 1.0))
    forward_left = float(observation.get("forwardLeftDistanceFraction", 1.0))
    forward_right = float(observation.get("forwardRightDistanceFraction", 1.0))
    wide_left = float(observation.get("wideLeftDistanceFraction", 1.0))
    wide_right = float(observation.get("wideRightDistanceFraction", 1.0))
    left_pressure = float(observation.get("leftPressure", 0.0))
    right_pressure = float(observation.get("rightPressure", 0.0))
    front_blocked = bool(observation.get("frontBlocked", False))
    is_stuck = bool(observation.get("isStuck", False))
    is_escaping = bool(observation.get("isEscaping", False))

    if is_stuck or is_escaping:
        safer_turn = 1.0 if (forward_left + wide_left) < (forward_right + wide_right) else -1.0
        return -0.9, safer_turn, "Recover"

    if front_blocked or forward < 0.18:
        safer_turn = 1.0 if (forward_left + wide_left) < (forward_right + wide_right) else -1.0
        return -0.65, safer_turn, "Avoid Front"

    obstacle_bias = clamp((left_pressure - right_pressure) * 0.7, -0.6, 0.6)
    steering = clamp(goal_x * 1.8 + obstacle_bias, -1.0, 1.0)

    # `goal_z` is negative when the target is in front of the ship.
    if goal_z > 0.2:
        steering = clamp(steering + (1.0 if goal_x >= 0.0 else -1.0) * 0.5, -1.0, 1.0)

    if distance_to_goal > 0.4:
        throttle = 0.85
    elif distance_to_goal > 0.14:
        throttle = 0.45
    else:
        throttle = 0.18

    if min(forward_left, wide_left, forward_right, wide_right) < 0.12:
        throttle = min(throttle, 0.45)

    return throttle, steering, "Navigate"


def handle_message(message: dict, rollout_file) -> None:
    message_type = message.get("type")
    if message_type == "decide":
        observation = message.get("observation", {})
        throttle, turn, debug_state = choose_action(observation)
        emit(
            {
                "type": "action",
                "protocolVersion": PROTOCOL_VERSION,
                "shipId": message.get("shipId", ""),
                "sequence": int(message.get("sequence", 0)),
                "throttle": throttle,
                "turn": turn,
                "debugState": debug_state,
            }
        )
        return

    if message_type == "transition":
        rollout_file.write(json.dumps(message) + "\n")
        rollout_file.flush()
        return


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--rollout-path", required=True)
    args = parser.parse_args()

    rollout_path = Path(args.rollout_path)
    rollout_path.parent.mkdir(parents=True, exist_ok=True)

    with rollout_path.open("a", encoding="utf-8") as rollout_file:
        emit({"type": "ready", "protocolVersion": PROTOCOL_VERSION})

        for raw_line in sys.stdin:
            line = raw_line.strip()
            if not line:
                continue

            try:
                message = json.loads(line)
            except json.JSONDecodeError as exc:
                print(f"invalid json: {exc}", file=sys.stderr, flush=True)
                continue

            if int(message.get("protocolVersion", 0)) != PROTOCOL_VERSION:
                print("protocol version mismatch", file=sys.stderr, flush=True)
                continue

            try:
                handle_message(message, rollout_file)
            except Exception as exc:  # pragma: no cover - local dev guardrail
                print(f"worker error: {exc}", file=sys.stderr, flush=True)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
