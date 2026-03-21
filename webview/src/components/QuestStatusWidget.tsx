import type { PortState } from "../types";
import { BASE } from "../utils/helpers";

const QUEST_ICON = `${BASE}icons/flat/treasure-map.svg`;

export function QuestStatusWidget({
  state,
  panelOpen,
  onOpenQuests,
}: {
  state: PortState | null;
  panelOpen: boolean;
  onOpenQuests: () => void;
}) {
  const activeQuest = state?.quests.active;
  if (!state || !activeQuest) return null;

  const completedSteps = activeQuest.steps.filter((step) => step.isComplete).length;
  const totalSteps = Math.max(1, activeQuest.steps.length);
  const progressPct = Math.round((completedSteps / totalSteps) * 100);
  const helperText = activeQuest.isReadyToTurnIn
    ? activeQuest.giverPortName
      ? `Return to ${activeQuest.giverPortName}`
      : `Return to ${activeQuest.giverName}`
    : `${completedSteps}/${totalSteps} steps`;

  return (
    <aside
      className={`quest-status-widget ${panelOpen ? "panel-open" : ""}`}
      aria-label="Active quest widget"
    >
      <button
        type="button"
        className="quest-status-card"
        onClick={onOpenQuests}
      >
        <div className="quest-status-header">
          <div className="quest-status-icon-wrap">
            <img src={QUEST_ICON} alt="" className="quest-status-icon" />
          </div>
          <div className="quest-status-copy">
            <div className="quest-status-kicker">Active Quest</div>
            <div className="quest-status-title">{activeQuest.title}</div>
            <div className="quest-status-meta">
              {helperText} • {progressPct}%
            </div>
          </div>
        </div>

        <div className="quest-status-checklist">
          {activeQuest.steps.map((step) => (
            <div key={step.label} className={`quest-status-row ${step.isComplete ? "complete" : ""}`}>
              <span>{step.label}</span>
              <strong>
                {step.currentValue}/{step.requiredValue}
              </strong>
            </div>
          ))}
        </div>
      </button>
    </aside>
  );
}
