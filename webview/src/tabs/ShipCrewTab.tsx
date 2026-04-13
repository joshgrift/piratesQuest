import type { PortState, QuestSummary, TavernCharacter } from "../types";
import { BASE, fmt, formatStatName } from "../utils/helpers";
import { getCrewImpact } from "../utils/shipBonuses";
import { SCARLETT_CHARACTER_ID } from "./tavernHelpers";

interface ShipCrewTabProps {
  state: PortState;
  onTalk: (characterId: string) => void;
  onFire: (characterId: string) => void;
  onQuest: (characterId: string) => void;
  showSummary?: boolean;
}

export function ShipCrewTab({
  state,
  onTalk,
  onFire,
  onQuest,
  showSummary = true,
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
      <div className="section-title">Crew</div>
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
        {showSummary && (
          <>
            <div className="ship-crew-summary-divider" />
            <div className="ship-crew-summary-head">
              <div className="ship-crew-summary-title">Effective Ship Stats</div>
              <div className="ship-crew-summary-note">Crew bonuses are shown in green.</div>
            </div>
            <div className="stats-grid ship-crew-stats-grid">
              {Object.entries(state.stats).map(([stat, value]) => {
                const bonus = impact[stat] ?? 0;
                return (
                  <div className="stat-row" key={stat}>
                    <span className="stat-label">{formatStatName(stat)}</span>
                    <span className="stat-value">
                      {fmt(value)}
                      {bonus !== 0 && <span className="stat-bonus">(+{fmt(bonus)})</span>}
                    </span>
                  </div>
                );
              })}
            </div>
          </>
        )}
      </div>

      {hiredCrew.length === 0 ? (
        <div className="empty-state">No active crew yet. Hire crew in a port tavern.</div>
      ) : (
          <div className="npc-card-list">
            {hiredCrew.map((character) => {
              const availableQuest = questByNpcId.get(character.id) ?? null;
              const isScarlett = character.id === SCARLETT_CHARACTER_ID;
              return (
                <article key={character.id} className="card npc-card npc-card--list npc-card--crew">
                  <div className="npc-card-main">
                    <div className="npc-card-identity">
                      <img
                        className="npc-card-portrait npc-card-portrait--compact"
                        src={`${BASE}images/characters/${character.portrait}`}
                        alt={character.name}
                      />
                      <div className="npc-card-copy">
                        <div className="npc-card-name">{character.name}</div>
                        <div className="npc-card-role">{character.role}</div>
                        <div className="npc-card-impact npc-card-impact--inline">
                          {renderImpact(character)}
                        </div>
                      </div>
                    </div>
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
    </>
  );
}
