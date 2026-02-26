# Port WebView

React + TypeScript UI for the in-game port screen. When the player docks at a port, this app slides in from the right side of the screen and provides the full port experience: buying and selling goods, purchasing and managing ship components, viewing stats, and repairing the hull.

## How It Works

The app runs inside a native OS webview ([godot_wry](https://github.com/doceazedo/godot_wry)) overlaid on the Godot game window. Communication between Godot and the React app is bidirectional:

- **Godot to React**: `Hud.cs` calls `eval()` on the webview to invoke typed window functions (`openPort`, `closePort`, `updateState`) with a `PortState` JSON payload.
- **React to Godot**: The React app sends IPC messages via `window.ipc.postMessage()` with a discriminated `action` field. `Hud.cs` deserializes these into typed C# records and executes the appropriate game logic.

All types are defined in:
- TypeScript: `src/types.ts`
- C#: `godot/scripts/data/PortIpcMessages.cs`

## Building

```bash
npm install
npm run build
```

Build output goes to `../api/fragments/webview/`, which the API server serves as static files. The webview loads from `{ApiBaseUrl}/fragments/webview/`.

## Development

```bash
npm run dev
```

This starts a Vite dev server with hot reload. To test with the game, temporarily change the webview URL in `Hud.cs` to point at the dev server (e.g. `http://localhost:5173/fragments/webview/`).
