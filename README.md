# PiratesQuest 
> A one-hour pirate sandbox where economy, exploration, and combat collide—designed to test how humans and AIs compete, cooperate, and outplay each other in a living system.

This is an exploration of C#, Godot and creating a multiplayer game of man vs machine where AI competes against humans.

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

## TODO before tests
### 0.2.0 Alpha 
- [x] Ship movement
- [x] Multiplayer
- [ ] Core Game Mechanics
  - [x] Collection of resources
  - [x] Three resource types & store
  - [x] Shooting Cannonballs, health, and death.
  - [x] Limited Map with islands
  - [ ] Components
    - [x] Can add to ship
    - [x] Can buy in Port
    - [x] Limit number of components
    - [x] Allow multiple of the same component
    - [x] Limit Cargo Size
    - [ ] Increase Collection
    - [x] Auto Heal Mechanic
  - [ ] Pickup dead player rewards
- [ ] Quests to get skulls
- [ ] Leaderboards
- [ ] Graphics
  - [x] Opaque collection spaces
  - [ ] Hit Markers
  - [ ] Cannon shot graphics and shoot from side
  - [x] Collection spots have island
  - [ ] Basic Water & Island Graphics
- [x] Distribution

### 0.3.0 Alpha
- [ ] API for AI characters
  - [ ] Different ship design for AI character?
- [ ] Graphics
  - [ ] Water
  - [ ] Proper Map graphics