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
  /**
   * When true, the IPC mock simulates Godot by calling window.updateState
   * after every postMessage. This is needed for tests that exercise
   * sendIpcAndWait (e.g. Buy & Repair, Buy All & Build).
   */
  simulateGodot?: boolean;
}

/**
 * Render the App, simulate Godot opening the port, and optionally switch tabs.
 *
 * Returns Testing Library queries, the PortState used, and the IPC spy.
 */
export function renderApp(overridesOrOptions?: Partial<PortState> | RenderAppOptions) {
  let stateOverrides: Partial<PortState> | undefined;
  let tab: Tab | undefined;
  let simulateGodot = false;

  if (overridesOrOptions && "state" in overridesOrOptions) {
    stateOverrides = overridesOrOptions.state;
    tab = overridesOrOptions.tab;
    simulateGodot = (overridesOrOptions as RenderAppOptions).simulateGodot ?? false;
  } else {
    stateOverrides = overridesOrOptions as Partial<PortState> | undefined;
  }

  const state = makePortState(stateOverrides);
  const user = userEvent.setup();
  const result = render(<App />);

  act(() => {
    window.openPort?.(state);
  });

  // When simulateGodot is on, make the IPC mock behave like the real game:
  // after every postMessage, push updated state back via window.updateState.
  // This lets sendIpcAndWait promises resolve in tests.
  if (simulateGodot) {
    const spy = window.ipc!.postMessage as Mock;
    spy.mockImplementation(() => {
      queueMicrotask(() => {
        act(() => {
          window.updateState?.(state);
        });
      });
    });
  }

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
  return spy.mock.calls.map((args) => JSON.parse(args[0] as string));
}
