import type { LeaderboardEntry } from "../types";

function formatGold(value: number) {
  return new Intl.NumberFormat("en-US").format(value);
}

function normalizeCaptainName(value: string | null | undefined) {
  return (value ?? "").trim().toLowerCase();
}

function displayCaptainName(value: string | null | undefined) {
  const trimmed = (value ?? "").trim();
  return trimmed.length > 0 ? trimmed : "Unknown Captain";
}

export function LeaderboardPanel({
  entries,
  playerName,
}: {
  entries: LeaderboardEntry[];
  playerName: string;
}) {
  if (entries.length === 0) {
    return <div className="empty-state">No captains have stacked enough gold to enter the hall yet.</div>;
  }

  const topThree = entries.slice(0, 3);
  const normalizedPlayerName = playerName.trim().toLowerCase();
  const localIndex = entries.findIndex((entry) => normalizeCaptainName(entry.captainName) === normalizedPlayerName);
  const localEntry = localIndex >= 0 ? entries[localIndex] : null;

  return (
    <>
      <div className="section-title">Hall of Captains</div>
      <div className="card leaderboard-card">
        <div className="leaderboard-hero">
          <div>
            <div className="leaderboard-kicker">Server Fortune Board</div>
            <div className="leaderboard-title">Every coin in your hold and every coin in your vault.</div>
            <div className="leaderboard-subtitle">
              The harbor's hidden ledger is tallied ashore, then the latest fortunes are nailed to this board each minute.
            </div>
          </div>
          {localEntry ? (
            <div className="leaderboard-local-banner">
              <span className="leaderboard-local-label">Your standing</span>
              <strong>#{localIndex + 1}</strong>
              <span>{formatGold(localEntry.totalGold)} gold</span>
            </div>
          ) : (
            <div className="leaderboard-local-banner muted">
              <span className="leaderboard-local-label">Your standing</span>
              <strong>Unranked</strong>
              <span>Stash more gold to claim a chair.</span>
            </div>
          )}
        </div>

        <div className="leaderboard-podium">
          {topThree.map((entry, index) => {
            const rank = index + 1;
            const isLocal = normalizeCaptainName(entry.captainName) === normalizedPlayerName;
            const captainName = displayCaptainName(entry.captainName);

            return (
              <article
                key={`${captainName}-${rank}`}
                className={`leaderboard-podium-card place-${rank} ${isLocal ? "local" : ""}`}
              >
                <div className="leaderboard-podium-rank">#{rank}</div>
                <div className="leaderboard-podium-name">{captainName}</div>
                <div className="leaderboard-podium-total">{formatGold(entry.totalGold)} gold</div>
                <div className="leaderboard-podium-breakdown">
                  <span>Hold {formatGold(entry.inventoryGold)}</span>
                  <span>Vault {formatGold(entry.vaultGold)}</span>
                </div>
              </article>
            );
          })}
        </div>

        <div className="leaderboard-ledger">
          <div className="leaderboard-ledger-header">
            <span>Rank</span>
            <span>Captain</span>
            <span>Hold</span>
            <span>Vault</span>
            <span>Total</span>
          </div>
          <div className="leaderboard-list">
            {entries.map((entry, index) => {
              const isLocal = normalizeCaptainName(entry.captainName) === normalizedPlayerName;
              const captainName = displayCaptainName(entry.captainName);

              return (
                <div
                  key={`${captainName}-${index}`}
                  className={`leaderboard-row ${isLocal ? "local" : ""}`}
                >
                  <div className="leaderboard-rank">#{index + 1}</div>
                  <div className="leaderboard-name">
                    {captainName}
                    {isLocal ? <span className="leaderboard-badge">You</span> : null}
                  </div>
                  <div className="leaderboard-hold">{formatGold(entry.inventoryGold)}</div>
                  <div className="leaderboard-vault">{formatGold(entry.vaultGold)}</div>
                  <div className="leaderboard-score">{formatGold(entry.totalGold)}</div>
                </div>
              );
            })}
          </div>
        </div>
      </div>
    </>
  );
}

export const LeaderboardTab = LeaderboardPanel;
