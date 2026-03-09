import { describe, it, expect } from "vitest";
import { renderApp } from "./helpers";
import { makeOwnedComponent } from "./fixtures";

describe("App", () => {
  it("shows the port panel after openPort is called", () => {
    const { container } = renderApp();
    // The port panel should now be in the DOM
    expect(container.querySelector(".port-panel")).toBeInTheDocument();
  });

  it("displays the port name", () => {
    const { getByText } = renderApp({ portName: "Nassau" });
    expect(getByText("Nassau")).toBeInTheDocument();
  });

  it("sends a ready IPC message on mount", () => {
    const { ipcSpy } = renderApp();
    // App sends { action: "ready" } when it first mounts
    expect(ipcSpy).toHaveBeenCalledWith(
      JSON.stringify({ action: "ready" }),
    );
  });

  it("shows the ship status widget with player, health, and cannonballs", () => {
    const { getByTestId } = renderApp({
      playerName: "Scarlet Jack",
      shipTier: 1,
      health: 77,
      maxHealth: 120,
      inventory: { CannonBall: 19, Coin: 90 },
      shipTiers: [
        { name: "Sloop", description: "A nimble starter vessel", componentSlots: 4, cost: {} },
        { name: "Brigantine", description: "A sturdy mid-size warship", componentSlots: 6, cost: {} },
      ],
    });

    expect(getByTestId("ship-status-player")).toHaveTextContent("Scarlet Jack");
    expect(getByTestId("ship-status-health")).toHaveTextContent("77 / 120");
    expect(getByTestId("ship-status-cannonballs")).toHaveTextContent("19 balls");
  });

  it("shows cooling cannon state and overburdened warning", () => {
    const { getByTestId } = renderApp({
      cannonReady: false,
      cannonCooldownRemaining: 1.2,
      isOverburdened: true,
    });

    expect(getByTestId("ship-status-cannon-state")).toHaveTextContent("Cooling 1.2s");
    expect(getByTestId("ship-status-overburdened")).toHaveTextContent("Overburdened");
  });

  it("shows component and crew slot usage", () => {
    const { getByTestId } = renderApp({
      componentCapacity: 4,
      ownedComponents: [
        makeOwnedComponent({ name: "Hull", isEquipped: true }),
        makeOwnedComponent({ name: "Sails", isEquipped: true }),
        makeOwnedComponent({ name: "Range", isEquipped: false }),
      ],
      crew: {
        crewSlots: 3,
        hiredCharacterIds: ["crew-a", "crew-b"],
        characters: [],
      },
    });

    expect(getByTestId("ship-status-component-slots")).toHaveTextContent("2/4");
    expect(getByTestId("ship-status-crew-slots")).toHaveTextContent("2/3");
  });
});
