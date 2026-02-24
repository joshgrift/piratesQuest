# PiratesQuest

## Architecture
PiratesQuest uses three pieces:

- `Game` (`godot/`): the player client and also the dedicated multiplayer server.
- `API` (`server/`): login/signup and server list.
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

## WebView (Info Panel)

The game embeds a native browser panel on the right third of the screen using [godot_wry](https://github.com/doceazedo/godot_wry). It loads a React app served by the API at `/fragments/info-panel/`.

**How it works:**
- `Hud.cs` creates the WebView once the local player is found. It's always visible.
- The React app (`server/webview-app/`) shows inventory, a shop, and game controls.
- Communication is two-way:
  - **Godot → React**: `eval()` calls `window.updateInventory(data)` on every inventory change.
  - **React → Godot**: `window.ipc.postMessage(json)` sends purchase requests, handled via the `ipc_message` signal in `Hud.cs`.

**Canvas stretch fix:** The project uses `canvas_items` stretch mode (1920x1080 base). Since godot_wry is a native OS overlay (not Godot-rendered), `get_size()` returns virtual pixels but the overlay needs physical pixels. `SyncWebViewSize()` uses the canvas transform to convert between the two coordinate systems.

**Editor note (Godot 4.5+):** Native webviews don't work in embedded mode. Uncheck "Embed Game on Next Play" in the Game panel before running.

### Building the React app

```bash
cd server/webview-app
npm install
npm run build
```

The build output goes to `server/fragments/info-panel/`, which the API serves as static files.

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
- [ ] React UI so it can explain a lot more
  - [ ] More details
  - [ ] Stacking components
  - [ ] Satisfying animations
  - [ ] Buy in 5, 10, 50 increments
  - [ ] Sell all button
  - [ ] Quick buy for components
- [x] Safe zone in port
- [ ] Stash
- [ ] AI ships
- [ ] Ship upgrades (persistent)
- [x] UI to show when you are harvesting things
- [x] Persistent world
- [x] Login
- [ ] More incremental progress
- [ ] Health too expensive?

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
