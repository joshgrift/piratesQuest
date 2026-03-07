import { BASE } from "../utils/helpers";
import { ConversationPanel } from "../components/ConversationPanel";
import { GUIDE_DIALOGUE } from "./guideDialogue";

export function GuideTab() {
  return (
    <ConversationPanel
      tree={GUIDE_DIALOGUE}
      speakerName="Scarlett"
      speakerPortraitSrc={`${BASE}images/characters/character2.png`}
      speakerPortraitAlt="Scarlett"
      classNamePrefix="guide"
      initialNodeId="root"
      instantNodeIds={["root"]}
    />
  );
}
