import type { ConversationTree } from "./ConversationPanel";
import { ConversationThreadPanel } from "./ConversationThreadPanel";

interface CharacterConversationOverlayProps {
  isOpen: boolean;
  speakerName: string;
  speakerPortraitSrc: string;
  speakerPortraitAlt: string;
  tree: ConversationTree;
  instantNodeIds?: string[];
  onAction?: (actionId: string) => string | void;
  onClose: () => void;
}

export function CharacterConversationOverlay({
  isOpen,
  speakerName,
  speakerPortraitSrc,
  speakerPortraitAlt,
  tree,
  instantNodeIds,
  onAction,
  onClose,
}: CharacterConversationOverlayProps) {
  if (!isOpen) return null;

  return (
    <div className="character-chat-overlay" role="dialog" aria-modal="true" aria-label={`${speakerName} conversation`}>
      <div className="character-chat-overlay-inner">
        <div className="character-chat-overlay-header">
          <button
            type="button"
            className="character-chat-overlay-close-btn"
            onClick={onClose}
          >
            End Conversation
          </button>
        </div>

        <ConversationThreadPanel
          key={speakerName}
          tree={tree}
          speakerPortraitSrc={speakerPortraitSrc}
          speakerPortraitAlt={speakerPortraitAlt}
          initialNodeId="root"
          instantNodeIds={instantNodeIds}
          onAction={onAction}
        />
      </div>
    </div>
  );
}
