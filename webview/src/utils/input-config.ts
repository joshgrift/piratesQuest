/**
 * Central configuration for all input forwarded from the webview to Godot.
 *
 * Keybindings: maps browser `e.key.toLowerCase()` to the action name sent via IPC.
 * Camera: controls how raw browser event deltas are scaled before being sent.
 *
 * Godot-side tuning lives in CameraPivot's exported properties (MouseSensitivity,
 * TrackpadRotationSensitivity, ZoomSpeed, etc.). The values here are an additional
 * multiplier applied in the webview layer — keep them at 1.0 unless you want
 * browser-specific adjustment on top of the Godot settings.
 */
export const INPUT_CONFIG = {
  keybindings: {
    // Ship movement
    w: "move_forward",
    s: "move_back",
    a: "move_left",
    d: "move_right",
    // Cannons
    q: "fire_left",
    e: "fire_right",
  } satisfies Record<string, string>,

  camera: {
    /**
     * Which mouse button starts a drag-to-rotate gesture.
     * 0 = left, 1 = middle, 2 = right
     */
    dragButton: 0,

    /**
     * Multiplier applied to mouse movementX/Y before sending to Godot.
     * Godot's CameraPivot.MouseSensitivity does the primary tuning.
     */
    dragSensitivity: 1,

    /**
     * Multiplier applied to wheel deltaY / 100 before sending as zoom delta.
     * Godot's CameraPivot.ZoomSpeed does the primary tuning.
     */
    scrollZoomSensitivity: 0.5,

    /**
     * Multiplier applied to wheel deltaX before sending as horizontal rotate.
     * Godot's CameraPivot.TrackpadRotationSensitivity does the primary tuning.
     */
    panSensitivity: 0.3,
  },

  /**
   * CSS selectors used to detect UI elements.
   * Mouse camera controls are suppressed when the event target is inside
   * any of these elements.
   */
  uiSelectors: [
    ".port-panel",
    ".mode-rail",
    ".left-inventory-panel",
    ".left-health-panel",
  ],
} as const;
