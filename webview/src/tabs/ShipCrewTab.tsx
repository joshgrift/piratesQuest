import { useEffect, useMemo, useState } from "react";
import { ConversationPanel } from "../components/ConversationPanel";
import type { ConversationTree } from "../components/ConversationPanel";
import type { PortState, TavernCharacter } from "../types";
import { BASE, fmt, formatStatName } from "../utils/helpers";

function getCrewImpact(state: PortState): Record<string, number> {
  const hired = new Set(state.tavern.hiredCharacterIds);
  const totals: Record<string, number> = {};

  for (const character of state.tavern.characters) {
    if (!hired.has(character.id)) continue;
    for (const change of character.statChanges) {
      if (change.modifier !== "Additive") continue;
      totals[change.stat] = (totals[change.stat] ?? 0) + change.value;
    }
  }

  return totals;
}

function buildOnboardDialogue(character: TavernCharacter): ConversationTree {
  const additiveChanges = character.statChanges.filter((s) => s.modifier === "Additive");
  const bonusText = additiveChanges.length === 0
    ? "No direct numbers from my post, Captain, but discipline stays tight."
    : additiveChanges
      .map((s) => `${formatStatName(s.stat)} +${fmt(s.value)}`)
      .join(", ");

  return {
    root: {
      text: `${character.name} tips their hat. \"${character.role} on duty, Captain. What d'ye need?\"`,
      responses: [
        { label: "Give me your status report.", next: "status" },
        { label: "What do ye add to the ship?", next: "impact" },
        { label: "Any advice for this voyage?", next: "advice" },
      ],
    },
    status: {
      text: `\"Crew is steady and watch is sharp. We keep this hull ready for trouble.\"`,
      responses: [{ label: "Back", next: "root" }],
    },
    impact: {
      text: `\"Current contribution: ${bonusText}\"`,
      responses: [{ label: "Back", next: "root" }],
    },
    advice: {
      text: "\"Keep cargo under control, keep powder dry, and never sail blind into a broadside lane.\"",
      responses: [{ label: "Back", next: "root" }],
    },
  };
}

export function ShipCrewTab({ state }: { state: PortState }) {
  const hiredSet = new Set(state.tavern.hiredCharacterIds);
  const hiredCrew = state.tavern.characters.filter((c) => hiredSet.has(c.id));
  const impact = getCrewImpact(state);
  const [activeCharacterId, setActiveCharacterId] = useState(hiredCrew[0]?.id ?? "");

  useEffect(() => {
    const stillExists = hiredCrew.some((c) => c.id === activeCharacterId);
    if (!stillExists) {
      setActiveCharacterId(hiredCrew[0]?.id ?? "");
    }
  }, [hiredCrew, activeCharacterId]);

  const activeCharacter = hiredCrew.find((c) => c.id === activeCharacterId) ?? hiredCrew[0] ?? null;
  const dialogue = useMemo(
    () => (activeCharacter ? buildOnboardDialogue(activeCharacter) : null),
    [activeCharacter],
  );

  return (
    <>
      <div className="section-title">Crew Berths</div>
      <div className="card">
        <div className="tavern-slots-row">
          <div className="capacity-slots">
            {Array.from({ length: state.tavern.crewSlots }).map((_, i) => (
              <div key={i} className={`slot ${i < hiredCrew.length ? "filled" : ""}`} />
            ))}
          </div>
          <div className="tavern-slots-count">
            {hiredCrew.length}/{state.tavern.crewSlots} Hired
          </div>
        </div>
        <div className="tavern-crew-help">
          Talk to any hired crewmate below for onboard reports while at sea.
        </div>
      </div>

      {hiredCrew.length === 0 ? (
        <div className="empty-state">No active crew yet. Hire crew in a port tavern.</div>
      ) : (
        <>
          <div className="tavern-roster">
            {hiredCrew.map((character) => {
              const isActive = character.id === activeCharacter?.id;
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
                    <span className="tavern-character-talking">{character.role}</span>
                  </span>
                  <span className="tavern-badge hired">Aboard</span>
                </button>
              );
            })}
          </div>

          {activeCharacter && dialogue && (
            <ConversationPanel
              key={activeCharacter.id}
              tree={dialogue}
              speakerName={activeCharacter.name}
              speakerPortraitSrc={`${BASE}images/characters/${activeCharacter.portrait}`}
              speakerPortraitAlt={activeCharacter.name}
              classNamePrefix="tavern-chat"
              initialNodeId="root"
              instantNodeIds={["root"]}
            />
          )}
        </>
      )}

      <div className="section-title">Crew Impact</div>
      <div className="card">
        {Object.keys(impact).length === 0 ? (
          <div className="empty-state">No active crew bonuses yet.</div>
        ) : (
          <div className="stats-grid">
            {Object.entries(impact).map(([stat, bonus]) => (
              <div key={stat} className="stat-row">
                <span className="stat-label">{formatStatName(stat)}</span>
                <span className="stat-value stat-bonus">+{fmt(bonus)}</span>
              </div>
            ))}
          </div>
        )}
      </div>

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
