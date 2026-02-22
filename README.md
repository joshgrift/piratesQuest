# PiratesQuest

## Architecture
PiratesQuest uses three pieces:

- `Game` (`godot/`): the player client and also the dedicated multiplayer server.
- `API` (`server/`): login/signup and server list.
- `Database` (via Docker): stores users and API data.

## Running
### Run backend (API + DB)
From repo root:

```bash
cd server
docker compose up -d
dotnet run
```

Default API URL is `http://localhost:5236` (matches `godot/scripts/Configuration.cs`).

### Run game server
From repo root:

```bash
cd godot
./run.sh --server
```

### Run client in editor
- Open `godot/project.godot` in Godot 4
- Press Play
- Login/signup, then join a server

To run without the editor:
- Run `run.sh`

Please submit a PR, learning godot, so any and all suggestions welcome.

## Releasing
- Update Version in Project Settings
- Run `./build.sh`
- Add new Git Release in github
- Upload builds in dist to github release

## Third Party
- [GoDot](https://godotengine.org/)
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
- [ ] Artifacts are long term growth options.

### Later
- [ ] Quests
- [ ] Island mesh?
   - [ ] Better island cannonball collision
- [ ] Fix water.
- [ ] Backwords should be a more gradual movement
- [ ] Proximity voice chat

### Suggestions
- [ ] Close port menu early
- [ ] Multiple Cannonball types?
- [ ] In Combat Timer
  - [ ] Can't heal if you're in combat
  - [ ] When in port, the timer pauses
