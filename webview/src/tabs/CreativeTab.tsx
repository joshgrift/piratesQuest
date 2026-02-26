import { useState } from "react";
import { sendIpc } from "../utils/ipc";
import { iconUrl, inventoryIcon } from "../utils/helpers";
import type { PortState } from "../types";

const ITEM_TYPES = ["Wood", "Iron", "Fish", "Tea", "Coin", "CannonBall", "Trophy"];
const STEP_AMOUNTS = [1, 50, 100] as const;

export function CreativeTab({ state }: { state: PortState }) {
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

      <div className="section-title">Ship Tier</div>
      <div className="card">
        <div className="creative-vault-buttons">
          {(state.shipTiers ?? []).map((tier, i) => (
            <button
              key={i}
              className={`creative-preset-btn ${state.shipTier === i ? "creative-preset-active" : ""}`}
              onClick={() => sendIpc({ action: "set_ship_tier", tier: i })}
            >
              {tier.name}
            </button>
          ))}
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

      <div className="section-sep" />

      <div className="section-title">Vault</div>
      <div className="card">
        {state.vault ? (
          <>
            <div className="creative-vault-info">
              <img
                className="creative-vault-icon"
                src={iconUrl("inventory", "vault.png")}
                alt="Vault"
              />
              <div>
                <div className="creative-vault-detail">
                  <strong>{state.vault.portName}</strong> · Level {state.vault.level}
                </div>
                <div className="creative-vault-detail-sub">
                  {Object.values(state.vault.items).reduce((s, n) => s + n, 0)} items stored
                </div>
              </div>
            </div>
            <div className="creative-vault-buttons">
              {([1, 2, 3, 4, 5] as const).map((lvl) => (
                <button
                  key={lvl}
                  className={`creative-preset-btn ${state.vault?.level === lvl ? "creative-preset-active" : ""}`}
                  onClick={() =>
                    sendIpc({ action: "set_vault", portName: state.vault!.portName, level: lvl })
                  }
                >
                  Lvl {lvl}
                </button>
              ))}
            </div>
            <button
              className="creative-danger-btn"
              onClick={() => sendIpc({ action: "delete_vault" })}
            >
              Delete Vault
            </button>
          </>
        ) : (
          <>
            <div className="creative-components-info">
              <span className="empty-state">No vault built</span>
            </div>
            <div className="creative-vault-buttons">
              <button
                className="creative-preset-btn"
                onClick={() =>
                  sendIpc({ action: "set_vault", portName: state.portName, level: 1 })
                }
              >
                Build Here (Lvl 1)
              </button>
              <button
                className="creative-preset-btn"
                onClick={() =>
                  sendIpc({ action: "set_vault", portName: state.portName, level: 5 })
                }
              >
                Build Here (Lvl 5)
              </button>
            </div>
          </>
        )}
      </div>

      <div className="section-sep" />

      <DebugStatePanel state={state} />
    </>
  );
}

// ── Debug State Panel ─────────────────────────────────────────────────

function buildServerState(state: PortState) {
  return {
    Inventory: { ...state.inventory },
    Components: state.ownedComponents.map((c) => ({
      Name: c.name,
      IsEquipped: c.isEquipped,
    })),
    Health: state.health,
    Vault: state.vault
      ? {
          PortName: state.vault.portName,
          Level: state.vault.level,
          Items: { ...state.vault.items },
        }
      : null,
  };
}

function DebugStatePanel({ state }: { state: PortState }) {
  const [open, setOpen] = useState(false);
  const [copied, setCopied] = useState(false);
  const serverState = buildServerState(state);
  const json = JSON.stringify(serverState, null, 2);

  const copyToClipboard = () => {
    navigator.clipboard.writeText(json).then(() => {
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    });
  };

  return (
    <div className="creative-debug-section">
      <button
        className="creative-debug-toggle"
        onClick={() => setOpen((o) => !o)}
      >
        {open ? "▾" : "▸"} Server State
      </button>
      {open && (
        <>
          <div className="creative-debug-toolbar">
            <button className="creative-debug-copy" onClick={copyToClipboard}>
              {copied ? "Copied!" : "Copy"}
            </button>
          </div>
          <textarea
            className="creative-debug-textarea"
            readOnly
            value={json}
            rows={18}
          />
        </>
      )}
    </div>
  );
}
