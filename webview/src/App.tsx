import { useState, useEffect, useRef } from "react";
import "./App.css";
import type { PortState } from "./types";
import { sendIpc } from "./utils/ipc";
import { useInputCapture } from "./hooks/useInputCapture";
import { BASE } from "./utils/helpers";
import { CreativePanel } from "./tabs/CreativePanel";
import { QuestsPanel } from "./tabs/QuestsPanel";
import { LeaderboardPanel } from "./tabs/LeaderboardPanel";
import { StatsPanel } from "./tabs/StatsPanel";
import { SCARLETT_CHARACTER_ID, getFirePrompt, getRandomTalkPhrase } from "./tabs/tavernHelpers";
import { ShipStatusWidget } from "./components/ShipStatusWidget";
import { QuestStatusWidget } from "./components/QuestStatusWidget";
import { NpcCommentToast, type NpcCommentToastData } from "./components/NpcCommentToast";
import type { QuestSummary, TavernCharacter } from "./types";
import { PortPanel, type PortPanelTab } from "./panels/PortPanel";
import { ShipPanel } from "./panels/ShipPanel";

type PanelId = "ship" | "quests" | "port" | "creative" | "stats" | "leaderboard";

const SHIP_ICON = `${BASE}icons/flat/caravel.svg`;
const QUESTS_ICON = `${BASE}icons/flat/tied-scroll.svg`;
const PORT_ICON = `${BASE}icons/flat/anchor.svg`;
const CREATIVE_ICON = `${BASE}icons/flat/pirate-skull.svg`;
const STATS_ICON = `${BASE}icons/flat/sextant.svg`;
const LEADERBOARD_ICON = `${BASE}icons/flat/pirate-hat.svg`;
const SEA_CHART_IMAGE = `${BASE}images/map.png`;

function findQuestForNpc(state: PortState, characterId: string): QuestSummary | null {
  return state.quests.available.find((quest) => quest.giverNpcId === characterId && !hasText(quest.rewardCrewNpcId)) ?? null;
}

function findHireQuestForNpc(state: PortState, characterId: string): QuestSummary | null {
  return state.quests.available.find((quest) => quest.rewardCrewNpcId === characterId) ?? null;
}

function isNextQuestStepTalkToNpc(state: PortState, character: TavernCharacter): boolean {
  const activeQuest = state.quests.active;
  if (!activeQuest) return false;

  const nextIncompleteStep = activeQuest.steps.find((step) => !step.isComplete);
  if (!nextIncompleteStep) return false;

  return nextIncompleteStep.label.trim() === `Talk to ${character.name}`;
}

function hasText(value: string | null | undefined): value is string {
  return typeof value === "string" && value.trim().length > 0;
}

function isTypingInInput(): boolean {
  const el = document.activeElement;
  if (!(el instanceof HTMLElement)) return false;

  return (
    el.tagName === "INPUT" ||
    el.tagName === "TEXTAREA" ||
    el.isContentEditable
  );
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
  const [activePanel, setActivePanel] = useState<PanelId | null>("ship");
  const [activePortPanelTab, setActivePortPanelTab] = useState<PortPanelTab>("market");
  const [isMapOpen, setIsMapOpen] = useState(false);
  const [npcCommentQueue, setNpcCommentQueue] = useState<NpcCommentToastData[]>([]);
  const prevIsInPortRef = useRef<boolean | null>(null);
  const prevActiveQuestIdRef = useRef<string | null>(null);
  const previousPanelBeforeDockRef = useRef<PanelId | null>("ship");

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

    // Auto-open the Port panel only on sea -> port transition.
    if (isInPort && prevIsInPort !== true) {
      previousPanelBeforeDockRef.current = activePanel;
      setActivePanel("port");
      return;
    }

    // Leaving port restores the panel we had before docking
    // (including hidden state if the panel was collapsed).
    if (!isInPort && prevIsInPort === true) {
      setActivePanel(previousPanelBeforeDockRef.current);
    }
  }, [portState?.isInPort, activePanel]);

  useEffect(() => {
    function onKeyDown(e: KeyboardEvent) {
      if (isTypingInInput()) return;

      if (e.key === "Tab") {
        e.preventDefault();
        if (e.repeat) return;
        setIsMapOpen((current) => !current);
        return;
      }

      if (e.key !== "Escape") return;
      if (e.repeat) return;

      if (isMapOpen) {
        e.preventDefault();
        setIsMapOpen(false);
        return;
      }

      if (npcCommentQueue.length > 0) {
        e.preventDefault();
        setNpcCommentQueue((current) => current.slice(1));
        return;
      }

      if (activePanel !== null) {
        e.preventDefault();
        setActivePanel(null);
      }
    }

    window.addEventListener("keydown", onKeyDown);
    return () => {
      window.removeEventListener("keydown", onKeyDown);
    };
  }, [activePanel, isMapOpen, npcCommentQueue.length]);

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
    if (!portState) return;

    const character = findCharacterById(characterId);
    if (!character) return;

    recordNpcInteraction(characterId);

    // If talking to this NPC is the very next quest step, let the quest
    // resolve immediately instead of covering it with ambient dialogue.
    if (isNextQuestStepTalkToNpc(portState, character)) {
      return;
    }

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
    if (!portState) return;

    const character = findCharacterById(characterId);
    if (!character) return;

    const hireQuest = findHireQuestForNpc(portState, character.id);
    const crewIsFull = portState.crew.hiredCharacterIds.length >= portState.crew.crewSlots;
    const hasDifferentActiveQuest = portState.quests.active !== null;

    if (!crewIsFull && !hasDifferentActiveQuest && hireQuest && hasText(hireQuest.offerText)) {
      recordNpcInteraction(characterId);
      enqueuePopup(buildCharacterPopup(
        character,
        hireQuest.offerText,
        [
          {
            label: "Accept Quest",
            tone: "primary",
            onSelect: () => {
              sendIpc({
                action: "accept_quest",
                questId: hireQuest.id,
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
      return;
    }

    const unavailableMessage = crewIsFull
      ? "Looks like your berths are full. Free up a crew slot before asking me aboard."
      : hasDifferentActiveQuest
        ? "Finish your current quest first, then come back if you still want to prove yourself to me."
        : `${character.name} is not ready to join yet.`;

    enqueuePopup(buildCharacterPopup(character, unavailableMessage));
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

  const panelClass = ["port-panel", activePanel === null ? "hidden" : ""].filter(Boolean).join(" ");
  const isInPort = !!portState?.isInPort;
  const panelTitle = activePanel === "port"
    ? (portState?.portName ?? "Port")
    : activePanel === "creative"
      ? "Creative"
    : activePanel === "quests"
      ? "quests"
    : activePanel === "stats"
      ? "stats"
    : activePanel === "leaderboard"
      ? "Hall of Captains"
      : "ship";

  const handlePanelSelect = (panel: PanelId) => {
    if (panel === "port" && !isInPort) return;
    setActivePanel((prev) => (prev === panel ? null : panel));
  };

  return (
    <>
      <aside
        className={`sea-chart ${isMapOpen ? "open" : ""}`}
        aria-hidden={!isMapOpen}
      >
        <div className="sea-chart__panel">
          <img className="sea-chart__image" src={SEA_CHART_IMAGE} alt="World map of the sea" />
        </div>
      </aside>

      <QuestStatusWidget
        state={portState}
        panelOpen={activePanel !== null}
        onOpenQuests={() => {
          setActivePanel("quests");
        }}
      />
      <ShipStatusWidget state={portState} panelOpen={activePanel !== null} />

      <div className={`mode-rail ${activePanel === null ? "collapsed" : ""}`} role="tablist" aria-label="Panel mode">
        <button
          className={`rail-mode-btn ${activePanel === "port" ? "active" : ""}`}
          onClick={() => handlePanelSelect("port")}
          type="button"
          role="tab"
          aria-selected={activePanel === "port"}
          aria-label="Port mode"
          title={isInPort ? "Port" : "Port mode is only available while docked."}
          disabled={!isInPort}
        >
          <img className="rail-mode-icon" src={PORT_ICON} alt="" />
        </button>
        <button
          className={`rail-mode-btn ${activePanel === "ship" ? "active" : ""}`}
          onClick={() => handlePanelSelect("ship")}
          type="button"
          role="tab"
          aria-selected={activePanel === "ship"}
          aria-label="Ship mode"
          title="Ship"
        >
          <img className="rail-mode-icon" src={SHIP_ICON} alt="" />
        </button>
        <button
          className={`rail-mode-btn ${activePanel === "quests" ? "active" : ""}`}
          onClick={() => handlePanelSelect("quests")}
          type="button"
          role="tab"
          aria-selected={activePanel === "quests"}
          aria-label="Quests mode"
          title="Quests"
        >
          <img className="rail-mode-icon" src={QUESTS_ICON} alt="" />
        </button>
        {portState?.isCreative && (
          <button
            className={`rail-mode-btn ${activePanel === "creative" ? "active" : ""}`}
            onClick={() => handlePanelSelect("creative")}
            type="button"
            role="tab"
            aria-selected={activePanel === "creative"}
            aria-label="Creative mode"
            title="Creative"
          >
            <img className="rail-mode-icon" src={CREATIVE_ICON} alt="" />
          </button>
        )}
        <button
          className={`rail-mode-btn ${activePanel === "stats" ? "active" : ""}`}
          onClick={() => handlePanelSelect("stats")}
          type="button"
          role="tab"
          aria-selected={activePanel === "stats"}
          aria-label="Stats mode"
          title="Stats"
        >
          <img className="rail-mode-icon" src={STATS_ICON} alt="" />
        </button>
        <button
          className={`rail-mode-btn ${activePanel === "leaderboard" ? "active" : ""}`}
          onClick={() => handlePanelSelect("leaderboard")}
          type="button"
          role="tab"
          aria-selected={activePanel === "leaderboard"}
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

        {portState && (
          activePanel === "port" ? (
            <PortPanel
              state={portState}
              activeTab={activePortPanelTab}
              onSelectTab={setActivePortPanelTab}
              hasUnlockedFeature={hasUnlockedFeature}
              onTalkToCharacter={openTalkPopup}
              onHireCharacter={openHirePopup}
              onQuestForCharacter={openQuestPopup}
            />
          ) : (
            <div className="tab-content">
              {activePanel === "ship" ? (
                <ShipPanel
                  state={portState}
                  onTalkToCrewmate={openTalkPopup}
                  onFireCrewmate={openFirePopup}
                  onQuestForCrewmate={openQuestPopup}
                />
              ) : activePanel === "quests" ? (
                <QuestsPanel state={portState} />
              ) : activePanel === "stats" ? (
                <StatsPanel state={portState} />
              ) : activePanel === "leaderboard" ? (
                <LeaderboardPanel entries={portState.leaderboard} playerName={portState.playerName} />
              ) : activePanel === "creative" && portState.isCreative ? (
                <CreativePanel state={portState} />
              ) : (
                <div className="empty-state">Waiting for ship data from Godot...</div>
              )}
            </div>
          )
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
