import { useState, useEffect, useRef } from "react";
import "./App.css";
import type { PortState } from "./types";
import { sendIpc } from "./utils/ipc";
import { useInputCapture } from "./hooks/useInputCapture";
import { BASE, inventoryIcon } from "./utils/helpers";
import { MarketTab } from "./tabs/MarketTab";
import { ShipyardTab } from "./tabs/ShipyardTab";
import { VaultTab } from "./tabs/VaultTab";
import { GuideTab } from "./tabs/GuideTab";
import { CreativeTab } from "./tabs/CreativeTab";
import { TavernTab } from "./tabs/TavernTab";
import { ShipCrewTab } from "./tabs/ShipCrewTab";
import { LeaderboardTab } from "./tabs/LeaderboardTab";

type ShipTab = "guide" | "ship_crew" | "ship_status";
type PortTab = "market" | "shipyard" | "tavern" | "vault" | "creative";
type PanelMode = "ship" | "port" | "leaderboard";
type HireOutcome = "hired" | "already_hired" | "slots_full" | "not_hireable";
const INVENTORY_ORDER = ["Coin", "Wood", "Fish", "Iron", "Tea", "CannonBall", "Trophy"] as const;
const SHIP_ICON = `${BASE}icons/flat/ship-wheel.svg`;
const PORT_ICON = `${BASE}icons/flat/anchor.svg`;
const LEADERBOARD_ICON = `${BASE}icons/flat/pirate-hat.svg`;

export default function App() {
  useInputCapture();

  const [portState, setPortState] = useState<PortState | null>(null);
  const [activePanelMode, setActivePanelMode] = useState<PanelMode | null>("ship");
  const [activeShipTab, setActiveShipTab] = useState<ShipTab>("guide");
  const [activePortTab, setActivePortTab] = useState<PortTab>("market");
  const prevIsInPortRef = useRef<boolean | null>(null);
  const prePortPanelModeRef = useRef<PanelMode | null>("ship");

  useEffect(() => {
    window.openPort = (data: PortState) => {
      setPortState(data);
    };

    window.closePort = () => {
      // Keep the panel visible at all times. On close we reset to
      // safe ship defaults while preserving the latest state snapshot.
      // Godot calls closePort when undocking, so we force local state
      // to sea mode even before the next updateState payload arrives.
      setPortState((prev) => (prev ? { ...prev, isInPort: false } : prev));
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

    // Auto-open Port mode only on sea -> port transition.
    if (isInPort && prevIsInPort !== true) {
      prePortPanelModeRef.current = activePanelMode;
      setActivePanelMode("port");
      return;
    }

    // Leaving port restores the panel mode we had before docking
    // (including hidden state if the panel was collapsed).
    if (!isInPort && prevIsInPort === true) {
      setActivePanelMode(prePortPanelModeRef.current);
    }
  }, [portState?.isInPort, activePanelMode]);

  useEffect(() => {
    function onKeyDown(e: KeyboardEvent) {
      if (e.key !== "Escape") return;
      if (e.repeat) return;

      if (activePanelMode !== null) {
        e.preventDefault();
        setActivePanelMode(null);
      }
    }

    window.addEventListener("keydown", onKeyDown);
    return () => {
      window.removeEventListener("keydown", onKeyDown);
    };
  }, [activePanelMode]);

  const handleHireCharacter = (characterId: string): HireOutcome => {
    if (!portState) return "not_hireable";

    const character = portState?.tavern.characters.find((c) => c.id === characterId);
    if (!character?.hireable) return "not_hireable";

    const alreadyHired = portState.crew.hiredCharacterIds.includes(characterId);
    if (alreadyHired) return "already_hired";

    if (portState.crew.hiredCharacterIds.length >= portState.crew.crewSlots) {
      return "slots_full";
    }

    sendIpc({ action: "hire_character", characterId });
    return "hired";
  };

  const handleFireCharacter = (characterId: string) => {
    if (!portState) return;
    if (!portState?.crew.hiredCharacterIds.includes(characterId)) return;
    sendIpc({ action: "fire_character", characterId });
  };

  const panelClass = ["port-panel", activePanelMode === null ? "hidden" : ""].filter(Boolean).join(" ");
  const isInPort = !!portState?.isInPort;
  const healthPct = portState && portState.maxHealth > 0
    ? Math.max(0, Math.min(100, (portState.health / portState.maxHealth) * 100))
    : 0;
  const healthHue = Math.round((healthPct / 100) * 120);
  const panelTitle = activePanelMode === "port"
    ? (portState?.portName ?? "Port")
    : activePanelMode === "leaderboard"
      ? "Hall of Captains"
      : "Ship Operations";

  const handleModeSelect = (mode: PanelMode) => {
    if (mode === "port" && !isInPort) return;
    setActivePanelMode((prev) => (prev === mode ? null : mode));
  };

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

      <div className={`mode-rail ${activePanelMode === null ? "collapsed" : ""}`} role="tablist" aria-label="Panel mode">
        <button
          className={`rail-mode-btn ${activePanelMode === "port" ? "active" : ""}`}
          onClick={() => handleModeSelect("port")}
          type="button"
          role="tab"
          aria-selected={activePanelMode === "port"}
          aria-label="Port mode"
          title={isInPort ? "Port" : "Port mode is only available while docked."}
          disabled={!isInPort}
        >
          <img className="rail-mode-icon" src={PORT_ICON} alt="" />
        </button>
        <button
          className={`rail-mode-btn ${activePanelMode === "ship" ? "active" : ""}`}
          onClick={() => handleModeSelect("ship")}
          type="button"
          role="tab"
          aria-selected={activePanelMode === "ship"}
          aria-label="Ship mode"
          title="Ship"
        >
          <img className="rail-mode-icon" src={SHIP_ICON} alt="" />
        </button>
        <button
          className={`rail-mode-btn ${activePanelMode === "leaderboard" ? "active" : ""}`}
          onClick={() => handleModeSelect("leaderboard")}
          type="button"
          role="tab"
          aria-selected={activePanelMode === "leaderboard"}
          aria-label="Leaderboard mode"
          title="Leaderboard"
        >
          <img className="rail-mode-icon" src={LEADERBOARD_ICON} alt="" />
        </button>
      </div>

      <div className={panelClass}>
        <div className="port-header">
        <div className="port-name">{panelTitle}</div>
        {portState && (
          <div className="port-coins">
            {portState.inventory["Coin"] ?? 0} Gold
          </div>
        )}
        </div>

        {activePanelMode !== null && activePanelMode !== "leaderboard" && (
        <div className="tab-bar">
        {activePanelMode === "ship" ? (
          <>
            <button
              className={`tab-btn ${activeShipTab === "guide" ? "active" : ""}`}
              onClick={() => setActiveShipTab("guide")}
            >
              Scarlett
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
        )}

        {portState && (
          <div className="tab-content">
          {activePanelMode === "ship" ? (
            activeShipTab === "guide" ? (
              <GuideTab />
            ) : activeShipTab === "ship_status" ? (
              <ShipyardTab
                state={portState}
                isInPort={portState.isInPort}
                showForSale={false}
                showShipUpgrade={false}
              />
            ) : activeShipTab === "ship_crew" ? (
              <ShipCrewTab
                state={portState}
                onFireCharacter={handleFireCharacter}
              />
            ) : (
              <GuideTab />
            )
          ) : activePanelMode === "leaderboard" ? (
            <LeaderboardTab entries={portState.leaderboard} />
          ) : activePortTab === "market" ? (
            <MarketTab state={portState} />
          ) : activePortTab === "shipyard" ? (
            <ShipyardTab state={portState} isInPort={portState.isInPort} />
          ) : activePortTab === "tavern" ? (
            portState.isInPort ? (
              <TavernTab
                state={portState}
                onHireCharacter={handleHireCharacter}
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
