import { ConversationPanel, type ConversationTree } from "./ConversationPanel";

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

        <ConversationPanel
          key={`${speakerName}-${title}`}
          tree={tree}
          speakerName={speakerName}
          speakerPortraitSrc={speakerPortraitSrc}
          speakerPortraitAlt={speakerPortraitAlt}
          classNamePrefix="tavern-chat"
          initialNodeId="root"
          instantNodeIds={instantNodeIds}
          onAction={onAction}
        />
      </div>
    </div>
  );
}
