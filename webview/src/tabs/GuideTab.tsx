import type { QuestHudState } from "../types";
import { BASE } from "../utils/helpers";
import { ConversationPanel } from "../components/ConversationPanel";
import { buildScarlettDialogue } from "./guideDialogue";

export function GuideTab({
  quests,
  onAcceptScarlettQuest,
}: {
  quests: QuestHudState;
  onAcceptScarlettQuest: () => void;
}) {
  return (
    <ConversationPanel
      tree={buildScarlettDialogue(quests.available.some((quest) => quest.giverNpcId === "scarlett"))}
      speakerName="Scarlett"
      speakerPortraitSrc={`${BASE}images/characters/character2.png`}
      speakerPortraitAlt="Scarlett"
      classNamePrefix="guide"
      initialNodeId="root"
      instantNodeIds={["root"]}
      onAction={(actionId) => {
        if (actionId !== "accept_scarlett_quest") return;
        const scarlettQuest = quests.available.find((quest) => quest.giverNpcId === "scarlett");
        if (!scarlettQuest) return "quests_already_started";
        onAcceptScarlettQuest();
        return "quest_accept_success";
      }}
    />
  );
}
