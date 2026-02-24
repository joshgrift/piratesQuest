# PiratesQuest

## Architecture
PiratesQuest uses three pieces:

- `Game` (`godot/`): the player client and also the dedicated multiplayer server.
- `API` (`server/`): login/signup and server list.
- `WebView` (`webview/`): React/TypeScript port UI, served as a native browser overlay via godot_wry.
- `Database` (via Docker): stores users and API data.

## Running

```bash
./run.sh
```

Starts the backend (Docker DB + API), a game server, and a client. Use `--server` to skip the client. Default API URL is `http://localhost:5236`.

### Run client in editor
- Open `godot/project.godot` in Godot 4
- Press Play
- Login/signup, then join a server

Please submit a PR, learning godot, so any and all suggestions welcome.

## Scripts

All scripts are in the repo root and run from there.

| Script | Description |
|--------|-------------|
| `build-game.sh` | Exports the Godot project to macOS and Windows builds, zipped into `dist/<version>/`. |
| `run.sh` | Runs a local dev session. Starts the backend, a game server, and a client side-by-side. Supports `--server` (server only), `--user`, and `--password` flags. |
| `publish-backend.sh` | Publishes the API server in Release configuration to `server/bin/Release/net*/publish/`. |
| `manage.sh` | Admin CLI for the REST API. Manage users, game servers, roles, and game version. Requires `PQ_API_URL` and either `PQ_TOKEN` or a login. |

## Releasing
- Update Version in Project Settings
- Run `./build-game.sh`
- Add new Git Release in github
- Upload builds in dist to github release
- Run `publish-backend.sh` to publish the backend and the webview UI.

## Port WebView

The game uses a native browser panel ([godot_wry](https://github.com/doceazedo/godot_wry)) for the port UI. When the player docks at a port, a React/TypeScript app slides in on the right third of the screen, replacing the old Godot-based port menu. It handles buying/selling goods, purchasing and managing ship components, viewing stats, and repairing the hull.

**How it works:**
- `Hud.cs` creates the WebView when the player first docks. It stays hidden until needed.
- On dock, Godot pushes the full port state via `eval("window.openPort({...})")`.
- On depart, Godot calls `eval("window.closePort()")` to trigger a slide-out animation.
- After any action (buy, sell, equip, heal), Godot pushes refreshed state via `updateState`.
- All IPC messages are typed on both sides (TypeScript discriminated unions, C# polymorphic records).

**Canvas stretch fix:** The project uses `canvas_items` stretch mode (1920x1080 base). Since godot_wry is a native OS overlay (not Godot-rendered), `SyncWebViewSize()` converts between virtual and physical pixel coordinates.

**Editor note (Godot 4.5+):** Native webviews don't work in embedded mode. Uncheck "Embed Game on Next Play" in the Game panel before running.

See [`webview/README.md`](webview/README.md) for more details.

### Building the port UI

```bash
cd webview
npm install
npm run build
```

The build output goes to `server/fragments/webview/`, which the API serves as static files.

## Third Party
- [GoDot](https://godotengine.org/)
- [godot_wry](https://github.com/doceazedo/godot_wry) (native WebView for Godot)
- Kenney.nl
  - [Prototype Textures](https://kenney.nl/assets/prototype-textures)
  - [Pirate Pack](https://kenney.nl/assets/pirate-kit)
- Sounds
  - [TomMusic](https://tommusic.itch.io/)
  - [JCSounds](https://jcsounds.itch.io/piratesfxvol1)


## TODO
### 0.4.0 Alpha
- [x] React UI so it can explain a lot more
  - [x] More details
  - [x] Stacking components
  - [x] Satisfying animations
  - [x] Buy in 5, 10, 50 increments
  - [x] Sell all button
  - [x] Quick buy for components
- [x] Safe zone in port
- [ ] Stash
- [ ] AI ships
- [ ] Ship upgrades (persistent)
- [x] UI to show when you are harvesting things
- [x] Persistent world
- [x] Login
- [ ] More incremental progress
- [x] Health too expensive?

- [ ] Deployed to live server

### Later
- [ ] Quests
- [ ] Island mesh?
   - [ ] Better island cannonball collision
- [ ] Fix water.
- [ ] Backwords should be a more gradual movement
- [ ] Proximity voice chat
- [ ] Artifacts are long term growth options.

### Suggestions
- [ ] Close port menu early
- [ ] Multiple Cannonball types?
- [ ] In Combat Timer
  - [ ] Can't heal if you're in combat
  - [ ] When in port, the timer pauses
