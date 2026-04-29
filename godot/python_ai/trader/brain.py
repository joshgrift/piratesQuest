from __future__ import annotations

from shared import AiAction, AiObservation, BaseAiBrain, pick_safer_turn


class TraderAi(BaseAiBrain):
    def choose_action(self, observation: AiObservation) -> AiAction:
        if observation.isEscaping:
            throttle = -1.0 if observation.isStuck else 0.65
            return AiAction(throttle, pick_safer_turn(observation), False, False, "Escape")

        if observation.frontBlocked:
            return AiAction(-0.65, pick_safer_turn(observation), False, False, "Avoid Terrain")

        if observation.isStuck:
            return AiAction(-0.95, pick_safer_turn(observation), False, False, "Recover Stuck")

        goal_x = observation.nearestPortLocalX if observation.hasNearestPort else observation.goalLocalX
        distance_to_goal = observation.nearestPortDistance if observation.hasNearestPort else observation.distanceToGoal
        if observation.hasThreatShip:
            goal_x = -observation.threatShipLocalX
            distance_to_goal = observation.threatShipDistance
        left_pressure = observation.leftPressure
        right_pressure = observation.rightPressure

        port_turn = max(-1.0, min(1.0, goal_x / 0.72))
        obstacle_assist = (left_pressure - right_pressure) * 0.14
        turn = max(-1.0, min(1.0, port_turn + obstacle_assist))
        throttle = 0.82 if distance_to_goal > 0.1 else 0.35
        if observation.hasThreatShip:
            throttle = 1.0 if distance_to_goal < 0.08 else 0.75
            return AiAction(throttle, turn, False, False, "Avoid Ship")

        return AiAction(throttle, turn, False, False, "Travel")


def create_brain(rollout_path, checkpoint_path, post_exploration_checkpoint_path):
    return TraderAi()
