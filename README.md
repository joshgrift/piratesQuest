# PiratesQuest
> A one-hour pirate sandbox where economy, exploration, and combat collide—designed to test how humans and AIs compete, cooperate, and outplay each other in a living system.

This is an exploration of C#, Godot and creating a multiplayer game of man vs machine where AI competes against humans.
w
## Running
To run in the editor:
- Run `run.sh --server`
- Run the client in the editor

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


## TODO before tests
### 0.3.0 Alpha
- [ ] Bugs & Improvements
  - [x] Backing up controls feel weird
  - [x] camera can go upside down
  - [ ] Auto equip components when you buy them
  - [x] Cannon ball goes too far
  - [x] Need the ability to shoot, move, and turn at the same time
  - [ ] Collection points are too hard to see
  - [x] Improve turn radius
  - [ ] UI to show you collecting items
  - [ ] Reset Port UI when you buy
  - [ ] Status Tool Tip
- [ ] A way to heal
- [x] Hit Markers
- [x] Limit Player name characters
- [ ] Death & Respawn
  - [ ] Make death less punishing somehow
  - [ ] Stop player from shooting when dead
  - [ ] Respawn player automatically after 5 seconds instead of back to menu
- [x] Broadcast sound across all players
- [ ] Fix cannonball collision

### Later
- [ ] Quests
- [ ] Ship Upgrades
- [ ] API for AI characters
- [ ] Island mesh?
- [ ] Fix water.

### Suggestions
- [ ] Close port menu early
- [ ] Multiple Cannonball types?
- [ ] In Combat Timer
  - [ ] Can't heal if you're in combat
  - [ ] When in port, the timer pauses