import { useEffect, useMemo, useState } from "react";
import { ConversationPanel } from "../components/ConversationPanel";
import { BASE, fmt, formatStatName } from "../utils/helpers";
import type { PortState, TavernCharacter } from "../types";
import { getDialogueForCharacter, toStatBonusMap } from "./tavernData";

type HireOutcome = "hired" | "already_hired" | "slots_full" | "not_hireable";

interface TavernTabProps {
  state: PortState;
  onHireCharacter: (characterId: string) => HireOutcome;
  onFireCharacter: (characterId: string) => void;
}

function sumActiveCrewBonuses(
  characters: TavernCharacter[],
  hiredIds: string[],
): Record<string, number> {
  const hiredSet = new Set(hiredIds);
  const totals: Record<string, number> = {};

  for (const character of characters) {
    if (!hiredSet.has(character.id)) continue;

    const bonuses = toStatBonusMap(character.statChanges);
    for (const [stat, bonus] of Object.entries(bonuses)) {
      totals[stat] = (totals[stat] ?? 0) + bonus;
    }
  }

  return totals;
}

export function TavernTab({
  state,
  onHireCharacter,
  onFireCharacter,
}: TavernTabProps) {
  const [activeCharacterId, setActiveCharacterId] = useState(
    state.tavern.characters[0]?.id ?? "",
  );

  useEffect(() => {
    const hasActive = state.tavern.characters.some((c) => c.id === activeCharacterId);
    if (!hasActive) {
      setActiveCharacterId(state.tavern.characters[0]?.id ?? "");
    }
  }, [state.tavern.characters, activeCharacterId]);

  const activeCharacter =
    state.tavern.characters.find((c) => c.id === activeCharacterId)
    ?? state.tavern.characters[0]
    ?? null;

  const hiredSet = useMemo(
    () => new Set(state.tavern.hiredCharacterIds),
    [state.tavern.hiredCharacterIds],
  );

  const totalBonuses = useMemo(
    () => sumActiveCrewBonuses(state.tavern.characters, state.tavern.hiredCharacterIds),
    [state.tavern.characters, state.tavern.hiredCharacterIds],
  );

  const conversationTree = useMemo(() => {
    if (!activeCharacter) {
      return {
        root: {
          text: "No tavern regulars at this port yet.",
          responses: [{ label: "Back", next: "root" }],
        },
      };
    }

    const baseTree = getDialogueForCharacter(activeCharacter);
    const tree = Object.fromEntries(
      Object.entries(baseTree).map(([nodeId, node]) => [
        nodeId,
        { ...node, responses: [...node.responses] },
      ]),
    );

    const root = tree.root;
    if (!root) return tree;

    if (!tree.hire_offer) {
      tree.hire_offer = {
        text: "If we're to sail together, say the word plain.",
        responses: [
          { label: "Join my crew.", action: "hire" },
          { label: "Not today.", next: "root" },
        ],
      };
    }

    if (!tree.not_hireable) {
      tree.not_hireable = {
        text: `${activeCharacter.name} shakes their head. "I stay ashore."`,
        responses: [{ label: "Back", next: "root" }],
      };
    }

    if (!tree.hire_success) {
      tree.hire_success = {
        text: "Aye. I'll be ready at the dock.",
        responses: [{ label: "Back", next: "root" }],
      };
    }

    if (!tree.hire_blocked) {
      tree.hire_blocked = {
        text: "No bunk left. Make room first.",
        responses: [{ label: "Back", next: "root" }],
      };
    }

    if (!tree.already_hired) {
      tree.already_hired = {
        text: "Already aboard, Captain.",
        responses: [{ label: "Back", next: "root" }],
      };
    }

    if (!tree.fire_success) {
      tree.fire_success = {
        text: "Understood. I'll step off at the pier.",
        responses: [{ label: "Back", next: "root" }],
      };
    }

    if (hiredSet.has(activeCharacter.id)) {
      const bonusMap = toStatBonusMap(activeCharacter.statChanges);
      const bonusEntries = Object.entries(bonusMap);
      const bonusSummary = bonusEntries.length === 0
        ? "No numeric bonus from this post, but morale and discipline stay solid."
        : bonusEntries
          .map(([stat, value]) => `${formatStatName(stat)} +${fmt(value)}`)
          .join(", ");

      if (!tree.onboard_report) {
        tree.onboard_report = {
          text: `${activeCharacter.name} gives a crisp nod. "${activeCharacter.role} post is steady, Captain. Crew knows their rhythm."`,
          responses: [{ label: "Back", next: "root" }],
        };
      }

      if (!tree.onboard_numbers) {
        tree.onboard_numbers = {
          text: `Current contribution: ${bonusSummary}`,
          responses: [{ label: "Back", next: "root" }],
        };
      }

      if (!tree.onboard_need) {
        tree.onboard_need = {
          text: `"Keep us supplied and decisive, Captain. I'll handle the rest."`,
          responses: [{ label: "Back", next: "root" }],
        };
      }

      root.text = `${activeCharacter.name} straightens as you approach. "Aboard and ready, Captain."`;
      root.responses = [
        { label: "Any report from your station?", next: "onboard_report" },
        { label: "How are our numbers?", next: "onboard_numbers" },
        { label: "Need anything from me?", next: "onboard_need" },
        { label: "Stand down (fire crew).", action: "fire" },
        { label: "Back to tavern", next: "root" },
      ];
    }

    return tree;
  }, [activeCharacter, hiredSet]);

  if (!activeCharacter) {
    return <div className="empty-state">No tavern regulars at this port yet.</div>;
  }

  const handleConversationAction = (actionId: string): string | void => {
    if (actionId === "probe_hire") {
      if (hiredSet.has(activeCharacter.id)) return "already_hired";
      if (activeCharacter.hireable) return "hire_offer";
      return "not_hireable";
    }

    if (actionId === "hire") {
      const outcome = onHireCharacter(activeCharacter.id);

      switch (outcome) {
        case "hired":
          return "hire_success";
        case "already_hired":
          return "already_hired";
        case "slots_full":
          return "hire_blocked";
        default:
          return "root";
      }
    }

    if (actionId === "fire") {
      onFireCharacter(activeCharacter.id);
      return "fire_success";
    }
  };

  return (
    <div className="tavern-layout">
      <div className="card tavern-crew-card">
        <div className="section-title">Crew Berths</div>
        <div className="tavern-slots-row">
          <div className="capacity-slots">
            {Array.from({ length: state.tavern.crewSlots }).map((_, i) => (
              <div
                key={i}
                className={`slot ${i < state.tavern.hiredCharacterIds.length ? "filled" : ""}`}
              />
            ))}
          </div>
          <div className="tavern-slots-count">
            {state.tavern.hiredCharacterIds.length}/{state.tavern.crewSlots} Hired
          </div>
        </div>
        <div className="tavern-crew-help">
          No swapping at full capacity. Fire first, then hire someone new.
        </div>
      </div>

      <div className="tavern-roster">
        {state.tavern.characters.map((character) => {
          const isActive = character.id === activeCharacter.id;
          const isHired = hiredSet.has(character.id);

          return (
            <button
              key={character.id}
              className={`tavern-character-tile ${isActive ? "active" : ""}`}
              onClick={() => setActiveCharacterId(character.id)}
            >
              <img
                className={`tavern-chat-portrait tavern-character-tile-portrait ${isActive ? "dimmed" : ""}`}
                src={`${BASE}images/characters/${character.portrait}`}
                alt={character.name}
              />
              <span className="tavern-character-tile-main">
                <span className="tavern-character-name">{character.name}</span>
                {isActive && <span className="tavern-character-talking">Talking</span>}
              </span>
              {isHired && <span className="tavern-badge hired">Hired</span>}
            </button>
          );
        })}
      </div>

      <ConversationPanel
        key={activeCharacter.id}
        tree={conversationTree}
        speakerName={activeCharacter.name}
        speakerPortraitSrc={`${BASE}images/characters/${activeCharacter.portrait}`}
        speakerPortraitAlt={activeCharacter.name}
        classNamePrefix="tavern-chat"
        initialNodeId="root"
        instantNodeIds={Object.keys(conversationTree)}
        onAction={handleConversationAction}
      />

      <div className="card tavern-stats-card">
        <div className="section-title">Crew Impact</div>
        {Object.keys(totalBonuses).length === 0 ? (
          <div className="empty-state">No active crew bonuses yet.</div>
        ) : (
          <div className="tavern-bonus-list">
            {Object.entries(totalBonuses).map(([stat, value]) => (
              <div key={stat} className="stat-row">
                <span className="stat-label">{formatStatName(stat)}</span>
                <span className="stat-value stat-bonus">+{fmt(value)}</span>
              </div>
            ))}
          </div>
        )}

        <div className="tavern-stats-sep" />
        <div className="tavern-stats-title">Effective Ship Stats</div>
        <div className="stats-grid">
          {Object.entries(state.stats).map(([stat, value]) => {
            const bonus = totalBonuses[stat] ?? 0;
            return (
              <div key={stat} className="stat-row">
                <span className="stat-label">{formatStatName(stat)}</span>
                <span className="stat-value">
                  {fmt(value)}
                  {bonus !== 0 && <span className="stat-bonus"> (+{fmt(bonus)})</span>}
                </span>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
