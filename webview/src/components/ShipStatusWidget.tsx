import { useState } from "react";
import type { PortState } from "../types";
import { BASE, inventoryIcon } from "../utils/helpers";

const INVENTORY_CHIPS = ["Wood", "Iron", "Fish", "Tea", "Trophy"] as const;
const STATUS_ANVIL_ICON = `${BASE}icons/flat/anvil.svg`;
const STATUS_CANNON_ICON = `${BASE}icons/flat/cannon-shot.svg`;

function clampPct(value: number): number {
  return Math.max(0, Math.min(100, value));
}

function toRomanNumeral(value: number): string {
  if (value <= 0) return "I";
  const numerals: Array<{ v: number; s: string }> = [
    { v: 1000, s: "M" },
    { v: 900, s: "CM" },
    { v: 500, s: "D" },
    { v: 400, s: "CD" },
    { v: 100, s: "C" },
    { v: 90, s: "XC" },
    { v: 50, s: "L" },
    { v: 40, s: "XL" },
    { v: 10, s: "X" },
    { v: 9, s: "IX" },
    { v: 5, s: "V" },
    { v: 4, s: "IV" },
    { v: 1, s: "I" },
  ];

  let remaining = value;
  let output = "";
  for (const numeral of numerals) {
    while (remaining >= numeral.v) {
      output += numeral.s;
      remaining -= numeral.v;
    }
  }
  return output || "I";
}

function StatusPips({
  used,
  total,
  label,
  testId,
}: {
  used: number;
  total: number;
  label: string;
  testId?: string;
}) {
  return (
    <div
      className="ship-status-pips"
      role="img"
      aria-label={`${label}: ${used}/${total}`}
      data-testid={testId}
    >
      {Array.from({ length: total }).map((_, index) => (
        <span
          key={`${label}-${index}`}
          className={`ship-status-pip ${index < used ? "filled" : ""}`}
        />
      ))}
    </div>
  );
}

export function ShipStatusWidget({
  state,
  panelOpen,
}: {
  state: PortState | null;
  panelOpen: boolean;
}) {
  const [hoverTip, setHoverTip] = useState<"ring" | "cannon" | "burden" | null>(null);

  if (!state) return null;

  const shipLevel = state.shipTier + 1;
  const shipLevelRoman = toRomanNumeral(shipLevel);
  const healthPct = state.maxHealth > 0 ? clampPct((state.health / state.maxHealth) * 100) : 0;
  const hullCircumference = 2 * Math.PI * 32;
  const hullOffset = hullCircumference * (1 - healthPct / 100);
  const hullHue = Math.round((healthPct / 100) * 120);

  const cannonballs = state.inventory.CannonBall ?? 0;
  const gold = state.inventory.Coin ?? 0;
  const cooldownRemaining = Math.max(0, state.cannonCooldownRemaining ?? 0);
  const isCooling = !state.cannonReady && cooldownRemaining > 0;

  const componentUsed = state.ownedComponents.filter((item) => item.isEquipped).length;
  const componentTotal = Math.max(1, state.componentCapacity);
  const crewUsed = state.crew.hiredCharacterIds.length;
  const crewTotal = Math.max(1, state.crew.crewSlots);

  // Cargo capacity tracks physical goods only; gold does not consume hold space.
  const cargoUsed = Object.entries(state.inventory)
    .filter(([key]) => key !== "Coin")
    .reduce((sum, [, amount]) => sum + amount, 0);
  const cargoCap = Math.max(0, state.stats.ShipCapacity ?? 0);
  const cargoPct = cargoCap > 0 ? clampPct((cargoUsed / cargoCap) * 100) : 0;
  const cargoRingCircumference = 2 * Math.PI * 24;
  const cargoRingOffset = cargoRingCircumference * (1 - cargoPct / 100);

  return (
    <aside
      className={`ship-status-widget ${panelOpen ? "panel-open" : ""} ${healthPct <= 35 ? "danger" : ""}`}
      data-testid="ship-status-widget"
      aria-label="Ship status widget"
    >
      {hoverTip && (
        <div className="ship-status-hover-tooltip" role="tooltip" aria-live="polite">
          <div className="ship-status-hover-bar">
            <div className="ship-status-hover-title">
              {hoverTip === "ring"
                ? "Captain's Readout"
                : hoverTip === "cannon"
                  ? "Cannons"
                  : "Cargo Burden"}
            </div>
          </div>
          <div className="ship-status-hover-body">
            {hoverTip === "ring" ? (
              <>
                <section className="ship-status-hover-section">
                  <div className="ship-status-hover-section-title">Vitals</div>
                  <div className="ship-status-hover-row">
                    <span className="ship-status-hover-label">Hull Health</span>
                    <strong>{state.health} / {state.maxHealth}</strong>
                  </div>
                  <div className="ship-status-hover-row">
                    <span className="ship-status-hover-label">Cargo Capacity</span>
                    <strong>{cargoUsed} / {cargoCap || "∞"}</strong>
                  </div>
                </section>

                <section className="ship-status-hover-section">
                  <div className="ship-status-hover-section-title">Crew & Gear</div>
                  <div className="ship-status-hover-row">
                    <span className="ship-status-hover-label">Crew Berths</span>
                    <StatusPips
                      used={crewUsed}
                      total={crewTotal}
                      label="Crew slots"
                      testId="ship-status-crew-slots"
                    />
                  </div>
                  <div className="ship-status-hover-row">
                    <span className="ship-status-hover-label">Component Slots</span>
                    <StatusPips
                      used={componentUsed}
                      total={componentTotal}
                      label="Component slots"
                      testId="ship-status-component-slots"
                    />
                  </div>
                </section>

                <section className="ship-status-hover-section">
                  <div className="ship-status-hover-section-title">Status</div>
                  <div className="ship-status-hover-row">
                    <span className="ship-status-hover-label">Cannons</span>
                    <strong>{isCooling ? `Loading (${cooldownRemaining.toFixed(1)}s)` : "Ready to Fire"}</strong>
                  </div>
                  <div className="ship-status-hover-row">
                    <span className="ship-status-hover-label">Cargo Burden</span>
                    <strong>{state.isOverburdened ? "Burdened (handling reduced)" : "Full Speed"}</strong>
                  </div>
                </section>

                <section className="ship-status-hover-section">
                  <div className="ship-status-hover-section-title">Inventory Hold</div>
                  <div className="ship-status-hover-inventory">
                    {INVENTORY_CHIPS.map((itemType) => (
                      <div className="ship-status-hover-item" key={itemType}>
                        <img src={inventoryIcon(itemType)} alt={itemType} />
                        <span>{itemType}</span>
                        <strong>{state.inventory[itemType] ?? 0}</strong>
                      </div>
                    ))}
                  </div>
                </section>
              </>
            ) : hoverTip === "cannon" ? (
              <section className="ship-status-hover-section">
                <div className="ship-status-hover-section-title">Combat Detail</div>
                <div className="ship-status-hover-row">
                  <span className="ship-status-hover-label">State</span>
                  <strong>{isCooling ? `Loading ${cooldownRemaining.toFixed(1)}s` : "Ready"}</strong>
                </div>
                <div className="ship-status-hover-row">
                  <span className="ship-status-hover-label">Cannonballs</span>
                  <strong>{cannonballs}</strong>
                </div>
              </section>
            ) : (
              <section className="ship-status-hover-section">
                <div className="ship-status-hover-section-title">Load Detail</div>
                <div className="ship-status-hover-row">
                  <span className="ship-status-hover-label">State</span>
                  <strong>{state.isOverburdened ? "Burdened" : "Full Speed"}</strong>
                </div>
                <div className="ship-status-hover-row">
                  <span className="ship-status-hover-label">Effect</span>
                  <strong>{state.isOverburdened ? "Reduced handling" : "Normal handling"}</strong>
                </div>
              </section>
            )}
          </div>
        </div>
      )}

      <div className="ship-status-map-overlay" aria-hidden="true" />

      <div className="ship-status-grid">
        <section className="ship-status-card ship-status-hull">
          <div className="ship-status-hull-gauge">
            <div className="ship-status-hull-main">
              <div className="ship-status-ring-wrap">
                <svg viewBox="0 0 80 80" className="ship-status-ring" aria-hidden="true">
                  <circle className="ring-track" cx="40" cy="40" r="32" />
                  <circle
                    className="ring-fill"
                    cx="40"
                    cy="40"
                    r="32"
                    style={{
                      strokeDasharray: `${hullCircumference}`,
                      strokeDashoffset: `${hullOffset}`,
                      stroke: `hsl(${hullHue}, 72%, 52%)`,
                    }}
                  />
                  <circle className="cargo-ring-track" cx="40" cy="40" r="24" />
                  <circle
                    className="cargo-ring-fill"
                    cx="40"
                    cy="40"
                    r="24"
                    style={{
                      strokeDasharray: `${cargoRingCircumference}`,
                      strokeDashoffset: `${cargoRingOffset}`,
                    }}
                  />
                </svg>
                <span className="ship-status-hull-level" aria-hidden="true">
                  {shipLevelRoman}
                </span>
                <button
                  type="button"
                  className="ship-status-ring-hover-target"
                  aria-label="Show ship indicator details"
                  onMouseEnter={() => setHoverTip("ring")}
                  onMouseLeave={() => setHoverTip(null)}
                />
              </div>
              <div className="ship-status-hull-text">
                <span className="ship-status-player" data-testid="ship-status-player">
                  {state.playerName || "Unknown Captain"}
                </span>
                <div className="ship-status-resource-row" data-testid="ship-status-cannonballs">
                  <span className="ship-status-resource-pill">
                    <img src={inventoryIcon("CannonBall")} alt="Cannonballs" />
                    <strong>{cannonballs}</strong>
                  </span>
                  <span className="ship-status-resource-pill">
                    <img src={inventoryIcon("Coin")} alt="Gold" />
                    <strong>{gold}</strong>
                  </span>
                </div>
                <div className="ship-status-badge-row">
                  <div
                    className={`ship-status-icon-pill ${isCooling ? "loading" : "ready"}`}
                    data-testid="ship-status-cannon-state"
                    aria-label={isCooling ? `Loading ${cooldownRemaining.toFixed(1)} seconds` : "Ready"}
                    onMouseEnter={() => setHoverTip("cannon")}
                    onMouseLeave={() => setHoverTip(null)}
                  >
                    <img src={STATUS_CANNON_ICON} alt="" />
                  </div>
                  <div
                    className={`ship-status-icon-pill ${state.isOverburdened ? "over" : "stable"}`}
                    data-testid="ship-status-overburdened"
                    aria-label={state.isOverburdened ? "Burdened" : "Full Speed"}
                    onMouseEnter={() => setHoverTip("burden")}
                    onMouseLeave={() => setHoverTip(null)}
                  >
                    <img src={STATUS_ANVIL_ICON} alt="" />
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

      </div>
    </aside>
  );
}
