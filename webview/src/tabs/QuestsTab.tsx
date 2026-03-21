import type { ReactNode } from "react";
import { sendIpc } from "../utils/ipc";
import type { PortState, QuestSummary } from "../types";

function QuestCard({
  quest,
  action,
}: {
  quest: QuestSummary;
  action?: ReactNode;
}) {
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
        Unlocks: {quest.unlocks.length > 0 ? quest.unlocks.join(", ") : "Nothing yet"}
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
            No job is currently in progress. Check the available list below for leads.
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
            ? `Talk to ${quest.giverName}${quest.giverPortName ? ` at ${quest.giverPortName}` : ""} to accept this quest.`
            : "The way to begin this quest is still a mystery. Keep exploring and talking to people.";

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
            Nothing new is unlocked right now. Finish active work or keep poking around the world.
          </div>
        </div>
      )}

      {completedIds.length > 0 && (
        <div className="quest-history">Completed quests: {completedIds.length}</div>
      )}
    </>
  );
}
