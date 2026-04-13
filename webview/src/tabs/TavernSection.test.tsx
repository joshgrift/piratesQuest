import { describe, it, expect } from "vitest";
import { fireEvent, screen } from "@testing-library/react";
import { renderApp } from "../test/helpers";
import { getIpcMessages } from "../test/helpers";

describe("TavernSection", () => {
  it("sends accept quest IPC when starting a hire quest", async () => {
    const { ipcSpy } = renderApp({ tab: "market" });

    const hireButtons = screen.getAllByRole("button", { name: "Hire" });
    fireEvent.click(hireButtons[0] as HTMLElement);
    const popupHireButtons = await screen.findAllByRole("button", { name: "Accept Quest" });
    fireEvent.click(popupHireButtons[popupHireButtons.length - 1] as HTMLElement);

    const actions = getIpcMessages(ipcSpy) as { action: string; characterId?: string; questId?: string }[];
    expect(actions).toContainEqual({
      action: "accept_quest",
      questId: "hire_gideon_gearlock",
      characterId: "gideon-gearlock",
    });
  });

  it("blocks hire quest acceptance when crew slots are already full", async () => {
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
        quests: {
          available: [
            {
              id: "hire_elder_bertram",
              title: "Earn Elder Bertram's Trust",
              giverNpcId: "elder-bertram",
              giverName: "Elder Bertram",
              giverPortrait: "character17.png",
              giverPortName: "Tortuga",
              revealGiverInQuestLog: true,
              canAcceptFromQuestLog: false,
              canCancel: true,
              offerText: "Fit out your ship properly, then talk to me.",
              acceptedText: "Equip two components, then return.",
              description: "Bertram wants proof you prepare your ship.",
              completionText: "That will do.",
              rewardCrewNpcId: "elder-bertram",
              unlocks: [],
              steps: [],
            },
          ],
          active: null,
          all: [],
          completedIds: [],
          recentlyCompletedIds: [],
          unlockedFeatures: [
            "SellGoods",
            "TavernTalk",
            "BuyGoods",
          ],
        },
      },
    });

    fireEvent.click(screen.getByRole("button", { name: "Hire" }));
    expect(await screen.findByText(/berths are full/i)).toBeInTheDocument();

    const actions = getIpcMessages(ipcSpy) as { action: string }[];
    expect(actions.find((a) => a.action === "accept_quest")).toBeUndefined();
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

  it("shows complete quest when talking to an npc would finish the next quest step", () => {
    renderApp({
      tab: "market",
      state: {
        quests: {
          available: [],
          active: {
            id: "hire_gideon_gearlock",
            title: "Earn Gideon Gearlock's Trust",
            giverNpcId: "gideon-gearlock",
            giverName: "Gideon Gearlock",
            giverPortrait: "character8.png",
            giverPortName: "Tortuga",
            revealGiverInQuestLog: true,
            canAcceptFromQuestLog: false,
            canCancel: true,
            offerText: "Earn 60 gold, then return and talk to me.",
            acceptedText: "Earn 60 gold from trading, then come back and speak with me.",
            description: "Gideon will join your crew once you prove you can trade for profit.",
            completionText: "Those numbers look respectable. I am aboard.",
            rewardCrewNpcId: "gideon-gearlock",
            unlocks: [],
            steps: [
              {
                label: "Close a sale worth 60 gold",
                currentValue: 60,
                requiredValue: 60,
                isComplete: true,
              },
              {
                label: "Talk to Gideon Gearlock",
                currentValue: 0,
                requiredValue: 1,
                isComplete: false,
              },
            ],
          },
          all: [],
          completedIds: [],
          recentlyCompletedIds: [],
          unlockedFeatures: [
            "SellGoods",
            "TavernTalk",
            "BuyGoods",
          ],
        },
      },
    });

    expect(screen.getByRole("button", { name: "Complete Quest" })).toBeInTheDocument();
  });
});
