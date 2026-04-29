# AI Scripts

This folder is the shared home for AI code.

## Goal

The intended split is:

- `godot/scenes/ai_ship/` is for the in-scene `AiShip` node and scene/controller wiring.
- `godot/scripts/ai/` is for reusable AI logic and tools.
- Each concrete AI gets its own folder inside `godot/scripts/ai/`.

That keeps scene code focused on Godot node behavior, and AI code focused on decisions.

## What Lives In `scenes/ai_ship`

`AiShip.cs` is the scene-side controller.

It should own things like:

- exported node references
- physics and movement application
- raycasts and world queries
- cannon firing
- damage and sinking
- spawn setup
- water bob / visual motion
- multiplayer-facing scene behavior

In other words: this is the place for "make the ship exist in the world and do the action."

## What Lives In `scripts/ai`

This folder should hold logic that can work across AI ships.

Right now that includes:

- `IAiShipController.cs`
  - the decision interface
  - the scene-to-controller memory sync hook
  - the tiny command object returned by an AI brain each frame
- `AiShipContext.cs`
  - the read-only per-frame snapshot passed into the AI brain
  - includes nearby ship contacts and terrain ray readings for this frame
- `AiShipMemory.cs`
  - the per-ship runtime memory bag owned by the controller layer
- `AiShipDefinition.cs`
  - data for AI ship archetypes and one-time controller config

In other words: this is the place for "decide what the ship wants to do."

## Per-AI Folders

Each runtime AI implementation now lives in its own Python subfolder under `godot/python_ai/`.

The shared Godot-side C# layer still handles sensing, movement, multiplayer, and
worker process integration, but the archetype behavior itself lives in Python.

This makes it easier to:

- keep each AI self-contained
- compare different AI approaches
- load only the AI module that is actually needed at runtime
- add helper files that belong to one AI without cluttering the shared root

## Suggested Pattern For New AI

When adding a new AI:

1. Add shared interfaces, context fields, or common helpers to `godot/scripts/ai/` if they are useful to all AI ships.
2. Add the new archetype id to `AiShipDefinition.KnownIds`.
3. Create a new folder in `godot/python_ai/` for the AI itself.
4. Put that AI's `brain.py` and any AI-specific helpers in that folder.
5. Expose a `create_brain(...)` function so the worker can lazy-load it.
6. Keep `AiShip.cs` responsible for applying the returned control input.
7. Keep long-lived scene/runtime transport state in `AiShipMemory`, not in `AiShip.cs`.
8. Pass static tuning into the controller once when it is created instead of putting it in `AiShipContext`.
9. Put current-frame sensing in `AiShipContext`, and send the resulting observation to Python.

## Rule Of Thumb

If the code answers one of these questions, it probably belongs here:

- "What should the AI do next?"
- "What data does the AI need to decide?"
- "What reusable AI helper can multiple AI ships share?"
- "What should this AI ship remember between frames?"

If the code answers one of these questions, it probably belongs in `scenes/ai_ship`:

- "How does the ship move in Godot?"
- "How does it fire, take damage, or sink?"
- "How does it connect to scene nodes, raycasts, spawners, or multiplayer?"

## Why This Split Helps

- The scene stays easier to debug because gameplay actions stay in one place.
- AI brains stay easier to swap because they only return control input.
- Shared AI tools do not get mixed together with node setup code.
- New AI types can grow in their own folders without turning the root into a junk drawer.
