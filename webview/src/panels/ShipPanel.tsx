import type { PortState } from "../types";
import { ShipCrewTab } from "../tabs/ShipCrewTab";
import { ShipyardTab } from "../tabs/ShipyardTab";

interface ShipPanelProps {
  state: PortState;
  onTalkToCrewmate: (characterId: string) => void;
  onFireCrewmate: (characterId: string) => void;
  onQuestForCrewmate: (characterId: string) => void;
}

export function ShipPanel({
  state,
  onTalkToCrewmate,
  onFireCrewmate,
  onQuestForCrewmate,
}: ShipPanelProps) {
  return (
    <>
      <ShipyardTab
        state={state}
        isInPort={state.isInPort}
        showForSale={false}
        showShipUpgrade={false}
        showStats={false}
        showComponents={false}
        showPortLocked={false}
      />
      <ShipCrewTab
        state={state}
        onTalk={onTalkToCrewmate}
        onFire={onFireCrewmate}
        onQuest={onQuestForCrewmate}
        showSummary={false}
      />
      <ShipyardTab
        state={state}
        isInPort={state.isInPort}
        showForSale={false}
        showShipUpgrade={false}
        showHealth={false}
      />
    </>
  );
}
