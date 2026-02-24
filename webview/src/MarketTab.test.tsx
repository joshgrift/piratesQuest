import { describe, it, expect } from "vitest";
import { screen, within } from "@testing-library/react";
import { renderApp, getIpcMessages } from "./test/helpers";
import { makeShopItem } from "./test/fixtures";

// Helper: render the app with the Market tab open.
function renderMarket(overrides?: Parameters<typeof renderApp>[0] extends infer O ? O : never) {
  const stateOverrides = overrides && "state" in overrides ? overrides.state : overrides;
  return renderApp({ state: stateOverrides, tab: "market" });
}

describe("MarketTab", () => {
  // ── Mode toggle ──────────────────────────────────────────────────

  describe("mode toggle", () => {
    it("defaults to Buy Goods mode", () => {
      renderMarket();
      const buyBtn = screen.getByRole("button", { name: "Buy Goods" });
      expect(buyBtn).toHaveClass("active");
    });

    it("switches to Sell Goods mode", async () => {
      const { user } = renderMarket();
      await user.click(screen.getByRole("button", { name: "Sell Goods" }));
      expect(screen.getByRole("button", { name: "Sell Goods" })).toHaveClass("active");
      expect(screen.getByRole("button", { name: "Buy Goods" })).not.toHaveClass("active");
    });

    it("resets quantities when switching modes", async () => {
      const { user } = renderMarket({
        state: {
          itemsForSale: [makeShopItem({ type: "Wood", buyPrice: 10, sellPrice: 5 })],
          inventory: { Wood: 20, Coin: 100 },
        },
      });

      // Increment qty in buy mode
      const plusBtns = screen.getAllByRole("button", { name: "+" });
      await user.click(plusBtns[0]!);
      expect(screen.getByText("1")).toBeInTheDocument();

      // Switch to sell — qty should reset
      await user.click(screen.getByRole("button", { name: "Sell Goods" }));
      const qtyValues = screen.getAllByText("0");
      expect(qtyValues.length).toBeGreaterThan(0);
    });
  });

  // ── Item filtering ────────────────────────────────────────────────

  describe("item filtering", () => {
    it("shows only items with buyPrice > 0 in buy mode", () => {
      renderMarket({
        state: {
          itemsForSale: [
            makeShopItem({ type: "Wood", buyPrice: 10, sellPrice: 5 }),
            makeShopItem({ type: "Tea", buyPrice: 0, sellPrice: 12 }),
          ],
        },
      });
      expect(screen.getByText("Wood")).toBeInTheDocument();
      expect(screen.queryByText("Tea")).not.toBeInTheDocument();
    });

    it("shows only items with sellPrice > 0 in sell mode", async () => {
      const { user } = renderMarket({
        state: {
          itemsForSale: [
            makeShopItem({ type: "Wood", buyPrice: 10, sellPrice: 0 }),
            makeShopItem({ type: "Tea", buyPrice: 0, sellPrice: 12 }),
          ],
          inventory: { Tea: 5, Coin: 100 },
        },
      });
      await user.click(screen.getByRole("button", { name: "Sell Goods" }));
      expect(screen.getByText("Tea")).toBeInTheDocument();
      expect(screen.queryByText("Wood")).not.toBeInTheDocument();
    });

    it("shows empty state when no items are available to buy", () => {
      renderMarket({
        state: { itemsForSale: [makeShopItem({ buyPrice: 0, sellPrice: 5 })] },
      });
      expect(screen.getByText("No items available to buy")).toBeInTheDocument();
    });

    it("shows empty state when no items are available to sell", async () => {
      const { user } = renderMarket({
        state: { itemsForSale: [makeShopItem({ buyPrice: 10, sellPrice: 0 })] },
      });
      await user.click(screen.getByRole("button", { name: "Sell Goods" }));
      expect(screen.getByText("No items available to sell")).toBeInTheDocument();
    });
  });

  // ── Buy mode quantities ───────────────────────────────────────────

  describe("buy mode quantities", () => {
    it("increments quantity with + button", async () => {
      const { user } = renderMarket({
        state: {
          itemsForSale: [makeShopItem({ type: "Wood", buyPrice: 5 })],
          inventory: { Coin: 100 },
        },
      });
      await user.click(screen.getByRole("button", { name: "+" }));
      // qty should now be 1
      const shopItem = screen.getByText("Wood").closest(".shop-item")!;
      expect(within(shopItem).getByText("1")).toBeInTheDocument();
    });

    it("decrements quantity with - button", async () => {
      const { user } = renderMarket({
        state: {
          itemsForSale: [makeShopItem({ type: "Wood", buyPrice: 5 })],
          inventory: { Coin: 100 },
        },
      });
      // Go to 2, then back to 1
      await user.click(screen.getByRole("button", { name: "+" }));
      await user.click(screen.getByRole("button", { name: "+" }));
      await user.click(screen.getByRole("button", { name: "-" }));
      // qty = 1, total should show 5
      const qtyValue = screen.getByText("1");
      expect(qtyValue).toBeInTheDocument();
    });

    it("cannot go below 0", () => {
      renderMarket({
        state: {
          itemsForSale: [makeShopItem({ type: "Wood", buyPrice: 5 })],
          inventory: { Coin: 100 },
        },
      });
      // The minus and reset buttons should be disabled at qty 0
      expect(screen.getByRole("button", { name: "-" })).toBeDisabled();
      expect(screen.getByRole("button", { name: "×" })).toBeDisabled();
    });

    it("cannot exceed max buyable (coins / price)", async () => {
      const { user } = renderMarket({
        state: {
          itemsForSale: [makeShopItem({ type: "Wood", buyPrice: 30 })],
          inventory: { Coin: 60 },
        },
      });
      // max = 60 / 30 = 2, click + three times — third should be capped
      await user.click(screen.getByRole("button", { name: "+" }));
      await user.click(screen.getByRole("button", { name: "+" }));
      expect(screen.getByRole("button", { name: "+" })).toBeDisabled();
    });

    it("+5 button adds 5 to quantity", async () => {
      const { user } = renderMarket({
        state: {
          itemsForSale: [makeShopItem({ type: "Wood", buyPrice: 1 })],
          inventory: { Coin: 100 },
        },
      });
      await user.click(screen.getByRole("button", { name: "+5" }));
      const shopItem = screen.getByText("Wood").closest(".shop-item")!;
      const qtyValue = within(shopItem).getByText("5", { selector: ".qty-value" });
      expect(qtyValue).toBeInTheDocument();
    });

    it("+50 button is disabled when it would exceed max", () => {
      renderMarket({
        state: {
          itemsForSale: [makeShopItem({ type: "Wood", buyPrice: 10 })],
          inventory: { Coin: 30 },
        },
      });
      // max = 3, so +50 should be disabled
      expect(screen.getByRole("button", { name: "+50" })).toBeDisabled();
    });

    it("reset (×) button resets quantity to 0", async () => {
      const { user } = renderMarket({
        state: {
          itemsForSale: [makeShopItem({ type: "Wood", buyPrice: 1 })],
          inventory: { Coin: 100 },
        },
      });
      await user.click(screen.getByRole("button", { name: "+5" }));
      await user.click(screen.getByRole("button", { name: "×" }));
      // The qty-value element should show 0
      const shopItem = screen.getByText("Wood").closest(".shop-item")!;
      expect(within(shopItem).getByText("0")).toBeInTheDocument();
    });
  });

  // ── Sell mode quantities ──────────────────────────────────────────

  describe("sell mode quantities", () => {
    function renderSellMode() {
      const result = renderMarket({
        state: {
          itemsForSale: [makeShopItem({ type: "Wood", buyPrice: 10, sellPrice: 5 })],
          inventory: { Wood: 50, Coin: 100 },
        },
      });
      return result;
    }

    it("shows owned count in sell mode", async () => {
      const { user } = renderSellMode();
      await user.click(screen.getByRole("button", { name: "Sell Goods" }));
      expect(screen.getByText(/owned: 50/)).toBeInTheDocument();
    });

    it("All button sets quantity to max sellable", async () => {
      const { user } = renderSellMode();
      await user.click(screen.getByRole("button", { name: "Sell Goods" }));
      await user.click(screen.getByRole("button", { name: "All" }));
      expect(screen.getByText("50")).toBeInTheDocument();
    });

    it("cannot exceed owned amount", async () => {
      const { user } = renderMarket({
        state: {
          itemsForSale: [makeShopItem({ type: "Wood", buyPrice: 10, sellPrice: 5 })],
          inventory: { Wood: 2, Coin: 100 },
        },
      });
      await user.click(screen.getByRole("button", { name: "Sell Goods" }));
      await user.click(screen.getByRole("button", { name: "+" }));
      await user.click(screen.getByRole("button", { name: "+" }));
      expect(screen.getByRole("button", { name: "+" })).toBeDisabled();
    });

    it("sell mode has -100, -50, -5 bulk decrement buttons", async () => {
      const { user } = renderSellMode();
      await user.click(screen.getByRole("button", { name: "Sell Goods" }));
      // These buttons label as negative numbers in sell mode
      expect(screen.getByRole("button", { name: "-100" })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: "-50" })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: "-5" })).toBeInTheDocument();
    });
  });

  // ── Price display ─────────────────────────────────────────────────

  describe("price display", () => {
    it("shows buy price per item in buy mode", () => {
      renderMarket({
        state: {
          itemsForSale: [makeShopItem({ type: "Wood", buyPrice: 15, sellPrice: 7 })],
          inventory: { Coin: 100 },
        },
      });
      expect(screen.getByText(/15 each/)).toBeInTheDocument();
    });

    it("shows sell price per item in sell mode", async () => {
      const { user } = renderMarket({
        state: {
          itemsForSale: [makeShopItem({ type: "Wood", buyPrice: 15, sellPrice: 7 })],
          inventory: { Wood: 5, Coin: 100 },
        },
      });
      await user.click(screen.getByRole("button", { name: "Sell Goods" }));
      expect(screen.getByText(/7 each/)).toBeInTheDocument();
    });

    it("shows line-item total when quantity > 0", async () => {
      const { user, container } = renderMarket({
        state: {
          itemsForSale: [makeShopItem({ type: "Wood", buyPrice: 8 })],
          inventory: { Coin: 100 },
        },
      });
      await user.click(screen.getByRole("button", { name: "+5" }));
      // 5 * 8 = 40 — shown in the .shop-item-total element
      const itemTotal = container.querySelector(".shop-item-total")!;
      expect(itemTotal.textContent).toBe("40");
    });
  });

  // ── Confirm / IPC ─────────────────────────────────────────────────

  describe("confirm and IPC", () => {
    it("confirm button is disabled when nothing is selected", () => {
      renderMarket();
      expect(screen.getByRole("button", { name: "Purchase" })).toBeDisabled();
    });

    it("confirm button text says 'Purchase' in buy mode", () => {
      renderMarket();
      expect(screen.getByRole("button", { name: "Purchase" })).toBeInTheDocument();
    });

    it("confirm button text says 'Sell' in sell mode", async () => {
      const { user } = renderMarket();
      await user.click(screen.getByRole("button", { name: "Sell Goods" }));
      expect(screen.getByRole("button", { name: "Sell" })).toBeInTheDocument();
    });

    it("sends buy_items IPC when confirming a purchase", async () => {
      const { user, ipcSpy } = renderMarket({
        state: {
          itemsForSale: [makeShopItem({ type: "Wood", buyPrice: 10 })],
          inventory: { Coin: 100 },
        },
      });
      await user.click(screen.getByRole("button", { name: "+" }));
      ipcSpy.mockClear();
      await user.click(screen.getByRole("button", { name: "Purchase" }));

      const messages = getIpcMessages(ipcSpy);
      expect(messages).toContainEqual({
        action: "buy_items",
        items: [{ type: "Wood", quantity: 1 }],
      });
    });

    it("sends sell_items IPC when confirming a sale", async () => {
      const { user, ipcSpy } = renderMarket({
        state: {
          itemsForSale: [makeShopItem({ type: "Wood", buyPrice: 10, sellPrice: 5 })],
          inventory: { Wood: 20, Coin: 100 },
        },
      });
      await user.click(screen.getByRole("button", { name: "Sell Goods" }));
      await user.click(screen.getByRole("button", { name: "+" }));
      await user.click(screen.getByRole("button", { name: "+" }));
      await user.click(screen.getByRole("button", { name: "+" }));
      ipcSpy.mockClear();
      await user.click(screen.getByRole("button", { name: "Sell" }));

      const messages = getIpcMessages(ipcSpy);
      expect(messages).toContainEqual({
        action: "sell_items",
        items: [{ type: "Wood", quantity: 3 }],
      });
    });

    it("resets quantities after confirming", async () => {
      const { user } = renderMarket({
        state: {
          itemsForSale: [makeShopItem({ type: "Wood", buyPrice: 10 })],
          inventory: { Coin: 100 },
        },
      });
      await user.click(screen.getByRole("button", { name: "+" }));
      await user.click(screen.getByRole("button", { name: "Purchase" }));

      // After confirm, qty should be back to 0
      const shopItem = screen.getByText("Wood").closest(".shop-item")!;
      expect(within(shopItem).getByText("0")).toBeInTheDocument();
    });

    it("only sends items with qty > 0 (ignores zero-qty items)", async () => {
      const { user, ipcSpy } = renderMarket({
        state: {
          itemsForSale: [
            makeShopItem({ type: "Wood", buyPrice: 10 }),
            makeShopItem({ type: "Fish", buyPrice: 8 }),
          ],
          inventory: { Coin: 100 },
        },
      });
      // Only increment Wood, leave Fish at 0
      const plusBtns = screen.getAllByRole("button", { name: "+" });
      await user.click(plusBtns[0]!);
      ipcSpy.mockClear();
      await user.click(screen.getByRole("button", { name: "Purchase" }));

      const messages = getIpcMessages(ipcSpy);
      const buyMsg = messages.find(
        (m) => (m as { action: string }).action === "buy_items",
      ) as { action: string; items: { type: string; quantity: number }[] };
      expect(buyMsg.items).toHaveLength(1);
      expect(buyMsg.items[0]!.type).toBe("Wood");
    });
  });

  // ── Total cost display ────────────────────────────────────────────

  describe("total cost", () => {
    it("shows combined total for multiple items", async () => {
      const { user } = renderMarket({
        state: {
          itemsForSale: [
            makeShopItem({ type: "Wood", buyPrice: 10 }),
            makeShopItem({ type: "Fish", buyPrice: 8 }),
          ],
          inventory: { Coin: 200 },
        },
      });
      // Buy 2 Wood (20) + 3 Fish (24) = 44
      const plusBtns = screen.getAllByRole("button", { name: "+" });
      await user.click(plusBtns[0]!); // Wood +1
      await user.click(plusBtns[0]!); // Wood +1
      await user.click(plusBtns[1]!); // Fish +1
      await user.click(plusBtns[1]!); // Fish +1
      await user.click(plusBtns[1]!); // Fish +1

      const footer = screen.getByRole("button", { name: "Purchase" }).closest(".trade-footer")!;
      expect(within(footer).getByText("44")).toBeInTheDocument();
    });
  });

  // ── Edge cases ────────────────────────────────────────────────────

  describe("edge cases", () => {
    it("handles zero coins gracefully (max buyable is 0)", () => {
      renderMarket({
        state: {
          itemsForSale: [makeShopItem({ type: "Wood", buyPrice: 10 })],
          inventory: { Coin: 0 },
        },
      });
      // + button should be disabled because you can't buy anything
      expect(screen.getByRole("button", { name: "+" })).toBeDisabled();
    });

    it("handles item not in inventory for sell mode (0 owned)", async () => {
      const { user } = renderMarket({
        state: {
          itemsForSale: [makeShopItem({ type: "Tea", buyPrice: 0, sellPrice: 12 })],
          inventory: { Coin: 100 },
        },
      });
      await user.click(screen.getByRole("button", { name: "Sell Goods" }));
      expect(screen.getByText(/owned: 0/)).toBeInTheDocument();
      expect(screen.getByRole("button", { name: "+" })).toBeDisabled();
    });

    it("shows gold count in the header", () => {
      renderMarket({ state: { inventory: { Coin: 999 } } });
      expect(screen.getByText(/999 Gold/)).toBeInTheDocument();
    });
  });
});
