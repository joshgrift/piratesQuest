import { describe, it, expect } from "vitest";
import { fireEvent, screen } from "@testing-library/react";
import { renderApp } from "../test/helpers";
import { getIpcMessages } from "../test/helpers";

describe("TavernTab", () => {
  it("sends hire IPC when hiring a character", async () => {
    const { ipcSpy } = renderApp({ tab: "tavern" });

    fireEvent.click(screen.getByRole("button", { name: /Talk about ship work\./ }));
    fireEvent.click(await screen.findByRole("button", { name: /What would ye do on my deck\?/ }));
    fireEvent.click(await screen.findByRole("button", { name: /Ask him plain to sign aboard\./ }));
    fireEvent.click(await screen.findByRole("button", { name: /Join my crew\./ }));

    const actions = getIpcMessages(ipcSpy) as { action: string; characterId?: string }[];
    expect(actions).toContainEqual({
      action: "hire_character",
      characterId: "gideon-gearlock",
    });
  });

  it("blocks hire IPC when crew slots are already full", async () => {
    const { ipcSpy } = renderApp({
      tab: "tavern",
      state: {
        crew: {
          crewSlots: 2,
          hiredCharacterIds: ["gideon-gearlock", "tommy-fuse"],
          characters: [],
        },
        tavern: {
          characters: [
            {
              id: "gideon-gearlock",
              name: "Gideon Gearlock",
              role: "Merchant Broker",
              portrait: "character8.png",
              hireable: true,
              statChanges: [{ stat: "SellPriceBonus", modifier: "Additive", value: 0.005 }],
            },
            {
              id: "tommy-fuse",
              name: "Tommy Fuse",
              role: "Powder Runner",
              portrait: "character7.png",
              hireable: true,
              statChanges: [{ stat: "AttackRange", modifier: "Additive", value: 3 }],
            },
            {
              id: "elder-bertram",
              name: "Elder Bertram",
              role: "Retired Shipwright",
              portrait: "character17.png",
              hireable: true,
              statChanges: [{ stat: "ShipHullStrength", modifier: "Additive", value: 12 }],
            },
          ],
        },
      },
    });

    fireEvent.click(screen.getByRole("button", { name: /Elder Bertram/ }));
    fireEvent.click(screen.getByRole("button", { name: /Talk about ship work\./ }));
    fireEvent.click(await screen.findByRole("button", { name: /What would change on the ship\?/ }));
    fireEvent.click(await screen.findByRole("button", { name: /Ask him plain to sign aboard\./ }));
    fireEvent.click(await screen.findByRole("button", { name: /Join my crew\./ }));

    const actions = getIpcMessages(ipcSpy) as { action: string }[];
    expect(actions.find((a) => a.action === "hire_character")).toBeUndefined();
  });

  it("hides hireability status in the roster until conversation", () => {
    renderApp({ tab: "tavern" });

    expect(screen.queryByText("Talk")).not.toBeInTheDocument();
    expect(screen.queryByText("Hire")).not.toBeInTheDocument();
  });

  it("reveals non-hireable result only after talking", async () => {
    renderApp({ tab: "tavern" });

    fireEvent.click(screen.getByRole("button", { name: /Valora Rumwhisper/ }));
    fireEvent.click(screen.getByRole("button", { name: /Talk about ship work\./ }));
    fireEvent.click(await screen.findByRole("button", { name: /Would ye sail with me at all\?/ }));
    fireEvent.click(await screen.findByRole("button", { name: /Ask her plain to sign aboard\./ }));

    expect(await screen.findByText(/i stay where stories wash ashore/i)).toBeInTheDocument();
  });
});
