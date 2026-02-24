import { useState, useEffect, useCallback } from "react";
import "./App.css";
import type { PortState, IpcMessage, ComponentData, OwnedComponent, ShopItem } from "./types";

type Tab = "market" | "shipyard" | "guide" | "creative";
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
  const [activeTab, setActiveTab] = useState<Tab>("guide");

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
        setActiveTab("guide");
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
        <button
          className={`tab-btn ${activeTab === "guide" ? "active" : ""}`}
          onClick={() => setActiveTab("guide")}
        >
          Scarlett
        </button>
        {portState?.isCreative && (
          <button
            className={`tab-btn creative-tab-btn ${activeTab === "creative" ? "active" : ""}`}
            onClick={() => setActiveTab("creative")}
          >
            Creative
          </button>
        )}
      </div>

      {portState && (
        <div className="tab-content">
          {activeTab === "market" ? (
            <MarketTab state={portState} />
          ) : activeTab === "shipyard" ? (
            <ShipyardTab state={portState} />
          ) : activeTab === "creative" && portState.isCreative ? (
            <CreativeTab state={portState} />
          ) : (
            <GuideTab />
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
  const healthHue = Math.round((healthPct / 100) * 120); // 0 = red, 120 = green

  // Buy & Heal: find the most HP healable by buying Wood/Fish from the shop
  const woodShop = state.itemsForSale.find(i => i.type === "Wood");
  const fishShop = state.itemsForSale.find(i => i.type === "Fish");
  const coinsAvail = state.inventory["Coin"] ?? 0;

  let maxBuyHeal = 0;
  if (woodShop && fishShop && healthNeeded > 0) {
    for (let h = 1; h <= healthNeeded; h++) {
      const goldNeeded =
        Math.max(0, h * 5 - woodAvail) * woodShop.buyPrice +
        Math.max(0, h - fishAvail) * fishShop.buyPrice;
      if (goldNeeded > coinsAvail) break;
      maxBuyHeal = h;
    }
  }
  const woodToBuy = Math.max(0, maxBuyHeal * 5 - woodAvail);
  const fishToBuy = Math.max(0, maxBuyHeal - fishAvail);
  const buyHealGoldCost = woodShop && fishShop
    ? woodToBuy * woodShop.buyPrice + fishToBuy * fishShop.buyPrice
    : null;

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
                backgroundColor: `hsl(${healthHue}, 75%, 42%)`,
              }}
            />
          </div>
          <div className="health-text">
            {state.health} / {state.maxHealth}
          </div>
        </div>
        {healthNeeded <= 0 ? (
          <div className="hull-full">Hull at Full Strength</div>
        ) : (
          <>
            <div className="repair-options">
              <div className="repair-row">
                <button
                  className="repair-btn"
                  disabled={maxHeal <= 0}
                  onClick={() => sendIpc({ action: "heal" })}
                >
                  Repair Hull
                </button>
                <div className="repair-costs">
                  <span className={`cost-chip ${woodAvail < healthNeeded * 5 ? "chip-short" : "chip-wood"}`}>
                    <img src={inventoryIcon("Wood")} alt="Wood" className="chip-icon" />{healthNeeded * 5}
                  </span>
                  <span className={`cost-chip ${fishAvail < healthNeeded ? "chip-short" : "chip-fish"}`}>
                    <img src={inventoryIcon("Fish")} alt="Fish" className="chip-icon" />{healthNeeded}
                  </span>
                  <span className="repair-hp">+{maxHeal} HP</span>
                </div>
              </div>
              {buyHealGoldCost !== null && (
                <div className="repair-row">
                  <button
                    className="repair-btn repair-btn-gold"
                    disabled={maxBuyHeal <= 0}
                    onClick={() => {
                      const items: { type: string; quantity: number }[] = [];
                      if (woodToBuy > 0) items.push({ type: "Wood", quantity: woodToBuy });
                      if (fishToBuy > 0) items.push({ type: "Fish", quantity: fishToBuy });
                      if (items.length > 0) sendIpc({ action: "buy_items", items });
                      sendIpc({ action: "heal" });
                    }}
                  >
                    Buy & Repair
                  </button>
                  <div className="repair-costs">
                    <span className={`cost-chip ${maxBuyHeal <= 0 ? "chip-short" : "chip-gold"}`}>
                      <img src={inventoryIcon("Coin")} alt="Gold" className="chip-icon" />{buyHealGoldCost}
                    </span>
                    <span className="repair-hp">+{maxBuyHeal} HP</span>
                  </div>
                </div>
              )}
            </div>
            <div className="repair-inventory">
              <span className={woodAvail < healthNeeded * 5 ? "resource-short" : ""}>
                <img src={inventoryIcon("Wood")} alt="Wood" className="chip-icon" />{woodAvail}
              </span>
              <span className={fishAvail < healthNeeded ? "resource-short" : ""}>
                <img src={inventoryIcon("Fish")} alt="Fish" className="chip-icon" />{fishAvail}
              </span>
              {buyHealGoldCost !== null && (
                <span className={maxBuyHeal <= 0 ? "resource-short" : ""}>
                  <img src={inventoryIcon("Coin")} alt="Gold" className="chip-icon" />{coinsAvail}
                </span>
              )}
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

// ── Creative Tab ─────────────────────────────────────────────────────

const ITEM_TYPES = ["Wood", "Iron", "Fish", "Tea", "Coin", "CannonBall", "Trophy"];
const STEP_AMOUNTS = [1, 50, 100] as const;

function CreativeTab({ state }: { state: PortState }) {
  const [flashItem, setFlashItem] = useState<string | null>(null);

  const flash = (key: string) => {
    setFlashItem(key);
    setTimeout(() => setFlashItem(null), 400);
  };

  const setItem = (type: string, quantity: number) => {
    const clamped = Math.max(0, quantity);
    sendIpc({ action: "set_inventory", items: [{ type, quantity: clamped }] });
    flash(type);
  };

  const setAllTo = (amount: number) => {
    const items = ITEM_TYPES.map((t) => ({ type: t, quantity: amount }));
    sendIpc({ action: "set_inventory", items });
    flash("__all__");
  };

  const healthPct = state.maxHealth > 0 ? (state.health / state.maxHealth) * 100 : 0;
  const healthHue = Math.round((healthPct / 100) * 120);

  return (
    <>
      <div className="creative-banner">Creative Mode</div>

      <div className="section-title">Inventory</div>

      <div className="creative-presets">
        <button className="creative-preset-btn" onClick={() => setAllTo(0)}>
          Clear All
        </button>
        <button className="creative-preset-btn" onClick={() => setAllTo(100)}>
          100 Each
        </button>
        <button className="creative-preset-btn" onClick={() => setAllTo(1000)}>
          1k Each
        </button>
        <button className="creative-preset-btn" onClick={() => setAllTo(100000)}>
          100k Each
        </button>
      </div>

      {ITEM_TYPES.map((type) => {
        const current = state.inventory[type] ?? 0;
        return (
          <div
            className={`creative-item ${flashItem === type || flashItem === "__all__" ? "creative-flash" : ""}`}
            key={type}
          >
            <img
              className="creative-item-icon"
              src={inventoryIcon(type)}
              alt={type}
            />
            <div className="creative-item-name">{type}</div>
            <div className="creative-stepper">
              {[...STEP_AMOUNTS].reverse().map((n) => (
                <button
                  key={`minus-${n}`}
                  className="qty-btn"
                  disabled={current - n < 0}
                  onClick={() => setItem(type, current - n)}
                >
                  -{n}
                </button>
              ))}
              <div className="creative-qty-value">{current}</div>
              {STEP_AMOUNTS.map((n) => (
                <button
                  key={`plus-${n}`}
                  className="qty-btn"
                  onClick={() => setItem(type, current + n)}
                >
                  +{n}
                </button>
              ))}
              <button
                className="qty-btn qty-btn-reset"
                disabled={current === 0}
                onClick={() => setItem(type, 0)}
              >
                ×
              </button>
            </div>
          </div>
        );
      })}

      <div className="section-sep" />

      <div className="section-title">Health</div>
      <div className="card creative-health-section">
        <div className="health-bar-container">
          <div className="health-bar">
            <div
              className="health-bar-fill"
              style={{
                width: `${healthPct}%`,
                backgroundColor: `hsl(${healthHue}, 75%, 42%)`,
              }}
            />
          </div>
          <div className="health-text">
            {state.health} / {state.maxHealth}
          </div>
        </div>
        <div className="creative-health-buttons">
          <button
            className="creative-preset-btn"
            onClick={() => sendIpc({ action: "set_health", health: 1 })}
          >
            1 HP
          </button>
          <button
            className="creative-preset-btn"
            onClick={() =>
              sendIpc({ action: "set_health", health: Math.round(state.maxHealth * 0.25) })
            }
          >
            25%
          </button>
          <button
            className="creative-preset-btn"
            onClick={() =>
              sendIpc({ action: "set_health", health: Math.round(state.maxHealth * 0.5) })
            }
          >
            50%
          </button>
          <button
            className="creative-preset-btn"
            onClick={() =>
              sendIpc({ action: "set_health", health: state.maxHealth })
            }
          >
            Full
          </button>
        </div>
      </div>

      <div className="section-sep" />

      <div className="section-title">Components</div>
      <div className="card">
        <div className="creative-components-info">
          {state.ownedComponents.length === 0 ? (
            <span className="empty-state">No components owned</span>
          ) : (
            <span>
              {state.ownedComponents.length} owned
              {" · "}
              {state.ownedComponents.filter((c) => c.isEquipped).length} equipped
            </span>
          )}
        </div>
        <button
          className="creative-danger-btn"
          disabled={state.ownedComponents.length === 0}
          onClick={() => sendIpc({ action: "clear_components" })}
        >
          Delete All Components
        </button>
      </div>
    </>
  );
}

// ── Guide Tab — Dialogue Tree ─────────────────────────────────────────

interface DialogueNode {
  text: string;
  responses: { label: string; next: string }[];
}

const GUIDE_DIALOGUE: Record<string, DialogueNode> = {
  root: {
    text: "Ahoy there, sailor! Pull up a chair. Name's Scarlett \u2014 been sailin' these waters longer than most. What would ye like to know?",
    responses: [
      { label: "How do I sail my ship?", next: "sailing" },
      { label: "How does trading work?", next: "trading" },
      { label: "Tell me about combat", next: "combat" },
      { label: "What are resources for?", next: "resources" },
      { label: "How do I collect resources?", next: "collecting" },
      { label: "How do ship upgrades work?", next: "upgrades" },
      { label: "What if I'm overburdened?", next: "overburdened" },
      { label: "How does the leaderboard work?", next: "leaderboard" },
      { label: "What happens when I die?", next: "death" },
      { label: "What can I do at ports?", next: "ports" },
    ],
  },

  // ── Sailing ──
  sailing: {
    text: "Sailin' is simple, love. W moves ye forward, S slows ye down, and A/D turns the ship. She's got momentum though \u2014 plan yer turns early or you'll be kissin' the shoreline!",
    responses: [
      { label: "Any sailing tips?", next: "sailing_tips" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  sailing_tips: {
    text: "Here's a free one: a loaded ship handles worse. The heavier yer cargo, the slower ye turn. You'll see a 'Heavy' warning when near capacity.\n\nQuick quiz \u2014 when yer ship is heavy with cargo, what happens?",
    responses: [
      { label: "It turns slower", next: "sailing_right" },
      { label: "It goes faster", next: "sailing_wrong" },
      { label: "Nothing changes", next: "sailing_wrong" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  sailing_right: {
    text: "Sharp as a cutlass! Heavy ships handle like a drunken whale. Keep that in mind when ye're loaded with trade goods heading through tight waters!",
    responses: [{ label: "What else can I learn?", next: "root" }],
  },
  sailing_wrong: {
    text: "Not quite, love! A heavy ship turns slower and handles worse. Ye'll see the 'Heavy' icon on screen when near capacity. Sell off some goods or choose yer route carefully!",
    responses: [{ label: "Good to know! What else?", next: "root" }],
  },

  // ── Trading ──
  trading: {
    text: "Now ye're speakin' my language! Each port has different prices for goods. The secret? Buy cheap at one port, sail across the map, and sell dear at another. Supply and demand, sailor!",
    responses: [
      { label: "How do I buy and sell?", next: "trading_how" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  trading_how: {
    text: "When ye dock, check the Market tab \u2014 right here! Switch between 'Buy Goods' and 'Sell Goods' at the top, set yer quantities, and confirm. Simple as breathin'!\n\nNow tell me \u2014 to make the most gold, what should ye do?",
    responses: [
      { label: "Buy low at one port, sell high at another", next: "trading_right" },
      { label: "Sell everything at the first port", next: "trading_wrong" },
      { label: "Only collect, never buy", next: "trading_wrong" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  trading_right: {
    text: "Ha! Ye'll be a merchant prince in no time! Every port has different prices \u2014 compare before ye sell. Gold is king, sailor. It buys components and puts ye on the leaderboard!",
    responses: [{ label: "What else can I learn?", next: "root" }],
  },
  trading_wrong: {
    text: "Bless yer heart, no! The trick is buyin' where it's cheap and sellin' where it's dear. Every port has different prices \u2014 always check before ye unload!",
    responses: [{ label: "I'll remember that! What else?", next: "root" }],
  },

  // ── Combat ──
  combat: {
    text: "Time to talk firepower! Press Q to fire yer port-side cannons (that's left), and E for starboard (right). Each shot uses a cannonball from yer hold, with a 2-second cooldown between volleys.",
    responses: [
      { label: "What can I fight?", next: "combat_targets" },
      { label: "Any combat tips?", next: "combat_tips" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  combat_targets: {
    text: "Other players are the biggest prize \u2014 sink one and ye get half their inventory! But they're thinkin' the same about you, so stay sharp out there.",
    responses: [
      { label: "Any combat tips?", next: "combat_tips" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  combat_tips: {
    text: "Always keep cannonballs stocked! Nothin' worse than an empty cannon in a fight. And remember \u2014 ports are safe zones. If things go south, run for shore!\n\nPop quiz, sailor! Which key fires yer LEFT cannons?",
    responses: [
      { label: "Q", next: "combat_right" },
      { label: "E", next: "combat_wrong" },
      { label: "Space", next: "combat_wrong" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  combat_right: {
    text: "That's it! Q for port, E for starboard. A true sailor always knows their port from their starboard. Now get out there and give 'em hell!",
    responses: [{ label: "What else should I know?", next: "root" }],
  },
  combat_wrong: {
    text: "Almost! Q fires port-side (left), E fires starboard (right). Easy to remember: Q is on the left of yer keyboard \u2014 just like port side!",
    responses: [{ label: "Got it! What else?", next: "root" }],
  },

  // ── Collecting ──
  collecting: {
    text: "See those glowing spots out on the water? Those are collection points! Each one gives a different resource \u2014 Wood, Iron, Fish, or Tea. Just sail yer ship close and yer crew handles the rest.",
    responses: [
      { label: "How does it work exactly?", next: "collecting_how" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  collecting_how: {
    text: "When ye enter a collection point, you'll see a progress indicator on screen. Stay inside the ring and resources flow into yer hold automatically. The longer ye stay, the more ye collect!\n\nBut watch yer cargo capacity \u2014 a full hold means ye can't gather more. Sell or spend yer goods to make room.",
    responses: [
      { label: "Can I collect faster?", next: "collecting_upgrades" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  collecting_upgrades: {
    text: "Aye! There are ship components that boost yer collection rates by 50% \u2014 Advanced Fish Nets, Reinforced Lumber Tools, and Enhanced Mining Tools. Buy 'em in the Shipyard tab and equip 'em before ye head out.\n\nAlso, the Expanded Cargo Hold gives ye more room to carry what ye gather. A smart sailor upgrades before they harvest!",
    responses: [{ label: "Good to know! What else?", next: "root" }],
  },

  // ── Resources ──
  resources: {
    text: "Resources are the lifeblood of yer journey! Sail near the glowing collection points scattered around the map and yer crew will start gatherin' automatically.",
    responses: [
      { label: "What resources are there?", next: "resources_list" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  resources_list: {
    text: "Six things ye need to know:\n\n\u2022 Wood \u2014 repairs, crafting, trading\n\u2022 Iron \u2014 components, trading\n\u2022 Fish \u2014 repairs, trading\n\u2022 Tea \u2014 valuable trade good\n\u2022 Gold \u2014 the universal currency\n\u2022 Cannonballs \u2014 don't leave port without 'em!\n\nNow, what do ye need to repair yer ship?",
    responses: [
      { label: "Wood and Fish", next: "resources_right" },
      { label: "Just Gold", next: "resources_wrong" },
      { label: "Iron and Tea", next: "resources_wrong" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  resources_right: {
    text: "Aye! 5 Wood and 1 Fish per point of hull ye repair. Always keep some in reserve \u2014 ye never know when you'll limp into port full of holes!",
    responses: [{ label: "Good tip! What else?", next: "root" }],
  },
  resources_wrong: {
    text: "Not quite! Ye need Wood and Fish \u2014 5 Wood and 1 Fish per hull point. Head to the Shipyard tab to repair. Ye can also buy materials with Gold if you're short!",
    responses: [{ label: "I'll stock up! What else?", next: "root" }],
  },

  // ── Upgrades ──
  upgrades: {
    text: "Ship components are what separate a floatin' plank from a war vessel! Buy 'em in the Shipyard tab with resources, then equip 'em to boost yer stats \u2014 speed, damage, cargo space, and more.",
    responses: [
      { label: "How do I equip them?", next: "upgrades_equip" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  upgrades_equip: {
    text: "Head to the Shipyard tab when docked. Components for sale are at the bottom \u2014 buy one, then equip it from yer owned list. Ye've got limited slots, so choose wisely!\n\nImportant question \u2014 what happens to yer components when ye die?",
    responses: [
      { label: "They're all lost", next: "upgrades_right" },
      { label: "They stay equipped", next: "upgrades_wrong" },
      { label: "Half are lost", next: "upgrades_wrong" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  upgrades_right: {
    text: "Aye... the harsh truth of the sea. Every equipped component is gone when ye sink. Smart sailors only invest heavy when they can defend themselves!",
    responses: [{ label: "That's rough! What else?", next: "root" }],
  },
  upgrades_wrong: {
    text: "I wish, love! When ye die, ALL equipped components are lost forever. It's a cruel sea \u2014 only load up on upgrades when ye've got the firepower to keep 'em!",
    responses: [{ label: "I'll be careful! What else?", next: "root" }],
  },

  // ── Overburdened ──
  overburdened: {
    text: "When yer hold is stuffed near capacity, yer ship gets heavy. You'll see a 'Heavy' warning on screen \u2014 that means you're overburdened, sailor!",
    responses: [
      { label: "What happens when I'm heavy?", next: "overburdened_effects" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  overburdened_effects: {
    text: "A heavy ship turns slower and handles like a brick. Ye'll struggle to dodge enemies and navigate tight waters. Worse, ye can't collect any more resources until ye free up space.\n\nSell goods at a port, spend resources on components, or dump what ye don't need. A nimble ship is a livin' ship!",
    responses: [{ label: "I'll keep that in mind! What else?", next: "root" }],
  },

  // ── Leaderboard ──
  leaderboard: {
    text: "See that list on the left side of yer screen? That's the leaderboard! It ranks every sailor on the server by their Trophy count.",
    responses: [
      { label: "How do I get trophies?", next: "leaderboard_trophies" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  leaderboard_trophies: {
    text: "Trophies are earned through glory \u2014 sinkin' other players, completin' challenges, and provin' yer worth on the high seas. The more ye have, the higher ye climb.\n\nFair warning though: when ye die, ye lose half yer trophies just like everything else. So stay alive if ye want to stay on top!",
    responses: [{ label: "I'll aim for the top! What else?", next: "root" }],
  },

  // ── Death ──
  death: {
    text: "Death ain't the end, but it bites! When yer ship sinks, ye lose half yer inventory and ALL equipped components. Ye'll respawn after a short wait at a random spot with basic supplies.",
    responses: [
      { label: "How can I protect my stuff?", next: "death_protect" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  death_protect: {
    text: "Smart thinkin'! Best advice: spend yer resources before headin' into danger. Buy components, trade at ports, invest in upgrades. Resources sittin' in yer hold are resources ye could lose!",
    responses: [{ label: "Good advice! What else?", next: "root" }],
  },

  // ── Ports ──
  ports: {
    text: "Ports are yer safe haven! When docked, ye can't take damage. Perfect for catchin' yer breath after a rough fight or a long sail.",
    responses: [
      { label: "What can I do here?", next: "ports_features" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  ports_features: {
    text: "Plenty! The Market for buyin' and sellin' goods, the Shipyard for components and repairs, and me \u2014 yer humble guide! Each port has different prices, so it pays to explore.",
    responses: [{ label: "Thanks! What else can I learn?", next: "root" }],
  },

};

// ── Guide Tab ─────────────────────────────────────────────────────────

function GuideTab() {
  const [nodeId, setNodeId] = useState("root");
  const [charIndex, setCharIndex] = useState(GUIDE_DIALOGUE.root.text.length);
  const [animKey, setAnimKey] = useState(0);

  const node = GUIDE_DIALOGUE[nodeId];
  const isTyping = charIndex < node.text.length;

  useEffect(() => {
    if (!isTyping) return;
    const id = setTimeout(() => setCharIndex((c) => c + 1), 25);
    return () => clearTimeout(id);
  }, [charIndex, isTyping]);

  const skip = () => {
    if (isTyping) setCharIndex(node.text.length);
  };

  const navigate = (next: string) => {
    setNodeId(next);
    setCharIndex(next === "root" ? GUIDE_DIALOGUE.root.text.length : 0);
    setAnimKey((k) => k + 1);
  };

  return (
    <div className="guide-container">
      <div className="guide-portrait-wrap">
        <img
          className="guide-portrait"
          src={`${BASE}images/characters/character2.png`}
          alt="Scarlett"
        />
        <div className="guide-name">Scarlett</div>
      </div>

      <div className="guide-speech" onClick={skip}>
        <div className="guide-text">
          {node.text.slice(0, charIndex)}
          {isTyping && <span className="guide-cursor">{"\u258C"}</span>}
        </div>
        {isTyping && <div className="guide-skip-hint">click to skip</div>}
      </div>

      {!isTyping && (
        <div className="guide-responses">
          {node.responses.map((r, i) => (
            <button
              key={`${animKey}-${i}`}
              className="guide-response-btn"
              style={{ animationDelay: `${i * 0.08}s` }}
              onClick={() => navigate(r.next)}
            >
              <span className="guide-response-num">{i + 1}</span>
              {r.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
