import type { PortState, QuestSummary, TavernCharacter } from "../types";
import { BASE, fmt, formatStatName } from "../utils/helpers";
import { SCARLETT_CHARACTER_ID } from "./tavernHelpers";

function getCrewImpact(state: PortState): Record<string, number> {
  const hired = new Set(state.crew.hiredCharacterIds);
  const totals: Record<string, number> = {};

  for (const character of state.crew.characters) {
    if (!hired.has(character.id)) continue;
    for (const change of character.statChanges) {
      if (change.modifier !== "Additive") continue;
      totals[change.stat] = (totals[change.stat] ?? 0) + change.value;
    }
  }

  return totals;
}

interface ShipCrewTabProps {
  state: PortState;
  onTalk: (characterId: string) => void;
  onFire: (characterId: string) => void;
  onQuest: (characterId: string) => void;
}

export function ShipCrewTab({
  state,
  onTalk,
  onFire,
  onQuest,
}: ShipCrewTabProps) {
  const hiredSet = new Set(state.crew.hiredCharacterIds);
  const hiredCrew = state.crew.characters.filter((c) => hiredSet.has(c.id));
  const impact = getCrewImpact(state);
  const questByNpcId = new Map<string, QuestSummary>();

  for (const quest of state.quests.available) {
    if (!questByNpcId.has(quest.giverNpcId)) {
      questByNpcId.set(quest.giverNpcId, quest);
    }
  }

  function renderImpact(character: TavernCharacter) {
    const additiveChanges = character.statChanges.filter((s) => s.modifier === "Additive");
    if (additiveChanges.length === 0) {
      return <div className="npc-card-impact-empty">Guide contact aboard.</div>;
    }

    return (
      <div className="npc-card-impact-list">
        {additiveChanges.map((change) => (
          <div key={`${character.id}-${change.stat}`} className="npc-card-impact-row">
            <span>{formatStatName(change.stat)}</span>
            <strong>+{fmt(change.value)}</strong>
          </div>
        ))}
      </div>
    );
  }

  return (
    <>
      <div className="section-title">Crew Berths</div>
      <div className="card">
        <div className="tavern-slots-row">
          <div className="capacity-slots">
            {Array.from({ length: state.crew.crewSlots }).map((_, i) => (
              <div key={i} className={`slot ${i < hiredCrew.length ? "filled" : ""}`} />
            ))}
          </div>
          <div className="tavern-slots-count">
            {hiredCrew.length}/{state.crew.crewSlots} Hired
          </div>
        </div>
      </div>

      {hiredCrew.length === 0 ? (
        <div className="empty-state">No active crew yet. Hire crew in a port tavern.</div>
      ) : (
          <div className="npc-card-grid npc-card-grid--crew">
            {hiredCrew.map((character) => {
              const availableQuest = questByNpcId.get(character.id) ?? null;
              const isScarlett = character.id === SCARLETT_CHARACTER_ID;
              return (
                <article key={character.id} className="card npc-card npc-card--crew">
                  <img
                    className="npc-card-portrait"
                    src={`${BASE}images/characters/${character.portrait}`}
                    alt={character.name}
                  />
                  <div className="npc-card-copy">
                    <div className="npc-card-name">{character.name}</div>
                    <div className="npc-card-role">{character.role}</div>
                  </div>
                  <div className="npc-card-impact">
                    {renderImpact(character)}
                  </div>
                  <div className="npc-card-actions">
                    {!isScarlett && (
                      <button type="button" className="npc-card-action-btn npc-card-action-btn--danger" onClick={() => onFire(character.id)}>
                        Fire
                      </button>
                    )}
                    <button type="button" className="npc-card-action-btn npc-card-action-btn--talk" onClick={() => onTalk(character.id)}>
                      Talk
                    </button>
                    {availableQuest && (
                      <button type="button" className="npc-card-action-btn npc-card-action-btn--quest" onClick={() => onQuest(character.id)}>
                        Quest
                      </button>
                    )}
                  </div>
                </article>
              );
            })}
          </div>
      )}

      <div className="section-title">Effective Ship Stats</div>
      <div className="card">
        <div className="stats-grid">
          {Object.entries(state.stats).map(([stat, value]) => {
            const bonus = impact[stat] ?? 0;
            return (
              <div className="stat-row" key={stat}>
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
    </>
  );
}
