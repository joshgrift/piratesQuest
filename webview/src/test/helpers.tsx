import { render, act } from "@testing-library/react";
import App from "../App";
import { makePortState } from "./fixtures";
import type { PortState } from "../types";

/**
 * Render the App and simulate Godot opening the port UI.
 *
 * Accepts optional PortState overrides so each test can tweak just the
 * fields it cares about. Returns the standard Testing Library queries
 * plus a helper to grab the IPC spy for asserting messages sent to Godot.
 */
export function renderApp(overrides?: Partial<PortState>) {
  const state = makePortState(overrides);
  const result = render(<App />);

  // Simulate what Godot does: call window.openPort with the port data.
  // Wrapped in act() because it triggers React state updates.
  act(() => {
    window.openPort?.(state);
  });

  return {
    ...result,
    /** The PortState that was used to open the UI. */
    state,
    /** The mock IPC postMessage function — use to assert messages sent to Godot. */
    get ipcSpy() {
      return window.ipc!.postMessage as ReturnType<typeof import("vitest").vi.fn>;
    },
  };
}
