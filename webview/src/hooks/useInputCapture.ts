import { useEffect, useRef } from "react";
import { sendIpc } from "../utils/ipc";
import { INPUT_CONFIG } from "../utils/input-config";

/**
 * Captures all keyboard and mouse input in the webview and forwards it to
 * Godot via IPC. This is necessary because the native webview overlay
 * intercepts OS-level input before Godot can see it.
 *
 * Camera controls (drag, zoom, pan) are suppressed when the pointer is over
 * a UI panel. The drag suppression is sticky — if a drag begins on the game
 * surface and the pointer drifts over UI mid-drag, the drag continues
 * uninterrupted.
 */
export function useInputCapture() {
  // True while a camera-drag gesture started on the game surface.
  const dragActiveRef = useRef(false);

  useEffect(() => {
    const cfg = INPUT_CONFIG;

    // ── Helpers ─────────────────────────────────────────────────────────────

    /** True when the event target is inside a UI panel element. */
    function isOverUI(target: EventTarget | null): boolean {
      if (!(target instanceof HTMLElement)) return false;
      const selector = cfg.uiSelectors.join(", ");
      return target.closest(selector) !== null;
    }

    /** True when a text input or textarea is focused (don't steal typing). */
    function isTypingInInput(): boolean {
      const el = document.activeElement;
      if (!el) return false;
      return (
        el.tagName === "INPUT" ||
        el.tagName === "TEXTAREA" ||
        (el as HTMLElement).contentEditable === "true"
      );
    }

    // ── Keyboard ─────────────────────────────────────────────────────────────

    function handleKeyDown(e: KeyboardEvent) {
      if (isTypingInInput()) return;
      const key = e.key.toLowerCase();
      if (key in cfg.keybindings) {
        e.preventDefault();
        sendIpc({ action: "input_key", key, pressed: true });
      }
    }

    function handleKeyUp(e: KeyboardEvent) {
      if (isTypingInInput()) return;
      const key = e.key.toLowerCase();
      if (key in cfg.keybindings) {
        sendIpc({ action: "input_key", key, pressed: false });
      }
    }

    /** Release all held keys when the window loses focus. */
    function handleBlur() {
      for (const key of Object.keys(cfg.keybindings)) {
        sendIpc({ action: "input_key", key, pressed: false });
      }
      dragActiveRef.current = false;
    }

    // ── Mouse drag (camera rotate) ────────────────────────────────────────────

    function handleMouseDown(e: MouseEvent) {
      if (e.button === cfg.camera.dragButton && !isOverUI(e.target)) {
        dragActiveRef.current = true;
      }
    }

    function handleMouseUp(e: MouseEvent) {
      if (e.button === cfg.camera.dragButton) {
        dragActiveRef.current = false;
      }
    }

    function handleMouseMove(e: MouseEvent) {
      if (!dragActiveRef.current) return;
      sendIpc({
        action: "input_camera_rotate",
        deltaX: e.movementX * cfg.camera.dragSensitivity,
        deltaY: e.movementY * cfg.camera.dragSensitivity,
      });
    }

    // ── Scroll wheel / trackpad ──────────────────────────────────────────────

    function handleWheel(e: WheelEvent) {
      if (isOverUI(e.target)) {
        // Keep chat/list scrolling native, but block browser pinch-zoom and
        // horizontal two-finger navigation while the pointer is over UI.
        if (e.ctrlKey || e.deltaX !== 0) {
          e.preventDefault();
        }
        return;
      }
      e.preventDefault();

      if (e.ctrlKey) {
        // Trackpad pinch-to-zoom: browser reports this as wheel + ctrlKey.
        // Normalize to the same -1..1 range we use for regular scroll.
        sendIpc({
          action: "input_camera_zoom",
          delta: (e.deltaY / 100) * cfg.camera.scrollZoomSensitivity,
        });
        return;
      }

      // Vertical scroll → zoom
      if (e.deltaY !== 0) {
        sendIpc({
          action: "input_camera_zoom",
          delta: (e.deltaY / 100) * cfg.camera.scrollZoomSensitivity,
        });
      }

      // Horizontal scroll (trackpad two-finger swipe) → rotate camera
      if (e.deltaX !== 0) {
        sendIpc({
          action: "input_camera_pan",
          deltaX: e.deltaX * cfg.camera.panSensitivity,
        });
      }
    }

    // ── Event registration ───────────────────────────────────────────────────

    window.addEventListener("keydown", handleKeyDown);
    window.addEventListener("keyup", handleKeyUp);
    window.addEventListener("blur", handleBlur);
    window.addEventListener("mousedown", handleMouseDown);
    window.addEventListener("mouseup", handleMouseUp);
    window.addEventListener("mousemove", handleMouseMove);
    // passive: false so we can call preventDefault() on wheel events
    window.addEventListener("wheel", handleWheel, { passive: false });

    return () => {
      window.removeEventListener("keydown", handleKeyDown);
      window.removeEventListener("keyup", handleKeyUp);
      window.removeEventListener("blur", handleBlur);
      window.removeEventListener("mousedown", handleMouseDown);
      window.removeEventListener("mouseup", handleMouseUp);
      window.removeEventListener("mousemove", handleMouseMove);
      window.removeEventListener("wheel", handleWheel);
    };
  }, []);
}
