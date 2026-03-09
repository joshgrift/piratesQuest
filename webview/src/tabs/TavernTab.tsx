import { useEffect, useMemo, useState } from "react";
import { ConversationPanel } from "../components/ConversationPanel";
import { BASE } from "../utils/helpers";
import type { PortState } from "../types";
import { getDialogueForCharacter } from "./tavernData";

type HireOutcome = "hired" | "already_hired" | "slots_full" | "not_hireable";

interface TavernTabProps {
  state: PortState;
  onHireCharacter: (characterId: string) => HireOutcome;
}

export function TavernTab({
  state,
  onHireCharacter,
}: TavernTabProps) {
  // Tavern is now "recruiting only": discover NPCs and hire them.
  // Crew management (firing, active bonuses, onboard reports) lives in Ship > Crew.
  const hiredSet = useMemo(
    () => new Set(state.crew.hiredCharacterIds),
    [state.crew.hiredCharacterIds],
  );

  // Hide characters already in your crew: Tavern is for new recruits.
  const visibleCharacters = useMemo(
    () => state.tavern.characters.filter((c) => !hiredSet.has(c.id)),
    [state.tavern.characters, hiredSet],
  );

  const [activeCharacterId, setActiveCharacterId] = useState(
    visibleCharacters[0]?.id ?? "",
  );

  useEffect(() => {
    const hasActive = visibleCharacters.some((c) => c.id === activeCharacterId);
    if (!hasActive) {
      setActiveCharacterId(visibleCharacters[0]?.id ?? "");
    }
  }, [visibleCharacters, activeCharacterId]);

  const activeCharacter =
    visibleCharacters.find((c) => c.id === activeCharacterId)
    ?? visibleCharacters[0]
    ?? null;

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

    return tree;
  }, [activeCharacter, hiredSet]);

  if (!activeCharacter) {
    return <div className="empty-state">No new tavern recruits at this port right now.</div>;
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

    return;
  };

  return (
    <div className="tavern-layout">
      <div className="tavern-roster">
        {visibleCharacters.map((character) => {
          const isActive = character.id === activeCharacter.id;

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
    </div>
  );
}
