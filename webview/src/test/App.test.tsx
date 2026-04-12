import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { getIpcMessages, renderApp } from "./helpers";
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

  it("marks completed quest widget steps with the complete styling hook", () => {
    const { container, getByText } = renderApp({
      quests: {
        available: [],
        active: {
          id: "scarlett-first-quest",
          title: "Scarlett's Starter Run",
          giverNpcId: "scarlett",
          giverName: "Scarlett",
          giverPortrait: "character1.png",
          giverPortName: "Tortuga",
          revealGiverInQuestLog: true,
          canAcceptFromQuestLog: true,
          description: "A short tutorial quest.",
          completionText: "Nicely done.",
          unlocks: [],
          steps: [
            { label: "Buy 1 Tea", currentValue: 1, requiredValue: 1, isComplete: true },
            { label: "Sell 1 Fish", currentValue: 0, requiredValue: 1, isComplete: false },
          ],
        },
        all: [],
        completedIds: [],
        unlockedFeatures: [],
      },
    });

    const completedRow = getByText("Buy 1 Tea").closest(".quest-status-row");
    const completedLabel = container.querySelector(".quest-status-row.complete .quest-status-row-label");
    const completedValue = container.querySelector(".quest-status-row.complete .quest-status-row-value");

    expect(completedRow).toHaveClass("complete");
    expect(completedLabel).toHaveTextContent("Buy 1 Tea");
    expect(completedValue).toHaveTextContent("1/1");
  });

  it("shows AcceptedText when a quest is already active from auto-accept", () => {
    renderApp({
      isInPort: false,
      quests: {
        available: [],
        active: {
          id: "scarlett_learn_to_sail",
          title: "Learn to Sail",
          giverNpcId: "scarlett",
          giverName: "Scarlett",
          giverPortrait: "character2.png",
          giverPortName: "",
          revealGiverInQuestLog: true,
          canAcceptFromQuestLog: true,
          acceptedText: "Welcome to the Seas! Press W, A, S, or D and make the ship answer.",
          description: "Move the ship once.",
          completionText: "",
          unlocks: [],
          steps: [],
        },
        all: [],
        completedIds: [],
        unlockedFeatures: [],
      },
    });

    expect(screen.getByText(/Press W, A, S, or D/i)).toBeInTheDocument();
  });

  it("does not auto-open Scarlett onboarding or auto-accept her quest at sea", () => {
    const { ipcSpy } = renderApp({
      isInPort: false,
      quests: {
        available: [
          {
            id: "scarlett_learn_to_sail",
            title: "Learn to Sail",
            giverNpcId: "scarlett",
            giverName: "Scarlett",
            giverPortrait: "character2.png",
            giverPortName: "",
            revealGiverInQuestLog: true,
            canAcceptFromQuestLog: true,
            description: "Dock once.",
            completionText: "Nicely done.",
            unlocks: ["SellGoods", "TavernTalk"],
            steps: [],
          },
        ],
        active: null,
        all: [],
        completedIds: [],
        unlockedFeatures: [],
      },
    });

    expect(screen.queryByText(/top-left Quests button/i)).not.toBeInTheDocument();

    const actions = getIpcMessages(ipcSpy) as { action: string; questId?: string; characterId?: string }[];
    expect(actions).not.toContainEqual({
      action: "accept_quest",
      questId: "scarlett_learn_to_sail",
      characterId: "scarlett",
    });
  });
});
