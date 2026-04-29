from __future__ import annotations

from shared import AiAction, AiObservation, BaseAiBrain, pick_safer_turn


class RaiderAi(BaseAiBrain):
    def choose_action(self, observation: AiObservation) -> AiAction:
        if observation.isEscaping:
            throttle = -1.0 if observation.isStuck else 0.55
            return AiAction(throttle, pick_safer_turn(observation), False, False, "Escape")

        if observation.frontBlocked:
            return AiAction(-0.75, pick_safer_turn(observation), False, False, "Avoid Shore")

        if observation.isStuck:
            return AiAction(-0.90, pick_safer_turn(observation), False, False, "Recover Stuck")

        has_target_ship = observation.hasTargetShip
        goal_x = observation.targetShipLocalX if has_target_ship else observation.goalLocalX
        distance_to_goal = observation.targetShipDistance if has_target_ship else observation.distanceToGoal
        left_pressure = observation.leftPressure
        right_pressure = observation.rightPressure
        goal_z = observation.targetShipLocalZ if has_target_ship else observation.goalLocalZ

        chase_turn = max(-1.0, min(1.0, goal_x / 0.85))
        obstacle_assist = (left_pressure - right_pressure) * 0.10
        turn = max(-1.0, min(1.0, chase_turn + obstacle_assist))

        if distance_to_goal > 0.2:
            throttle = 1.0
        elif distance_to_goal < 0.08:
            throttle = 0.2
        else:
            throttle = 0.65

        if has_target_ship and distance_to_goal < 0.08 and abs(goal_x) > 0.04 and abs(goal_z) < 0.08:
            fire_right = goal_x > 0.0
            fire_left = goal_x < 0.0
            return AiAction(throttle, turn, fire_left, fire_right, "Fire Broadside")

        return AiAction(throttle, turn, False, False, "Chase")


def create_brain(rollout_path, checkpoint_path, post_exploration_checkpoint_path):
    return RaiderAi()
