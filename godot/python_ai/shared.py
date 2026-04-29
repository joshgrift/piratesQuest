from __future__ import annotations

from abc import ABC, abstractmethod
from dataclasses import dataclass
from pathlib import Path
from typing import TextIO

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


def _float_value(value: object, default: float = 0.0) -> float:
    if value is None:
        return default
    return float(value)


def _bool_value(value: object, default: bool = False) -> bool:
    if value is None:
        return default
    return bool(value)


@dataclass(frozen=True)
class AiObservation:
    """Typed view of the observation payload Godot sends to Python.

    The field names intentionally match the JSON keys from C# so people can
    compare the Python code to the worker messages without doing mental
    translation.
    """

    aiType: str = ""

    # Generic movement and goal fields shared by all AI types.
    goalLocalX: float = 0.0
    goalLocalZ: float = 0.0
    distanceToGoal: float = 0.0
    speedFraction: float = 0.0

    # Terrain sensing rays.
    forwardDistanceFraction: float = 0.0
    forwardLeftDistanceFraction: float = 0.0
    forwardRightDistanceFraction: float = 0.0
    wideLeftDistanceFraction: float = 0.0
    wideRightDistanceFraction: float = 0.0
    leftPressure: float = 0.0
    rightPressure: float = 0.0

    # Recovery state from the shared C# controller.
    frontBlocked: bool = False
    isStuck: bool = False
    isEscaping: bool = False

    # Combat target details used by raiders.
    hasTargetShip: bool = False
    targetShipIsPlayer: bool = False
    targetShipDistance: float = 0.0
    targetShipLocalX: float = 0.0
    targetShipLocalZ: float = 0.0

    # Nearby threat details used by traders.
    hasThreatShip: bool = False
    threatShipDistance: float = 0.0
    threatShipLocalX: float = 0.0
    threatShipLocalZ: float = 0.0

    # Port details used by traders and the patrol trainer.
    hasNearestPort: bool = False
    nearestPortDistance: float = 0.0
    nearestPortLocalX: float = 0.0
    nearestPortLocalZ: float = 0.0

    @staticmethod
    def from_dict(data: dict[str, object]) -> "AiObservation":
        return AiObservation(
            aiType=str(data.get("aiType", "")),
            goalLocalX=_float_value(data.get("goalLocalX")),
            goalLocalZ=_float_value(data.get("goalLocalZ")),
            distanceToGoal=_float_value(data.get("distanceToGoal")),
            speedFraction=_float_value(data.get("speedFraction")),
            forwardDistanceFraction=_float_value(data.get("forwardDistanceFraction")),
            forwardLeftDistanceFraction=_float_value(data.get("forwardLeftDistanceFraction")),
            forwardRightDistanceFraction=_float_value(data.get("forwardRightDistanceFraction")),
            wideLeftDistanceFraction=_float_value(data.get("wideLeftDistanceFraction")),
            wideRightDistanceFraction=_float_value(data.get("wideRightDistanceFraction")),
            leftPressure=_float_value(data.get("leftPressure")),
            rightPressure=_float_value(data.get("rightPressure")),
            frontBlocked=_bool_value(data.get("frontBlocked")),
            isStuck=_bool_value(data.get("isStuck")),
            isEscaping=_bool_value(data.get("isEscaping")),
            hasTargetShip=_bool_value(data.get("hasTargetShip")),
            targetShipIsPlayer=_bool_value(data.get("targetShipIsPlayer")),
            targetShipDistance=_float_value(data.get("targetShipDistance")),
            targetShipLocalX=_float_value(data.get("targetShipLocalX")),
            targetShipLocalZ=_float_value(data.get("targetShipLocalZ")),
            hasThreatShip=_bool_value(data.get("hasThreatShip")),
            threatShipDistance=_float_value(data.get("threatShipDistance")),
            threatShipLocalX=_float_value(data.get("threatShipLocalX")),
            threatShipLocalZ=_float_value(data.get("threatShipLocalZ")),
            hasNearestPort=_bool_value(data.get("hasNearestPort")),
            nearestPortDistance=_float_value(data.get("nearestPortDistance")),
            nearestPortLocalX=_float_value(data.get("nearestPortLocalX")),
            nearestPortLocalZ=_float_value(data.get("nearestPortLocalZ")),
        )

    def to_dict(self) -> dict[str, object]:
        return {
            "aiType": self.aiType,
            "goalLocalX": self.goalLocalX,
            "goalLocalZ": self.goalLocalZ,
            "distanceToGoal": self.distanceToGoal,
            "speedFraction": self.speedFraction,
            "forwardDistanceFraction": self.forwardDistanceFraction,
            "forwardLeftDistanceFraction": self.forwardLeftDistanceFraction,
            "forwardRightDistanceFraction": self.forwardRightDistanceFraction,
            "wideLeftDistanceFraction": self.wideLeftDistanceFraction,
            "wideRightDistanceFraction": self.wideRightDistanceFraction,
            "leftPressure": self.leftPressure,
            "rightPressure": self.rightPressure,
            "frontBlocked": self.frontBlocked,
            "isStuck": self.isStuck,
            "isEscaping": self.isEscaping,
            "hasTargetShip": self.hasTargetShip,
            "targetShipIsPlayer": self.targetShipIsPlayer,
            "targetShipDistance": self.targetShipDistance,
            "targetShipLocalX": self.targetShipLocalX,
            "targetShipLocalZ": self.targetShipLocalZ,
            "hasThreatShip": self.hasThreatShip,
            "threatShipDistance": self.threatShipDistance,
            "threatShipLocalX": self.threatShipLocalX,
            "threatShipLocalZ": self.threatShipLocalZ,
            "hasNearestPort": self.hasNearestPort,
            "nearestPortDistance": self.nearestPortDistance,
            "nearestPortLocalX": self.nearestPortLocalX,
            "nearestPortLocalZ": self.nearestPortLocalZ,
        }


@dataclass(frozen=True)
class AiAction:
    """Single action returned by a Python brain."""

    throttle: float
    turn: float
    fireLeft: bool = False
    fireRight: bool = False
    debugState: str = ""

    @staticmethod
    def from_dict(data: dict[str, object]) -> "AiAction":
        return AiAction(
            throttle=_float_value(data.get("throttle")),
            turn=_float_value(data.get("turn")),
            fireLeft=_bool_value(data.get("fireLeft")),
            fireRight=_bool_value(data.get("fireRight")),
            debugState=str(data.get("debugState", "")),
        )

    def to_dict(self) -> dict[str, object]:
        return {
            "throttle": self.throttle,
            "turn": self.turn,
            "fireLeft": self.fireLeft,
            "fireRight": self.fireRight,
            "debugState": self.debugState,
        }


@dataclass(frozen=True)
class AiTransition:
    """Typed training transition used by RL-style brains."""

    protocolVersion: int
    shipId: str
    episodeId: str
    observation: AiObservation
    action: AiAction
    reward: float
    nextObservation: AiObservation
    done: bool
    doneReason: str

    @staticmethod
    def from_message(message: dict[str, object]) -> "AiTransition":
        return AiTransition(
            protocolVersion=int(message.get("protocolVersion", 1)),
            shipId=str(message.get("shipId", "")),
            episodeId=str(message.get("episodeId", "")),
            observation=AiObservation.from_dict(message.get("observation", {})),
            action=AiAction.from_dict(message.get("action", {})),
            reward=_float_value(message.get("reward")),
            nextObservation=AiObservation.from_dict(message.get("nextObservation", {})),
            done=_bool_value(message.get("done")),
            doneReason=str(message.get("doneReason", "")),
        )

    def to_dict(self) -> dict[str, object]:
        return {
            "type": "transition",
            "protocolVersion": self.protocolVersion,
            "shipId": self.shipId,
            "episodeId": self.episodeId,
            "observation": self.observation.to_dict(),
            "action": self.action.to_dict(),
            "reward": self.reward,
            "nextObservation": self.nextObservation.to_dict(),
            "done": self.done,
            "doneReason": self.doneReason,
        }


class BaseAiBrain(ABC):
    """Base class every Python AI brain should inherit from."""

    checkpoint_path: Path | None = None

    @abstractmethod
    def choose_action(self, observation: AiObservation) -> AiAction:
        """Return the action for the current observation."""

    def record_transition(self, transition: AiTransition, rollout_file: TextIO) -> None:
        """Optional hook for brains that learn from transitions."""

    def save_checkpoint(self, path: Path) -> None:
        """Optional hook for brains that need to persist training state."""


@dataclass
class Transition:
    state: list[float]
    action_index: int
    reward: float
    next_state: list[float]
    done: bool


def observation_to_vector(observation: AiObservation) -> list[float]:
    values = [float(getattr(observation, key, 0.0)) for key in OBSERVATION_KEYS]
    values.append(1.0 if observation.frontBlocked else 0.0)
    values.append(1.0 if observation.isStuck else 0.0)
    values.append(1.0 if observation.isEscaping else 0.0)
    return values


def action_to_index(action: AiAction) -> int:
    throttle = action.throttle
    turn = action.turn

    best_index = 0
    best_distance = float("inf")
    for index, (candidate_throttle, candidate_turn) in enumerate(ACTION_SET):
        distance = (candidate_throttle - throttle) ** 2 + (candidate_turn - turn) ** 2
        if distance < best_distance:
            best_distance = distance
            best_index = index

    return best_index


def pick_safer_turn(observation: AiObservation) -> float:
    left_space = observation.forwardLeftDistanceFraction + observation.wideLeftDistanceFraction
    right_space = observation.forwardRightDistanceFraction + observation.wideRightDistanceFraction
    return 1.0 if left_space < right_space else -1.0
