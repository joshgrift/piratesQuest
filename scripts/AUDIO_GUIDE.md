# Audio Guide for PiratesQuest

## Overview

This project uses a **hybrid approach** for playing sounds:

1. **AudioManager** (autoload singleton) - For global, non-positional sounds
2. **AudioStreamPlayer3D** - For positional sounds that come from specific objects

## When to Use Each

### Use AudioManager for:
- ✅ UI sounds (button clicks, menu sounds)
- ✅ Coin collection feedback
- ✅ Global game events
- ✅ Music and ambient loops
- ✅ Sounds that should be heard equally by all players

**Example:**
```csharp
// Get the AudioManager autoload
var audioManager = GetNode<AudioManager>("/root/AudioManager");

// Play a one-shot sound
audioManager.PlaySound("res://art/sounds/jcsounds/Misc Sfx/sfx_coin_clink_01.wav");

// Play a looping ambient sound
audioManager.PlayLoop("ocean", "res://art/sounds/sea.wav");

// Stop a loop later
audioManager.StopLoop("ocean");
```

### Use AudioStreamPlayer3D for:
- ✅ Cannon fire (positional - other players hear it from the ship)
- ✅ Footsteps
- ✅ Explosions
- ✅ Any sound that should be louder when you're close, quieter when far

**Example:**
```csharp
// Create or get an AudioStreamPlayer3D
var soundPlayer = new AudioStreamPlayer3D();
soundPlayer.Stream = GD.Load<AudioStream>("res://art/sounds/jcsounds/Misc Sfx/sfx_cannon_fire_01.wav");
soundPlayer.MaxDistance = 100.0f; // Can be heard up to 100 units away
AddChild(soundPlayer);

// Play it
soundPlayer.Play();
```

## Signals vs Direct Calls

**Signals are great for decoupling**, but you don't always need them:

- **Use signals** when:
  - Multiple systems need to react to the same event
  - You want to keep components loosely coupled
  - Example: `CannonFired` signal - UI, sound, and effects all react

- **Use direct calls** when:
  - Only one system needs to react
  - The relationship is clear and direct
  - Example: Player directly calls `AudioManager.PlaySound()` for coin collection

## Best Practices

1. **Preload sounds** - AudioManager caches sounds automatically
2. **Use AudioStreamPlayer3D for positional audio** - Makes the game feel more immersive
3. **Don't create/destroy players constantly** - AudioManager uses a pool
4. **Adjust MaxDistance** - Set appropriate ranges for 3D sounds
5. **Use volumeDb** - Negative values = quieter, positive = louder (0 = normal)

## Example: Connecting Signals to Play Sounds

If you want to use signals (like you asked about), here's how:

```csharp
// In Player.cs - signal is already defined
[Signal] public delegate void CannonFiredEventHandler();

// In another script (like HUD.cs) - connect to the signal
public override void _Ready()
{
    var player = GetNode<Player>("../SpawnPoint/player_1");
    player.CannonFired += OnCannonFired;
}

private void OnCannonFired()
{
    // Play a UI sound or visual effect
    var audioManager = GetNode<AudioManager>("/root/AudioManager");
    audioManager.PlaySound("res://art/sounds/jcsounds/Misc Sfx/sfx_compass_clicks_01.wav");
}
```

## Available Sounds

Check `art/sounds/` for all available sound files:
- Cannon fire: `jcsounds/Misc Sfx/sfx_cannon_fire_*.wav`
- Coins: `jcsounds/Misc Sfx/sfx_coin_*.wav`
- Ambient ocean: `sea.wav`, `sea_storm.wav`
- And many more!
