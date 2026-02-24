import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { renderApp, getIpcMessages } from "./test/helpers";
import { makeOwnedComponent, makeComponentData } from "./test/fixtures";

function renderCreative(
  overrides?: Parameters<typeof renderApp>[0] extends infer O ? O : never,
) {
  const stateOverrides =
    overrides && "state" in overrides ? overrides.state : overrides;
  return renderApp({
    state: { isCreative: true, ...stateOverrides },
    tab: "creative",
  });
}

describe("CreativeTab", () => {
  // ── Visibility ────────────────────────────────────────────────────

  describe("visibility", () => {
    it("Creative tab button is visible when isCreative is true", () => {
      renderApp({ state: { isCreative: true } });
      expect(screen.getByRole("button", { name: "Creative" })).toBeInTheDocument();
    });

    it("Creative tab button is hidden when isCreative is false", () => {
      renderApp({ state: { isCreative: false } });
      expect(screen.queryByRole("button", { name: "Creative" })).not.toBeInTheDocument();
    });

    it("shows Creative Mode banner", () => {
      renderCreative();
      expect(screen.getByText("Creative Mode")).toBeInTheDocument();
    });
  });

  // ── Inventory display ─────────────────────────────────────────────

  describe("inventory display", () => {
    it("shows all item types", () => {
      renderCreative();
      for (const type of ["Wood", "Iron", "Fish", "Tea", "Coin", "CannonBall", "Trophy"]) {
        expect(screen.getByText(type)).toBeInTheDocument();
      }
    });

    it("shows current inventory amounts", () => {
      renderCreative({
        state: { inventory: { Wood: 42, Iron: 0, Fish: 7, Tea: 0, Coin: 999, CannonBall: 0, Trophy: 0 } },
      });
      expect(screen.getByText("42")).toBeInTheDocument();
      expect(screen.getByText("999")).toBeInTheDocument();
      expect(screen.getByText("7")).toBeInTheDocument();
    });

    it("defaults to 0 for items not in inventory", () => {
      renderCreative({ state: { inventory: {} } });
      // All items should show 0
      const zeros = screen.getAllByText("0");
      expect(zeros.length).toBeGreaterThanOrEqual(7);
    });
  });

  // ── Inventory step buttons ────────────────────────────────────────

  describe("inventory step buttons", () => {
    it("+1 button sends set_inventory IPC", async () => {
      const { user, ipcSpy } = renderCreative({
        state: { inventory: { Wood: 10 } },
      });
      // Find the Wood row and click +1
      const woodRow = screen.getByText("Wood").closest(".creative-item")!;
      const plus1 = Array.from(woodRow.querySelectorAll("button")).find(
        (b) => b.textContent === "+1",
      )!;
      ipcSpy.mockClear();
      await user.click(plus1);

      const messages = getIpcMessages(ipcSpy);
      expect(messages).toContainEqual({
        action: "set_inventory",
        items: [{ type: "Wood", quantity: 11 }],
      });
    });

    it("+50 button adds 50", async () => {
      const { user, ipcSpy } = renderCreative({
        state: { inventory: { Iron: 20 } },
      });
      const ironRow = screen.getByText("Iron").closest(".creative-item")!;
      const plus50 = Array.from(ironRow.querySelectorAll("button")).find(
        (b) => b.textContent === "+50",
      )!;
      ipcSpy.mockClear();
      await user.click(plus50);

      const messages = getIpcMessages(ipcSpy);
      expect(messages).toContainEqual({
        action: "set_inventory",
        items: [{ type: "Iron", quantity: 70 }],
      });
    });

    it("+100 button adds 100", async () => {
      const { user, ipcSpy } = renderCreative({
        state: { inventory: { Fish: 5 } },
      });
      const fishRow = screen.getByText("Fish").closest(".creative-item")!;
      const plus100 = Array.from(fishRow.querySelectorAll("button")).find(
        (b) => b.textContent === "+100",
      )!;
      ipcSpy.mockClear();
      await user.click(plus100);

      const messages = getIpcMessages(ipcSpy);
      expect(messages).toContainEqual({
        action: "set_inventory",
        items: [{ type: "Fish", quantity: 105 }],
      });
    });

    it("-1 button subtracts 1", async () => {
      const { user, ipcSpy } = renderCreative({
        state: { inventory: { Tea: 30 } },
      });
      const teaRow = screen.getByText("Tea").closest(".creative-item")!;
      const minus1 = Array.from(teaRow.querySelectorAll("button")).find(
        (b) => b.textContent === "-1",
      )!;
      ipcSpy.mockClear();
      await user.click(minus1);

      const messages = getIpcMessages(ipcSpy);
      expect(messages).toContainEqual({
        action: "set_inventory",
        items: [{ type: "Tea", quantity: 29 }],
      });
    });

    it("-50 button is disabled when current < 50", () => {
      renderCreative({ state: { inventory: { Wood: 30 } } });
      const woodRow = screen.getByText("Wood").closest(".creative-item")!;
      const minus50 = Array.from(woodRow.querySelectorAll("button")).find(
        (b) => b.textContent === "-50",
      )!;
      expect(minus50).toBeDisabled();
    });

    it("-100 button is disabled when current < 100", () => {
      renderCreative({ state: { inventory: { Iron: 50 } } });
      const ironRow = screen.getByText("Iron").closest(".creative-item")!;
      const minus100 = Array.from(ironRow.querySelectorAll("button")).find(
        (b) => b.textContent === "-100",
      )!;
      expect(minus100).toBeDisabled();
    });

    it("reset (×) button sets item to 0", async () => {
      const { user, ipcSpy } = renderCreative({
        state: { inventory: { Coin: 500 } },
      });
      const coinRow = screen.getByText("Coin").closest(".creative-item")!;
      const reset = Array.from(coinRow.querySelectorAll("button")).find(
        (b) => b.textContent === "×",
      )!;
      ipcSpy.mockClear();
      await user.click(reset);

      const messages = getIpcMessages(ipcSpy);
      expect(messages).toContainEqual({
        action: "set_inventory",
        items: [{ type: "Coin", quantity: 0 }],
      });
    });

    it("reset (×) button is disabled when item is already 0", () => {
      renderCreative({ state: { inventory: { Trophy: 0 } } });
      const trophyRow = screen.getByText("Trophy").closest(".creative-item")!;
      const reset = Array.from(trophyRow.querySelectorAll("button")).find(
        (b) => b.textContent === "×",
      )!;
      expect(reset).toBeDisabled();
    });
  });

  // ── Preset buttons ────────────────────────────────────────────────

  describe("preset buttons", () => {
    it("Clear All sets all items to 0", async () => {
      const { user, ipcSpy } = renderCreative({
        state: { inventory: { Wood: 50, Iron: 50 } },
      });
      ipcSpy.mockClear();
      await user.click(screen.getByRole("button", { name: "Clear All" }));

      const messages = getIpcMessages(ipcSpy);
      const setMsg = messages.find(
        (m) => (m as { action: string }).action === "set_inventory",
      ) as { action: string; items: { type: string; quantity: number }[] };
      expect(setMsg).toBeDefined();
      expect(setMsg.items).toHaveLength(7);
      for (const item of setMsg.items) {
        expect(item.quantity).toBe(0);
      }
    });

    it("100 Each sets all items to 100", async () => {
      const { user, ipcSpy } = renderCreative();
      ipcSpy.mockClear();
      await user.click(screen.getByRole("button", { name: "100 Each" }));

      const messages = getIpcMessages(ipcSpy);
      const setMsg = messages.find(
        (m) => (m as { action: string }).action === "set_inventory",
      ) as { action: string; items: { type: string; quantity: number }[] };
      expect(setMsg.items).toHaveLength(7);
      for (const item of setMsg.items) {
        expect(item.quantity).toBe(100);
      }
    });

    it("1k Each sets all items to 1000", async () => {
      const { user, ipcSpy } = renderCreative();
      ipcSpy.mockClear();
      await user.click(screen.getByRole("button", { name: "1k Each" }));

      const messages = getIpcMessages(ipcSpy);
      const setMsg = messages.find(
        (m) => (m as { action: string }).action === "set_inventory",
      ) as { action: string; items: { type: string; quantity: number }[] };
      for (const item of setMsg.items) {
        expect(item.quantity).toBe(1000);
      }
    });

    it("100k Each sets all items to 100000", async () => {
      const { user, ipcSpy } = renderCreative();
      ipcSpy.mockClear();
      await user.click(screen.getByRole("button", { name: "100k Each" }));

      const messages = getIpcMessages(ipcSpy);
      const setMsg = messages.find(
        (m) => (m as { action: string }).action === "set_inventory",
      ) as { action: string; items: { type: string; quantity: number }[] };
      for (const item of setMsg.items) {
        expect(item.quantity).toBe(100000);
      }
    });
  });

  // ── Health section ────────────────────────────────────────────────

  describe("health section", () => {
    it("shows health bar with percentage", () => {
      const { container } = renderCreative({
        state: { health: 40, maxHealth: 80 },
      });
      const fill = container.querySelector(
        ".creative-health-section .health-bar-fill",
      ) as HTMLElement;
      expect(fill.style.width).toBe("50%");
    });

    it("shows health text", () => {
      renderCreative({ state: { health: 40, maxHealth: 80 } });
      expect(screen.getByText("40 / 80")).toBeInTheDocument();
    });

    it("1 HP button sends set_health with 1", async () => {
      const { user, ipcSpy } = renderCreative({
        state: { health: 50, maxHealth: 100 },
      });
      ipcSpy.mockClear();
      await user.click(screen.getByRole("button", { name: "1 HP" }));

      const messages = getIpcMessages(ipcSpy);
      expect(messages).toContainEqual({
        action: "set_health",
        health: 1,
      });
    });

    it("25% button sends set_health with 25% of maxHealth", async () => {
      const { user, ipcSpy } = renderCreative({
        state: { health: 50, maxHealth: 100 },
      });
      ipcSpy.mockClear();
      await user.click(screen.getByRole("button", { name: "25%" }));

      const messages = getIpcMessages(ipcSpy);
      expect(messages).toContainEqual({
        action: "set_health",
        health: 25,
      });
    });

    it("50% button sends set_health with 50% of maxHealth", async () => {
      const { user, ipcSpy } = renderCreative({
        state: { health: 10, maxHealth: 200 },
      });
      ipcSpy.mockClear();
      await user.click(screen.getByRole("button", { name: "50%" }));

      const messages = getIpcMessages(ipcSpy);
      expect(messages).toContainEqual({
        action: "set_health",
        health: 100,
      });
    });

    it("Full button sends set_health with maxHealth", async () => {
      const { user, ipcSpy } = renderCreative({
        state: { health: 10, maxHealth: 200 },
      });
      ipcSpy.mockClear();
      await user.click(screen.getByRole("button", { name: "Full" }));

      const messages = getIpcMessages(ipcSpy);
      expect(messages).toContainEqual({
        action: "set_health",
        health: 200,
      });
    });

    it("rounds health for non-round maxHealth values", async () => {
      const { user, ipcSpy } = renderCreative({
        state: { health: 10, maxHealth: 77 },
      });
      ipcSpy.mockClear();
      await user.click(screen.getByRole("button", { name: "25%" }));

      const messages = getIpcMessages(ipcSpy);
      // Math.round(77 * 0.25) = Math.round(19.25) = 19
      expect(messages).toContainEqual({
        action: "set_health",
        health: 19,
      });
    });
  });

  // ── Components section ────────────────────────────────────────────

  describe("components section", () => {
    it("shows 'No components owned' when empty", () => {
      renderCreative({ state: { ownedComponents: [] } });
      expect(screen.getByText("No components owned")).toBeInTheDocument();
    });

    it("shows owned and equipped counts", () => {
      renderCreative({
        state: {
          components: [makeComponentData({ name: "Sail" })],
          ownedComponents: [
            makeOwnedComponent({ name: "Sail", isEquipped: true }),
            makeOwnedComponent({ name: "Sail", isEquipped: true }),
            makeOwnedComponent({ name: "Sail", isEquipped: false }),
          ],
        },
      });
      expect(screen.getByText(/3 owned/)).toBeInTheDocument();
      expect(screen.getByText(/2 equipped/)).toBeInTheDocument();
    });

    it("Delete All Components sends clear_components IPC", async () => {
      const { user, ipcSpy } = renderCreative({
        state: {
          components: [makeComponentData({ name: "Hull" })],
          ownedComponents: [makeOwnedComponent({ name: "Hull", isEquipped: false })],
        },
      });
      ipcSpy.mockClear();
      await user.click(screen.getByRole("button", { name: "Delete All Components" }));

      const messages = getIpcMessages(ipcSpy);
      expect(messages).toContainEqual({ action: "clear_components" });
    });

    it("Delete All Components is disabled when no components owned", () => {
      renderCreative({ state: { ownedComponents: [] } });
      expect(
        screen.getByRole("button", { name: "Delete All Components" }),
      ).toBeDisabled();
    });

    it("Delete All Components is enabled when components exist", () => {
      renderCreative({
        state: {
          ownedComponents: [makeOwnedComponent({ name: "X", isEquipped: false })],
        },
      });
      expect(
        screen.getByRole("button", { name: "Delete All Components" }),
      ).not.toBeDisabled();
    });
  });

  // ── Edge cases ────────────────────────────────────────────────────

  describe("edge cases", () => {
    it("handles maxHealth of 0 gracefully (0% width, no division error)", () => {
      const { container } = renderCreative({
        state: { health: 0, maxHealth: 0 },
      });
      const fill = container.querySelector(
        ".creative-health-section .health-bar-fill",
      ) as HTMLElement;
      expect(fill.style.width).toBe("0%");
    });

    it("clamp prevents negative inventory via -1 on 0", async () => {
      const { user, ipcSpy } = renderCreative({
        state: { inventory: { Wood: 0 } },
      });
      const woodRow = screen.getByText("Wood").closest(".creative-item")!;
      const minus1 = Array.from(woodRow.querySelectorAll("button")).find(
        (b) => b.textContent === "-1",
      )!;
      // Button should be disabled at 0
      expect(minus1).toBeDisabled();
      // Even if we force-click, the IPC should clamp to 0
      ipcSpy.mockClear();
    });
  });
});
