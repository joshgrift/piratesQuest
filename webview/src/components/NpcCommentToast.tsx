import { useEffect, useRef, useState, type CSSProperties, type KeyboardEvent } from "react";

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

const AUTO_DISMISS_DURATION_MS = 5000;

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
  const dismissRef = useRef(onDismiss);
  const expiresAtRef = useRef(0);
  const remainingMsRef = useRef(AUTO_DISMISS_DURATION_MS);
  const isPausedRef = useRef(false);
  const hasActions = (comment?.actions?.length ?? 0) > 0;
  const hasQueuedFollowups = queueCount > 1;
  const shouldAutoDismiss = comment !== null && !hasActions;

  useEffect(() => {
    dismissRef.current = onDismiss;
  }, [onDismiss]);

  useEffect(() => {
    if (!shouldAutoDismiss) {
      setCountdownProgress(1);
      remainingMsRef.current = AUTO_DISMISS_DURATION_MS;
      isPausedRef.current = false;
      return;
    }

    // Start the timer when this specific toast appears.
    // Follow-up toasts can be added later without resetting the countdown.
    expiresAtRef.current = performance.now() + AUTO_DISMISS_DURATION_MS;
    remainingMsRef.current = AUTO_DISMISS_DURATION_MS;
    isPausedRef.current = false;
    setCountdownProgress(1);

    const intervalId = window.setInterval(() => {
      if (isPausedRef.current) return;

      const remainingMs = Math.max(0, expiresAtRef.current - performance.now());
      remainingMsRef.current = remainingMs;
      const nextProgress = remainingMs / AUTO_DISMISS_DURATION_MS;
      setCountdownProgress(nextProgress);

      if (remainingMs > 0) return;

      window.clearInterval(intervalId);
      dismissRef.current();
    }, 100);

    return () => {
      window.clearInterval(intervalId);
    };
  }, [comment?.id, shouldAutoDismiss]);

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

  const handleMouseEnter = () => {
    if (!shouldAutoDismiss || isPausedRef.current) return;

    remainingMsRef.current = Math.max(0, expiresAtRef.current - performance.now());
    isPausedRef.current = true;
    setCountdownProgress(remainingMsRef.current / AUTO_DISMISS_DURATION_MS);
  };

  const handleMouseLeave = () => {
    if (!shouldAutoDismiss || !isPausedRef.current) return;

    expiresAtRef.current = performance.now() + remainingMsRef.current;
    isPausedRef.current = false;
  };

  const toastClassName = [
    "npc-comment-toast",
    comment.celebrate ? "npc-comment-toast--celebrate" : "",
    hasQueuedFollowups ? "npc-comment-toast--stacked" : "",
  ].filter(Boolean).join(" ");

  const queueButtonClassName = [
    "npc-comment-toast-queue-badge",
    hasQueuedFollowups ? "npc-comment-toast-queue-badge--stacked" : "npc-comment-toast-queue-badge--solo",
    shouldAutoDismiss ? "npc-comment-toast-queue-badge--timed" : "",
  ].join(" ");

  const queueButtonStyle = shouldAutoDismiss
    ? ({ "--npc-comment-progress": countdownProgress } as CSSProperties)
    : undefined;

  return (
    <div className="npc-comment-toast-wrap" aria-live="polite">
      <div
        className={toastClassName}
        role={hasActions ? "dialog" : "button"}
        tabIndex={hasActions ? -1 : 0}
        onClick={hasActions ? undefined : onDismiss}
        onKeyDown={handleKeyDown}
        onMouseEnter={handleMouseEnter}
        onMouseLeave={handleMouseLeave}
        aria-label={hasActions ? `${comment.name} conversation` : `Dismiss message from ${comment.name}`}
      >
        {hasQueuedFollowups && (
          <>
            <div className="npc-comment-toast-stack npc-comment-toast-stack--back" aria-hidden="true" />
            <div className="npc-comment-toast-stack npc-comment-toast-stack--mid" aria-hidden="true" />
          </>
        )}
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
        <img
          className="npc-comment-toast-portrait"
          src={comment.portraitSrc}
          alt={comment.portraitAlt}
        />

        <div className="npc-comment-toast-copy">
          <div className="npc-comment-toast-kicker">
            {hasActions ? "Waiting on your answer" : "Deck chatter"}
          </div>
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
        <button
          type="button"
          className={queueButtonClassName}
          aria-label={hasQueuedFollowups ? `${queueCount - 1} more waiting` : `Close message from ${comment.name}`}
          style={queueButtonStyle}
          onClick={(event) => {
            event.stopPropagation();
            onDismiss();
          }}
        >
          {hasQueuedFollowups ? (
            <>
              <span className="npc-comment-toast-queue-caption">next</span>
              <span className="npc-comment-toast-queue-count">+{queueCount - 1}</span>
            </>
          ) : (
            <span className="npc-comment-toast-queue-count npc-comment-toast-queue-count--close">close</span>
          )}
        </button>
      </div>
    </div>
  );
}
