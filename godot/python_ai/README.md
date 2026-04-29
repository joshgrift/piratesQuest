# Python AI

This folder is the main home for AI authoring.

## One Place To Define AI Types

Add new AI types in [AI_Ships.cs](./AI_Ships.cs).

Each entry there defines:

- the AI id
- ship stats and visuals
- default spawn count
- goal mode
- reward mode

That file is the only place in the C# codebase that should need direct AI type ids.

## Per-AI Folders

Each AI type lives in its own folder:

- `raider/brain.py`
- `trader/brain.py`
- `neural_patrol/brain.py`

The worker loads these folders on demand based on the known AI ids from C#.
It does not import every brain at startup.

## Shared Contract

Use [shared.py](./shared.py) as the source of truth for the Python AI API.

It contains:

- `BaseAiBrain`: the abstract base class every brain should inherit from
- `AiObservation`: the typed observation Godot sends into `choose_action(...)`
- `AiAction`: the action a brain returns to Godot
- `AiTransition`: the typed training message used by learning brains

The field names on `AiObservation` intentionally match the JSON keys from C#.
That makes it easier to compare Godot output to Python code.

## Creating A New AI

1. Add a new folder such as `my_ai/`
2. Create `my_ai/brain.py`
3. Inherit from `BaseAiBrain`
4. Export a `create_brain(...)` function that returns your brain instance
5. Add one matching entry to `AI_Ships.cs`

Example:

```python
from shared import AiAction, AiObservation, BaseAiBrain


class MyAi(BaseAiBrain):
    def choose_action(self, observation: AiObservation) -> AiAction:
        return AiAction(
            throttle=0.8,
            turn=0.0,
            debugState=f"goalX={observation.goalLocalX:.2f}",
        )


def create_brain(rollout_path, checkpoint_path, post_exploration_checkpoint_path):
    return MyAi()
```

Learning brains can also override `record_transition(...)` and `save_checkpoint(...)`.
