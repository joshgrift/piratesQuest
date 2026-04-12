import { describe, expect, it } from "vitest";
import { screen } from "@testing-library/react";
import { renderApp } from "../test/helpers";

describe("QuestsTab", () => {
  it("shows only the active quest and completed quests in the quest log", () => {
    renderApp({
      tab: "quests",
      state: {
        quests: {
          available: [
            {
              id: "available-quest",
              title: "Still Locked Away",
              giverNpcId: "gideon",
              giverName: "Gideon",
              giverPortrait: "character8.png",
              giverPortName: "Saint Johns",
              revealGiverInQuestLog: true,
              canAcceptFromQuestLog: true,
              description: "This should stay hidden from the quests panel.",
              completionText: "",
              unlocks: [],
              steps: [],
            },
          ],
          active: {
            id: "active-quest",
            title: "Current Orders",
            giverNpcId: "scarlett",
            giverName: "Scarlett",
            giverPortrait: "character1.png",
            giverPortName: "Tortuga",
            revealGiverInQuestLog: true,
            canAcceptFromQuestLog: true,
            description: "Do the thing you're working on right now.",
            completionText: "",
            unlocks: [],
            steps: [{ label: "Finish the job", currentValue: 0, requiredValue: 1, isComplete: false }],
          },
          all: [
            {
              id: "active-quest",
              title: "Current Orders",
              giverNpcId: "scarlett",
              giverName: "Scarlett",
              giverPortrait: "character1.png",
              giverPortName: "Tortuga",
              revealGiverInQuestLog: true,
              canAcceptFromQuestLog: true,
              description: "Do the thing you're working on right now.",
              completionText: "",
              unlocks: [],
              steps: [{ label: "Finish the job", currentValue: 0, requiredValue: 1, isComplete: false }],
            },
            {
              id: "completed-quest",
              title: "Done and Dusted",
              giverNpcId: "caspian",
              giverName: "Governor Caspian Vale",
              giverPortrait: "character3.png",
              giverPortName: "Haven",
              revealGiverInQuestLog: true,
              canAcceptFromQuestLog: false,
              description: "A completed quest should stay collapsed by default.",
              completionText: "",
              unlocks: [],
              steps: [{ label: "Turn it in", currentValue: 1, requiredValue: 1, isComplete: true }],
            },
            {
              id: "available-quest",
              title: "Still Locked Away",
              giverNpcId: "gideon",
              giverName: "Gideon",
              giverPortrait: "character8.png",
              giverPortName: "Saint Johns",
              revealGiverInQuestLog: true,
              canAcceptFromQuestLog: true,
              description: "This should stay hidden from the quests panel.",
              completionText: "",
              unlocks: [],
              steps: [],
            },
          ],
          completedIds: ["completed-quest"],
          recentlyCompletedIds: [],
          unlockedFeatures: [],
        },
      },
    });

    expect(screen.getByText("Current Orders")).toBeInTheDocument();
    expect(screen.getByText("Done and Dusted")).toBeInTheDocument();
    expect(screen.queryByText("Still Locked Away")).not.toBeInTheDocument();
    expect(screen.queryByText("Available Quests")).not.toBeInTheDocument();
  });

  it("keeps completed quests collapsed until you open them", async () => {
    const { user } = renderApp({
      tab: "quests",
      state: {
        quests: {
          available: [],
          active: null,
          all: [
            {
              id: "completed-quest",
              title: "Done and Dusted",
              giverNpcId: "caspian",
              giverName: "Governor Caspian Vale",
              giverPortrait: "character3.png",
              giverPortName: "Haven",
              revealGiverInQuestLog: true,
              canAcceptFromQuestLog: false,
              description: "A completed quest should stay collapsed by default.",
              completionText: "",
              unlocks: [],
              steps: [{ label: "Turn it in", currentValue: 1, requiredValue: 1, isComplete: true }],
            },
          ],
          completedIds: ["completed-quest"],
          recentlyCompletedIds: [],
          unlockedFeatures: [],
        },
      },
    });

    expect(screen.getByText("Done and Dusted")).toBeInTheDocument();
    expect(screen.queryByText("A completed quest should stay collapsed by default.")).not.toBeInTheDocument();
    expect(screen.queryByText("Turn it in")).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /Done and Dusted/i }));

    expect(screen.getByText("A completed quest should stay collapsed by default.")).toBeInTheDocument();
    expect(screen.getByText("Turn it in")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Hide details/i })).toBeInTheDocument();
  });
});
