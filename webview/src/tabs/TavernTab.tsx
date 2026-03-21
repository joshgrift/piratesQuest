import { useMemo } from "react";
import type { ConversationTree } from "../components/ConversationPanel";
import type { PortState, QuestSummary, TavernCharacter } from "../types";
import { BASE } from "../utils/helpers";
import { describeQuestUnlocks } from "../utils/questUnlocks";
import { getDialogueForCharacter } from "./tavernData";

interface TavernTabProps {
  state: PortState;
  onOpenConversation: (characterId: string) => void;
  activeConversationCharacterId?: string | null;
}

export function buildTavernConversationTree(
  character: TavernCharacter,
  availableQuest: QuestSummary | null,
): ConversationTree {
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
        text: `I'm ${character.name}. Say what you need, Captain.`,
        responses: [{ label: "Leave", next: "root" }],
      },
    };
  }

  if (!tree.hire_offer) {
    tree.hire_offer = {
      text: "I'm interested, is that an offer I hear?",
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
      text: "Alright. I'll meet you at the dock.",
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

  if (availableQuest?.giverNpcId === character.id) {
    tree.root.responses.unshift({ label: `Ask about: ${availableQuest.title}`, next: "quest_offer" });
    tree.quest_offer = {
      text: `${availableQuest.description}\n\n${describeQuestUnlocks(availableQuest.unlocks)}`,
      responses: [
        { label: "Alright, I'm in.", action: "accept_quest" },
        { label: "Maybe later.", next: "root" },
      ],
    };
    tree.quest_accept_success = {
      text: "Perfect. Go do the job, then come back with a story that's at least a little embarrassing.",
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
