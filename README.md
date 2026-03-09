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
| `build-game.sh` | Builds the port UI and publishes it to `api/fragments/webview/`, then exports macOS, Windows, and Linux server builds zipped into `dist/<version>/`. Supports `--skip-notarization` (or `--no-notarize`) to skip macOS notarization (and codesign) for that run. |
| `run.sh` | Runs a local dev session. Builds menu + admin, starts the port UI Vite dev server (`localhost:5173`), backend, a game server, and a client side-by-side. Supports `--server` (server only), `--prod` (use production API at pirates.quest), `--user`, and `--password` flags. |
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
### 0.6.0 Alpha
- [x] Characters
- [x] New embed code
  - [x] Built by HAND to fix all of the issues we're seeing
- [x] UI Upgrade!
  - [x] New Status Widget
  - [x] New inventory display
  - [x] Better panel management
  - [x] More interactive chat menu
  - [x] Re-theme

### Alpha 7 - Quests
- [ ] Quests
  - [ ] Basically a todo list. The list will search a new long term storage item:
  - [ ] Player Stats, which are different from ship stats. Player stats are a list of things that is relevant for a quest.
    - [ ] There will be two versions. The long term one, and the "Since new quest" one. 
    - [ ] When you accept a quest, your quest one will be reset. 
    - [ ] Whenever you do an action, it will be updated in your player stats. If a quest if active, it will do the quest one too
    - [ ] Each time stats are updated, quests todo list is reloaded to see how close you are to completing the quest. 

Items to add to stats:
- How many items collected of each
- How many items bought of each item
- How many time have you visited ports
- List of ports visited
- How many cannonballs shot
- How many ships hit
- How many ships sunk
- Which components have been bought
- Which crew have been hired
- Which NPCs have been talked to
- Which ship level
- How much money earned total
- How much money spent

Quest List:
- (default) Sail to port (Scarlett)
  - completed when ports_visited.length >= 1
  - Unlocks Selling & Tavern
- Harvest For someone (NPC)
  - compelted when harvest greater then one for each item
  - Unlocks buy
- Trade for the merchant (Merchant)
  - Buy one of each type
  - Sell one of each type of item for profit
  - Make at least 100 gold
  - Unlocks components
- Beef up your ship (Defense guy)
 - When components eqiped > 1
 - Unlocks Ship leveling up.
- Kill 5 ships (Cool)
 - Ships killed > 5
 - Unlocked Vault

### Alpha 8 - AI Update
- [ ] AI ships

### Alpha 9 - World Update
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
