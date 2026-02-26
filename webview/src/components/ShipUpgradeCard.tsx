import { sendIpc } from "../utils/ipc";
import { inventoryIcon } from "../utils/helpers";
import type { ShipTierData } from "../types";

// Shows the player's current ship tier and, if available, a preview
// of the next tier with its cost and an upgrade button.
export function ShipUpgradeCard({
  currentTier,
  tiers,
  inventory,
}: {
  currentTier: number;
  tiers: ShipTierData[];
  inventory: Record<string, number>;
}) {
  const currentData = tiers[currentTier];
  const nextTier = currentTier + 1;
  const nextData = nextTier < tiers.length ? tiers[nextTier] : undefined;

  const canAffordUpgrade =
    nextData != null &&
    Object.entries(nextData.cost).every(
      ([type, amount]) => (inventory[type] ?? 0) >= amount
    );

  return (
    <div className="card ship-upgrade-card">
      <div className="ship-upgrade-current">
        <div className="ship-upgrade-tier-badge">Tier {currentTier + 1}</div>
        <div className="ship-upgrade-info">
          <span className="ship-upgrade-name">{currentData?.name ?? "Unknown"}</span>
          <span className="ship-upgrade-slots">
            {currentData?.componentSlots ?? 4} component slots
          </span>
        </div>
      </div>

      {nextData == null ? (
        <div className="ship-upgrade-max">Maximum Ship Class Reached</div>
      ) : (
        <ShipUpgradeNextTier
          nextTier={nextTier}
          nextData={nextData}
          currentSlots={currentData?.componentSlots ?? 4}
          canAfford={canAffordUpgrade}
          inventory={inventory}
        />
      )}
    </div>
  );
}

// The "next tier" preview section shown inside ShipUpgradeCard when
// an upgrade is available.
function ShipUpgradeNextTier({
  nextTier,
  nextData,
  currentSlots,
  canAfford,
  inventory,
}: {
  nextTier: number;
  nextData: ShipTierData;
  currentSlots: number;
  canAfford: boolean;
  inventory: Record<string, number>;
}) {
  return (
    <div className="ship-upgrade-next">
      <div className="ship-upgrade-arrow">▼</div>
      <div className="ship-upgrade-preview">
        <div className="ship-upgrade-tier-badge tier-next">
          Tier {nextTier + 1}
        </div>
        <div className="ship-upgrade-info">
          <span className="ship-upgrade-name">{nextData.name}</span>
          <span className="ship-upgrade-desc">{nextData.description}</span>
          <span className="ship-upgrade-slots">
            {nextData.componentSlots} component slots
            <span className="ship-upgrade-slot-bonus">
              (+{nextData.componentSlots - currentSlots})
            </span>
          </span>
        </div>
      </div>
      <div className="ship-upgrade-cost">
        {Object.entries(nextData.cost).map(([type, amount]) => (
          <span
            key={type}
            className={`cost-chip ${
              (inventory[type] ?? 0) < amount ? "unaffordable" : `chip-${type.toLowerCase()}`
            }`}
          >
            <img
              src={inventoryIcon(type)}
              alt={type}
              className="chip-icon"
            />
            {amount}
          </span>
        ))}
      </div>
      <button
        className="ship-upgrade-btn"
        disabled={!canAfford}
        onClick={() => sendIpc({ action: "upgrade_ship" })}
      >
        Upgrade to {nextData.name}
      </button>
    </div>
  );
}
