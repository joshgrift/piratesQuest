import type { PortState } from "../types";

interface ProgressSnapshot {
  itemsCollected: Record<string, number>;
  itemsBought: Record<string, number>;
  itemsSold: Record<string, number>;
  soldProfit: Record<string, number>;
  portsVisitedCount: number;
  portsVisited: string[];
  cannonballsShot: number;
  shipsHit: number;
  shipsSunk: number;
  boughtComponentNames: string[];
  hiredCrewIds: string[];
  talkedToNpcIds: string[];
  highestShipTierReached: number;
  totalMoneyEarned: number;
  totalMoneySpent: number;
}

interface PlayerProgressState {
  lifetime: ProgressSnapshot;
  completedQuestIds: string[];
}

interface StatCardData {
  label: string;
  value: string;
  hint: string;
  accent: "gold" | "sea" | "coral" | "jade";
}

interface CargoRow {
  item: string;
  collected: number;
  bought: number;
  sold: number;
  profit: number;
  activity: number;
}

const EMPTY_PROGRESS: ProgressSnapshot = {
  itemsCollected: {},
  itemsBought: {},
  itemsSold: {},
  soldProfit: {},
  portsVisitedCount: 0,
  portsVisited: [],
  cannonballsShot: 0,
  shipsHit: 0,
  shipsSunk: 0,
  boughtComponentNames: [],
  hiredCrewIds: [],
  talkedToNpcIds: [],
  highestShipTierReached: 0,
  totalMoneyEarned: 0,
  totalMoneySpent: 0,
};

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function readNumber(source: Record<string, unknown>, camelKey: string, pascalKey: string): number {
  const value = source[camelKey] ?? source[pascalKey];
  return typeof value === "number" ? value : 0;
}

function readStringArray(source: Record<string, unknown>, camelKey: string, pascalKey: string): string[] {
  const value = source[camelKey] ?? source[pascalKey];
  return Array.isArray(value) ? value.filter((entry): entry is string => typeof entry === "string") : [];
}

function readNumberRecord(source: Record<string, unknown>, camelKey: string, pascalKey: string): Record<string, number> {
  const value = source[camelKey] ?? source[pascalKey];
  if (!isRecord(value)) return {};

  const parsed: Record<string, number> = {};
  for (const [key, entry] of Object.entries(value)) {
    if (typeof entry === "number") parsed[key] = entry;
  }
  return parsed;
}

function parseLifetimeProgress(serverStateJson: string): PlayerProgressState {
  try {
    const parsed = JSON.parse(serverStateJson) as unknown;
    if (!isRecord(parsed)) {
      return { lifetime: EMPTY_PROGRESS, completedQuestIds: [] };
    }

    const progressValue = parsed.progress ?? parsed.Progress;
    if (!isRecord(progressValue)) {
      return { lifetime: EMPTY_PROGRESS, completedQuestIds: [] };
    }

    const lifetimeValue = progressValue.lifetime ?? progressValue.Lifetime;
    const lifetime = isRecord(lifetimeValue) ? lifetimeValue : {};

    return {
      lifetime: {
        itemsCollected: readNumberRecord(lifetime, "itemsCollected", "ItemsCollected"),
        itemsBought: readNumberRecord(lifetime, "itemsBought", "ItemsBought"),
        itemsSold: readNumberRecord(lifetime, "itemsSold", "ItemsSold"),
        soldProfit: readNumberRecord(lifetime, "soldProfit", "SoldProfit"),
        portsVisitedCount: readNumber(lifetime, "portsVisitedCount", "PortsVisitedCount"),
        portsVisited: readStringArray(lifetime, "portsVisited", "PortsVisited"),
        cannonballsShot: readNumber(lifetime, "cannonballsShot", "CannonballsShot"),
        shipsHit: readNumber(lifetime, "shipsHit", "ShipsHit"),
        shipsSunk: readNumber(lifetime, "shipsSunk", "ShipsSunk"),
        boughtComponentNames: readStringArray(lifetime, "boughtComponentNames", "BoughtComponentNames"),
        hiredCrewIds: readStringArray(lifetime, "hiredCrewIds", "HiredCrewIds"),
        talkedToNpcIds: readStringArray(lifetime, "talkedToNpcIds", "TalkedToNpcIds"),
        highestShipTierReached: readNumber(lifetime, "highestShipTierReached", "HighestShipTierReached"),
        totalMoneyEarned: readNumber(lifetime, "totalMoneyEarned", "TotalMoneyEarned"),
        totalMoneySpent: readNumber(lifetime, "totalMoneySpent", "TotalMoneySpent"),
      },
      completedQuestIds: readStringArray(progressValue, "completedQuestIds", "CompletedQuestIds"),
    };
  } catch {
    return { lifetime: EMPTY_PROGRESS, completedQuestIds: [] };
  }
}

function formatNumber(value: number) {
  return new Intl.NumberFormat("en-US").format(value);
}

function formatShipTier(tier: number, shipTiers: PortState["shipTiers"]) {
  const tierData = shipTiers[tier];
  return tierData ? tierData.name : `Tier ${tier + 1}`;
}

function buildCargoRows(progress: ProgressSnapshot): CargoRow[] {
  const itemNames = new Set([
    ...Object.keys(progress.itemsCollected),
    ...Object.keys(progress.itemsBought),
    ...Object.keys(progress.itemsSold),
    ...Object.keys(progress.soldProfit),
  ]);

  return [...itemNames]
    .map((item) => {
      const collected = progress.itemsCollected[item] ?? 0;
      const bought = progress.itemsBought[item] ?? 0;
      const sold = progress.itemsSold[item] ?? 0;
      const profit = progress.soldProfit[item] ?? 0;
      return {
        item,
        collected,
        bought,
        sold,
        profit,
        activity: collected + bought + sold,
      };
    })
    .filter((row) => row.activity > 0 || row.profit > 0)
    .sort((left, right) => right.activity - left.activity || right.profit - left.profit || left.item.localeCompare(right.item))
    .slice(0, 5);
}

function buildHighlights(progress: ProgressSnapshot, completedQuestCount: number, shipTiers: PortState["shipTiers"]) {
  const highlights: { label: string; detail: string }[] = [];

  highlights.push({
    label: "Voyage Ledger",
    detail: progress.portsVisitedCount > 0
      ? `Docked ${formatNumber(progress.portsVisitedCount)} times across ${formatNumber(progress.portsVisited.length)} named ports.`
      : "No recorded port calls yet. The map still waits for your first legend.",
  });

  if (progress.totalMoneyEarned > 0 || progress.totalMoneySpent > 0) {
    const net = progress.totalMoneyEarned - progress.totalMoneySpent;
    highlights.push({
      label: "Trade Wake",
      detail: `Earned ${formatNumber(progress.totalMoneyEarned)} gold, spent ${formatNumber(progress.totalMoneySpent)}, netting ${net >= 0 ? "+" : ""}${formatNumber(net)}.`,
    });
  }

  if (progress.highestShipTierReached > 0) {
    highlights.push({
      label: "Flagship Milestone",
      detail: `Reached ${formatShipTier(progress.highestShipTierReached, shipTiers)} and proved this crew can sail heavier steel.`,
    });
  }

  if (progress.shipsSunk > 0 || progress.shipsHit > 0 || progress.cannonballsShot > 0) {
    const accuracy = progress.cannonballsShot > 0
      ? Math.round((progress.shipsHit / progress.cannonballsShot) * 100)
      : 0;
    highlights.push({
      label: "Gun Deck Record",
      detail: `${formatNumber(progress.cannonballsShot)} shots fired, ${formatNumber(progress.shipsHit)} hits landed, ${formatNumber(progress.shipsSunk)} ships sunk, ${accuracy}% strike accuracy.`,
    });
  }

  if (completedQuestCount > 0 || progress.talkedToNpcIds.length > 0 || progress.hiredCrewIds.length > 0) {
    highlights.push({
      label: "Crew Stories",
      detail: `Completed ${formatNumber(completedQuestCount)} quests, met ${formatNumber(progress.talkedToNpcIds.length)} faces, and signed ${formatNumber(progress.hiredCrewIds.length)} sailors to the roster.`,
    });
  }

  return highlights.slice(0, 4);
}

export function StatsTab({ state }: { state: PortState }) {
  const progressState = parseLifetimeProgress(state.serverStateJson);
  const progress = progressState.lifetime;
  const netWorth = progress.totalMoneyEarned - progress.totalMoneySpent;
  const accuracy = progress.cannonballsShot > 0 ? (progress.shipsHit / progress.cannonballsShot) * 100 : 0;
  const cargoRows = buildCargoRows(progress);
  const highlights = buildHighlights(progress, progressState.completedQuestIds.length, state.shipTiers);

  const heroCards: StatCardData[] = [
    {
      label: "Gold Through the Wake",
      value: formatNumber(progress.totalMoneyEarned),
      hint: "Lifetime earnings across all runs and trades.",
      accent: "gold",
    },
    {
      label: "Ports Called",
      value: formatNumber(progress.portsVisitedCount),
      hint: `${formatNumber(progress.portsVisited.length)} unique harbors discovered.`,
      accent: "sea",
    },
    {
      label: "Ships Sent Under",
      value: formatNumber(progress.shipsSunk),
      hint: `${formatNumber(progress.shipsHit)} hits landed from ${formatNumber(progress.cannonballsShot)} shots.`,
      accent: "coral",
    },
    {
      label: "Legend Tier",
      value: formatShipTier(progress.highestShipTierReached, state.shipTiers),
      hint: `${formatNumber(progressState.completedQuestIds.length)} quests finished for the logbook.`,
      accent: "jade",
    },
  ];

  return (
    <div className="stats-tab">
      <section className="stats-hero-card">
        <div className="stats-hero-backdrop" />
        <div className="stats-hero-copy">
          <div className="stats-kicker">Lifetime History</div>
          <h2 className="stats-hero-title">Captain&apos;s Wake</h2>
          <p className="stats-hero-text">
            Every port call, broadside, bargain, and bold promotion leaves a mark here. This is your long-form pirate ledger.
          </p>
        </div>
        <div className="stats-hero-ring" aria-hidden="true">
          <div className="stats-hero-ring-core">
            <span>Net Ledger</span>
            <strong>{netWorth >= 0 ? "+" : ""}{formatNumber(netWorth)}</strong>
          </div>
        </div>
      </section>

      <div className="stats-grid">
        {heroCards.map((card) => (
          <article key={card.label} className={`stats-hero-stat stats-hero-stat--${card.accent}`}>
            <div className="stats-hero-stat-label">{card.label}</div>
            <div className="stats-hero-stat-value">{card.value}</div>
            <div className="stats-hero-stat-hint">{card.hint}</div>
          </article>
        ))}
      </div>

      <div className="stats-layout">
        <section className="card stats-panel">
          <div className="section-title">Career Highlights</div>
          <div className="stats-timeline">
            {highlights.map((highlight) => (
              <article key={highlight.label} className="stats-timeline-row">
                <div className="stats-timeline-pin" />
                <div className="stats-timeline-body">
                  <div className="stats-timeline-label">{highlight.label}</div>
                  <p>{highlight.detail}</p>
                </div>
              </article>
            ))}
          </div>
        </section>

        <section className="card stats-panel">
          <div className="section-title">Battle Chart</div>
          <div className="stats-battle-grid">
            <div className="stats-battle-meter">
              <div className="stats-battle-meter-label">Accuracy</div>
              <div className="stats-battle-meter-track">
                <div
                  className="stats-battle-meter-fill"
                  style={{ width: `${Math.max(8, Math.min(100, accuracy))}%` }}
                />
              </div>
              <div className="stats-battle-meter-value">{Math.round(accuracy)}%</div>
            </div>
            <div className="stats-battle-pill">
              <span>Broadsides</span>
              <strong>{formatNumber(progress.cannonballsShot)}</strong>
            </div>
            <div className="stats-battle-pill">
              <span>Confirmed Hits</span>
              <strong>{formatNumber(progress.shipsHit)}</strong>
            </div>
            <div className="stats-battle-pill">
              <span>Harbor Talks</span>
              <strong>{formatNumber(progress.talkedToNpcIds.length)}</strong>
            </div>
            <div className="stats-battle-pill">
              <span>Crew Recruited</span>
              <strong>{formatNumber(progress.hiredCrewIds.length)}</strong>
            </div>
            <div className="stats-battle-pill">
              <span>Gear Bought</span>
              <strong>{formatNumber(progress.boughtComponentNames.length)}</strong>
            </div>
          </div>
        </section>
      </div>

      <section className="card stats-panel">
        <div className="section-title">Cargo Legends</div>
        {cargoRows.length === 0 ? (
          <div className="empty-state">No lifetime cargo history yet. Trade, gather, or sell to start the ledger.</div>
        ) : (
          <div className="stats-cargo-list">
            {cargoRows.map((row) => {
              const sellWeight = row.activity > 0 ? Math.round((row.sold / row.activity) * 100) : 0;
              return (
                <article key={row.item} className="stats-cargo-row">
                  <div className="stats-cargo-header">
                    <div>
                      <div className="stats-cargo-item">{row.item}</div>
                      <div className="stats-cargo-meta">
                        Collected {formatNumber(row.collected)} · Bought {formatNumber(row.bought)} · Sold {formatNumber(row.sold)}
                      </div>
                    </div>
                    <div className="stats-cargo-profit">{row.profit > 0 ? `+${formatNumber(row.profit)}` : formatNumber(row.activity)}</div>
                  </div>
                  <div className="stats-cargo-bar">
                    <div className="stats-cargo-bar-track" />
                    <div className="stats-cargo-bar-fill" style={{ width: `${Math.max(12, sellWeight)}%` }} />
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </section>
    </div>
  );
}
