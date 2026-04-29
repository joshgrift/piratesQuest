from __future__ import annotations

import json
import random
from collections import deque

import torch
from torch import nn
from torch.nn import functional as F

from shared import ACTION_SET, OBSERVATION_KEYS, AiAction, AiObservation, AiTransition, BaseAiBrain, Transition, action_to_index, observation_to_vector

SEED = 7
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


class NeuralPatrolAi(BaseAiBrain):
    def __init__(self, checkpoint_path, post_exploration_checkpoint_path) -> None:
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

    def choose_action(self, observation: AiObservation) -> AiAction:
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
        return AiAction(throttle, turn, False, False, f"Port RL {mode} eps={epsilon:.2f}")

    def record_transition(self, transition: AiTransition, rollout_file) -> None:
        rollout_file.write(json.dumps(transition.to_dict()) + "\n")
        rollout_file.flush()

        self.replay.append(
            Transition(
                state=observation_to_vector(transition.observation),
                action_index=action_to_index(transition.action),
                reward=transition.reward,
                next_state=observation_to_vector(transition.nextObservation),
                done=transition.done,
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

    def save_checkpoint(self, path) -> None:
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


def create_brain(rollout_path, checkpoint_path, post_exploration_checkpoint_path):
    return NeuralPatrolAi(checkpoint_path, post_exploration_checkpoint_path)
