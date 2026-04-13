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
| `server.sh` | Docker-based dedicated server manager. Supports `list`, `new`, `start`, and `stop` for named server containers. |
| `manage.sh` | Admin CLI for the REST API. Manage users, game servers, roles, and game version. Requires `PQ_API_URL` and either `PQ_TOKEN` or a login. |
| `admin/` | React/TypeScript admin panel that replaces most `manage.sh` usage. Build output is `api/wwwroot/admin/` and is served by the API at `/admin/`. |

## Wiki Editing

The in-game help guide now lives in the repo-root `wiki/` folder and is served by the API at `/wiki`.

When a human or agent edits the wiki:

- Treat `wiki/` as the canonical home for player-facing help content.
- Prefer updating the relevant page in `wiki/` instead of adding long gameplay prose elsewhere.
- Keep each page small, beginner-friendly, and focused on one topic.
- Use the existing frontmatter fields on every page: `title`, `summary`, `section`, `order`, `template`, and `showToc`.
- Keep `wiki/navigation.json` in sync when you add, rename, reorder, or remove pages.
- Link between pages with normal Markdown links like `./trading.md`. The API rewrites those to `/wiki/...`.
- If mechanics change, update the matching wiki page in the same change so the help guide stays accurate.
- Use `GAMEPLAY.md` as a compact design overview, not as the place to preserve old tutorial dialogue or full wiki copy.

Suggested workflow:

1. Read the existing wiki page for the system you are changing.
2. Update that page and `wiki/navigation.json` if needed.
3. If the change affects high-level design, trim or refresh `GAMEPLAY.md` so it stays short and current.
4. Build the API if you changed wiki-serving code, or at least verify the Markdown files remain valid and well-linked if the change is docs-only.

## Releasing
- Update Version in Project Settings
- Log into Docker and your DigitalOcean registry first, for example with `doctl registry login`
- Run `./build-game.sh`
- Add new Git Release in github
- Upload builds in dist to github release
- Pushing to main deploys the api
- `build-game.sh` also builds and pushes the dedicated server image as `registry.digitalocean.com/piratesquest/piratesquest-server:<version>` and `:latest`

### Dedicated Server Containers

The dedicated server now has a separate Docker image and a small host-side manager script.
The Docker image files live under `godot/server-docker/` so the server-specific container setup stays next to the Godot dedicated server export.

Release image:

```bash
./build-game.sh
```

That pushes:

- `registry.digitalocean.com/piratesquest/piratesquest-server:<game-version>`
- `registry.digitalocean.com/piratesquest/piratesquest-server:latest`

If you rebuild the same game version again, `build-game.sh` publishes a fresh image and moves the same version tag to that newest upload.

If you ever need a different registry, you can still override it with `DOCR_REGISTRY=... ./build-game.sh`.
The dedicated server image is built as `linux/amd64` by default because the Godot server export is currently Linux x64.

Server host usage:

```bash
./server.sh list
./server.sh new 12 your-server-api-key 7777
./server.sh stop 12
./server.sh start 12
./server.sh destroy 12
```

Helpful environment variables:

- `PQ_SERVER_IMAGE` overrides the image to pull/run
- `PQ_SERVER_PLATFORM` overrides the Docker platform (defaults to `linux/amd64`)
- `PQ_API_URL` passes a custom API base URL to the game server

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

## Importing Terrain
- Open the blender terrain.blend file
  - Terrain was created from this tutorial: https://www.youtube.com/watch?v=D9lBhr5gq2s
  - add -col to the object if you want collision enabled
- Edit terrain
- Export .glb file in /godot/art
- Go into Godot file browser, right click and select "reimport"

## Third Party
- [GoDot](https://godotengine.org/)
- [godot_wry](https://github.com/doceazedo/godot_wry) (native WebView for Godot)
- Kenney.nl
  - [Prototype Textures](https://kenney.nl/assets/prototype-textures)
  - [Pirate Pack](https://kenney.nl/assets/pirate-kit)
- Sounds
  - [TomMusic](https://tommusic.itch.io/)
  - [JCSounds](https://jcsounds.itch.io/piratesfxvol1)
  - [olexmazur](https://olexmazur.itch.io/fantasy-card-game)
  - [cyrex-studios](https://cyrex-studios.itch.io/universal-ui-soundpack)
  - [psionicgames](https://psionicgames.itch.io/my-game-sound-fx)
  - [hzsmith](https://hzsmith.itch.io/free-sfx-pack-vol-4)
- Icons
  - [Game-icons.net](https://game-icons.net/tags/pirate.html)
- Shaders
  - [Synty Drop in Shader](https://godotshaders.com/shader/synty-core-drop-in-water-shader/)


## ROADMAP
### Alpha 0.9.0
- Ghost ships when improper disconnect happens 

### Planned
- [ ] In game map
- [ ] Game freezes on first explosion animation
- [ ] EU Servers
- [ ] Serve HUD from Local package instead of from web
- [ ] Would it be nice if camera turned with the ship?
- [ ] Flexible turn Speed when slow or fast

### Suggestions
- [ ] Multiple Cannonball types?
- [ ] Proximity voice chat
- [ ] Mini games
  - [ ] Capture the flag
- [ ] Weapons
  - [ ] Sea Mines
  - [ ] Mermaid summoning
  - [ ] Bow Cannon (weak)
  - [ ] Grenade Shot
- [ ] Player Economy
- [ ] Burry items instead of vault, can check the spot for a chance to find the treasure
- [ ] Do think there should be some sort of defense move if possible. Don't know what it could be but would be helpful.
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
- [ ] UI to show you where a port is and directions 
- [ ] Shooting direction is still frustrating
