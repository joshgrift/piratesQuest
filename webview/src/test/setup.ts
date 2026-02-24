import "@testing-library/jest-dom";
import { afterEach, vi } from "vitest";
import { cleanup } from "@testing-library/react";

// Clean up the DOM after each test so components don't leak between tests.
afterEach(() => {
  cleanup();

  // Remove the window functions that App.tsx installs during useEffect,
  // so each test starts with a clean slate.
  delete window.openPort;
  delete window.closePort;
  delete window.updateState;
});

// Provide a mock IPC channel that tests can spy on.
// App.tsx calls window.ipc?.postMessage(json) to send messages to Godot.
Object.defineProperty(window, "ipc", {
  value: { postMessage: vi.fn() },
  writable: true,
});

// jsdom's requestAnimationFrame doesn't fire callbacks synchronously,
// which makes it hard to test code that relies on it (like the open
// animation in App.tsx). This shim calls the callback immediately.
window.requestAnimationFrame = (cb: FrameRequestCallback): number => {
  cb(0);
  return 0;
};
