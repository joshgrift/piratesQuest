import { sendIpc, sendIpcAndWait } from "../utils/ipc";
import { inventoryIcon, formatStatName, fmt } from "../utils/helpers";
import { ComponentCard } from "../components/ComponentCard";
import { ShipUpgradeCard } from "../components/ShipUpgradeCard";
import type { PortState, ComponentData } from "../types";
import { getCrewImpact, getEquippedComponentImpact } from "../utils/shipBonuses";

export function ShipyardTab({
  state,
  isInPort = true,
  showForSale = true,
  showShipUpgrade = true,
  showHealth = true,
  showStats = true,
  showComponents = true,
  showPortLocked = true,
}: {
  state: PortState;
  isInPort?: boolean;
  showForSale?: boolean;
  showShipUpgrade?: boolean;
  showHealth?: boolean;
  showStats?: boolean;
  showComponents?: boolean;
  showPortLocked?: boolean;
}) {
  const buyUnlocked = state.quests.unlockedFeatures.includes("BuyGoods");
  const componentsUnlocked = state.quests.unlockedFeatures.includes("ShipyardComponents");
  const shipTiersUnlocked = state.quests.unlockedFeatures.includes("ShipTierUpgrades");
  const equippedCount = state.ownedComponents.filter((c) => c.isEquipped).length;
  const canEquipMore = equippedCount < state.componentCapacity;
  const lockReason = "Port required: dock before changing loadout or buying upgrades.";

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

  const componentImpact = getEquippedComponentImpact(equipped);
  const crewImpact = getCrewImpact(state);

  const canAfford = (cost: Record<string, number>): boolean => {
    return Object.entries(cost).every(
      ([type, amount]) => (state.inventory[type] ?? 0) >= amount
    );
  };

  const healthNeeded = state.maxHealth - state.health;
  const woodAvail = state.inventory["Wood"] ?? 0;
  const fishAvail = state.inventory["Fish"] ?? 0;
  // Pull repair costs from C# payload so UI never hardcodes gameplay values.
  const woodPerHp = state.costs.repair.woodPerHp;
  const fishPerHp = state.costs.repair.fishPerHp;
  const maxHeal = Math.min(healthNeeded, Math.floor(woodAvail / woodPerHp), Math.floor(fishAvail / fishPerHp));
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
        Math.max(0, h * woodPerHp - woodAvail) * woodShop.buyPrice +
        Math.max(0, h * fishPerHp - fishAvail) * fishShop.buyPrice;
      if (goldNeeded > coinsAvail) break;
      maxBuyHeal = h;
    }
  }
  const woodToBuy = Math.max(0, maxBuyHeal * woodPerHp - woodAvail);
  const fishToBuy = Math.max(0, maxBuyHeal * fishPerHp - fishAvail);
  const buyHealGoldCost = woodShop && fishShop
    ? woodToBuy * woodShop.buyPrice + fishToBuy * fishShop.buyPrice
    : null;

  return (
    <>
      {showHealth && (
        <>
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
                      disabled={!isInPort || maxHeal <= 0}
                      onClick={() => sendIpc({ action: "heal" })}
                      title={!isInPort ? "Port required: repair hull while docked." : undefined}
                    >
                      Repair Hull
                    </button>
                    <div className="repair-costs">
                      <span className={`cost-chip ${woodAvail < healthNeeded * woodPerHp ? "chip-short" : "chip-wood"}`}>
                        <img src={inventoryIcon("Wood")} alt="Wood" className="chip-icon" />{healthNeeded * woodPerHp}
                      </span>
                      <span className={`cost-chip ${fishAvail < healthNeeded * fishPerHp ? "chip-short" : "chip-fish"}`}>
                        <img src={inventoryIcon("Fish")} alt="Fish" className="chip-icon" />{healthNeeded * fishPerHp}
                      </span>
                      <span className="repair-hp">+{maxHeal} HP</span>
                    </div>
                  </div>
                  {buyHealGoldCost !== null && (
                    <div className="repair-row">
                      <button
                        className="repair-btn repair-btn-gold"
                        disabled={!isInPort || maxBuyHeal <= 0 || !buyUnlocked}
                        onClick={async () => {
                          const items: { type: string; quantity: number }[] = [];
                          if (woodToBuy > 0) items.push({ type: "Wood", quantity: woodToBuy });
                          if (fishToBuy > 0) items.push({ type: "Fish", quantity: fishToBuy });
                          if (items.length > 0) {
                            await sendIpcAndWait({ action: "buy_items", items });
                          }
                          sendIpc({ action: "heal" });
                        }}
                        title={
                          !isInPort
                            ? "Port required: buy and repair while docked."
                            : !buyUnlocked
                              ? "Complete Harvest For Someone to unlock buying goods."
                              : undefined
                        }
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
                  <span className={woodAvail < healthNeeded * woodPerHp ? "resource-short" : ""}>
                    <img src={inventoryIcon("Wood")} alt="Wood" className="chip-icon" />{woodAvail}
                  </span>
                  <span className={fishAvail < healthNeeded * fishPerHp ? "resource-short" : ""}>
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
        </>
      )}

      {/* Ship Upgrade */}
      {showShipUpgrade && state.shipTiers && state.shipTiers.length > 0 && (
        <>
          {shipTiersUnlocked && (
            <>
              <div className="section-title">Ship Class</div>
              <ShipUpgradeCard
                currentTier={state.shipTier ?? 0}
                tiers={state.shipTiers}
                inventory={state.inventory}
                canUpgrade={isInPort}
              />
            </>
          )}
        </>
      )}

      {showComponents && componentsUnlocked && (
        <>
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
                disableAllActions={!isInPort || !componentsUnlocked}
                lockReason={lockReason}
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
                  disableAllActions={!isInPort || !componentsUnlocked}
                  lockReason={lockReason}
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
          {showForSale && forSale.length > 0 && (
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
                  disableAllActions={!isInPort}
                  lockReason={lockReason}
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
      )}

      {showStats && (
        <>
          <div className="section-title">Ship Stats</div>
          <div className="card mb-12">
            <div className="ship-stat-list">
              {Object.entries(state.stats).map(([stat, value]) => {
                const componentBonus = componentImpact[stat];
                const crewBonus = crewImpact[stat] ?? 0;
                const impactNotes: string[] = [];
                const additiveTerms: string[] = [];
                const componentAdditiveBonus = componentBonus?.additive ?? 0;
                const additiveBonus = crewBonus + componentAdditiveBonus;
                const multiplicativeBonus = componentBonus?.multiplicative ?? 1;
                const isNumericStat = typeof value === "number";
                const baseValue = isNumericStat
                  ? (value / multiplicativeBonus) - additiveBonus
                  : null;
                const roundedBaseValue = baseValue === null ? null : fmt(baseValue);
                const roundedFinalValue = isNumericStat ? fmt(value) : value;

                if (crewBonus !== 0) {
                  impactNotes.push(`Crew +${fmt(crewBonus)}`);
                  additiveTerms.push(`+ ${fmt(crewBonus)}`);
                }

                if (componentAdditiveBonus !== 0) {
                  impactNotes.push(`Components +${fmt(componentAdditiveBonus)}`);
                  additiveTerms.push(`+ ${fmt(componentAdditiveBonus)}`);
                }

                if (componentBonus && componentBonus.multiplicative !== 1) {
                  impactNotes.push(`Components +${Math.round((componentBonus.multiplicative - 1) * 100)}%`);
                }

                return (
                  <div className="ship-stat-list-row" key={stat}>
                    <div className="ship-stat-list-copy">
                      <span className="stat-label">{formatStatName(stat)}</span>
                      {impactNotes.length > 0 && (
                        <span className="ship-stat-impact">
                          Includes {impactNotes.join(" • ")}
                        </span>
                      )}
                    </div>
                    <span className="ship-stat-formula">
                      {isNumericStat && impactNotes.length > 0 ? (
                        <>
                          <span className="ship-stat-formula-line">
                            <span className="ship-stat-formula-base">{roundedBaseValue}</span>
                            {additiveTerms.map((term) => (
                              <span key={`${stat}-${term}`} className="ship-stat-formula-part">{term}</span>
                            ))}
                            {multiplicativeBonus !== 1 && additiveTerms.length > 0 && (
                              <span className="ship-stat-formula-part"> </span>
                            )}
                            {multiplicativeBonus !== 1 && (
                              <span className="ship-stat-formula-part">x {fmt(multiplicativeBonus)}</span>
                            )}
                          </span>
                          <span className="ship-stat-formula-result">
                            <span className="ship-stat-formula-equals">=</span>
                            <span className="ship-stat-formula-final">{roundedFinalValue}</span>
                          </span>
                        </>
                      ) : (
                        <span className="stat-value">{roundedFinalValue}</span>
                      )}
                    </span>
                  </div>
                );
              })}
            </div>
          </div>
        </>
      )}

      {showPortLocked && !isInPort && (
        <div className="card port-locked-card">
          <div className="section-title">Port Services Locked</div>
          <div className="empty-state">
            You are at sea. Dock to repair, buy, equip, unequip, or upgrade your ship class.
          </div>
        </div>
      )}
    </>
  );
}
