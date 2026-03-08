import type { LeaderboardEntry } from "../types";

export function LeaderboardTab({ entries }: { entries: LeaderboardEntry[] }) {
  if (entries.length === 0) {
    return <div className="empty-state">No captains on the board yet.</div>;
  }

  return (
    <>
      <div className="section-title">Top Captains</div>
      <div className="card leaderboard-card">
        <div className="leaderboard-list">
          {entries.map((entry, index) => (
            <div
              key={`${entry.nickname}-${index}`}
              className={`leaderboard-row ${entry.isLocal ? "local" : ""}`}
            >
              <div className="leaderboard-rank">#{index + 1}</div>
              <div className="leaderboard-name">{entry.nickname}</div>
              <div className="leaderboard-score">{entry.trophies} trophies</div>
            </div>
          ))}
        </div>
      </div>
    </>
  );
}
