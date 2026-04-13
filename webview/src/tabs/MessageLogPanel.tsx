import { useMemo } from "react";

interface MessageLogEntry {
  id: string;
  portraitSrc: string;
  portraitAlt: string;
  name: string;
  message: string;
  receivedAt: number;
}

function formatRelativeTime(receivedAt: number): string {
  const elapsedSeconds = Math.max(0, Math.floor((Date.now() - receivedAt) / 1000));

  if (elapsedSeconds < 10) return "just now";
  if (elapsedSeconds < 60) return `${elapsedSeconds}s ago`;

  const elapsedMinutes = Math.floor(elapsedSeconds / 60);
  if (elapsedMinutes < 60) return `${elapsedMinutes}m ago`;

  const elapsedHours = Math.floor(elapsedMinutes / 60);
  if (elapsedHours < 24) return `${elapsedHours}h ago`;

  const elapsedDays = Math.floor(elapsedHours / 24);
  return `${elapsedDays}d ago`;
}

export function MessageLogPanel({ entries }: { entries: MessageLogEntry[] }) {
  const visibleEntries = useMemo(
    () => [...entries].sort((left, right) => right.receivedAt - left.receivedAt),
    [entries],
  );

  if (visibleEntries.length === 0) {
    return (
      <div className="message-log-panel">
        <div className="message-log-empty">
          <div className="message-log-empty-title">No entries yet</div>
          <p>NPC messages, quest chatter, and crew comments will show up here after they fade from the toast.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="message-log-panel">
      <div className="message-log-hero">
        <div className="message-log-kicker">Ship log</div>
        <h2 className="message-log-title">Recent messages from the crew and harbors</h2>
        <p className="message-log-subtitle">The newest note stays at the top so it is easy to catch up after a busy dock or voyage.</p>
      </div>

      <div className="message-log-list">
        {visibleEntries.map((entry) => (
          <article key={entry.id} className="message-log-entry">
            <img
              className="message-log-entry-portrait"
              src={entry.portraitSrc}
              alt={entry.portraitAlt}
            />
            <div className="message-log-entry-copy">
              <div className="message-log-entry-head">
                <div className="message-log-entry-name">{entry.name}</div>
                <div className="message-log-entry-time">{formatRelativeTime(entry.receivedAt)}</div>
              </div>
              <p className="message-log-entry-message">{entry.message}</p>
            </div>
          </article>
        ))}
      </div>
    </div>
  );
}
