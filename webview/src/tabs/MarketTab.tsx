import { useState, useCallback } from "react";
import { sendIpc } from "../utils/ipc";
import { inventoryIcon } from "../utils/helpers";
import type { PortState } from "../types";

type TradeMode = "buy" | "sell";

export function MarketTab({ state }: { state: PortState }) {
  const [mode, setMode] = useState<TradeMode>("buy");
  const [quantities, setQuantities] = useState<Record<string, number>>({});
  const [flashConfirm, setFlashConfirm] = useState(false);

  const items = state.itemsForSale.filter((item) =>
    mode === "buy" ? item.buyPrice > 0 : item.sellPrice > 0
  );

  const setQty = useCallback((itemType: string, qty: number) => {
    setQuantities((prev) => ({ ...prev, [itemType]: Math.max(0, qty) }));
  }, []);

  const getQty = (itemType: string): number => quantities[itemType] ?? 0;

  // Total non-coin items already in the player's hold
  const currentCargo = Object.entries(state.inventory)
    .filter(([key]) => key !== "Coin")
    .reduce((sum, [, count]) => sum + count, 0);
  const holdCapacity = state.stats["ShipCapacity"] ?? Infinity;
  // How many total items are currently in the buy cart
  const cartTotal = Object.values(quantities).reduce((sum, q) => sum + q, 0);

  const maxBuyable = (itemType: string, price: number): number => {
    const coins = state.inventory["Coin"] ?? 0;
    const affordable = price > 0 ? Math.floor(coins / price) : 0;
    // Remaining hold space, giving back this item's current cart qty
    // since it's already counted in cartTotal
    const holdRoom = Math.max(0, holdCapacity - currentCargo - cartTotal + getQty(itemType));
    return Math.min(affordable, holdRoom);
  };

  const maxSellable = (itemType: string): number => {
    return state.inventory[itemType] ?? 0;
  };

  const totalCost = items.reduce((sum, item) => {
    const q = getQty(item.type);
    const price = mode === "buy" ? item.buyPrice : item.sellPrice;
    return sum + q * price;
  }, 0);

  const hasSelection = items.some((item) => getQty(item.type) > 0);

  const handleConfirm = () => {
    const selected = items
      .filter((item) => getQty(item.type) > 0)
      .map((item) => ({ type: item.type, quantity: getQty(item.type) }));

    if (selected.length === 0) return;

    sendIpc({
      action: mode === "buy" ? "buy_items" : "sell_items",
      items: selected,
    });

    setQuantities({});
    setFlashConfirm(true);
    setTimeout(() => setFlashConfirm(false), 500);
  };

  const handleModeChange = (newMode: TradeMode) => {
    setMode(newMode);
    setQuantities({});
  };

  return (
    <>
      <div className="mode-toggle">
        <button
          className={`mode-btn ${mode === "buy" ? "active" : ""}`}
          onClick={() => handleModeChange("buy")}
        >
          Buy Goods
        </button>
        <button
          className={`mode-btn ${mode === "sell" ? "active" : ""}`}
          onClick={() => handleModeChange("sell")}
        >
          Sell Goods
        </button>
      </div>

      {items.length === 0 ? (
        <div className="empty-state">
          No items available to {mode}
        </div>
      ) : (
        <>
          {items.map((item) => {
            const qty = getQty(item.type);
            const price = mode === "buy" ? item.buyPrice : item.sellPrice;
            const max =
              mode === "buy"
                ? maxBuyable(item.type, item.buyPrice)
                : maxSellable(item.type);

            return (
              <div className="shop-item" key={item.type}>
                <img
                  className="shop-item-icon"
                  src={inventoryIcon(item.type)}
                  alt={item.type}
                />
                <div className="shop-item-info">
                  <div className="shop-item-name">{item.type}</div>
                  <div className="shop-item-price">
                    <img src={inventoryIcon("Coin")} alt="coin" />
                    {price} each
                    {mode === "sell" && (
                      <span> &middot; owned: {state.inventory[item.type] ?? 0}</span>
                    )}
                  </div>
                </div>
                <div className="qty-stepper">
                  {mode === "sell" ? (
                    <>
                      {([100, 50, 5] as const).map((n) => (
                        <button
                          key={n}
                          className="qty-btn"
                          disabled={qty + n > max}
                          onClick={() => setQty(item.type, Math.min(max, qty + n))}
                        >
                          -{n}
                        </button>
                      ))}
                      <button
                        className="qty-btn"
                        disabled={qty <= 0}
                        onClick={() => setQty(item.type, qty - 1)}
                      >
                        -
                      </button>
                      <div className="qty-value">{qty}</div>
                      <button
                        className="qty-btn"
                        disabled={qty >= max}
                        onClick={() => setQty(item.type, qty + 1)}
                      >
                        +
                      </button>
                      <button
                        className="qty-btn qty-btn-all"
                        disabled={qty >= max}
                        onClick={() => setQty(item.type, max)}
                      >
                        All
                      </button>
                      <button
                        className="qty-btn qty-btn-reset"
                        disabled={qty <= 0}
                        onClick={() => setQty(item.type, 0)}
                      >
                        ×
                      </button>
                    </>
                  ) : (
                    <>
                      <button
                        className="qty-btn qty-btn-reset"
                        disabled={qty <= 0}
                        onClick={() => setQty(item.type, 0)}
                      >
                        ×
                      </button>
                      <button
                        className="qty-btn"
                        disabled={qty <= 0}
                        onClick={() => setQty(item.type, qty - 1)}
                      >
                        -
                      </button>
                      <div className="qty-value">{qty}</div>
                      <button
                        className="qty-btn"
                        disabled={qty >= max}
                        onClick={() => setQty(item.type, qty + 1)}
                      >
                        +
                      </button>
                      {([5, 50, 100] as const).map((n) => (
                        <button
                          key={n}
                          className="qty-btn"
                          disabled={qty + n > max}
                          onClick={() => setQty(item.type, Math.min(max, qty + n))}
                        >
                          +{n}
                        </button>
                      ))}
                    </>
                  )}
                </div>
                <div className="shop-item-total">
                  {qty > 0 ? qty * price : ""}
                </div>
              </div>
            );
          })}

          <div className={`trade-footer ${flashConfirm ? "confirm-flash" : ""}`}>
            <div className="trade-total">
              <img src={inventoryIcon("Coin")} alt="coin" />
              {totalCost}
            </div>
            <button
              className="confirm-btn"
              disabled={!hasSelection}
              onClick={handleConfirm}
            >
              {mode === "buy" ? "Purchase" : "Sell"}
            </button>
          </div>
        </>
      )}
    </>
  );
}
