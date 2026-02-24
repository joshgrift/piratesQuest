import { render, act, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import App from "../App";
import { makePortState } from "./fixtures";
import type { PortState } from "../types";
import type { Mock } from "vitest";

type Tab = "market" | "shipyard" | "guide" | "creative";

const TAB_LABELS: Record<Tab, string> = {
  market: "Market",
  shipyard: "Shipyard",
  guide: "Scarlett",
  creative: "Creative",
};

interface RenderAppOptions {
  /** Override any PortState fields. */
  state?: Partial<PortState>;
  /** Which tab to activate after opening. Defaults to "guide" (the App default). */
  tab?: Tab;
}

/**
 * Render the App, simulate Godot opening the port, and optionally switch tabs.
 *
 * Returns Testing Library queries, the PortState used, and the IPC spy.
 */
export function renderApp(overridesOrOptions?: Partial<PortState> | RenderAppOptions) {
  // Support both the old simple signature and the new options object.
  let stateOverrides: Partial<PortState> | undefined;
  let tab: Tab | undefined;

  if (overridesOrOptions && "state" in overridesOrOptions) {
    stateOverrides = overridesOrOptions.state;
    tab = overridesOrOptions.tab;
  } else {
    stateOverrides = overridesOrOptions as Partial<PortState> | undefined;
  }

  const state = makePortState(stateOverrides);
  const user = userEvent.setup();
  const result = render(<App />);

  // Simulate what Godot does: call window.openPort with the port data.
  act(() => {
    window.openPort?.(state);
  });

  // Navigate to the requested tab if it's not the default.
  if (tab && tab !== "guide") {
    const tabButton = screen.getByRole("button", { name: TAB_LABELS[tab] });
    act(() => {
      tabButton.click();
    });
  }

  return {
    ...result,
    user,
    state,
    get ipcSpy() {
      return window.ipc!.postMessage as Mock;
    },
  };
}

/**
 * Helper to extract all IPC messages sent since the spy was last cleared.
 * Returns parsed objects so tests can assert on structured data.
 */
export function getIpcMessages(spy: Mock): unknown[] {
  return spy.mock.calls.map(([json]: [string]) => JSON.parse(json));
}
