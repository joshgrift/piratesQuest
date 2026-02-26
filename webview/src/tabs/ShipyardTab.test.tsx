import { describe, it, expect } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import { renderApp, getIpcMessages } from "../test/helpers";
import {
  makeShopItem,
  makeComponentData,
  makeOwnedComponent,
  makeStatChange,
} from "../test/fixtures";

function renderShipyard(opts?: {
  state?: Partial<import("../types").PortState>;
  simulateGodot?: boolean;
}) {
  return renderApp({ state: opts?.state, tab: "shipyard", simulateGodot: opts?.simulateGodot });
}

describe("ShipyardTab", () => {
  // ── Health bar ────────────────────────────────────────────────────

  describe("health bar", () => {
    it("shows current and max health", () => {
      renderShipyard({ state: { health: 60, maxHealth: 100 } });
      expect(screen.getByText("60 / 100")).toBeInTheDocument();
    });

    it("renders the health bar at the correct percentage width", () => {
      const { container } = renderShipyard({ state: { health: 75, maxHealth: 100 } });
      const fill = container.querySelector(".health-bar-fill") as HTMLElement;
      expect(fill.style.width).toBe("75%");
    });

    it("shows 'Hull at Full Strength' when health equals maxHealth", () => {
      renderShipyard({ state: { health: 100, maxHealth: 100 } });
      expect(screen.getByText("Hull at Full Strength")).toBeInTheDocument();
    });

    it("does not show repair buttons when at full health", () => {
      renderShipyard({ state: { health: 100, maxHealth: 100 } });
      expect(screen.queryByRole("button", { name: "Repair Hull" })).not.toBeInTheDocument();
    });

    it("uses a red-ish colour at low health", () => {
      const { container } = renderShipyard({ state: { health: 1, maxHealth: 100 } });
      const fill = container.querySelector(".health-bar-fill") as HTMLElement;
      // jsdom converts hsl(1, 75%, 42%) to rgb. At ~1% health the hue is
      // close to 0 (red), so the red channel should dominate.
      const bg = fill.style.backgroundColor;
      expect(bg).toBeTruthy();
      const match = bg.match(/rgb\((\d+), (\d+), (\d+)\)/);
      expect(match).not.toBeNull();
      const [, r, g] = match!.map(Number);
      expect(r!).toBeGreaterThan(g!);
    });
  });

  // ── Repair ────────────────────────────────────────────────────────

  describe("repair", () => {
    it("shows repair button when damaged", () => {
      renderShipyard({
        state: {
          health: 80,
          maxHealth: 100,
          inventory: { Wood: 100, Fish: 20, Coin: 50 },
        },
      });
      expect(screen.getByRole("button", { name: "Repair Hull" })).toBeInTheDocument();
    });

    it("shows required repair materials (wood * 5 and fish per HP needed)", () => {
      const { container } = renderShipyard({
        state: {
          health: 90,
          maxHealth: 100,
          inventory: { Wood: 100, Fish: 20, Coin: 0 },
        },
      });
      // 10 HP needed → 50 Wood, 10 Fish
      const repairCosts = container.querySelector(".repair-costs")!;
      const woodChip = repairCosts.querySelector(".cost-chip:nth-child(1)")!;
      const fishChip = repairCosts.querySelector(".cost-chip:nth-child(2)")!;
      expect(woodChip.textContent).toContain("50");
      expect(fishChip.textContent).toContain("10");
    });

    it("repair button sends heal IPC", async () => {
      const { user, ipcSpy } = renderShipyard({
        state: {
          health: 90,
          maxHealth: 100,
          inventory: { Wood: 100, Fish: 20, Coin: 50 },
        },
      });
      ipcSpy.mockClear();
      await user.click(screen.getByRole("button", { name: "Repair Hull" }));

      const messages = getIpcMessages(ipcSpy);
      expect(messages).toContainEqual({ action: "heal" });
    });

    it("repair button is disabled when insufficient resources", () => {
      renderShipyard({
        state: {
          health: 80,
          maxHealth: 100,
          inventory: { Wood: 0, Fish: 0, Coin: 50 },
        },
      });
      expect(screen.getByRole("button", { name: "Repair Hull" })).toBeDisabled();
    });

    it("shows how many HP can be healed", () => {
      renderShipyard({
        state: {
          health: 80,
          maxHealth: 100,
          // Can heal min(20 needed, floor(25/5)=5 from wood, 3 from fish) = 3
          inventory: { Wood: 25, Fish: 3, Coin: 50 },
        },
      });
      expect(screen.getByText("+3 HP")).toBeInTheDocument();
    });
  });

  // ── Buy & Repair ──────────────────────────────────────────────────

  describe("buy and repair", () => {
    it("shows Buy & Repair button when shop has Wood and Fish", () => {
      renderShipyard({
        state: {
          health: 90,
          maxHealth: 100,
          itemsForSale: [
            makeShopItem({ type: "Wood", buyPrice: 2 }),
            makeShopItem({ type: "Fish", buyPrice: 3 }),
          ],
          inventory: { Coin: 1000, Wood: 0, Fish: 0 },
        },
      });
      expect(screen.getByRole("button", { name: "Buy & Repair" })).toBeInTheDocument();
    });

    it("Buy & Repair sends buy_items, waits for state update, then sends heal", async () => {
      const { user, ipcSpy } = renderShipyard({
        state: {
          health: 99,
          maxHealth: 100,
          itemsForSale: [
            makeShopItem({ type: "Wood", buyPrice: 2 }),
            makeShopItem({ type: "Fish", buyPrice: 3 }),
          ],
          inventory: { Coin: 1000, Wood: 0, Fish: 0 },
        },
        simulateGodot: true,
      });
      ipcSpy.mockClear();
      await user.click(screen.getByRole("button", { name: "Buy & Repair" }));

      // The heal is sent after awaiting the updateState callback from Godot,
      // so we need to wait for the async chain to complete.
      await waitFor(() => {
        const messages = getIpcMessages(ipcSpy);
        expect(messages).toContainEqual({
          action: "buy_items",
          items: expect.arrayContaining([
            { type: "Wood", quantity: 5 },
            { type: "Fish", quantity: 1 },
          ]),
        });
        expect(messages).toContainEqual({ action: "heal" });
      });
    });

    it("Buy & Repair is disabled when not affordable", () => {
      renderShipyard({
        state: {
          health: 90,
          maxHealth: 100,
          itemsForSale: [
            makeShopItem({ type: "Wood", buyPrice: 100 }),
            makeShopItem({ type: "Fish", buyPrice: 100 }),
          ],
          inventory: { Coin: 0, Wood: 0, Fish: 0 },
        },
      });
      expect(screen.getByRole("button", { name: "Buy & Repair" })).toBeDisabled();
    });

    it("buys only wood when fish is already plentiful, then heals", async () => {
      // Regression: 50 fish, 0 wood, 20 gold — previously sent two separate
      // IPC messages synchronously, so the heal raced the buy.
      const { user, ipcSpy } = renderShipyard({
        state: {
          health: 95,
          maxHealth: 100,
          itemsForSale: [
            makeShopItem({ type: "Wood", buyPrice: 2 }),
            makeShopItem({ type: "Fish", buyPrice: 3 }),
          ],
          inventory: { Coin: 20, Wood: 0, Fish: 50 },
        },
        simulateGodot: true,
      });
      ipcSpy.mockClear();
      await user.click(screen.getByRole("button", { name: "Buy & Repair" }));

      await waitFor(() => {
        const messages = getIpcMessages(ipcSpy);
        // Should only buy wood (fish already plentiful)
        expect(messages).toContainEqual({
          action: "buy_items",
          items: [{ type: "Wood", quantity: 10 }],
        });
        // And then heal after the buy completes
        expect(messages).toContainEqual({ action: "heal" });
      });
    });

    it("does not show Buy & Repair when shop lacks Wood or Fish", () => {
      renderShipyard({
        state: {
          health: 90,
          maxHealth: 100,
          itemsForSale: [makeShopItem({ type: "Iron", buyPrice: 10 })],
          inventory: { Coin: 100, Wood: 0, Fish: 0 },
        },
      });
      expect(screen.queryByRole("button", { name: "Buy & Repair" })).not.toBeInTheDocument();
    });
  });

  // ── Ship stats ────────────────────────────────────────────────────

  describe("ship stats", () => {
    it("displays all stats with formatted names", () => {
      renderShipyard({
        state: { stats: { MaxSpeed: 10, TurnRate: 5, CargoCapacity: 100 } },
      });
      expect(screen.getByText("Max Speed")).toBeInTheDocument();
      expect(screen.getByText("Turn Rate")).toBeInTheDocument();
      expect(screen.getByText("Cargo Capacity")).toBeInTheDocument();
    });

    it("shows stat values", () => {
      renderShipyard({ state: { stats: { Speed: 12 } } });
      expect(screen.getByText("12")).toBeInTheDocument();
    });

    it("shows additive bonuses from equipped components", () => {
      renderShipyard({
        state: {
          stats: { Speed: 5 },
          components: [
            makeComponentData({
              name: "Sail",
              statChanges: [makeStatChange({ stat: "Speed", modifier: "Additive", value: 3 })],
            }),
          ],
          ownedComponents: [makeOwnedComponent({ name: "Sail", isEquipped: true })],
        },
      });
      expect(screen.getByText("+3")).toBeInTheDocument();
    });

    it("shows multiplicative bonuses as percentage", () => {
      renderShipyard({
        state: {
          stats: { Speed: 10 },
          components: [
            makeComponentData({
              name: "Boost",
              statChanges: [makeStatChange({ stat: "Speed", modifier: "Multiplicative", value: 1.5 })],
            }),
          ],
          ownedComponents: [makeOwnedComponent({ name: "Boost", isEquipped: true })],
        },
      });
      expect(screen.getByText("+50%")).toBeInTheDocument();
    });
  });

  // ── Component capacity ────────────────────────────────────────────

  describe("component capacity", () => {
    it("shows equipped count and total capacity", () => {
      renderShipyard({
        state: {
          componentCapacity: 4,
          ownedComponents: [
            makeOwnedComponent({ name: "A", isEquipped: true }),
            makeOwnedComponent({ name: "B", isEquipped: true }),
          ],
          components: [
            makeComponentData({ name: "A" }),
            makeComponentData({ name: "B" }),
          ],
        },
      });
      expect(screen.getByText("2/4")).toBeInTheDocument();
    });

    it("renders filled and empty slot indicators", () => {
      const { container } = renderShipyard({
        state: {
          componentCapacity: 3,
          ownedComponents: [makeOwnedComponent({ name: "A", isEquipped: true })],
          components: [makeComponentData({ name: "A" })],
        },
      });
      const filled = container.querySelectorAll(".slot.filled");
      const empty = container.querySelectorAll(".slot:not(.filled)");
      expect(filled).toHaveLength(1);
      expect(empty).toHaveLength(2);
    });

    it("shows 'No components equipped' when none are equipped", () => {
      renderShipyard({
        state: { ownedComponents: [], componentCapacity: 4 },
      });
      expect(screen.getByText("No components equipped")).toBeInTheDocument();
    });
  });

  // ── Equipped components ───────────────────────────────────────────

  describe("equipped components", () => {
    it("shows equipped component names", () => {
      const { container } = renderShipyard({
        state: {
          components: [makeComponentData({ name: "Iron Hull" })],
          ownedComponents: [makeOwnedComponent({ name: "Iron Hull", isEquipped: true })],
        },
      });
      // The component name appears in both the equipped card and the for-sale card.
      // Verify the equipped card (has the "equipped" class) shows the name.
      const equippedCard = container.querySelector(".component-card.equipped")!;
      expect(equippedCard).toBeInTheDocument();
      expect(equippedCard.querySelector(".component-name")!.textContent).toBe("Iron Hull");
    });

    it("shows unequip button for equipped components", () => {
      renderShipyard({
        state: {
          components: [makeComponentData({ name: "Iron Hull" })],
          ownedComponents: [makeOwnedComponent({ name: "Iron Hull", isEquipped: true })],
        },
      });
      expect(screen.getByRole("button", { name: "Unequip" })).toBeInTheDocument();
    });

    it("unequip sends IPC message", async () => {
      const { user, ipcSpy } = renderShipyard({
        state: {
          components: [makeComponentData({ name: "Iron Hull" })],
          ownedComponents: [makeOwnedComponent({ name: "Iron Hull", isEquipped: true })],
        },
      });
      ipcSpy.mockClear();
      await user.click(screen.getByRole("button", { name: "Unequip" }));

      const messages = getIpcMessages(ipcSpy);
      expect(messages).toContainEqual({
        action: "unequip_component",
        name: "Iron Hull",
      });
    });

    it("shows count badge when multiple of same component equipped", () => {
      const { container } = renderShipyard({
        state: {
          components: [makeComponentData({ name: "Sail" })],
          ownedComponents: [
            makeOwnedComponent({ name: "Sail", isEquipped: true }),
            makeOwnedComponent({ name: "Sail", isEquipped: true }),
            makeOwnedComponent({ name: "Sail", isEquipped: true }),
          ],
          componentCapacity: 5,
        },
      });
      const equippedCard = container.querySelector(".component-card.equipped")!;
      const countBadge = equippedCard.querySelector(".component-count")!;
      expect(countBadge.textContent).toBe("3");
    });

    it("shows stat contribution as +value for equipped components", () => {
      renderShipyard({
        state: {
          stats: { Speed: 10 },
          components: [
            makeComponentData({
              name: "Sail",
              statChanges: [makeStatChange({ stat: "Speed", modifier: "Additive", value: 4 })],
            }),
          ],
          ownedComponents: [makeOwnedComponent({ name: "Sail", isEquipped: true })],
        },
      });
      expect(screen.getByText(/Speed: \+4/)).toBeInTheDocument();
    });
  });

  // ── Owned (unequipped) components ─────────────────────────────────

  describe("owned unequipped components", () => {
    it("shows owned section when unequipped components exist", () => {
      renderShipyard({
        state: {
          components: [makeComponentData({ name: "Rudder" })],
          ownedComponents: [makeOwnedComponent({ name: "Rudder", isEquipped: false })],
        },
      });
      expect(screen.getByText("Owned Components")).toBeInTheDocument();
    });

    it("hides owned section when all components are equipped", () => {
      renderShipyard({
        state: {
          components: [makeComponentData({ name: "Rudder" })],
          ownedComponents: [makeOwnedComponent({ name: "Rudder", isEquipped: true })],
        },
      });
      expect(screen.queryByText("Owned Components")).not.toBeInTheDocument();
    });

    it("equip button sends IPC message", async () => {
      const { user, ipcSpy } = renderShipyard({
        state: {
          components: [makeComponentData({ name: "Rudder" })],
          ownedComponents: [makeOwnedComponent({ name: "Rudder", isEquipped: false })],
          componentCapacity: 4,
        },
      });
      ipcSpy.mockClear();
      await user.click(screen.getByRole("button", { name: "Equip" }));

      const messages = getIpcMessages(ipcSpy);
      expect(messages).toContainEqual({
        action: "equip_component",
        name: "Rudder",
      });
    });

    it("equip button disabled when at capacity", () => {
      renderShipyard({
        state: {
          components: [
            makeComponentData({ name: "A" }),
            makeComponentData({ name: "B" }),
          ],
          ownedComponents: [
            makeOwnedComponent({ name: "A", isEquipped: true }),
            makeOwnedComponent({ name: "B", isEquipped: false }),
          ],
          componentCapacity: 1,
        },
      });
      expect(screen.getByRole("button", { name: "Equip" })).toBeDisabled();
    });

    it("shows current → projected stat for equip cards", () => {
      const { container } = renderShipyard({
        state: {
          stats: { Speed: 10 },
          components: [
            makeComponentData({
              name: "Sail",
              statChanges: [makeStatChange({ stat: "Speed", modifier: "Additive", value: 3 })],
            }),
          ],
          ownedComponents: [makeOwnedComponent({ name: "Sail", isEquipped: false })],
          componentCapacity: 4,
        },
      });
      // The owned (unequipped) card is not .equipped, find the stat-change text
      const cards = container.querySelectorAll(".component-card:not(.equipped)");
      const statTexts = Array.from(cards).flatMap((card) =>
        Array.from(card.querySelectorAll(".stat-change")).map((el) => el.textContent),
      );
      expect(statTexts.some((t) => t?.includes("10") && t?.includes("13"))).toBe(true);
    });
  });

  // ── Components for sale ───────────────────────────────────────────

  describe("available components for sale", () => {
    it("shows available components section", () => {
      renderShipyard({
        state: {
          components: [makeComponentData({ name: "Cannon" })],
          inventory: { Iron: 100, Coin: 100 },
        },
      });
      expect(screen.getByText("Available Components")).toBeInTheDocument();
    });

    it("shows component cost when showCost is true", () => {
      renderShipyard({
        state: {
          components: [makeComponentData({ name: "Cannon", cost: { Iron: 10, Coin: 30 } })],
          inventory: { Iron: 100, Coin: 100 },
        },
      });
      expect(screen.getByText(/10 Iron/)).toBeInTheDocument();
      expect(screen.getByText(/30 Coin/)).toBeInTheDocument();
    });

    it("buy button sends purchase_component IPC", async () => {
      const { user, ipcSpy } = renderShipyard({
        state: {
          components: [makeComponentData({ name: "Cannon", cost: { Coin: 5 } })],
          inventory: { Coin: 100 },
        },
      });
      ipcSpy.mockClear();
      await user.click(screen.getByRole("button", { name: "Buy" }));

      const messages = getIpcMessages(ipcSpy);
      expect(messages).toContainEqual({
        action: "purchase_component",
        name: "Cannon",
      });
    });

    it("buy button disabled when can't afford", () => {
      renderShipyard({
        state: {
          components: [makeComponentData({ name: "Cannon", cost: { Coin: 999 } })],
          inventory: { Coin: 0 },
        },
      });
      expect(screen.getByRole("button", { name: "Buy" })).toBeDisabled();
    });

    it("marks unaffordable cost items with the unaffordable class", () => {
      const { container } = renderShipyard({
        state: {
          components: [makeComponentData({ name: "Hull", cost: { Iron: 50, Coin: 10 } })],
          inventory: { Iron: 5, Coin: 100 },
        },
      });
      const costItems = container.querySelectorAll(".cost-item");
      const ironItem = Array.from(costItems).find((el) => el.textContent?.includes("Iron"));
      const coinItem = Array.from(costItems).find((el) => el.textContent?.includes("Coin"));
      expect(ironItem).toHaveClass("unaffordable");
      expect(coinItem).not.toHaveClass("unaffordable");
    });

    it("shows Buy All & Build button for components sold at this port", () => {
      renderShipyard({
        state: {
          components: [makeComponentData({ name: "Hull", cost: { Iron: 5 } })],
          itemsForSale: [makeShopItem({ type: "Iron", buyPrice: 10 })],
          inventory: { Coin: 100, Iron: 0 },
        },
      });
      expect(screen.getByRole("button", { name: "Buy All & Build" })).toBeInTheDocument();
    });

    it("Buy All & Build sends buy_items, waits for update, then purchase_component", async () => {
      const { user, ipcSpy } = renderShipyard({
        state: {
          components: [makeComponentData({ name: "Hull", cost: { Iron: 5 } })],
          itemsForSale: [makeShopItem({ type: "Iron", buyPrice: 10 })],
          inventory: { Coin: 100, Iron: 2 },
        },
        simulateGodot: true,
      });
      ipcSpy.mockClear();
      await user.click(screen.getByRole("button", { name: "Buy All & Build" }));

      await waitFor(() => {
        const messages = getIpcMessages(ipcSpy);
        expect(messages).toContainEqual({
          action: "buy_items",
          items: [{ type: "Iron", quantity: 3 }],
        });
        expect(messages).toContainEqual({
          action: "purchase_component",
          name: "Hull",
        });
      });
    });

    it("Buy All & Build disabled when total gold is insufficient", () => {
      renderShipyard({
        state: {
          components: [makeComponentData({ name: "Hull", cost: { Iron: 5 } })],
          itemsForSale: [makeShopItem({ type: "Iron", buyPrice: 100 })],
          inventory: { Coin: 1, Iron: 0 },
        },
      });
      expect(screen.getByRole("button", { name: "Buy All & Build" })).toBeDisabled();
    });

    it("shows projected stat changes for purchasable components", () => {
      renderShipyard({
        state: {
          stats: { Speed: 5 },
          components: [
            makeComponentData({
              name: "Turbo",
              cost: { Coin: 1 },
              statChanges: [makeStatChange({ stat: "Speed", modifier: "Additive", value: 10 })],
            }),
          ],
          inventory: { Coin: 100 },
        },
      });
      expect(screen.getByText(/Speed: 5 → 15/)).toBeInTheDocument();
    });

    it("shows multiplicative stat change as current → projected", () => {
      renderShipyard({
        state: {
          stats: { Speed: 10 },
          components: [
            makeComponentData({
              name: "Boost",
              cost: { Coin: 1 },
              statChanges: [makeStatChange({ stat: "Speed", modifier: "Multiplicative", value: 2 })],
            }),
          ],
          inventory: { Coin: 100 },
        },
      });
      expect(screen.getByText(/Speed: 10 → 20/)).toBeInTheDocument();
    });
  });
});
