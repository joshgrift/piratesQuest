import { describe, it, expect } from "vitest";
import { fireEvent, screen, within } from "@testing-library/react";
import { getIpcMessages, renderApp } from "../test/helpers";

describe("ShipCrewTab", () => {
  it("sends fire IPC from Ship > Crew tab", async () => {
    const { ipcSpy } = renderApp({
      tab: "ship_crew",
      state: {
        crew: {
          crewSlots: 2,
          hiredCharacterIds: ["dorian-blackwake"],
          characters: [
            {
              id: "dorian-blackwake",
              name: "Dorian Blackwake",
              role: "Broken Cannoneer",
              portrait: "character31.png",
              hireable: true,
              talkPhrases: ["Decide, then commit."],
              hireText: "Good. Give me powder, space, and silence.",
              fireText: "Fine. The sea still remembers who taught your gunners.",
              statChanges: [{ stat: "AttackDamage", modifier: "Additive", value: 5 }],
            },
          ],
        },
        tavern: {
          characters: [],
        },
      },
    });

    const card = screen.getByText("Dorian Blackwake").closest(".npc-card") as HTMLElement | null;
    expect(card).not.toBeNull();
    fireEvent.click(within(card as HTMLElement).getByRole("button", { name: "Fire" }));
    fireEvent.click(await screen.findByRole("button", { name: "Fire" }));

    const actions = getIpcMessages(ipcSpy) as { action: string; characterId?: string }[];
    expect(actions).toContainEqual({
      action: "fire_character",
      characterId: "dorian-blackwake",
    });
  });
});
