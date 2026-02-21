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


## TODO
### 0.4.0 Alpha
- [ ] React UI so it can explain a lot more
  - [ ] More details
  - [ ] Stacking components
  - [ ] Satisfying animations
  - [ ] Buy in 5, 10, 50 increments
  - [ ] Sell all button
- [ ] Safe zone in port
- [ ] Stash
- [ ] AI ships
- [ ] Ship upgrades (persistent)
- [ ] Proximity voice chat
- [ ] UI to show when you are harvesting things
- [ ] Persistent world
- [ ] Login
- [ ] More incremental progress
- [ ] Quick buy for components
- [ ] Backwords should be a more gradual movement
- [ ] Health too expensive?
- [ ] Artifacts are long term growth options.

### Later
- [ ] Quests
- [ ] Ship Upgrades
- [ ] API for AI characters
- [ ] Island mesh?
   - [ ] Better island cannonball collision
- [ ] Fix water.

### Suggestions
- [ ] Close port menu early
- [ ] Multiple Cannonball types?
- [ ] In Combat Timer
  - [ ] Can't heal if you're in combat
  - [ ] When in port, the timer pauses