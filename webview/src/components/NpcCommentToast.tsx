import type { KeyboardEvent } from "react";

export interface NpcCommentAction {
  label: string;
  onSelect?: () => void;
  tone?: "primary" | "secondary" | "danger";
  dismissOnSelect?: boolean;
}

export interface NpcCommentToastData {
  id: string;
  portraitSrc: string;
  portraitAlt: string;
  name: string;
  message: string;
  actions?: NpcCommentAction[];
}

export function NpcCommentToast({
  comment,
  onDismiss,
}: {
  comment: NpcCommentToastData | null;
  onDismiss: () => void;
}) {
  if (!comment) return null;

  const hasActions = (comment.actions?.length ?? 0) > 0;

  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (hasActions) return;
    if (event.key !== "Enter" && event.key !== " ") return;
    event.preventDefault();
    onDismiss();
  };

  const handleActionClick = (action: NpcCommentAction) => {
    action.onSelect?.();
    if (action.dismissOnSelect !== false) {
      onDismiss();
    }
  };

  return (
    <div className="npc-comment-toast-wrap" aria-live="polite">
      <div
        className="npc-comment-toast"
        role={hasActions ? "dialog" : "button"}
        tabIndex={hasActions ? -1 : 0}
        onClick={hasActions ? undefined : onDismiss}
        onKeyDown={handleKeyDown}
        aria-label={hasActions ? `${comment.name} conversation` : `Dismiss message from ${comment.name}`}
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
          {hasActions && (
            <div className="npc-comment-toast-actions">
              {comment.actions?.map((action) => (
                <button
                  key={action.label}
                  type="button"
                  className={`npc-comment-toast-action npc-comment-toast-action--${action.tone ?? "secondary"}`}
                  onClick={() => handleActionClick(action)}
                >
                  {action.label}
                </button>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
