import { useState, useEffect, useCallback } from "react";
import "./App.css";
import type { PortState, IpcMessage, ComponentData, OwnedComponent } from "./types";

type Tab = "market" | "shipyard";
type TradeMode = "buy" | "sell";

const BASE = import.meta.env.BASE_URL;

function sendIpc(msg: IpcMessage): void {
  window.ipc?.postMessage(JSON.stringify(msg));
}

function iconUrl(folder: "components" | "inventory", filename: string): string {
  return `${BASE}icons/${folder}/${filename}`;
}

function inventoryIcon(itemType: string): string {
  const map: Record<string, string> = {
    Wood: "wood.png",
    Iron: "iron.png",
    Fish: "fish.png",
    Tea: "tea.png",
    Coin: "coin.png",
    CannonBall: "cannon_ball.png",
    Trophy: "trophy.png",
  };
  return iconUrl("inventory", map[itemType] ?? "coin.png");
}

function formatStatName(stat: string): string {
  return stat.replace(/([A-Z])/g, " $1").trim();
}

// ── App ──────────────────────────────────────────────────────────────

export default function App() {
  const [portState, setPortState] = useState<PortState | null>(null);
  const [isOpen, setIsOpen] = useState(false);
  const [isClosing, setIsClosing] = useState(false);
  const [activeTab, setActiveTab] = useState<Tab>("market");

  useEffect(() => {
    window.openPort = (data: PortState) => {
      setPortState(data);
      setIsClosing(false);
      requestAnimationFrame(() => setIsOpen(true));
    };

    window.closePort = () => {
      setIsClosing(true);
      setIsOpen(false);
      setTimeout(() => {
        setPortState(null);
        setIsClosing(false);
        setActiveTab("market");
      }, 400);
    };

    window.updateState = (data: PortState) => {
      setPortState(data);
    };

    sendIpc({ action: "ready" });

    // When the user clicks the webview, the OS gives it keyboard focus.
    // We immediately ask Godot to reclaim focus so movement key events
    // always reach the game and never get "stuck".
    const returnFocus = () => sendIpc({ action: "focus_parent" });
    window.addEventListener("focus", returnFocus);
    return () => window.removeEventListener("focus", returnFocus);
  }, []);

  if (!portState && !isClosing) return null;

  const panelClass = [
    "port-panel",
    isOpen ? "open" : "",
    isClosing ? "closing" : "",
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <div className={panelClass}>
      <div className="port-header">
        <div className="port-name">{portState?.portName ?? ""}</div>
        {portState && (
          <div className="port-coins">
            <img src={inventoryIcon("Coin")} alt="coins" />
            {portState.inventory["Coin"] ?? 0} Gold
          </div>
        )}
      </div>

      <div className="tab-bar">
        <button
          className={`tab-btn ${activeTab === "market" ? "active" : ""}`}
          onClick={() => setActiveTab("market")}
        >
          Market
        </button>
        <button
          className={`tab-btn ${activeTab === "shipyard" ? "active" : ""}`}
          onClick={() => setActiveTab("shipyard")}
        >
          Shipyard
        </button>
      </div>

      {portState && (
        <div className="tab-content">
          {activeTab === "market" ? (
            <MarketTab state={portState} />
          ) : (
            <ShipyardTab state={portState} />
          )}
        </div>
      )}
    </div>
  );
}

// ── Market Tab ───────────────────────────────────────────────────────

function MarketTab({ state }: { state: PortState }) {
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

  const maxBuyable = (_itemType: string, price: number): number => {
    const coins = state.inventory["Coin"] ?? 0;
    return price > 0 ? Math.floor(coins / price) : 0;
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

// ── Shipyard Tab ─────────────────────────────────────────────────────

function ShipyardTab({ state }: { state: PortState }) {
  const equippedCount = state.ownedComponents.filter((c) => c.isEquipped).length;
  const canEquipMore = equippedCount < state.componentCapacity;

  const countBy = (ocs: OwnedComponent[]) => {
    const m = new Map<string, number>();
    ocs.forEach((oc) => m.set(oc.name, (m.get(oc.name) ?? 0) + 1));
    return m;
  };

  const equippedCounts = countBy(state.ownedComponents.filter((oc) => oc.isEquipped));
  const equipped = Array.from(equippedCounts.entries())
    .map(([name, count]) => ({ count, data: state.components.find((c) => c.name === name) }))
    .filter((x): x is { count: number; data: ComponentData } => !!x.data);

  const unequippedCounts = countBy(state.ownedComponents.filter((oc) => !oc.isEquipped));
  const ownedUnequipped = Array.from(unequippedCounts.entries())
    .map(([name, count]) => ({ count, data: state.components.find((c) => c.name === name) }))
    .filter((x): x is { count: number; data: ComponentData } => !!x.data);

  const forSale = state.components;

  // Total additive/multiplicative contribution per stat from all equipped components
  const statBonuses: Record<string, { add: number; multi: number }> = {};
  for (const { count, data } of equipped) {
    for (const sc of data.statChanges) {
      if (!statBonuses[sc.stat]) statBonuses[sc.stat] = { add: 0, multi: 1 };
      if (sc.modifier === "Additive") {
        statBonuses[sc.stat].add += sc.value * count;
      } else {
        statBonuses[sc.stat].multi *= Math.pow(sc.value, count);
      }
    }
  }

  const canAfford = (cost: Record<string, number>): boolean => {
    return Object.entries(cost).every(
      ([type, amount]) => (state.inventory[type] ?? 0) >= amount
    );
  };

  const healthNeeded = state.maxHealth - state.health;
  const woodAvail = state.inventory["Wood"] ?? 0;
  const fishAvail = state.inventory["Fish"] ?? 0;
  const maxHeal = Math.min(healthNeeded, Math.floor(woodAvail / 5), fishAvail);
  const healthPct = state.maxHealth > 0 ? (state.health / state.maxHealth) * 100 : 0;
  const healthBarPos = 100 - healthPct;

  // Buy & Heal: figure out how much Wood/Fish to purchase to fully heal
  const woodShop = state.itemsForSale.find(i => i.type === "Wood");
  const fishShop = state.itemsForSale.find(i => i.type === "Fish");
  const coinsAvail = state.inventory["Coin"] ?? 0;

  // Wood/Fish still needed after existing inventory
  const woodToBuy = woodShop ? Math.max(0, healthNeeded * 5 - woodAvail) : null;
  const fishToBuy = fishShop ? Math.max(0, healthNeeded - fishAvail) : null;
  const buyHealGoldCost =
    woodToBuy !== null && fishToBuy !== null
      ? woodToBuy * (woodShop!.buyPrice) + fishToBuy * (fishShop!.buyPrice)
      : null;
  const canBuyAndHeal =
    buyHealGoldCost !== null &&
    healthNeeded > 0 &&
    coinsAvail >= buyHealGoldCost;

  return (
    <>
      {/* Health & Repair */}
      <div className="section-title">Ship Health</div>
      <div className="card health-section">
        <div className="health-bar-container">
          <div className="health-bar">
            <div
              className="health-bar-fill"
              style={{
                width: `${healthPct}%`,
                backgroundPosition: `${healthBarPos}% 0`,
              }}
            />
          </div>
          <div className="health-text">
            {state.health} / {state.maxHealth}
          </div>
        </div>
        <button
          className="heal-btn"
          disabled={maxHeal <= 0}
          onClick={() => sendIpc({ action: "heal" })}
        >
          {healthNeeded <= 0
            ? "Hull at Full Strength"
            : `Repair Hull (+${maxHeal} HP)`}
        </button>
        {healthNeeded > 0 && (
          <div className="heal-cost">
            <div>Cost: {maxHeal * 5} Wood + {maxHeal} Fish per repair</div>
            <div className="heal-resources">
              <span className={woodAvail < maxHeal * 5 ? "resource-short" : ""}>
                Wood: {woodAvail}
              </span>
              <span className={fishAvail < maxHeal ? "resource-short" : ""}>
                Fish: {fishAvail}
              </span>
            </div>
            {maxHeal < healthNeeded && (
              <div className="heal-limited">
                (limited by {Math.floor(woodAvail / 5) <= fishAvail ? "Wood" : "Fish"})
              </div>
            )}
          </div>
        )}
        {healthNeeded > 0 && buyHealGoldCost !== null && (
          <>
            <div className="heal-divider">— or —</div>
            <button
              className="heal-btn heal-btn-gold"
              disabled={!canBuyAndHeal}
              onClick={() => {
                const items: { type: string; quantity: number }[] = [];
                if (woodToBuy! > 0) items.push({ type: "Wood", quantity: woodToBuy! });
                if (fishToBuy! > 0) items.push({ type: "Fish", quantity: fishToBuy! });
                if (items.length > 0) sendIpc({ action: "buy_items", items });
                sendIpc({ action: "heal" });
              }}
            >
              Buy Resources & Repair (+{healthNeeded} HP)
            </button>
            <div className="heal-cost">
              <div>Cost: {buyHealGoldCost} Gold</div>
              {woodToBuy! > 0 && <div className="heal-buy-detail">Buy {woodToBuy} Wood @ {woodShop!.buyPrice}G ea</div>}
              {fishToBuy! > 0 && <div className="heal-buy-detail">Buy {fishToBuy} Fish @ {fishShop!.buyPrice}G ea</div>}
              <div className="heal-resources">
                <span className={!canBuyAndHeal ? "resource-short" : ""}>
                  Gold: {coinsAvail}
                </span>
              </div>
            </div>
          </>
        )}
      </div>

      {/* Stats */}
      <div className="section-title">Ship Stats</div>
      <div className="card mb-12">
        <div className="stats-grid">
          {Object.entries(state.stats).map(([stat, value]) => {
            const bonus = statBonuses[stat];
            const parts: string[] = [];
            if (bonus?.add) parts.push(`+${fmt(bonus.add)}`);
            if (bonus?.multi && bonus.multi !== 1)
              parts.push(`+${Math.round((bonus.multi - 1) * 100)}%`);
            return (
              <div className="stat-row" key={stat}>
                <span className="stat-label">{formatStatName(stat)}</span>
                <span className="stat-value">
                  {typeof value === "number" ? fmt(value) : value}
                  {parts.length > 0 && (
                    <span className="stat-bonus">{parts.join(" ")}</span>
                  )}
                </span>
              </div>
            );
          })}
        </div>
      </div>

      <div className="section-sep" />

      {/* Equipped Components */}
      <div className="section-title">Equipped Components</div>
      <div className="capacity-bar">
        <span>Slots:</span>
        <div className="capacity-slots">
          {Array.from({ length: state.componentCapacity }).map((_, i) => (
            <div
              key={i}
              className={`slot ${i < equippedCount ? "filled" : ""}`}
            />
          ))}
        </div>
        <span>
          {equippedCount}/{state.componentCapacity}
        </span>
      </div>

      {equipped.length === 0 ? (
        <div className="empty-state mb-12">No components equipped</div>
      ) : (
        equipped.map(({ count, data }) => (
          <ComponentCard
            key={data.name}
            component={data}
            count={count}
            actionLabel="Unequip"
            actionClass="unequip"
            onAction={() =>
              sendIpc({ action: "unequip_component", name: data.name })
            }
            inventory={state.inventory}
            stats={state.stats}
          />
        ))
      )}

      {/* Owned but not equipped */}
      {ownedUnequipped.length > 0 && (
        <>
          <div className="section-sep" />
          <div className="section-title">Owned Components</div>
          {ownedUnequipped.map(({ count, data }) => (
            <ComponentCard
              key={data.name}
              component={data}
              count={count}
              actionLabel="Equip"
              actionClass="equip"
              disabled={!canEquipMore}
              onAction={() =>
                sendIpc({ action: "equip_component", name: data.name })
              }
              inventory={state.inventory}
              stats={state.stats}
            />
          ))}
        </>
      )}

      {/* Components for sale */}
      {forSale.length > 0 && (
        <>
          <div className="section-sep" />
          <div className="section-title">Available Components</div>
          {forSale.map((comp) => (
            <ComponentCard
              key={comp.name}
              component={comp}
              actionLabel="Buy"
              actionClass="buy"
              disabled={!canAfford(comp.cost)}
              showCost
              onAction={() =>
                sendIpc({ action: "purchase_component", name: comp.name })
              }
              inventory={state.inventory}
              stats={state.stats}
              itemsForSale={state.itemsForSale}
            />
          ))}
        </>
      )}
    </>
  );
}

// ── Component Card ───────────────────────────────────────────────────

interface ComponentCardProps {
  component: ComponentData;
  count?: number;
  actionLabel: string;
  actionClass: "buy" | "equip" | "unequip";
  disabled?: boolean;
  showCost?: boolean;
  onAction: () => void;
  inventory: Record<string, number>;
  stats: Record<string, number>;
  itemsForSale?: ShopItem[];
}

function fmt(n: number): string {
  const r = Math.round(n * 100) / 100;
  return r % 1 === 0 ? String(r) : r.toFixed(2).replace(/\.?0+$/, "");
}

function ComponentCard({
  component,
  count,
  actionLabel,
  actionClass,
  disabled,
  showCost,
  onAction,
  inventory,
  stats,
  itemsForSale,
}: ComponentCardProps) {
  const buyAndBuildInfo = itemsForSale
    ? (() => {
        const needed: { type: string; quantity: number }[] = [];
        let goldCost = 0;
        let canBuy = true;
        for (const [type, amount] of Object.entries(component.cost)) {
          const missing = Math.max(0, amount - (inventory[type] ?? 0));
          if (missing > 0) {
            const shopItem = itemsForSale.find((s) => s.type === type && s.buyPrice > 0);
            if (!shopItem) { canBuy = false; break; }
            needed.push({ type, quantity: missing });
            goldCost += missing * shopItem.buyPrice;
          }
        }
        const affordable = canBuy && (inventory["Coin"] ?? 0) >= goldCost;
        return { needed, goldCost, affordable };
      })()
    : null;
  return (
    <div className={`component-card ${actionClass === "unequip" ? "equipped" : ""}`}>
      <div className="component-icon-wrap">
        <img
          className="component-icon"
          src={iconUrl("components", component.icon)}
          alt={component.name}
        />
        {count !== undefined && count > 1 && (
          <span className="component-count">{count}</span>
        )}
      </div>
      <div className="component-body">
        <div className="component-name">{component.name}</div>
        <div className="component-desc">{component.description}</div>

        {showCost && (
          <div className="component-cost">
            {Object.entries(component.cost).map(([type, amount]) => {
              const owned = inventory[type] ?? 0;
              return (
                <span
                  key={type}
                  className={`cost-item ${owned < amount ? "unaffordable" : ""}`}
                >
                  <img src={inventoryIcon(type)} alt={type} />
                  {amount} {type}
                </span>
              );
            })}
          </div>
        )}

        <div className="component-stats">
          {component.statChanges.map((sc) => {
            const label = formatStatName(sc.stat);
            const isMulti = sc.modifier === "Multiplicative";
            if (actionClass === "unequip") {
              const display = isMulti
                ? `+${Math.round((sc.value - 1) * 100)}%`
                : `+${fmt(sc.value)}`;
              return (
                <span key={sc.stat} className="stat-change positive">
                  {label}: {display}
                </span>
              );
            }
            const current = stats[sc.stat] ?? 0;
            const projected = isMulti
              ? current * sc.value
              : current + sc.value;
            return (
              <span key={sc.stat} className="stat-change positive">
                {label}: {fmt(current)} → {fmt(projected)}
              </span>
            );
          })}
        </div>
      </div>
      <div className="component-actions">
        <button
          className={`component-action-btn ${actionClass}`}
          disabled={disabled}
          onClick={onAction}
        >
          {actionLabel}
        </button>
        {buyAndBuildInfo && (
          <button
            className="component-action-btn buy-and-build"
            disabled={!buyAndBuildInfo.affordable}
            onClick={() => {
              if (buyAndBuildInfo.needed.length > 0) {
                sendIpc({ action: "buy_items", items: buyAndBuildInfo.needed });
              }
              sendIpc({ action: "purchase_component", name: component.name });
            }}
          >
            Buy All & Build
          </button>
        )}
      </div>
    </div>
  );
}
