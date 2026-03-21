# PiratesQuest

## Architecture
PiratesQuest uses three pieces:

- `Game` (`godot/`): the player client and also the dedicated multiplayer server.
- `API` (`api/`): login/signup and server list.
- `WebView` (`webview/`): React/TypeScript HUD. Local dev runs from Vite (`http://localhost:5173`), release builds are published to API fragments at `/fragments/webview`.
- `Menu` (`menu/`): React/TypeScript main menu UI, served as a native browser overlay via godot_wry.
- `Database` (via Docker): stores users and API data.

### API Stateless Server Runtime Data
- The API is stateless across instances/restarts.
- Dedicated servers send heartbeat data to `POST /api/server/{id}/heartbeat`.
- Heartbeat persists `playerCount`, `playerMax`, `serverVersion`, and `lastSeenUtc` on each `GameServer` row.
- `GET /api/servers` reads those DB fields and returns status (`online`/`offline`) plus runtime info for the menu server browser.

## Running

```bash
./run
```

Starts the backend (Docker DB + API), a game server, and a client in the background, then streams logs from all services in that terminal. Each `up` run clears the previous log files first. Use `--server` to skip the client. Default API URL is `http://localhost:5236`.

If you want to type `run` without `./`, there are two repo-local options:
- With `direnv`: run `direnv allow` once in this repo. The included `.envrc` adds the repo root to `PATH` only while you are here.
- Without `direnv`: start a repo-scoped shell with `./repo-shell`, then use `run`.

This runs everything, use this whenever in doubt. AI agents should never use the run command, as it's quite intensive.

The important part is that services keep running, so you can restart just one piece without restarting the rest:

```bash
./run up                 # start the full stack, then watch all logs
./run status             # see what's running
./run restart client     # restart only the client from another terminal
./run restart api        # restart only the API from another terminal
./run down client        # stop only the client
./run build godot        # build one project without starting it
```

There are also targeted `up` commands like `./run up api`, `./run up server`, and `./run up webview`.

### Run client in editor
- Open `godot/project.godot` in Godot 4
- Press Play
- Login/signup, then join a server

Please submit a PR, learning godot, so any and all suggestions welcome.

## Scripts

All scripts are in the repo root and run from there.

| Script | Description |
|--------|-------------|
| `build-game.sh` | Builds the port UI and publishes it to `api/fragments/webview/`, then exports macOS, Windows, and Linux server builds zipped into `dist/<version>/`. Supports `--skip-notarization` (or `--no-notarize`) to skip macOS notarization (and codesign) for that run. |
| `run` | Small local process manager for development. Supports `up`, `down`, `restart`, `status`, and `build`, so you can keep the stack running and restart only the service you changed. `up` streams the combined logs. Supports `--build`, `--server`, `--prod`, `--user`, and `--password` flags. |
| `publish-backend.sh` | Builds the menu webview + port webview + API into a Docker image (`piratesquest-api`). Pass an optional tag argument (default `latest`). |
| `manage.sh` | Admin CLI for the REST API. Manage users, game servers, roles, and game version. Requires `PQ_API_URL` and either `PQ_TOKEN` or a login. |
| `admin/` | React/TypeScript admin panel that replaces most `manage.sh` usage. Build output is `api/wwwroot/admin/` and is served by the API at `/admin/`. |

## Releasing
- Update Version in Project Settings
- Run `./build-game.sh`
- Add new Git Release in github
- Upload builds in dist to github release
- Run `./publish-backend.sh` to build the Docker image (`piratesquest-api`)
- Deploy the image to your cloud provider with these env vars:
  - `ConnectionStrings__Default` — Postgres connection string
  - `Jwt__Key` — JWT signing key (≥ 32 bytes)
  - `ServerApiKey` — shared key for game-server → API auth
  - The container listens on port **8080**

## Port WebView

The game uses a native browser panel ([godot_wry](https://github.com/doceazedo/godot_wry)) for the port UI. When the player docks at a port, a React/TypeScript app slides in on the right third of the screen, replacing the old Godot-based port menu. It handles buying/selling goods, purchasing and managing ship components, viewing stats, and repairing the hull.

Runtime URL behavior:
- Local debug/editor runs default to `http://localhost:5173/` (Vite dev server).
- Hosted/prod runs use `${API_URL}/fragments/webview`.
- You can override manually with `--webview-url <url>`.

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

`build-game.sh` publishes the build output to `api/fragments/webview/`.

### Building the menu UI

```bash
cd menu
npm install
npm run build
```

The build output goes to `../api/fragments/menu/`, which the API serves as static files.

### Building the admin UI

```bash
cd admin
npm install
npm run build
```

The build output goes to `../api/wwwroot/admin/`, served by the API at `/admin/`.

## Third Party
- [GoDot](https://godotengine.org/)
- [godot_wry](https://github.com/doceazedo/godot_wry) (native WebView for Godot)
- Kenney.nl
  - [Prototype Textures](https://kenney.nl/assets/prototype-textures)
  - [Pirate Pack](https://kenney.nl/assets/pirate-kit)
- Sounds
  - [TomMusic](https://tommusic.itch.io/)
  - [JCSounds](https://jcsounds.itch.io/piratesfxvol1)
- Icons
  - [Game-icons.net](https://game-icons.net/tags/pirate.html)


## ROADMAP
### Alpha 8 - Cleanup & Map
- [ ] Map?
- [ ] Polish and Stability
- [ ] New Terrain

### Alpha 9 - AI Update
- [ ] AI ships

### Alpha 10 - World Update
- [ ] Proper terrain with awesome islands and a large Island
- [ ] More characters, Quests, and a bunch of content

### Planned
- [ ] Better Island mesh
   - [ ] Better island cannonball collision
- [ ] Fix water.
- [ ] In game map
- [ ] Game freezes on first explosion animation
- [ ] Jitter when hitting a rock! 
- [ ] EU Servers
- [ ] Stop users from going off map
- [ ] Serve HUD from Local package instead of from web

### Suggestions
- [ ] Close port menu early
- [ ] Multiple Cannonball types?
- [ ] In Combat Timer
  - [ ] Can't heal if you're in combat
  - [ ] When in port, the timer pauses
- [ ] Proximity voice chat
- [ ] Mini games
  - [ ] Capture the flag
- [ ] Weapons
  - [ ] Sea Mines
  - [ ] Mermaid summoning
  - [ ] Bow Cannon (weak)
  - [ ] Grenade Shot
- [ ] Suggestion: Slow turn speed when going fast
- [ ] Player Economy
- [ ] Remove Trophies
- [ ] Burry items instead of vault, can check the spot for a chance to find the treasure
- [ ] Do think there should be some sort of defense move if possible. Don't know what it could be but would be helpful.
- [ ] In game wiki
- [ ] Easier to see which cannon is space and Shift
- [ ] Event driven bonuses (increase in fishing here)
- [ ] Cracken
- [ ] Curses
- [ ] Change cannon angle
- [ ] environmental hazards
  - Randomly spawn and can move around on the map, e.g., hurricane or tornado or shark attack - damaging ship while in its radius or temporarily destroying/exhausting a harvest spot
- [ ] Harvesting nodes have a limited harvest quantity and regen timer.
  - This helps balance the best harvesting spots, encourages diversifying where you harvest, and encourages return logins to get the resources you need.
- [ ] add weather/wind mechanic with variably wind speeds
   - [ ] affecting sailing speed based on corresponding wind and ship direction,
