import type { ReactNode } from "react";
import { sendIpc } from "../utils/ipc";
import { describeQuestUnlocks } from "../utils/questUnlocks";
import type { PortState, QuestSummary } from "../types";

function QuestCard({
  quest,
  action,
}: {
  quest: QuestSummary;
  action?: ReactNode;
}) {
  const turnInHint = quest.isReadyToTurnIn
    ? quest.giverPortName
      ? `Head back to ${quest.giverPortName} to wrap this one up.`
      : `Go talk to ${quest.giverName} to wrap this one up.`
    : null;

  return (
    <div className="card quest-card">
      <div className="quest-card-header">
        <div>
          <div className="quest-card-kicker">Quest</div>
          <div className="quest-card-title">{quest.title}</div>
        </div>
        <div className="quest-card-giver">
          {quest.revealGiverInQuestLog
            ? `${quest.giverName}${quest.giverPortName ? ` • ${quest.giverPortName}` : ""}`
            : "Unknown lead"}
        </div>
      </div>

      <div className="quest-card-desc">{quest.description}</div>

      {turnInHint && <div className="quest-helper quest-helper--turnin">{turnInHint}</div>}

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
  const { active, available, completedIds } = state.quests;
  const hasAvailableQuests = available.length > 0;

  return (
    <>
      <div className="section-title">Active Quest</div>
      {active ? (
        <QuestCard quest={active} />
      ) : (
        <div className="card quest-card">
          <div className="quest-card-title">No Active Quest</div>
          <div className="quest-card-desc">
            Nothing is active right now. Check the list below if you want a new lead.
          </div>
        </div>
      )}

      <div className="section-sep" />

      <div className="section-title">Available Quests</div>
      {hasAvailableQuests ? (
        available.map((quest) => {
          const canAcceptHere = quest.canAcceptFromQuestLog;
          const canRevealGiver = quest.revealGiverInQuestLog;
          const helperText = canRevealGiver
            ? `Talk to ${quest.giverName}${quest.giverPortName ? ` at ${quest.giverPortName}` : ""} to start this quest.`
            : "You haven't figured out how this one starts yet. Keep exploring and keep talking to people.";

          return (
            <QuestCard
              key={quest.id}
              quest={quest}
              action={
                canAcceptHere ? (
                  <button
                    type="button"
                    className="confirm-btn quest-accept-btn"
                    onClick={() =>
                      sendIpc({ action: "accept_quest", questId: quest.id, characterId: quest.giverNpcId })
                    }
                  >
                    Accept Quest
                  </button>
                ) : (
                  <div className="quest-helper">{helperText}</div>
                )
              }
            />
          );
        })
      ) : (
        <div className="card quest-card">
          <div className="quest-card-title">No Available Quests</div>
          <div className="quest-card-desc">
            Nothing new is unlocked right now. Finish what you're doing or wander around until someone gives you a quest.
          </div>
        </div>
      )}

      {completedIds.length > 0 && (
        <div className="quest-history">Completed quests: {completedIds.length}</div>
      )}
    </>
  );
}
