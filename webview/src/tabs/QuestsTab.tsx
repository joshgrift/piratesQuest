import { useState, type ReactNode } from "react";
import { describeQuestUnlocks } from "../utils/questUnlocks";
import type { PortState, QuestSummary } from "../types";

function hasText(value: string | null | undefined): value is string {
  return typeof value === "string" && value.trim().length > 0;
}

function QuestCard({
  quest,
  action,
  kicker = "Quest",
  collapsed = false,
  onToggle,
}: {
  quest: QuestSummary;
  action?: ReactNode;
  kicker?: string;
  collapsed?: boolean;
  onToggle?: () => void;
}) {
  if (collapsed) {
    return (
      <div className="card quest-card quest-card--collapsed">
        <button type="button" className="quest-card-toggle" onClick={onToggle} aria-expanded="false">
          <div className="quest-card-toggle-copy">
            <div className="quest-card-kicker">{kicker}</div>
            <div className="quest-card-title">{quest.title}</div>
            <div className="quest-card-summary">Completed. Open to review the details.</div>
          </div>
          <span className="quest-card-chevron" aria-hidden="true">+</span>
        </button>
      </div>
    );
  }

  return (
    <div className="card quest-card">
      <div className="quest-card-header">
        <div className="quest-card-heading">
          <div className="quest-card-kicker">{kicker}</div>
          <div className="quest-card-title">{quest.title}</div>
        </div>
        {onToggle ? (
          <button type="button" className="quest-card-toggle quest-card-toggle--inline" onClick={onToggle} aria-expanded="true">
            <span className="quest-card-toggle-label">Hide details</span>
            <span className="quest-card-chevron quest-card-chevron--open" aria-hidden="true">−</span>
          </button>
        ) : (
          <div className="quest-card-giver">
            {quest.revealGiverInQuestLog
              ? `${quest.giverName}${quest.giverPortName ? ` • ${quest.giverPortName}` : ""}`
              : "Unknown lead"}
          </div>
        )}
      </div>

      <div className="quest-card-giver">
        {quest.revealGiverInQuestLog
          ? `${quest.giverName}${quest.giverPortName ? ` • ${quest.giverPortName}` : ""}`
          : "Unknown lead"}
      </div>

      {hasText(quest.description) && (
        <div className="quest-card-desc">{quest.description}</div>
      )}

      <div className="quest-steps">
        {quest.steps.map((step) => (
          <div key={step.label} className={`quest-step ${step.isComplete ? "complete" : ""}`}>
            <span>{step.label}</span>
            <span>
              {step.currentValue}/{step.requiredValue}
            </span>
          </div>
        ))}
      </div>

      <div className="quest-rewards">
        {describeQuestUnlocks(quest.unlocks)}
      </div>

      {action}
    </div>
  );
}

export function QuestsTab({ state }: { state: PortState }) {
  const { active, all, completedIds } = state.quests;
  const [openCompletedQuestIds, setOpenCompletedQuestIds] = useState<string[]>([]);

  const completedQuests = completedIds
    .map((questId) => all.find((quest) => quest.id === questId))
    .filter((quest): quest is QuestSummary => quest !== undefined);

  function toggleCompletedQuest(questId: string) {
    setOpenCompletedQuestIds((currentIds) =>
      currentIds.includes(questId)
        ? currentIds.filter((currentId) => currentId !== questId)
        : [...currentIds, questId],
    );
  }

  return (
    <>
      <div className="section-title">Active Quest</div>
      {active ? (
        <QuestCard quest={active} />
      ) : (
        <div className="card quest-card">
          <div className="quest-card-title">No Active Quest</div>
          <div className="quest-card-desc">
            Nothing is active right now.
          </div>
        </div>
      )}

      <div className="section-sep" />

      <div className="section-title">Completed Quests</div>
      {completedQuests.length > 0 ? (
        completedQuests.map((quest) => {
          const isOpen = openCompletedQuestIds.includes(quest.id);
          return (
            <QuestCard
              key={quest.id}
              quest={quest}
              kicker="Completed Quest"
              collapsed={!isOpen}
              onToggle={() => toggleCompletedQuest(quest.id)}
            />
          );
        })
      ) : (
        <div className="card quest-card">
          <div className="quest-card-title">No Completed Quests</div>
          <div className="quest-card-desc">
            Finish a quest and it will be logged here.
          </div>
        </div>
      )}
    </>
  );
}
