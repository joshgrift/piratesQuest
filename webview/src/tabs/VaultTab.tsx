import { useState, useCallback } from "react";
import { sendIpc } from "../utils/ipc";
import { iconUrl, inventoryIcon } from "../utils/helpers";
import type { PortState } from "../types";
const VAULT_ITEM_TYPES = ["Wood", "Iron", "Fish", "Tea", "CannonBall", "Trophy"];

export function VaultTab({ state }: { state: PortState }) {
  const vault = state.vault;
  const [quantities, setQuantities] = useState<Record<string, number>>({});
  const [mode, setMode] = useState<"deposit" | "withdraw">("deposit");
  const [flashConfirm, setFlashConfirm] = useState(false);

  const setQty = useCallback((itemType: string, qty: number) => {
    setQuantities((prev) => ({ ...prev, [itemType]: Math.max(0, qty) }));
  }, []);
  const getQty = (itemType: string): number => quantities[itemType] ?? 0;

  const handleModeChange = (newMode: "deposit" | "withdraw") => {
    setMode(newMode);
    setQuantities({});
  };

  // No vault yet — show build prompt
  if (!vault) {
    const buildCost = state.costs.vaultBuild;
    const canAffordBuild = Object.entries(buildCost).every(
      ([type, amount]) => (state.inventory[type] ?? 0) >= amount
    );
    return (
      <div className="vault-build-container">
        <img className="vault-build-icon" src={iconUrl("inventory", "vault.png")} alt="Vault" />
        <div className="vault-build-title">Build a Vault</div>
        <div className="vault-build-desc">
          Construct a vault at this port to safely store items and gold.
          Your vault stays here permanently — visit anytime to deposit
          or withdraw. Items in the vault survive death!
        </div>
        <div className="vault-build-cost">
          {Object.entries(buildCost).map(([type, amount]) => {
            const owned = state.inventory[type] ?? 0;
            return (
              <span
                key={type}
                className={`cost-chip ${owned < amount ? "chip-short" : type === "Coin" ? "chip-gold" : type === "Wood" ? "chip-wood" : "chip-iron"}`}
              >
                <img src={inventoryIcon(type)} alt={type} className="chip-icon" />
                {amount}
              </span>
            );
          })}
        </div>
        <button
          className="vault-build-btn"
          disabled={!canAffordBuild}
          onClick={() => sendIpc({ action: "build_vault" })}
        >
          Build Vault
        </button>
      </div>
    );
  }

  // Vault exists but player is at a different port
  if (!vault.isHere) {
    return (
      <div className="vault-away-container">
        <img className="vault-away-icon" src={iconUrl("inventory", "vault.png")} alt="Vault" />
        <div className="vault-away-title">Vault Elsewhere</div>
        <div className="vault-away-desc">
          Your vault is at <strong>{vault.portName}</strong>.
          Sail there to access your stored items.
        </div>
        <div className="vault-away-summary">
          <span>Level {vault.level}</span>
          <span>·</span>
          <span>{Object.values(vault.items).reduce((s, n) => s + n, 0)} items stored</span>
        </div>
      </div>
    );
  }

  // Full vault UI — player is at their vault's port
  const vaultNonGold = Object.entries(vault.items)
    .filter(([k]) => k !== "Coin")
    .reduce((s, [, n]) => s + n, 0);
  const vaultGold = vault.items["Coin"] ?? 0;
  const itemPct = vault.itemCapacity > 0 ? (vaultNonGold / vault.itemCapacity) * 100 : 0;
  const goldPct = vault.goldCapacity > 0 ? (vaultGold / vault.goldCapacity) * 100 : 0;

  // Upgrade cost is sent by C# so UI and gameplay rules cannot drift.
  const upgradeCost = state.costs.vaultUpgrade;
  const canAffordUpgrade = upgradeCost
    ? Object.entries(upgradeCost).every(
        ([type, amount]) =>
          (state.inventory[type] ?? 0) + (vault.items[type] ?? 0) >= amount
      )
    : false;

  const itemTypes = mode === "deposit"
    ? VAULT_ITEM_TYPES.filter((t) => (state.inventory[t] ?? 0) > 0)
    : VAULT_ITEM_TYPES.filter((t) => (vault.items[t] ?? 0) > 0);

  const goldKey = "Coin";
  const goldQty = getQty(goldKey);
  const playerGold = state.inventory["Coin"] ?? 0;

  const maxGoldDeposit = Math.max(0, vault.goldCapacity - vaultGold);
  const maxGoldWithdraw = vaultGold;

  const hasSelection = [...itemTypes, goldKey].some((t) => getQty(t) > 0);

  const handleConfirm = () => {
    const allTypes = [...VAULT_ITEM_TYPES, goldKey];
    const selected = allTypes
      .filter((t) => getQty(t) > 0)
      .map((t) => ({ type: t, quantity: getQty(t) }));
    if (selected.length === 0) return;

    sendIpc({
      action: mode === "deposit" ? "vault_deposit" : "vault_withdraw",
      items: selected,
    });

    setQuantities({});
    setFlashConfirm(true);
    setTimeout(() => setFlashConfirm(false), 500);
  };

  return (
    <>
      {/* Vault Header */}
      <div className="vault-header">
        <img className="vault-header-icon" src={iconUrl("inventory", "vault.png")} alt="Vault" />
        <div className="vault-header-info">
          <div className="vault-level-badge">Level {vault.level}</div>
          <div className="vault-port-label">{vault.portName}</div>
        </div>
      </div>

      {/* Capacity Bars */}
      <div className="section-title">Storage</div>
      <div className="card vault-capacity-card">
        <div className="vault-capacity-row">
          <span className="vault-capacity-label">Items</span>
          <div className="vault-capacity-bar">
            <div
              className="vault-capacity-fill vault-fill-items"
              style={{ width: `${Math.min(100, itemPct)}%` }}
            />
          </div>
          <span className="vault-capacity-text">
            {vaultNonGold} / {vault.itemCapacity}
          </span>
        </div>
        <div className="vault-capacity-row">
          <span className="vault-capacity-label">
            <img src={inventoryIcon("Coin")} alt="Gold" className="chip-icon" />
          </span>
          <div className="vault-capacity-bar">
            <div
              className="vault-capacity-fill vault-fill-gold"
              style={{ width: `${Math.min(100, goldPct)}%` }}
            />
          </div>
          <span className="vault-capacity-text">
            {vaultGold} / {vault.goldCapacity}
          </span>
        </div>
      </div>

      {/* Upgrade */}
      {upgradeCost && (
        <>
          <div className="section-title">Upgrade to Level {vault.level + 1}</div>
          <div className="card vault-upgrade-card">
            <div className="vault-upgrade-cost">
              {Object.entries(upgradeCost).map(([type, amount]) => {
                const inInventory = state.inventory[type] ?? 0;
                const inVault = vault.items[type] ?? 0;
                const total = inInventory + inVault;
                return (
                  <span
                    key={type}
                    className={`cost-chip ${total < amount ? "chip-short" : type === "Coin" ? "chip-gold" : type === "Wood" ? "chip-wood" : "chip-iron"}`}
                    title={`Inventory: ${inInventory} + Vault: ${inVault}`}
                  >
                    <img src={inventoryIcon(type)} alt={type} className="chip-icon" />
                    {amount}
                  </span>
                );
              })}
            </div>
            <button
              className="vault-upgrade-btn"
              disabled={!canAffordUpgrade}
              onClick={() => sendIpc({ action: "upgrade_vault" })}
            >
              Upgrade Vault
            </button>
          </div>
        </>
      )}

      <div className="section-sep" />

      {/* Deposit / Withdraw */}
      <div className="mode-toggle">
        <button
          className={`mode-btn ${mode === "deposit" ? "active" : ""}`}
          onClick={() => handleModeChange("deposit")}
        >
          Deposit
        </button>
        <button
          className={`mode-btn ${mode === "withdraw" ? "active" : ""}`}
          onClick={() => handleModeChange("withdraw")}
        >
          Withdraw
        </button>
      </div>

      {/* Gold row */}
      <div className="shop-item vault-gold-row">
        <img className="shop-item-icon" src={inventoryIcon("Coin")} alt="Gold" />
        <div className="shop-item-info">
          <div className="shop-item-name">Gold</div>
          <div className="shop-item-price">
            {mode === "deposit"
              ? `Available: ${playerGold}`
              : `In vault: ${vaultGold}`}
          </div>
        </div>
        <div className="qty-stepper">
          <button
            className="qty-btn qty-btn-reset"
            disabled={goldQty <= 0}
            onClick={() => setQty(goldKey, 0)}
          >
            ×
          </button>
          <button
            className="qty-btn"
            disabled={goldQty <= 0}
            onClick={() => setQty(goldKey, goldQty - 1)}
          >
            -
          </button>
          <div className="qty-value">{goldQty}</div>
          <button
            className="qty-btn"
            disabled={
              mode === "deposit"
                ? goldQty >= Math.min(playerGold, maxGoldDeposit)
                : goldQty >= maxGoldWithdraw
            }
            onClick={() => setQty(goldKey, goldQty + 1)}
          >
            +
          </button>
          {([50, 100] as const).map((n) => {
            const max = mode === "deposit"
              ? Math.min(playerGold, maxGoldDeposit)
              : maxGoldWithdraw;
            return (
              <button
                key={n}
                className="qty-btn"
                disabled={goldQty + n > max}
                onClick={() => setQty(goldKey, Math.min(max, goldQty + n))}
              >
                +{n}
              </button>
            );
          })}
        </div>
      </div>

      {/* Item rows */}
      {itemTypes.length === 0 ? (
        <div className="empty-state">
          {mode === "deposit"
            ? "No items to deposit"
            : "Vault is empty"}
        </div>
      ) : (
        itemTypes.map((type) => {
          const qty = getQty(type);
          const playerAmt = state.inventory[type] ?? 0;
          const vaultAmt = vault.items[type] ?? 0;

          const cartItems = VAULT_ITEM_TYPES.reduce(
            (s, t) => s + (t === type ? 0 : getQty(t)),
            0
          );
          const maxDeposit = Math.min(
            playerAmt,
            Math.max(0, vault.itemCapacity - vaultNonGold - cartItems)
          );
          const maxWithdraw = vaultAmt;
          const max = mode === "deposit" ? maxDeposit : maxWithdraw;

          return (
            <div className="shop-item" key={type}>
              <img className="shop-item-icon" src={inventoryIcon(type)} alt={type} />
              <div className="shop-item-info">
                <div className="shop-item-name">{type}</div>
                <div className="shop-item-price">
                  {mode === "deposit"
                    ? `Available: ${playerAmt}`
                    : `In vault: ${vaultAmt}`}
                </div>
              </div>
              <div className="qty-stepper">
                <button
                  className="qty-btn qty-btn-reset"
                  disabled={qty <= 0}
                  onClick={() => setQty(type, 0)}
                >
                  ×
                </button>
                <button
                  className="qty-btn"
                  disabled={qty <= 0}
                  onClick={() => setQty(type, qty - 1)}
                >
                  -
                </button>
                <div className="qty-value">{qty}</div>
                <button
                  className="qty-btn"
                  disabled={qty >= max}
                  onClick={() => setQty(type, qty + 1)}
                >
                  +
                </button>
                {([5, 50] as const).map((n) => (
                  <button
                    key={n}
                    className="qty-btn"
                    disabled={qty + n > max}
                    onClick={() => setQty(type, Math.min(max, qty + n))}
                  >
                    +{n}
                  </button>
                ))}
                <button
                  className="qty-btn qty-btn-all"
                  disabled={qty >= max}
                  onClick={() => setQty(type, max)}
                >
                  All
                </button>
              </div>
            </div>
          );
        })
      )}

      {/* Confirm Footer */}
      <div className={`trade-footer vault-footer ${flashConfirm ? "confirm-flash" : ""}`}>
        <div className="trade-total">
          {mode === "deposit" ? "Deposit" : "Withdraw"}
        </div>
        <button
          className="confirm-btn"
          disabled={!hasSelection}
          onClick={handleConfirm}
        >
          Confirm
        </button>
      </div>
    </>
  );
}
