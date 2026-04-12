import { useMemo } from "react";
import type { PortState, QuestSummary } from "../types";
import { BASE } from "../utils/helpers";

interface TavernTabProps {
  state: PortState;
  onTalk: (characterId: string) => void;
  onHire: (characterId: string) => void;
  onQuest: (characterId: string) => void;
}

export function TavernTab({
  state,
  onTalk,
  onHire,
  onQuest,
}: TavernTabProps) {
  const hiredSet = useMemo(
    () => new Set(state.crew.hiredCharacterIds),
    [state.crew.hiredCharacterIds],
  );

  // Hide already-hired characters here: tavern is for new recruits.
  const visibleCharacters = useMemo(
    () => state.tavern.characters.filter((c) => !hiredSet.has(c.id)),
    [state.tavern.characters, hiredSet],
  );

  if (visibleCharacters.length === 0) {
    return <div className="empty-state">No new tavern recruits at this port right now.</div>;
  }

  const questByNpcId = new Map<string, QuestSummary>();
  for (const quest of state.quests.available) {
    if (quest.rewardCrewNpcId) continue;
    if (!questByNpcId.has(quest.giverNpcId)) {
      questByNpcId.set(quest.giverNpcId, quest);
    }
  }

  const nextIncompleteStepLabel = state.quests.active?.steps.find((step) => !step.isComplete)?.label ?? "";

  return (
    <div className="npc-card-grid">
        {visibleCharacters.map((character) => {
          const availableQuest = questByNpcId.get(character.id) ?? null;
          const isQuestTurnInTarget = nextIncompleteStepLabel === `Talk to ${character.name}`;

          return (
            <article key={character.id} className="card npc-card npc-card--tavern">
              <img
                className="npc-card-portrait"
                src={`${BASE}images/characters/${character.portrait}`}
                alt={character.name}
              />
              <div className="npc-card-copy">
                <div className="npc-card-name">{character.name}</div>
                <div className="npc-card-role">{character.role}</div>
              </div>
              <div className="npc-card-actions">
                {character.hireable && (
                  <button type="button" className="npc-card-action-btn" onClick={() => onHire(character.id)}>
                    Hire
                  </button>
                )}
                {availableQuest && (
                  <button type="button" className="npc-card-action-btn npc-card-action-btn--quest" onClick={() => onQuest(character.id)}>
                    Accept Quest
                  </button>
                )}
                <button type="button" className="npc-card-action-btn npc-card-action-btn--talk" onClick={() => onTalk(character.id)}>
                  {isQuestTurnInTarget ? "Complete Quest" : "Talk"}
                </button>
              </div>
            </article>
          );
        })}
    </div>
  );
}
