import type { ConversationTree } from "./ConversationPanel";
import { ConversationThreadPanel } from "./ConversationThreadPanel";

interface CharacterConversationOverlayProps {
  isOpen: boolean;
  title: string;
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
  title,
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
          <div className="character-chat-overlay-title">{title}</div>
          <button
            type="button"
            className="character-chat-overlay-close-btn"
            onClick={onClose}
          >
            End Conversation
          </button>
        </div>

        <ConversationThreadPanel
          key={`${speakerName}-${title}`}
          tree={tree}
          speakerName={speakerName}
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
