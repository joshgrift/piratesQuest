import { useEffect, useState, type KeyboardEvent } from "react";

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
  celebrate?: boolean;
  actions?: NpcCommentAction[];
}

const CONFETTI_PIECES = [
  { left: "8%", delay: "0s", rotation: "-24deg", color: "#f2c96f" },
  { left: "16%", delay: "0.08s", rotation: "18deg", color: "#82bcf0" },
  { left: "24%", delay: "0.02s", rotation: "-10deg", color: "#f08a5d" },
  { left: "32%", delay: "0.12s", rotation: "28deg", color: "#8fd694" },
  { left: "41%", delay: "0.04s", rotation: "-18deg", color: "#f7e7a1" },
  { left: "50%", delay: "0.1s", rotation: "14deg", color: "#db6c79" },
  { left: "58%", delay: "0.01s", rotation: "-32deg", color: "#6fd3ff" },
  { left: "66%", delay: "0.09s", rotation: "20deg", color: "#f2c96f" },
  { left: "74%", delay: "0.05s", rotation: "-16deg", color: "#9f86ff" },
  { left: "82%", delay: "0.14s", rotation: "24deg", color: "#8fd694" },
  { left: "90%", delay: "0.06s", rotation: "-22deg", color: "#ff9f68" },
];

export function NpcCommentToast({
  comment,
  queueCount,
  onDismiss,
}: {
  comment: NpcCommentToastData | null;
  queueCount: number;
  onDismiss: () => void;
}) {
  const [countdownProgress, setCountdownProgress] = useState(1);
  const hasActions = (comment?.actions?.length ?? 0) > 0;
  const hasQueuedFollowups = queueCount > 1;
  const shouldAutoAdvance = comment !== null && hasQueuedFollowups && !hasActions;

  useEffect(() => {
    if (!shouldAutoAdvance) {
      setCountdownProgress(1);
      return;
    }

    const durationMs = 5000;
    const startedAt = performance.now();
    setCountdownProgress(1);

    const intervalId = window.setInterval(() => {
      const elapsedMs = performance.now() - startedAt;
      const nextProgress = Math.max(0, 1 - (elapsedMs / durationMs));
      setCountdownProgress(nextProgress);

      if (elapsedMs < durationMs) return;

      window.clearInterval(intervalId);
      onDismiss();
    }, 100);

    return () => {
      window.clearInterval(intervalId);
    };
  }, [comment?.id, onDismiss, shouldAutoAdvance]);

  if (!comment) return null;

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
        className={`npc-comment-toast ${comment.celebrate ? "npc-comment-toast--celebrate" : ""}`}
        role={hasActions ? "dialog" : "button"}
        tabIndex={hasActions ? -1 : 0}
        onClick={hasActions ? undefined : onDismiss}
        onKeyDown={handleKeyDown}
        aria-label={hasActions ? `${comment.name} conversation` : `Dismiss message from ${comment.name}`}
      >
        {comment.celebrate && (
          <div className="npc-comment-toast-confetti" aria-hidden="true">
            {CONFETTI_PIECES.map((piece, index) => (
              <span
                key={`${comment.id}-confetti-${index}`}
                className="npc-comment-toast-confetti-piece"
                style={{
                  left: piece.left,
                  animationDelay: piece.delay,
                  rotate: piece.rotation,
                  backgroundColor: piece.color,
                }}
              />
            ))}
          </div>
        )}
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
          {hasQueuedFollowups && (
            <div className="npc-comment-toast-queue">
              <div className="npc-comment-toast-queue-head">
                <span className="npc-comment-toast-queue-badge">
                  {queueCount - 1} more
                </span>
                {shouldAutoAdvance && (
                  <span className="npc-comment-toast-queue-timer">
                    Next in {Math.max(1, Math.ceil(countdownProgress * 5))}s
                  </span>
                )}
              </div>
              {shouldAutoAdvance && (
                <div className="npc-comment-toast-progress" aria-hidden="true">
                  <div
                    className="npc-comment-toast-progress-fill"
                    style={{ transform: `scaleX(${countdownProgress})` }}
                  />
                </div>
              )}
            </div>
          )}
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
