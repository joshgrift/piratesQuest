import { useState, useEffect, useRef } from "react";
import "./App.css";
import type { PortState } from "./types";
import { sendIpc } from "./utils/ipc";
import { useInputCapture } from "./hooks/useInputCapture";
import { inventoryIcon } from "./utils/helpers";
import { MarketTab } from "./tabs/MarketTab";
import { ShipyardTab } from "./tabs/ShipyardTab";
import { VaultTab } from "./tabs/VaultTab";
import { GuideTab } from "./tabs/GuideTab";
import { CreativeTab } from "./tabs/CreativeTab";
import { TavernTab } from "./tabs/TavernTab";
import { ShipCrewTab } from "./tabs/ShipCrewTab";
import { LeaderboardTab } from "./tabs/LeaderboardTab";

type ShipTab = "guide" | "leaderboard" | "ship_crew" | "ship_status";
type PortTab = "market" | "shipyard" | "tavern" | "vault" | "creative";
type Section = "ship" | "port";
type HireOutcome = "hired" | "already_hired" | "slots_full" | "not_hireable";
const INVENTORY_ORDER = ["Coin", "Wood", "Fish", "Iron", "Tea", "CannonBall", "Trophy"] as const;

export default function App() {
  useInputCapture();

  const [portState, setPortState] = useState<PortState | null>(null);
  const [isPanelVisible, setIsPanelVisible] = useState(true);
  const [activeSection, setActiveSection] = useState<Section>("ship");
  const [activeShipTab, setActiveShipTab] = useState<ShipTab>("guide");
  const [activePortTab, setActivePortTab] = useState<PortTab>("market");
  const prevIsInPortRef = useRef<boolean | null>(null);

  useEffect(() => {
    window.openPort = (data: PortState) => {
      setPortState(data);
    };

    window.closePort = () => {
      // Keep the panel visible at all times. On close, just return
      // to ship defaults while preserving the latest state snapshot.
      setActiveSection("ship");
      setActiveShipTab("guide");
    };

    window.updateState = (data: PortState) => {
      setPortState(data);
    };

    sendIpc({ action: "ready" });
  }, []);

  useEffect(() => {
    const isInPort = !!portState?.isInPort;
    const prevIsInPort = prevIsInPortRef.current;
    prevIsInPortRef.current = isInPort;

    // Auto-open Port section only on sea -> port transition.
    if (isInPort && prevIsInPort !== true) {
      setActiveSection("port");
      return;
    }

    // If we leave port while viewing port-only pages, switch back to ship.
    if (!isInPort && activeSection === "port") {
      setActiveSection("ship");
    }
  }, [portState?.isInPort, activeSection]);

  const handleHireCharacter = (characterId: string): HireOutcome => {
    if (!portState) return "not_hireable";

    const character = portState?.tavern.characters.find((c) => c.id === characterId);
    if (!character?.hireable) return "not_hireable";

    const alreadyHired = portState.tavern.hiredCharacterIds.includes(characterId);
    if (alreadyHired) return "already_hired";

    if (portState.tavern.hiredCharacterIds.length >= portState.tavern.crewSlots) {
      return "slots_full";
    }

    sendIpc({ action: "hire_character", characterId });
    return "hired";
  };

  const handleFireCharacter = (characterId: string) => {
    if (!portState) return;
    if (!portState?.tavern.hiredCharacterIds.includes(characterId)) return;
    sendIpc({ action: "fire_character", characterId });
  };

  const panelClass = [
    "port-panel",
    isPanelVisible ? "open" : "hidden",
  ]
    .filter(Boolean)
    .join(" ");
  const healthPct = portState && portState.maxHealth > 0
    ? Math.max(0, Math.min(100, (portState.health / portState.maxHealth) * 100))
    : 0;
  const healthHue = Math.round((healthPct / 100) * 120);

  return (
    <>
      <div className="left-inventory-panel">
        <div className="left-hud-title">Inventory</div>
        {portState ? (
          <div className="left-inventory-list">
            {INVENTORY_ORDER.map((itemType) => (
              <div key={itemType} className="left-inventory-row">
                <img src={inventoryIcon(itemType)} alt={itemType} />
                <span>{itemType === "Coin" ? "Gold" : itemType}</span>
                <strong>{portState.inventory[itemType] ?? 0}</strong>
              </div>
            ))}
          </div>
        ) : (
          <div className="left-hud-empty">Waiting for ship data...</div>
        )}
      </div>

      <div className="left-health-panel">
        <div className="left-hud-title">Hull Health</div>
        {portState ? (
          <>
            <div className="left-health-bar">
              <div
                className="left-health-fill"
                style={{
                  width: `${healthPct}%`,
                  backgroundColor: `hsl(${healthHue}, 75%, 42%)`,
                }}
              />
            </div>
            <div className="left-health-text">
              {portState.health} / {portState.maxHealth}
            </div>
          </>
        ) : (
          <div className="left-hud-empty">Waiting for ship data...</div>
        )}
      </div>

      <button
        className={`panel-toggle ${isPanelVisible ? "" : "panel-hidden"}`.trim()}
        onClick={() => setIsPanelVisible((prev) => !prev)}
        type="button"
        aria-label={isPanelVisible ? "Hide port panel" : "Show port panel"}
      >
        {isPanelVisible ? "Hide" : "Show"}
      </button>

      <div className={panelClass}>
        <div className="section-bar">
        <button
          className={`section-btn ${activeSection === "ship" ? "active" : ""}`}
          onClick={() => setActiveSection("ship")}
        >
          Ship
        </button>
        <button
          className={`section-btn ${activeSection === "port" ? "active" : ""}`}
          onClick={() => setActiveSection("port")}
          disabled={!portState?.isInPort}
          title={!portState?.isInPort ? "Port section is only available while docked." : undefined}
        >
          Port
        </button>
        </div>

        <div className="port-header">
        <div className="view-context-title">
          {activeSection === "ship" ? "Ship Panel" : "Port Panel"}
        </div>
        <div className="port-name">
          {activeSection === "ship"
            ? "Ship Operations"
            : (portState?.portName ?? "Port")}
        </div>
        <div className={`location-badge ${portState?.isInPort ? "in-port" : "at-sea"}`}>
          {portState?.isInPort ? "In Port" : "At Sea"}
        </div>
        {portState && (
          <div className="port-coins">
            <img src={inventoryIcon("Coin")} alt="coins" />
            {portState.inventory["Coin"] ?? 0} Gold
          </div>
        )}
        </div>

        <div className="tab-bar">
        {activeSection === "ship" ? (
          <>
            <button
              className={`tab-btn ${activeShipTab === "guide" ? "active" : ""}`}
              onClick={() => setActiveShipTab("guide")}
            >
              Scarlett
            </button>
            <button
              className={`tab-btn ${activeShipTab === "leaderboard" ? "active" : ""}`}
              onClick={() => setActiveShipTab("leaderboard")}
            >
              Leaderboard
            </button>
            <button
              className={`tab-btn ${activeShipTab === "ship_crew" ? "active" : ""}`}
              onClick={() => setActiveShipTab("ship_crew")}
            >
              Crew
            </button>
            <button
              className={`tab-btn ${activeShipTab === "ship_status" ? "active" : ""}`}
              onClick={() => setActiveShipTab("ship_status")}
            >
              Ship Status
            </button>
          </>
        ) : (
          <>
            <button
              className={`tab-btn ${activePortTab === "market" ? "active" : ""}`}
              onClick={() => setActivePortTab("market")}
            >
              Market
            </button>
            <button
              className={`tab-btn ${activePortTab === "shipyard" ? "active" : ""}`}
              onClick={() => setActivePortTab("shipyard")}
            >
              Shipyard
            </button>
            <button
              className={`tab-btn ${activePortTab === "tavern" ? "active" : ""}`}
              onClick={() => setActivePortTab("tavern")}
            >
              Tavern
            </button>
            <button
              className={`tab-btn vault-tab-btn ${activePortTab === "vault" ? "active" : ""}`}
              onClick={() => setActivePortTab("vault")}
            >
              Vault
            </button>
            {portState?.isCreative && portState?.isInPort && (
              <button
                className={`tab-btn creative-tab-btn ${activePortTab === "creative" ? "active" : ""}`}
                onClick={() => setActivePortTab("creative")}
              >
                Creative
              </button>
            )}
          </>
        )}
        </div>

        {portState && (
          <div className="tab-content">
          {activeSection === "ship" ? (
            activeShipTab === "guide" ? (
              <GuideTab />
            ) : activeShipTab === "leaderboard" ? (
              <LeaderboardTab entries={portState.leaderboard} />
            ) : activeShipTab === "ship_status" ? (
              <ShipyardTab
                state={portState}
                isInPort={portState.isInPort}
                showForSale={false}
                showShipUpgrade={false}
              />
            ) : activeShipTab === "ship_crew" ? (
              <ShipCrewTab state={portState} />
            ) : (
              <LeaderboardTab entries={portState.leaderboard} />
            )
          ) : activePortTab === "market" ? (
            <MarketTab state={portState} />
          ) : activePortTab === "shipyard" ? (
            <ShipyardTab state={portState} isInPort={portState.isInPort} />
          ) : activePortTab === "tavern" ? (
            portState.isInPort ? (
              <TavernTab
                state={portState}
                onHireCharacter={handleHireCharacter}
                onFireCharacter={handleFireCharacter}
              />
            ) : (
              <div className="empty-state">Dock at a port to access tavern services.</div>
            )
          ) : activePortTab === "vault" ? (
            portState.isInPort ? (
              <VaultTab state={portState} />
            ) : (
              <div className="empty-state">Dock at a port to access the vault.</div>
            )
          ) : activePortTab === "creative" && portState.isCreative && portState.isInPort ? (
            <CreativeTab state={portState} />
          ) : (
            <div className="empty-state">Port services are unavailable while at sea.</div>
          )}
          </div>
        )}
        {!portState && (
          <div className="tab-content">
            <div className="empty-state">Waiting for ship data from Godot...</div>
          </div>
        )}
      </div>
    </>
  );
}
