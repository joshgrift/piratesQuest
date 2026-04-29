# AI Scripts

This folder is the C# side of the AI system.

- `AiShip.cs` owns the Godot node behavior: movement, firing, damage, and scene setup.
- `godot/scripts/ai/` owns shared C# AI support such as context, memory, definitions, and the Python worker bridge.
- `godot/python_ai/` owns the actual per-AI Python brains.

For the full AI authoring workflow, read [godot/python_ai/README.md](../../python_ai/README.md).

The main rule is:

- if you are adding or changing an AI type, start in `godot/python_ai/`
- if you are adding shared sensing or control plumbing, change `godot/scripts/ai/`
