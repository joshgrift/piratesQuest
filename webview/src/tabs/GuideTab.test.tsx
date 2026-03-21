import { describe, it, expect } from "vitest";
import { fireEvent, screen } from "@testing-library/react";
import { getIpcMessages, renderApp } from "../test/helpers";

function skipTypingIfNeeded() {
  const skipButton = screen.queryByRole("button", { name: "Skip Typing" });
  if (skipButton) fireEvent.click(skipButton);
}

describe("Scarlett crew guide", () => {
  it("shows Scarlett in the crew roster even without hired crew", () => {
    renderApp({ tab: "ship_crew" });

    expect(screen.getByRole("button", { name: /Scarlett/ })).toBeInTheDocument();
  });

  it("opens Scarlett in the shared conversation overlay", () => {
    renderApp({ tab: "ship_crew" });

    fireEvent.click(screen.getByRole("button", { name: /Scarlett/ }));

    expect(screen.getByRole("dialog", { name: "Scarlett conversation" })).toBeInTheDocument();
    expect(screen.getByText("I have some questions about ports.")).toBeInTheDocument();
    expect(screen.getByText("Teach me about sailing and combat.")).toBeInTheDocument();
    expect(screen.getByText("How do I grow stronger out here?")).toBeInTheDocument();
  });

  it("keeps port help conversational by nesting shipyard info under port questions", () => {
    renderApp({ tab: "ship_crew" });

    fireEvent.click(screen.getByRole("button", { name: /Scarlett/ }));
    fireEvent.click(screen.getByRole("button", { name: "I have some questions about ports." }));
    skipTypingIfNeeded();

    expect(screen.getByText("Explain the shipyard, components, and stats.")).toBeInTheDocument();
    expect(screen.queryByText("Teach me about sailing and combat.")).not.toBeInTheDocument();
  });

  it("accepts Scarlett's starter quest from the shared overlay", () => {
    const { ipcSpy } = renderApp({
      tab: "ship_crew",
      state: {
        quests: {
          available: [
            {
              id: "scarlett_sail_to_port",
              title: "Dock and Learn",
              giverNpcId: "scarlett",
              giverName: "Scarlett",
              giverPortName: "Tortuga",
              revealGiverInQuestLog: true,
              canAcceptFromQuestLog: true,
              description: "Accept Scarlett's first lesson and make port.",
              completionText: "Well sailed.",
              unlocks: ["SellGoods", "TavernTalk"],
              steps: [],
            },
          ],
          active: null,
          all: [],
          completedIds: [],
          unlockedFeatures: [],
        },
      },
    });

    fireEvent.click(screen.getByRole("button", { name: /Scarlett/ }));
    fireEvent.click(screen.getByRole("button", { name: "How do I grow stronger out here?" }));
    skipTypingIfNeeded();
    fireEvent.click(screen.getByRole("button", { name: "Tell me about quests." }));
    skipTypingIfNeeded();
    fireEvent.click(screen.getByRole("button", { name: "Give me that starter job." }));

    const actions = getIpcMessages(ipcSpy) as { action: string; questId?: string; characterId?: string }[];
    expect(actions).toContainEqual({
      action: "accept_quest",
      questId: "scarlett_sail_to_port",
      characterId: "scarlett",
    });
  });
});
