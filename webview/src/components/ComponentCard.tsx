import { sendIpc, sendIpcAndWait } from "../utils/ipc";
import { iconUrl, inventoryIcon, formatStatName, fmt } from "../utils/helpers";
import type { ComponentData, ShopItem } from "../types";

// Props accepted by ComponentCard. Used in the Shipyard tab for equipped,
// owned, and for-sale component listings.
export interface ComponentCardProps {
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

// A card that shows a ship component with its icon, description,
// stat changes, cost, and action buttons (Buy / Equip / Unequip).
export function ComponentCard({
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
  // Calculate whether we can "Buy All & Build" — buy missing materials
  // from the shop and then purchase the component in one click.
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
            onClick={async () => {
              if (buyAndBuildInfo.needed.length > 0) {
                await sendIpcAndWait({ action: "buy_items", items: buyAndBuildInfo.needed });
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
