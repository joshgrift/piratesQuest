import { describe, it, expect } from "vitest";
import { fireEvent, screen } from "@testing-library/react";
import { renderApp } from "../test/helpers";
import { getIpcMessages } from "../test/helpers";

describe("TavernTab", () => {
  it("sends hire IPC when hiring a character", async () => {
    const { ipcSpy } = renderApp({ tab: "market" });

    const hireButtons = screen.getAllByRole("button", { name: "Hire" });
    fireEvent.click(hireButtons[0] as HTMLElement);
    fireEvent.click(await screen.findByRole("button", { name: "Hire" }));

    const actions = getIpcMessages(ipcSpy) as { action: string; characterId?: string }[];
    expect(actions).toContainEqual({
      action: "hire_character",
      characterId: "gideon-gearlock",
    });
  });

  it("blocks hire IPC when crew slots are already full", async () => {
    const { ipcSpy } = renderApp({
      tab: "market",
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
              talkPhrases: ["Mind the spread."],
              hireText: "Give me a bunk.",
              fireText: "Then keep your own books.",
              statChanges: [{ stat: "SellPriceBonus", modifier: "Additive", value: 0.005 }],
            },
            {
              id: "tommy-fuse",
              name: "Tommy Fuse",
              role: "Powder Runner",
              portrait: "character7.png",
              hireable: true,
              talkPhrases: ["Keep powder dry."],
              hireText: "I can help the guns sing.",
              fireText: "I will take my thunder elsewhere.",
              statChanges: [{ stat: "AttackRange", modifier: "Additive", value: 3 }],
            },
            {
              id: "elder-bertram",
              name: "Elder Bertram",
              role: "Retired Shipwright",
              portrait: "character17.png",
              hireable: true,
              talkPhrases: ["Strong planks save lives."],
              hireText: "Keep your word and I will keep your hull alive.",
              fireText: "Understood.",
              statChanges: [{ stat: "ShipHullStrength", modifier: "Additive", value: 12 }],
            },
          ],
        },
      },
    });

    fireEvent.click(screen.getByRole("button", { name: "Hire" }));
    fireEvent.click(await screen.findByRole("button", { name: "Hire" }));

    const actions = getIpcMessages(ipcSpy) as { action: string }[];
    expect(actions.find((a) => a.action === "hire_character")).toBeUndefined();
  });

  it("shows tavern action buttons directly on the roster", () => {
    renderApp({ tab: "market" });

    expect(screen.getAllByText("Talk").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Hire").length).toBeGreaterThan(0);
  });

  it("lets you talk to a non-hireable tavern character", async () => {
    renderApp({ tab: "market" });

    const talkButtons = screen.getAllByRole("button", { name: "Talk" });
    fireEvent.click(talkButtons[talkButtons.length - 1] as HTMLElement);

    expect(await screen.findByText(/rumors beat blind sailing|buy a rumor/i)).toBeInTheDocument();
  });
});
