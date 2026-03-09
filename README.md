# PiratesQuest

## Architecture
PiratesQuest uses three pieces:

- `Game` (`godot/`): the player client and also the dedicated multiplayer server.
- `API` (`api/`): login/signup and server list.
- `WebView` (`webview/`): React/TypeScript HUD, built into `godot/webview/` and loaded locally by godot_wry.
- `Menu` (`menu/`): React/TypeScript main menu UI, served as a native browser overlay via godot_wry.
- `Database` (via Docker): stores users and API data.

### API Stateless Server Runtime Data
- The API is stateless across instances/restarts.
- Dedicated servers send heartbeat data to `POST /api/server/{id}/heartbeat`.
- Heartbeat persists `playerCount`, `playerMax`, `serverVersion`, and `lastSeenUtc` on each `GameServer` row.
- `GET /api/servers` reads those DB fields and returns status (`online`/`offline`) plus runtime info for the menu server browser.

## Running

```bash
./run.sh
```

Starts the backend (Docker DB + API), a game server, and a client. Use `--server` to skip the client. Default API URL is `http://localhost:5236`.

This runs everything, use this whenever in doubt. AI agents should never use the run command, as it's quite intensive.

### Run client in editor
- Open `godot/project.godot` in Godot 4
- Press Play
- Login/signup, then join a server

Please submit a PR, learning godot, so any and all suggestions welcome.

## Scripts

All scripts are in the repo root and run from there.

| Script | Description |
|--------|-------------|
| `build-game.sh` | Builds menu + local port UI, then exports the Godot project to macOS and Windows builds zipped into `dist/<version>/`. |
| `run.sh` | Runs a local dev session. Builds menu + admin + local port UI, starts the backend, a game server, and a client side-by-side. Supports `--server` (server only), `--prod` (use production API at pirates.quest), `--user`, and `--password` flags. |
| `publish-backend.sh` | Builds the menu webview + API into a Docker image (`piratesquest-api`). Pass an optional tag argument (default `latest`). |
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

The build output goes to `godot/webview/`. Godot loads it locally from `res://webview/index.html`.

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


## ROADMAP
### 0.6.0 Alpha
- [ ] Quests
- [ ] Characters
- [ ] AI ships
- [ ] New embed code
  - [ ] Built by HAND to fix all of the issues we're seeing

### Planned
- [ ] Better Island mesh
   - [ ] Better island cannonball collision
- [ ] Fix water.
- [ ] In game map
- [ ] Game freezes on first explosion animation
- [ ] Jitter when hitting a rock! 
- [ ] EU Servers
- [ ] Stop users from going off map

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
