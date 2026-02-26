# WebView DPI / Multi-Monitor Fix — Working Context

## The Problem

On macOS with a Retina MacBook (2x DPI) connected to a 1x external monitor, the godot_wry WebView panel either doesn't appear or is incorrectly sized/positioned.

## Root Cause

There are two interacting bugs:

### 1. Godot 4 Viewport Inflation (macOS)

Godot 4 with `canvas_items` stretch mode inflates the viewport size (`Root.Size`) by `screen_get_max_scale()` — the **highest** DPI scale across **all** connected monitors. So on a setup with a 2x Retina laptop + 1x external monitor:

- On the Retina laptop: `Root.Size` = 3024×1832 (correct — 1512×916 logical × 2)
- On the 1x external: `Root.Size` = 3840×2054 (inflated — actual window is ~1920×1027 but Godot multiplies by max scale 2)

### 2. WRY PhysicalPosition/PhysicalSize Division

godot_wry passes Godot's coordinates to WRY as `PhysicalPosition`/`PhysicalSize`. WRY then divides by the **current** monitor's `backingScaleFactor`:

- On Retina (scale=2): 3024/2 = 1512 logical → correct
- On 1x monitor (scale=1): 3840/1 = 3840 logical → 2× too large, pushes content off-screen

### Why `full_window_size = true` Doesn't Fix It

Looking at godot_wry v1.0.2 `rust/src/lib.rs` line 488-506:

```rust
fn resize(&self) {
    let rect = if self.full_window_size {
        let viewport_size = self.base().get_tree().get_root().get_size();
        Rect {
            position: PhysicalPosition::new(0, 0).into(),
            size: PhysicalSize::new(viewport_size.x, viewport_size.y).into(),
        }
    } else {
        let pos = self.base().get_screen_position();
        let size = self.base().get_size();
        Rect {
            position: PhysicalPosition::new(pos.x, pos.y).into(),
            size: PhysicalSize::new(size.x, size.y).into(),
        }
    };
    let _ = webview.set_bounds(rect);
}
```

Both paths pass Godot's inflated values directly to WRY as `PhysicalSize`. The inflation/division mismatch affects both modes equally.

### GitHub Issue

This is tracked as [godot_wry #54](https://github.com/doceazedo/godot_wry/issues/54) — "Webview scaling issue when using two different monitors". It was closed by commit `ac83077` in v1.0.2, but that fix only added `window_position` tracking to trigger a resize on monitor change. It does **not** fix the core DPI mismatch math.

## The Fix (Current Implementation)

### C# Side (`Hud.cs`)

1. **`full_window_size = false`** — we handle sizing ourselves
2. **`transparent = true`** — game shows through the transparent areas
3. **`CanvasLayer`** — the WebView node lives inside a `CanvasLayer`, which bypasses the `canvas_items` stretch transform. This means `get_screen_position()` and `get_size()` return raw physical pixel values (what WRY expects).
4. **DPI correction in `SyncWebViewSize()`:**

```csharp
private void SyncWebViewSize()
{
    if (_webView is not Control wv) return;

    var vpSize = GetTree().Root.Size;

    // Find the highest DPI scale across all monitors
    float maxScale = 1f;
    for (int i = 0; i < DisplayServer.GetScreenCount(); i++)
        maxScale = Math.Max(maxScale, DisplayServer.ScreenGetScale(i));

    // Get the current monitor's scale
    float currentScale = DisplayServer.ScreenGetScale(
        DisplayServer.WindowGetCurrentScreen());

    // Undo Godot's inflation: multiply by currentScale/maxScale
    float correction = currentScale / maxScale;
    float w = vpSize.X * correction;
    float h = vpSize.Y * correction;

    wv.Position = new Vector2(0, 0);
    wv.Size = new Vector2(w, h);
}
```

**Why this works:** Godot inflates `vpSize` by `maxScale`. WRY will divide by `currentScale`. By multiplying by `currentScale / maxScale`, we pre-correct so that after WRY's division, the result matches the actual physical window size.

| Monitor | vpSize.X | maxScale | currentScale | correction | w (after) | WRY divides by currentScale | Final logical |
|---------|----------|----------|-------------|------------|-----------|---------------------------|---------------|
| Retina  | 3024     | 2        | 2           | 1.0        | 3024      | 3024/2 = 1512             | Correct       |
| 1x ext  | 3840     | 2        | 1           | 0.5        | 1920      | 1920/1 = 1920             | Correct       |

5. **`OnWindowResized`** connected to `GetTree().Root.SizeChanged` calls `SyncWebViewSize()` via `CallDeferred` (deferred because the canvas transform isn't updated yet at signal time).
6. **`_ExitTree`** disconnects the signal.

### CSS Side (`App.css`)

The webview covers the entire window transparently. CSS positions the actual UI panel:

```css
html, body, #root {
    background: transparent;
    pointer-events: none;    /* clicks pass through to the game */
}

.port-panel {
    position: fixed;
    top: 0;
    right: 0;
    width: 33.333%;
    height: 100vh;
    pointer-events: auto;   /* panel itself receives clicks */
    /* ... backgrounds, transitions, etc. */
}
```

### Visibility

Show/hide uses godot_wry's `set_visible()`:

```csharp
_webView.Call("set_visible", true);   // show
_webView.Call("set_visible", false);  // hide
```

## Key Files

| File | Role |
|------|------|
| `godot/scenes/play/scenes/Hud.cs` | WebView creation, DPI fix, IPC handling, show/hide |
| `webview/src/App.css` | Panel positioning (right 1/3), transparent background |
| `webview/src/App.tsx` | React app, IPC communication, port state display |
| `godot/scenes/play/scenes/webview_node.tscn` | Minimal scene with just a `WebView` node |
| `godot/addons/godot_wry/` | Plugin v1.0.2 (latest as of Feb 2026) |

## Debug Values Reference

From actual testing:

```
# On 1x external monitor (game dragged there):
vpSize=(3840, 2054) screenScale=1 maxScale=2 correction=0.5
→ webview sized to 1920×1027 → WRY: 1920/1 = 1920 logical ✓

# On Retina laptop screen:
vpSize=(3024, 1832) screenScale=2 maxScale=2 correction=1.0
→ webview sized to 3024×1832 → WRY: 3024/2 = 1512 logical ✓
```

## Future: When This Can Be Simplified

If godot_wry fixes the DPI math upstream (dividing by `maxScale` before passing to WRY, or using logical coordinates), the fix simplifies to:

1. Set `full_window_size = true` + `transparent = true`
2. Delete `SyncWebViewSize()`, `OnWindowResized()`, `_ExitTree()`, and the `CanvasLayer`
3. CSS already handles the right-1/3 positioning

## Current Status

- DPI fix is working on both monitors
- WebView is currently set to always visible (`set_visible(true)` on creation) for debugging input issues
- CSS layout (right 1/3, transparent background, pointer-events passthrough) is in place

## Editor Note

godot_wry (native webview) does **not** work in Godot's embedded mode. Before running: uncheck **"Embed Game on Next Play"** in the Game panel/tab of the Godot editor.
