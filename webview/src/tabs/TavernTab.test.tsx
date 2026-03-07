import { describe, it, expect } from "vitest";
import { fireEvent, screen } from "@testing-library/react";
import { renderApp } from "../test/helpers";
import { getIpcMessages } from "../test/helpers";

describe("TavernTab", () => {
  it("sends hire IPC when hiring a character", () => {
    const { ipcSpy } = renderApp({ tab: "tavern" });

    fireEvent.click(screen.getByRole("button", { name: /Join my crew\./ }));

    const actions = getIpcMessages(ipcSpy) as { action: string; characterId?: string }[];
    expect(actions).toContainEqual({
      action: "hire_character",
      characterId: "scarred-gunner",
    });
  });

  it("blocks hire IPC when crew slots are already full", () => {
    const { ipcSpy } = renderApp({
      tab: "tavern",
      state: {
        tavern: {
          crewSlots: 2,
          hiredCharacterIds: ["scarred-gunner", "quartermaster-mira"],
          characters: [
            {
              id: "scarred-gunner",
              name: "Briggs",
              role: "Deck Gunner",
              portrait: "character6.png",
              hireable: true,
              statChanges: [{ stat: "AttackDamage", modifier: "Additive", value: 6 }],
            },
            {
              id: "quartermaster-mira",
              name: "Mira",
              role: "Quartermaster",
              portrait: "character13.png",
              hireable: true,
              statChanges: [{ stat: "ShipCapacity", modifier: "Additive", value: 120 }],
            },
            {
              id: "lazy-lookout",
              name: "Old Ned",
              role: "Lookout",
              portrait: "character24.png",
              hireable: true,
              statChanges: [],
            },
          ],
        },
      },
    });

    expect(screen.getByText("2/2 Hired")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: /Old Ned/ }));
    fireEvent.click(screen.getByRole("button", { name: /Join my crew\./ }));

    const actions = getIpcMessages(ipcSpy) as { action: string }[];
    expect(actions.find((a) => a.action === "hire_character")).toBeUndefined();
  });

  it("sends fire IPC for hired characters", () => {
    const { ipcSpy } = renderApp({
      tab: "tavern",
      state: {
        tavern: {
          crewSlots: 2,
          hiredCharacterIds: ["scarred-gunner"],
          characters: [
            {
              id: "scarred-gunner",
              name: "Briggs",
              role: "Deck Gunner",
              portrait: "character6.png",
              hireable: true,
              statChanges: [{ stat: "AttackDamage", modifier: "Additive", value: 6 }],
            },
          ],
        },
      },
    });

    fireEvent.click(screen.getByRole("button", { name: /You're fired\./ }));

    const actions = getIpcMessages(ipcSpy) as { action: string; characterId?: string }[];
    expect(actions).toContainEqual({
      action: "fire_character",
      characterId: "scarred-gunner",
    });
  });

  it("shows talk-only badge for non-hireable characters", () => {
    renderApp({ tab: "tavern" });

    expect(screen.getByRole("button", { name: /Pip/ })).toHaveTextContent("Talk");
  });
});
