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
  - the tiny command object returned by an AI brain each frame
- `AiShipContext.cs`
  - the read-only snapshot passed into the AI brain
- `AiShipDefinition.cs`
  - data for AI ship archetypes

In other words: this is the place for "decide what the ship wants to do."

## Per-AI Folders

Each AI implementation should live in its own subfolder.

Current example:

- `hunterDeterministic/`
  - contains `HunterAiShipController.cs`

This makes it easier to:

- keep each AI self-contained
- compare different AI approaches
- add helper classes that belong to one AI without cluttering the shared root

## Suggested Pattern For New AI

When adding a new AI:

1. Add shared interfaces, context fields, or common helpers to `godot/scripts/ai/` if they are useful to all AI ships.
2. Create a new folder in `godot/scripts/ai/` for the AI itself.
3. Put that AI's controller and any AI-specific helpers in that folder.
4. Keep `AiShip.cs` responsible for applying the returned control input.

## Rule Of Thumb

If the code answers one of these questions, it probably belongs here:

- "What should the AI do next?"
- "What data does the AI need to decide?"
- "What reusable AI helper can multiple AI ships share?"

If the code answers one of these questions, it probably belongs in `scenes/ai_ship`:

- "How does the ship move in Godot?"
- "How does it fire, take damage, or sink?"
- "How does it connect to scene nodes, raycasts, spawners, or multiplayer?"

## Why This Split Helps

- The scene stays easier to debug because gameplay actions stay in one place.
- AI brains stay easier to swap because they only return control input.
- Shared AI tools do not get mixed together with node setup code.
- New AI types can grow in their own folders without turning the root into a junk drawer.
