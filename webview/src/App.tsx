import { useState, useEffect, useRef } from "react";
import "./App.css";
import type { PortState } from "./types";
import { sendIpc } from "./utils/ipc";
import { useInputCapture } from "./hooks/useInputCapture";
import { BASE } from "./utils/helpers";
import { MarketTab } from "./tabs/MarketTab";
import { ShipyardTab } from "./tabs/ShipyardTab";
import { VaultTab } from "./tabs/VaultTab";
import { CreativeTab } from "./tabs/CreativeTab";
import { QuestsTab } from "./tabs/QuestsTab";
import { ShipCrewTab } from "./tabs/ShipCrewTab";
import { LeaderboardTab } from "./tabs/LeaderboardTab";
import { StatsTab } from "./tabs/StatsTab";
import { SCARLETT_CHARACTER_ID, getFirePrompt, getHirePrompt, getRandomTalkPhrase } from "./tabs/tavernHelpers";
import { ShipStatusWidget } from "./components/ShipStatusWidget";
import { QuestStatusWidget } from "./components/QuestStatusWidget";
import { NpcCommentToast, type NpcCommentToastData } from "./components/NpcCommentToast";
import type { QuestSummary, TavernCharacter } from "./types";

type PortTab = "market" | "shipyard" | "vault";
type PanelMode = "ship" | "quests" | "crew" | "port" | "creative" | "stats" | "leaderboard";
type HireOutcome = "hired" | "already_hired" | "slots_full" | "not_hireable";

const SHIP_ICON = `${BASE}icons/flat/caravel.svg`;
const QUESTS_ICON = `${BASE}icons/flat/tied-scroll.svg`;
const CREW_ICON = `${BASE}icons/flat/bandana.svg`;
const PORT_ICON = `${BASE}icons/flat/anchor.svg`;
const CREATIVE_ICON = `${BASE}icons/flat/pirate-skull.svg`;
const STATS_ICON = `${BASE}icons/flat/sextant.svg`;
const LEADERBOARD_ICON = `${BASE}icons/flat/pirate-hat.svg`;

function findQuestForNpc(state: PortState, characterId: string): QuestSummary | null {
  return state.quests.available.find((quest) => quest.giverNpcId === characterId) ?? null;
}

function hasText(value: string | null | undefined): value is string {
  return typeof value === "string" && value.trim().length > 0;
}

function buildCharacterPopup(
  character: TavernCharacter,
  message: string,
  actions?: NpcCommentToastData["actions"],
): NpcCommentToastData {
  return {
    id: `${character.id}-${Math.random().toString(36).slice(2, 10)}`,
    portraitSrc: `${BASE}images/characters/${character.portrait}`,
    portraitAlt: character.name,
    name: character.name,
    message,
    actions,
  };
}

function buildQuestCompletionComment(state: PortState, questId: string): NpcCommentToastData | null {
  const quest = state.quests.all.find((entry) => entry.id === questId);
  if (!quest) return null;
  if (!hasText(quest.completionText)) return null;

  return {
    id: `quest-complete-${quest.id}`,
    portraitSrc: `${BASE}images/characters/${quest.giverPortrait}`,
    portraitAlt: quest.giverName,
    name: quest.giverName,
    message: quest.completionText,
    celebrate: true,
  };
}

function buildQuestAcceptedComment(quest: QuestSummary): NpcCommentToastData | null {
  if (!hasText(quest.acceptedText)) return null;

  return {
    id: `quest-accepted-${quest.id}`,
    portraitSrc: `${BASE}images/characters/${quest.giverPortrait}`,
    portraitAlt: quest.giverName,
    name: quest.giverName,
    message: quest.acceptedText,
  };
}

export default function App() {
  useInputCapture();

  const [portState, setPortState] = useState<PortState | null>(null);
  const [activePanelMode, setActivePanelMode] = useState<PanelMode | null>("ship");
  const [activePortTab, setActivePortTab] = useState<PortTab>("market");
  const [npcCommentQueue, setNpcCommentQueue] = useState<NpcCommentToastData[]>([]);
  const prevIsInPortRef = useRef<boolean | null>(null);
  const prevActiveQuestIdRef = useRef<string | null>(null);
  const prePortPanelModeRef = useRef<PanelMode | null>("ship");

  const hasUnlockedFeature = (feature: string): boolean =>
    portState?.quests.unlockedFeatures.includes(feature) ?? false;

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

      if (npcCommentQueue.length > 0) {
        e.preventDefault();
        setNpcCommentQueue((current) => current.slice(1));
        return;
      }

      if (activePanelMode !== null) {
        e.preventDefault();
        setActivePanelMode(null);
      }
    }

    window.addEventListener("keydown", onKeyDown);
    return () => {
      window.removeEventListener("keydown", onKeyDown);
    };
  }, [activePanelMode, npcCommentQueue.length]);

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
    if (characterId === SCARLETT_CHARACTER_ID) return;
    if (!portState?.crew.hiredCharacterIds.includes(characterId)) return;
    sendIpc({ action: "fire_character", characterId });
  };

  const enqueuePopup = (popup: NpcCommentToastData) => {
    setNpcCommentQueue((current) => [...current, popup]);
  };

  const findCharacterById = (characterId: string): TavernCharacter | null => {
    if (!portState) return null;
    return portState.tavern.characters.find((character) => character.id === characterId)
      ?? portState.crew.characters.find((character) => character.id === characterId)
      ?? null;
  };

  const recordNpcInteraction = (characterId: string) => {
    sendIpc({ action: "talk_to_npc", characterId });
  };

  const openTalkPopup = (characterId: string) => {
    const character = findCharacterById(characterId);
    if (!character) return;

    recordNpcInteraction(characterId);
    enqueuePopup(buildCharacterPopup(character, getRandomTalkPhrase(character)));
  };

  const openQuestPopup = (characterId: string) => {
    if (!portState) return;
    const character = findCharacterById(characterId);
    const quest = character ? findQuestForNpc(portState, character.id) : null;
    if (!character || !quest) return;

    if (!hasText(quest.offerText)) return;

    recordNpcInteraction(characterId);
    enqueuePopup(buildCharacterPopup(
      character,
      quest.offerText,
      [
        {
          label: "Accept Quest",
          tone: "primary",
          onSelect: () => {
            sendIpc({
              action: "accept_quest",
              questId: quest.id,
              characterId: character.id,
            });
          },
        },
        {
          label: "Decline",
          tone: "secondary",
        },
      ],
    ));
  };

  const openHirePopup = (characterId: string) => {
    const character = findCharacterById(characterId);
    if (!character) return;

    recordNpcInteraction(characterId);
    enqueuePopup(buildCharacterPopup(
      character,
      getHirePrompt(character),
      [
        {
          label: "Hire",
          tone: "primary",
          onSelect: () => {
            handleHireCharacter(character.id);
          },
        },
        {
          label: "Decline",
          tone: "secondary",
        },
      ],
    ));
  };

  const openFirePopup = (characterId: string) => {
    const character = findCharacterById(characterId);
    if (!character) return;

    recordNpcInteraction(characterId);
    enqueuePopup(buildCharacterPopup(
      character,
      getFirePrompt(character),
      [
        {
          label: "Fire",
          tone: "danger",
          onSelect: () => {
            handleFireCharacter(character.id);
          },
        },
        {
          label: "Never mind",
          tone: "secondary",
        },
      ],
    ));
  };

  useEffect(() => {
    if (!portState) return;

    const newComments = (portState.quests.recentlyCompletedIds ?? [])
      .map((questId) => buildQuestCompletionComment(portState, questId))
      .filter((comment): comment is NpcCommentToastData => comment !== null);

    if (newComments.length === 0) return;

    setNpcCommentQueue((current) => [...current, ...newComments]);
  }, [portState]);

  useEffect(() => {
    const activeQuest = portState?.quests.active ?? null;
    const previousActiveQuestId = prevActiveQuestIdRef.current;
    prevActiveQuestIdRef.current = activeQuest?.id ?? null;

    if (!activeQuest) return;
    if (activeQuest.id === previousActiveQuestId) return;

    const acceptedComment = buildQuestAcceptedComment(activeQuest);
    if (!acceptedComment) return;

    setNpcCommentQueue((current) => [...current, acceptedComment]);
  }, [portState]);

  const panelClass = ["port-panel", activePanelMode === null ? "hidden" : ""].filter(Boolean).join(" ");
  const isInPort = !!portState?.isInPort;
  const panelTitle = activePanelMode === "port"
    ? (portState?.portName ?? "Port")
    : activePanelMode === "creative"
      ? "Creative"
    : activePanelMode === "quests"
      ? "quests"
      : activePanelMode === "crew"
        ? "crew"
    : activePanelMode === "stats"
      ? "stats"
    : activePanelMode === "leaderboard"
      ? "Hall of Captains"
      : "ship";

  const handleModeSelect = (mode: PanelMode) => {
    if (mode === "port" && !isInPort) return;
    setActivePanelMode((prev) => (prev === mode ? null : mode));
  };

  return (
    <>
      <QuestStatusWidget
        state={portState}
        panelOpen={activePanelMode !== null}
        onOpenQuests={() => {
          setActivePanelMode("quests");
        }}
      />
      <ShipStatusWidget state={portState} panelOpen={activePanelMode !== null} />

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
          className={`rail-mode-btn ${activePanelMode === "quests" ? "active" : ""}`}
          onClick={() => handleModeSelect("quests")}
          type="button"
          role="tab"
          aria-selected={activePanelMode === "quests"}
          aria-label="Quests mode"
          title="Quests"
        >
          <img className="rail-mode-icon" src={QUESTS_ICON} alt="" />
        </button>
        <button
          className={`rail-mode-btn ${activePanelMode === "crew" ? "active" : ""}`}
          onClick={() => handleModeSelect("crew")}
          type="button"
          role="tab"
          aria-selected={activePanelMode === "crew"}
          aria-label="Crew mode"
          title="Crew"
        >
          <img className="rail-mode-icon" src={CREW_ICON} alt="" />
        </button>
        {portState?.isCreative && (
          <button
            className={`rail-mode-btn ${activePanelMode === "creative" ? "active" : ""}`}
            onClick={() => handleModeSelect("creative")}
            type="button"
            role="tab"
            aria-selected={activePanelMode === "creative"}
            aria-label="Creative mode"
            title="Creative"
          >
            <img className="rail-mode-icon" src={CREATIVE_ICON} alt="" />
          </button>
        )}
        <button
          className={`rail-mode-btn ${activePanelMode === "stats" ? "active" : ""}`}
          onClick={() => handleModeSelect("stats")}
          type="button"
          role="tab"
          aria-selected={activePanelMode === "stats"}
          aria-label="Stats mode"
          title="Stats"
        >
          <img className="rail-mode-icon" src={STATS_ICON} alt="" />
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
        </div>

        {activePanelMode === "port" && (
          <div className="tab-bar">
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
            {hasUnlockedFeature("Vault") && (
              <button
                className={`tab-btn vault-tab-btn ${activePortTab === "vault" ? "active" : ""}`}
                onClick={() => setActivePortTab("vault")}
              >
                Vault
              </button>
            )}
          </div>
        )}

        {portState && (
          <div className="tab-content">
            {activePanelMode === "ship" ? (
              <ShipyardTab
                state={portState}
                isInPort={portState.isInPort}
                showForSale={false}
                showShipUpgrade={false}
              />
            ) : activePanelMode === "quests" ? (
              <QuestsTab state={portState} />
            ) : activePanelMode === "crew" ? (
              <ShipCrewTab
                state={portState}
                onTalk={openTalkPopup}
                onFire={openFirePopup}
                onQuest={openQuestPopup}
              />
            ) : activePanelMode === "stats" ? (
              <StatsTab state={portState} />
            ) : activePanelMode === "leaderboard" ? (
              <LeaderboardTab entries={portState.leaderboard} playerName={portState.playerName} />
            ) : activePanelMode === "creative" && portState.isCreative ? (
              <CreativeTab state={portState} />
            ) : activePortTab === "market" ? (
              <MarketTab
                state={portState}
                onTalkToCharacter={openTalkPopup}
                onHireCharacter={openHirePopup}
                onQuestForCharacter={openQuestPopup}
              />
            ) : activePortTab === "shipyard" ? (
              <ShipyardTab state={portState} isInPort={portState.isInPort} />
            ) : activePortTab === "vault" && hasUnlockedFeature("Vault") ? (
              portState.isInPort ? (
                <VaultTab state={portState} />
              ) : (
                <div className="empty-state">Dock at a port to access the vault.</div>
              )
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

      <NpcCommentToast
        comment={npcCommentQueue[0] ?? null}
        queueCount={npcCommentQueue.length}
        onDismiss={() => setNpcCommentQueue((current) => current.slice(1))}
      />
    </>
  );
}
