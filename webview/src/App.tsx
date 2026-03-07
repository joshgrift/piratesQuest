import { useState, useEffect } from "react";
import "./App.css";
import type { PortState } from "./types";
import { sendIpc } from "./utils/ipc";
import { inventoryIcon } from "./utils/helpers";
import { MarketTab } from "./tabs/MarketTab";
import { ShipyardTab } from "./tabs/ShipyardTab";
import { VaultTab } from "./tabs/VaultTab";
import { GuideTab } from "./tabs/GuideTab";
import { CreativeTab } from "./tabs/CreativeTab";
import { TavernTab } from "./tabs/TavernTab";

type Tab = "market" | "shipyard" | "tavern" | "vault" | "guide" | "creative";
type HireOutcome = "hired" | "already_hired" | "slots_full" | "not_hireable";

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
          className={`tab-btn ${activeTab === "tavern" ? "active" : ""}`}
          onClick={() => setActiveTab("tavern")}
        >
          Tavern
        </button>
        <button
          className={`tab-btn vault-tab-btn ${activeTab === "vault" ? "active" : ""}`}
          onClick={() => setActiveTab("vault")}
        >
          Vault
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
          ) : activeTab === "tavern" ? (
            <TavernTab
              state={portState}
              onHireCharacter={handleHireCharacter}
              onFireCharacter={handleFireCharacter}
            />
          ) : activeTab === "vault" ? (
            <VaultTab state={portState} />
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
