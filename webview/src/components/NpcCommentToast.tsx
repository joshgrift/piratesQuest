import type { KeyboardEvent } from "react";

export interface NpcCommentToastData {
  id: string;
  portraitSrc: string;
  portraitAlt: string;
  name: string;
  message: string;
}

export function NpcCommentToast({
  comment,
  onDismiss,
}: {
  comment: NpcCommentToastData | null;
  onDismiss: () => void;
}) {
  if (!comment) return null;

  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.key !== "Enter" && event.key !== " ") return;
    event.preventDefault();
    onDismiss();
  };

  return (
    <div className="npc-comment-toast-wrap" aria-live="polite">
      <div
        className="npc-comment-toast"
        role="button"
        tabIndex={0}
        onClick={onDismiss}
        onKeyDown={handleKeyDown}
        aria-label={`Dismiss message from ${comment.name}`}
      >
        <button
          type="button"
          className="npc-comment-toast-close"
          aria-label={`Close message from ${comment.name}`}
          onClick={(event) => {
            event.stopPropagation();
            onDismiss();
          }}
        >
          x
        </button>

        <img
          className="npc-comment-toast-portrait"
          src={comment.portraitSrc}
          alt={comment.portraitAlt}
        />

        <div className="npc-comment-toast-copy">
          <div className="npc-comment-toast-name">{comment.name}</div>
          <div className="npc-comment-toast-message">{comment.message}</div>
        </div>
      </div>
    </div>
  );
}
