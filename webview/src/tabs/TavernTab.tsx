import { useMemo } from "react";
import type { ConversationTree } from "../components/ConversationPanel";
import type { PortState, TavernCharacter } from "../types";
import { BASE } from "../utils/helpers";
import { getDialogueForCharacter } from "./tavernData";

interface TavernTabProps {
  state: PortState;
  onOpenConversation: (characterId: string) => void;
  activeConversationCharacterId?: string | null;
}

export function buildTavernConversationTree(character: TavernCharacter): ConversationTree {
  const baseTree = getDialogueForCharacter(character);
  const tree = Object.fromEntries(
    Object.entries(baseTree).map(([nodeId, node]) => [
      nodeId,
      { ...node, responses: [...node.responses] },
    ]),
  );

  if (!tree.root) {
    return {
      root: {
        text: `Name's ${character.name}. Say what you need, Captain.`,
        responses: [{ label: "Leave", next: "root" }],
      },
    };
  }

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
      text: "No, Captain. I stay ashore.",
      responses: [{ label: "Back", next: "root" }],
    };
  }

  if (!tree.hire_success) {
    tree.hire_success = {
      text: "Yes. I'll be ready at the dock.",
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
}

export function TavernTab({
  state,
  onOpenConversation,
  activeConversationCharacterId,
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

  return (
    <div className="tavern-layout">
      <div className="tavern-roster">
        {visibleCharacters.map((character) => {
          const isTalking = activeConversationCharacterId === character.id;

          return (
            <button
              key={character.id}
              className={`tavern-character-tile ${isTalking ? "active" : ""}`}
              onClick={() => onOpenConversation(character.id)}
            >
              <img
                className="tavern-chat-portrait tavern-character-tile-portrait"
                src={`${BASE}images/characters/${character.portrait}`}
                alt={character.name}
              />
              <span className="tavern-character-tile-main">
                <span className="tavern-character-name">{character.name}</span>
              </span>
            </button>
          );
        })}
      </div>
    </div>
  );
}
