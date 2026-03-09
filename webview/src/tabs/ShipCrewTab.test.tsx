import { describe, it, expect } from "vitest";
import { fireEvent, screen } from "@testing-library/react";
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
              statChanges: [{ stat: "AttackDamage", modifier: "Additive", value: 5 }],
            },
          ],
        },
        tavern: {
          characters: [],
        },
      },
    });

    fireEvent.click(await screen.findByRole("button", { name: /Stand down at next port\./ }));

    const actions = getIpcMessages(ipcSpy) as { action: string; characterId?: string }[];
    expect(actions).toContainEqual({
      action: "fire_character",
      characterId: "dorian-blackwake",
    });
  });
});
