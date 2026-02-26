import { sendIpc, sendIpcAndWait } from "../utils/ipc";
import { inventoryIcon, formatStatName, fmt } from "../utils/helpers";
import { ComponentCard } from "../components/ComponentCard";
import { ShipUpgradeCard } from "../components/ShipUpgradeCard";
import type { PortState, ComponentData } from "../types";

export function ShipyardTab({ state }: { state: PortState }) {
  const equippedCount = state.ownedComponents.filter((c) => c.isEquipped).length;
  const canEquipMore = equippedCount < state.componentCapacity;

  const countBy = (ocs: { name: string }[]) => {
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
      const bonus = statBonuses[sc.stat]!;
      if (sc.modifier === "Additive") {
        bonus.add += sc.value * count;
      } else {
        bonus.multi *= Math.pow(sc.value, count);
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
  const healthHue = Math.round((healthPct / 100) * 120);

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
                    onClick={async () => {
                      const items: { type: string; quantity: number }[] = [];
                      if (woodToBuy > 0) items.push({ type: "Wood", quantity: woodToBuy });
                      if (fishToBuy > 0) items.push({ type: "Fish", quantity: fishToBuy });
                      if (items.length > 0) {
                        await sendIpcAndWait({ action: "buy_items", items });
                      }
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

      {/* Ship Upgrade */}
      {state.shipTiers && state.shipTiers.length > 0 && (
        <>
          <div className="section-title">Ship Class</div>
          <ShipUpgradeCard
            currentTier={state.shipTier ?? 0}
            tiers={state.shipTiers}
            inventory={state.inventory}
          />
        </>
      )}

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
