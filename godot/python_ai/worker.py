#!/usr/bin/env python3
"""Simple PyTorch DQN worker for port navigation."""

from __future__ import annotations

import argparse
import json
import random
import sys
from collections import deque
from dataclasses import dataclass
from pathlib import Path

import torch
from torch import nn
from torch.nn import functional as F

PROTOCOL_VERSION = 1
SEED = 7

OBSERVATION_KEYS = [
    "goalLocalX",
    "goalLocalZ",
    "distanceToGoal",
    "speedFraction",
    "forwardDistanceFraction",
    "forwardLeftDistanceFraction",
    "forwardRightDistanceFraction",
    "wideLeftDistanceFraction",
    "wideRightDistanceFraction",
    "leftPressure",
    "rightPressure",
]

ACTION_SET = [
    (0.90, -1.00),
    (0.90, -0.45),
    (0.90, 0.00),
    (0.90, 0.45),
    (0.90, 1.00),
    (0.45, -1.00),
    (0.45, -0.45),
    (0.45, 0.00),
    (0.45, 0.45),
    (0.45, 1.00),
    (-0.60, -1.00),
    (-0.60, 0.00),
    (-0.60, 1.00),
]

REPLAY_CAPACITY = 50_000
MIN_REPLAY_SIZE = 256
BATCH_SIZE = 64
TRAIN_EVERY_TRANSITIONS = 4
TARGET_SYNC_EVERY_UPDATES = 200
CHECKPOINT_EVERY_UPDATES = 200
LEARNING_RATE = 0.001
GAMMA = 0.98
EPSILON_START = 1.00
EPSILON_END = 0.05
EPSILON_DECAY_DECISIONS = 12_000


def emit(message: dict) -> None:
    sys.stdout.write(json.dumps(message) + "\n")
    sys.stdout.flush()


def observation_to_vector(observation: dict) -> list[float]:
    values = [float(observation.get(key, 0.0)) for key in OBSERVATION_KEYS]
    values.append(1.0 if observation.get("frontBlocked", False) else 0.0)
    values.append(1.0 if observation.get("isStuck", False) else 0.0)
    values.append(1.0 if observation.get("isEscaping", False) else 0.0)
    return values


def action_to_index(action: dict) -> int:
    throttle = float(action.get("throttle", 0.0))
    turn = float(action.get("turn", 0.0))

    best_index = 0
    best_distance = float("inf")
    for index, (candidate_throttle, candidate_turn) in enumerate(ACTION_SET):
        distance = (candidate_throttle - throttle) ** 2 + (candidate_turn - turn) ** 2
        if distance < best_distance:
            best_distance = distance
            best_index = index

    return best_index


@dataclass
class Transition:
    state: list[float]
    action_index: int
    reward: float
    next_state: list[float]
    done: bool


class QNetwork(nn.Module):
    def __init__(self, input_size: int, output_size: int) -> None:
        super().__init__()
        self.layers = nn.Sequential(
            nn.Linear(input_size, 32),
            nn.ReLU(),
            nn.Linear(32, 32),
            nn.ReLU(),
            nn.Linear(32, output_size),
        )

    def forward(self, inputs: torch.Tensor) -> torch.Tensor:
        return self.layers(inputs)


class Trainer:
    def __init__(self, checkpoint_path: Path, post_exploration_checkpoint_path: Path) -> None:
        torch.manual_seed(SEED)

        self.random = random.Random(SEED)
        self.replay: deque[Transition] = deque(maxlen=REPLAY_CAPACITY)
        self.checkpoint_path = checkpoint_path
        self.post_exploration_checkpoint_path = post_exploration_checkpoint_path

        input_size = len(OBSERVATION_KEYS) + 3
        output_size = len(ACTION_SET)

        self.online_network = QNetwork(input_size, output_size)
        self.target_network = QNetwork(input_size, output_size)
        self.target_network.load_state_dict(self.online_network.state_dict())

        self.optimizer = torch.optim.Adam(self.online_network.parameters(), lr=LEARNING_RATE)
        self.decisions_made = 0
        self.transitions_seen = 0
        self.updates_completed = 0
        self.saved_post_exploration_checkpoint = self.post_exploration_checkpoint_path.exists()

        if self.checkpoint_path.exists():
            checkpoint = torch.load(self.checkpoint_path, map_location="cpu")
            self.online_network.load_state_dict(checkpoint["online_state_dict"])
            self.target_network.load_state_dict(checkpoint["target_state_dict"])
            self.optimizer.load_state_dict(checkpoint["optimizer_state_dict"])
            self.decisions_made = int(checkpoint["decisions_made"])
            self.transitions_seen = int(checkpoint["transitions_seen"])
            self.updates_completed = int(checkpoint["updates_completed"])
            if self.decisions_made >= EPSILON_DECAY_DECISIONS and not self.saved_post_exploration_checkpoint:
                self.save_checkpoint(self.post_exploration_checkpoint_path)
                self.saved_post_exploration_checkpoint = True

    def choose_action(self, observation: dict) -> tuple[float, float, str]:
        epsilon = self.current_epsilon()

        if self.random.random() < epsilon:
            action_index = self.random.randrange(len(ACTION_SET))
            mode = "Explore"
        else:
            state = torch.tensor(observation_to_vector(observation), dtype=torch.float32).unsqueeze(0)
            with torch.no_grad():
                action_index = int(torch.argmax(self.online_network(state), dim=1).item())
            mode = "Policy"

        self.decisions_made += 1
        if not self.saved_post_exploration_checkpoint and self.decisions_made >= EPSILON_DECAY_DECISIONS:
            self.save_checkpoint(self.post_exploration_checkpoint_path)
            self.saved_post_exploration_checkpoint = True

        throttle, turn = ACTION_SET[action_index]
        return throttle, turn, f"Port RL {mode} eps={epsilon:.2f}"

    def record_transition(self, message: dict, rollout_file) -> None:
        rollout_file.write(json.dumps(message) + "\n")
        rollout_file.flush()

        self.replay.append(
            Transition(
                state=observation_to_vector(message["observation"]),
                action_index=action_to_index(message["action"]),
                reward=float(message["reward"]),
                next_state=observation_to_vector(message["nextObservation"]),
                done=bool(message["done"]),
            )
        )
        self.transitions_seen += 1

        if self.transitions_seen % TRAIN_EVERY_TRANSITIONS == 0:
            self.train_step()

    def current_epsilon(self) -> float:
        if self.decisions_made >= EPSILON_DECAY_DECISIONS:
            return EPSILON_END

        progress = self.decisions_made / EPSILON_DECAY_DECISIONS
        return EPSILON_START + (EPSILON_END - EPSILON_START) * progress

    def train_step(self) -> None:
        if len(self.replay) < MIN_REPLAY_SIZE:
            return

        batch = self.random.sample(self.replay, BATCH_SIZE)

        states = torch.tensor([item.state for item in batch], dtype=torch.float32)
        actions = torch.tensor([item.action_index for item in batch], dtype=torch.int64)
        rewards = torch.tensor([item.reward for item in batch], dtype=torch.float32)
        next_states = torch.tensor([item.next_state for item in batch], dtype=torch.float32)
        done_mask = torch.tensor([1.0 if item.done else 0.0 for item in batch], dtype=torch.float32)

        with torch.no_grad():
            next_q = self.target_network(next_states).max(dim=1).values
            targets = rewards + (1.0 - done_mask) * GAMMA * next_q

        predicted_q = self.online_network(states).gather(1, actions.unsqueeze(1)).squeeze(1)
        loss = F.mse_loss(predicted_q, targets)

        self.optimizer.zero_grad()
        loss.backward()
        self.optimizer.step()

        self.updates_completed += 1

        if self.updates_completed % TARGET_SYNC_EVERY_UPDATES == 0:
            self.target_network.load_state_dict(self.online_network.state_dict())

        if self.updates_completed % CHECKPOINT_EVERY_UPDATES == 0:
            self.save_checkpoint(self.checkpoint_path)

    def save_checkpoint(self, path: Path) -> None:
        torch.save(
            {
                "online_state_dict": self.online_network.state_dict(),
                "target_state_dict": self.target_network.state_dict(),
                "optimizer_state_dict": self.optimizer.state_dict(),
                "decisions_made": self.decisions_made,
                "transitions_seen": self.transitions_seen,
                "updates_completed": self.updates_completed,
            },
            path,
        )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--rollout-path", required=True)
    args = parser.parse_args()

    rollout_path = Path(args.rollout_path)
    rollout_path.parent.mkdir(parents=True, exist_ok=True)

    checkpoint_path = rollout_path.with_name("port_policy_latest.pt")
    post_exploration_checkpoint_path = rollout_path.with_name("port_policy_after_exploration.pt")
    trainer = Trainer(checkpoint_path, post_exploration_checkpoint_path)

    with rollout_path.open("a", encoding="utf-8") as rollout_file:
        emit({"type": "ready", "protocolVersion": PROTOCOL_VERSION})

        for raw_line in sys.stdin:
            line = raw_line.strip()
            if not line:
                continue

            message = json.loads(line)

            if message["type"] == "decide":
                throttle, turn, debug_state = trainer.choose_action(message["observation"])
                emit(
                    {
                        "type": "action",
                        "protocolVersion": PROTOCOL_VERSION,
                        "shipId": message["shipId"],
                        "sequence": int(message["sequence"]),
                        "throttle": throttle,
                        "turn": turn,
                        "debugState": debug_state,
                    }
                )
                continue

            if message["type"] == "transition":
                trainer.record_transition(message, rollout_file)

    trainer.save_checkpoint(checkpoint_path)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
