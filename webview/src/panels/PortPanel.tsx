import type { PortState } from "../types";
import { MarketTab } from "../tabs/MarketTab";
import { ShipyardTab } from "../tabs/ShipyardTab";
import { VaultTab } from "../tabs/VaultTab";

export type PortPanelTab = "market" | "shipyard" | "vault";

interface PortPanelProps {
  state: PortState;
  activeTab: PortPanelTab;
  onSelectTab: (tab: PortPanelTab) => void;
  hasUnlockedFeature: (feature: string) => boolean;
  onTalkToCharacter: (characterId: string) => void;
  onHireCharacter: (characterId: string) => void;
  onQuestForCharacter: (characterId: string) => void;
}

export function PortPanel({
  state,
  activeTab,
  onSelectTab,
  hasUnlockedFeature,
  onTalkToCharacter,
  onHireCharacter,
  onQuestForCharacter,
}: PortPanelProps) {
  return (
    <>
      <div className="tab-bar">
        <button
          className={`tab-btn ${activeTab === "market" ? "active" : ""}`}
          onClick={() => onSelectTab("market")}
        >
          Market
        </button>
        <button
          className={`tab-btn ${activeTab === "shipyard" ? "active" : ""}`}
          onClick={() => onSelectTab("shipyard")}
        >
          Shipyard
        </button>
        {hasUnlockedFeature("Vault") && (
          <button
            className={`tab-btn vault-tab-btn ${activeTab === "vault" ? "active" : ""}`}
            onClick={() => onSelectTab("vault")}
          >
            Vault
          </button>
        )}
      </div>

      <div className="tab-content">
        {activeTab === "market" ? (
          <MarketTab
            state={state}
            onTalkToCharacter={onTalkToCharacter}
            onHireCharacter={onHireCharacter}
            onQuestForCharacter={onQuestForCharacter}
          />
        ) : activeTab === "shipyard" ? (
          <ShipyardTab state={state} isInPort={state.isInPort} />
        ) : activeTab === "vault" && hasUnlockedFeature("Vault") ? (
          state.isInPort ? (
            <VaultTab state={state} />
          ) : (
            <div className="empty-state">Dock at a port to access the vault.</div>
          )
        ) : (
          <div className="empty-state">Port services are unavailable while at sea.</div>
        )}
      </div>
    </>
  );
}
