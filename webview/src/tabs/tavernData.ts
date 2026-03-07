import type { ConversationTree } from "../components/ConversationPanel";
import type { StatChange, TavernCharacter } from "../types";

const DIALOGUE_BY_ID: Record<string, ConversationTree> = {
  "scarred-gunner": {
    root: {
      text: "Name's Briggs. I keep cannons fed and loud. Ye look like someone who can pay in victories.",
      responses: [
        { label: "What are ye best at?", next: "skills" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    skills: {
      text: "I train gunners to fire tighter volleys. Hire me and yer broadsides hit harder.",
      responses: [{ label: "Back", next: "root" }],
    },
    hire_success: {
      text: "Aye, Captain. Powder dry, fuses cut, and cannons singin'.",
      responses: [{ label: "Good. Back to business.", next: "root" }],
    },
    hire_blocked: {
      text: "No berth left for me. Fire someone first, then we talk coin and cannon smoke.",
      responses: [{ label: "Understood.", next: "root" }],
    },
    already_hired: {
      text: "I'm already on yer deck, Captain. Point me at the next target.",
      responses: [{ label: "Back", next: "root" }],
    },
    fire_success: {
      text: "A fair parting. If ye need firepower again, I'll be at this table.",
      responses: [{ label: "Back", next: "root" }],
    },
  },
  "quartermaster-mira": {
    root: {
      text: "Mira the Quartermaster. I make crowded decks run clean. Hire me and yer hold feels bigger overnight.",
      responses: [
        { label: "What do ye improve?", next: "skills" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    skills: {
      text: "Cargo placement and crew drills. Ye gain hold room and cleaner turns through reefs.",
      responses: [{ label: "Back", next: "root" }],
    },
    hire_success: {
      text: "A tidy manifest is a happy crew. I'll have yer ship humming by dawn.",
      responses: [{ label: "Back", next: "root" }],
    },
    hire_blocked: {
      text: "Berths are full, Captain. Fire someone before ye hire fresh hands.",
      responses: [{ label: "Back", next: "root" }],
    },
    already_hired: {
      text: "Already hired, Captain. I've got the ledgers and the ropes in order.",
      responses: [{ label: "Back", next: "root" }],
    },
    fire_success: {
      text: "Understood. I'll settle my tab and leave ye the clean books.",
      responses: [{ label: "Back", next: "root" }],
    },
  },
  "dockside-poet": {
    root: {
      text: "Ahoy! I'm Pip. I don't crew ships, but I know every rumor between here and the maelstrom.",
      responses: [
        { label: "Any rumor worth hearin'?", next: "rumor" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    rumor: {
      text: "Rumor says captains who keep a disciplined crew spend less time patchin' holes and more time takin' gold.",
      responses: [{ label: "Back", next: "root" }],
    },
  },
  "lazy-lookout": {
    root: {
      text: "Old Ned, former lookout. I'll serve if ye need another set of eyes. No fancy bonuses, just a steady hand.",
      responses: [
        { label: "So ye give no stat bonus?", next: "skills" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    skills: {
      text: "No numbers, no tricks. I keep calm in storms and spot trouble early. Some captains value that.",
      responses: [{ label: "Back", next: "root" }],
    },
    hire_success: {
      text: "Aye, Captain. I'll take the night watch and keep quiet.",
      responses: [{ label: "Back", next: "root" }],
    },
    hire_blocked: {
      text: "No spare bunk for me. Fire someone first.",
      responses: [{ label: "Back", next: "root" }],
    },
    already_hired: {
      text: "Already aboard, Captain. I'll stay sharp.",
      responses: [{ label: "Back", next: "root" }],
    },
    fire_success: {
      text: "No hard feelings. I'll be right here if ye need me again.",
      responses: [{ label: "Back", next: "root" }],
    },
  },
};

export function getDialogueForCharacter(character: TavernCharacter): ConversationTree {
  return DIALOGUE_BY_ID[character.id] ?? {
    root: {
      text: `${character.name} nods from the tavern corner.`,
      responses: [{ label: "Back to tavern", next: "root" }],
    },
  };
}

export function toStatBonusMap(statChanges: StatChange[]): Record<string, number> {
  const totals: Record<string, number> = {};

  for (const change of statChanges) {
    if (change.modifier !== "Additive") continue;
    totals[change.stat] = (totals[change.stat] ?? 0) + change.value;
  }

  return totals;
}
