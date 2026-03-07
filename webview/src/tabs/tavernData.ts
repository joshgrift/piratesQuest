import type { ConversationTree } from "../components/ConversationPanel";
import type { StatChange, TavernCharacter } from "../types";

const DIALOGUE_BY_ID: Record<string, ConversationTree> = {
  "gideon-gearlock": {
    root: {
      text: "Gideon Gearlock. Merchant ledgers, long memory, and a nose for underpriced cargo.",
      responses: [
        { label: "What markets do ye watch?", next: "markets" },
        { label: "Any advice for a trader-captain?", next: "advice" },
        { label: "Talk about ship work.", next: "work_open" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    markets: {
      text: "Saint Johns pays clean coin for food after storms. Krakenfall pays hardest for iron when gunsmith orders spike.",
      responses: [
        { label: "How do ye read that so fast?", next: "methods" },
        { label: "Back", next: "root" },
      ],
    },
    methods: {
      text: "I count dock delays, lamp oil orders, and which captains leave smiling. Prices tell stories before numbers do.",
      responses: [{ label: "Back", next: "root" }],
    },
    advice: {
      text: "Never sell in panic. Hold one run longer if the spread is weak. A patient hold beats a desperate unload.",
      responses: [{ label: "Back", next: "root" }],
    },
    work_open: {
      text: "Work aboard is trust first, coin second. I do not sail for captains who spend faster than they think.",
      responses: [
        { label: "What do ye expect from a captain?", next: "work_expect" },
        { label: "What would ye do on my deck?", next: "work_terms" },
        { label: "Back", next: "root" },
      ],
    },
    work_expect: {
      text: "Discipline. Logs kept clean. No dumping cargo just to look rich at the dock.",
      responses: [{ label: "And your terms?", next: "work_terms" }],
    },
    work_terms: {
      text: "I negotiate every sale. Small margins, repeated often. That is where fortunes are made.",
      responses: [
        { label: "Ask him plain to sign aboard.", action: "probe_hire" },
        { label: "Back", next: "work_open" },
      ],
    },
    hire_offer: {
      text: "Then we understand each other. Give me a bunk and I'll sharpen your sales.",
      responses: [
        { label: "Join my crew.", action: "hire" },
        { label: "Need more thought.", next: "root" },
      ],
    },
    hire_success: {
      text: "Good. I'll have your next unload priced before we even tie off.",
      responses: [{ label: "Back", next: "root" }],
    },
    hire_blocked: {
      text: "No berth, no bargain. Free space first.",
      responses: [{ label: "Back", next: "root" }],
    },
    already_hired: {
      text: "Already aboard. Sell when I say sell and we'll both sleep richer.",
      responses: [{ label: "Back", next: "root" }],
    },
    fire_success: {
      text: "Then keep your own books, Captain. Try not to bleed coin.",
      responses: [{ label: "Back", next: "root" }],
    },
  },

  "tommy-fuse": {
    root: {
      text: "Tommy Fuse. Young, quick, and never farther than two steps from powder.",
      responses: [
        { label: "Where'd ye learn guns?", next: "origin" },
        { label: "How do ye speed a shot?", next: "science" },
        { label: "Talk about ship work.", next: "work_open" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    origin: {
      text: "Ran powder from battery to battery in the harbor wars. If I slowed down, men died.",
      responses: [{ label: "Back", next: "root" }],
    },
    science: {
      text: "Dry powder, tighter pack, cleaner vent. Most crews waste force before the ball leaves the barrel.",
      responses: [
        { label: "Sounds dangerous.", next: "danger" },
        { label: "Back", next: "root" },
      ],
    },
    danger: {
      text: "Aye. So I obsess over drill. Fast is safe only when everyone moves right.",
      responses: [{ label: "Back", next: "root" }],
    },
    work_open: {
      text: "If I sail with ye, I run gun prep and shot rhythm. No panic firing.",
      responses: [
        { label: "What rules do ye keep?", next: "work_rules" },
        { label: "What do I gain from that?", next: "work_terms" },
        { label: "Back", next: "root" },
      ],
    },
    work_rules: {
      text: "Crews breathe between volleys, reload in sequence, and never hand me wet powder.",
      responses: [{ label: "Fine. Terms?", next: "work_terms" }],
    },
    work_terms: {
      text: "You'll feel shots leave quicker and cleaner. Better launch, better pressure on target ships.",
      responses: [
        { label: "Ask him plain to sign aboard.", action: "probe_hire" },
        { label: "Back", next: "work_open" },
      ],
    },
    hire_offer: {
      text: "Say the word and I'll be at your powder room before sunset.",
      responses: [
        { label: "Join my crew.", action: "hire" },
        { label: "Not yet.", next: "root" },
      ],
    },
    hire_success: {
      text: "Aye! Next broadside's gonna feel different.",
      responses: [{ label: "Back", next: "root" }],
    },
    hire_blocked: {
      text: "No bunk left. Make room and I'll make thunder.",
      responses: [{ label: "Back", next: "root" }],
    },
    already_hired: {
      text: "Already aboard. Keep cannonballs stocked and we're golden.",
      responses: [{ label: "Back", next: "root" }],
    },
    fire_success: {
      text: "Right then. I'll keep my hands busy till you call.",
      responses: [{ label: "Back", next: "root" }],
    },
  },

  "elder-bertram": {
    root: {
      text: "Bertram. Old shipwright. Quiet by habit, useful by trade.",
      responses: [
        { label: "What kind of hull work?", next: "craft" },
        { label: "Why so quiet?", next: "quiet" },
        { label: "Talk about ship work.", next: "work_open" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    craft: {
      text: "Hidden braces. Tight seams. The kind of work ye notice only when a hit should've sunk ye and didn't.",
      responses: [{ label: "Back", next: "root" }],
    },
    quiet: {
      text: "Used to speak more. Lost a crew once. Learned to let timber talk for me.",
      responses: [{ label: "Back", next: "root" }],
    },
    work_open: {
      text: "If I sign on, I stay below deck and keep to my bench.",
      responses: [
        { label: "What do ye need from me?", next: "work_need" },
        { label: "What would change on the ship?", next: "work_terms" },
        { label: "Back", next: "root" },
      ],
    },
    work_need: {
      text: "Time in port and no fool ordering me to rush wet planks.",
      responses: [{ label: "Understood. Terms?", next: "work_terms" }],
    },
    work_terms: {
      text: "I'll thicken her where strain runs hottest. She'll hold a little longer in a hard fight.",
      responses: [
        { label: "Ask him plain to sign aboard.", action: "probe_hire" },
        { label: "Back", next: "work_open" },
      ],
    },
    hire_offer: {
      text: "...Aye. If ye keep your word, I'll keep your hull alive.",
      responses: [
        { label: "Join my crew.", action: "hire" },
        { label: "Not yet.", next: "root" },
      ],
    },
    hire_success: {
      text: "I'll start at the ribs. Do not bother me unless we're sinking.",
      responses: [{ label: "Back", next: "root" }],
    },
    hire_blocked: {
      text: "No berth free. I can wait.",
      responses: [{ label: "Back", next: "root" }],
    },
    already_hired: {
      text: "Already aboard. Work's underway.",
      responses: [{ label: "Back", next: "root" }],
    },
    fire_success: {
      text: "Understood. I'll return to shore quietly.",
      responses: [{ label: "Back", next: "root" }],
    },
  },

  "dorian-blackwake": {
    root: {
      text: "Dorian Blackwake. Say what ye need and skip the pity.",
      responses: [
        { label: "Where'd ye earn that name?", next: "past" },
        { label: "What can ye do to enemy ships?", next: "skills" },
        { label: "Talk about ship work.", next: "work_open" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    past: {
      text: "Fleet mutiny. Smoke, fire, and one long night. That's enough history for one drink.",
      responses: [
        { label: "Fair. Back.", next: "root" },
        { label: "Any lesson from it?", next: "lesson" },
      ],
    },
    lesson: {
      text: "Hesitation kills faster than cannon fire. Decide, then commit.",
      responses: [{ label: "Back", next: "root" }],
    },
    skills: {
      text: "I train crews to hit harder with fewer wasted shots. Broadside discipline, not chaos.",
      responses: [{ label: "Back", next: "root" }],
    },
    work_open: {
      text: "You want me aboard, you run a hard deck. I don't work for soft captains.",
      responses: [
        { label: "Define hard deck.", next: "work_rules" },
        { label: "What do I get if I do?", next: "work_terms" },
        { label: "Back", next: "root" },
      ],
    },
    work_rules: {
      text: "No screaming, no panic turns, no half-loaded guns. Orders once, obeyed once.",
      responses: [{ label: "And the upside?", next: "work_terms" }],
    },
    work_terms: {
      text: "Your cannons hit with intent. Fewer lucky shots, more dead ships.",
      responses: [
        { label: "Ask him plain to sign aboard.", action: "probe_hire" },
        { label: "Back", next: "work_open" },
      ],
    },
    hire_offer: {
      text: "Good. Give me powder, space, and silence when I'm working.",
      responses: [
        { label: "Join my crew.", action: "hire" },
        { label: "Not now.", next: "root" },
      ],
    },
    hire_success: {
      text: "Then point me at something worth sinking.",
      responses: [{ label: "Back", next: "root" }],
    },
    hire_blocked: {
      text: "No room means no deal.",
      responses: [{ label: "Back", next: "root" }],
    },
    already_hired: {
      text: "I'm already yours. Stop checking and start fighting.",
      responses: [{ label: "Back", next: "root" }],
    },
    fire_success: {
      text: "Fine. The sea still remembers who taught your gunners.",
      responses: [{ label: "Back", next: "root" }],
    },
  },

  "valora-rumwhisper": {
    root: {
      text: "Valora Rumwhisper. I sell whispers that keep captains alive.",
      responses: [
        { label: "Tell me a rumor about this port.", next: "port_rumor" },
        { label: "Tell me a rumor about tavern folk.", next: "crew_rumor" },
        { label: "Talk about ship work.", next: "work_open" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    port_rumor: {
      text: "Governor audits manifests by moonlight. Anyone moving black powder in bulk gets watched by dawn.",
      responses: [{ label: "Back", next: "root" }],
    },
    crew_rumor: {
      text: "Merrick counts axe strokes out loud when he's angry. Dorian drinks water before battle, never rum.",
      responses: [{ label: "Back", next: "root" }],
    },
    work_open: {
      text: "Work? I don't swab decks. I keep one ear in every dockside door.",
      responses: [
        { label: "Could ye advise routes from shore?", next: "work_routes" },
        { label: "Would ye sail with me at all?", next: "work_terms" },
        { label: "Back", next: "root" },
      ],
    },
    work_routes: {
      text: "Pay for rumors and I'll tell ye where not to die. That is the deal I make with captains.",
      responses: [{ label: "Back", next: "work_open" }],
    },
    work_terms: {
      text: "I trade words, not berths. Boats sink. My table here does not.",
      responses: [
        { label: "Ask her plain to sign aboard.", action: "probe_hire" },
        { label: "Back", next: "work_open" },
      ],
    },
    not_hireable: {
      text: "Valora smiles thinly. 'No, Captain. I stay where stories wash ashore. But buy a rumor and we both profit.'",
      responses: [{ label: "Back", next: "root" }],
    },
  },

  "harlan-bentbeam": {
    root: {
      text: "Harlan Bentbeam. Master woodworker. I can teach timber to heal instead of split.",
      responses: [
        { label: "What makes your repairs different?", next: "craft" },
        { label: "Ever built warships?", next: "history" },
        { label: "Talk about ship work.", next: "work_open" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    craft: {
      text: "Most men patch holes. I rebalance strain across the frame so small damage stops snowballing.",
      responses: [{ label: "Back", next: "root" }],
    },
    history: {
      text: "Built patrol cutters and grain barges. Warships taught me this: survival lives in the small joints.",
      responses: [{ label: "Back", next: "root" }],
    },
    work_open: {
      text: "Aboard your ship I'd run timber maintenance and emergency brace drills.",
      responses: [
        { label: "What would ye need from crew?", next: "work_need" },
        { label: "What do we gain from that?", next: "work_terms" },
        { label: "Back", next: "root" },
      ],
    },
    work_need: {
      text: "Dry storage, proper tools, and no idiot hammering nails where grain runs wrong.",
      responses: [{ label: "And results?", next: "work_terms" }],
    },
    work_terms: {
      text: "You'll recover steadier between fights. Not dramatic. Reliable.",
      responses: [
        { label: "Ask him plain to sign aboard.", action: "probe_hire" },
        { label: "Back", next: "work_open" },
      ],
    },
    hire_offer: {
      text: "Aye. I'll keep your hull breathing.",
      responses: [
        { label: "Join my crew.", action: "hire" },
        { label: "Need to think.", next: "root" },
      ],
    },
    hire_success: {
      text: "I'll start with your keel seams at first light.",
      responses: [{ label: "Back", next: "root" }],
    },
    hire_blocked: {
      text: "No berth for a carpenter right now.",
      responses: [{ label: "Back", next: "root" }],
    },
    already_hired: {
      text: "Already aboard, Captain. Your beams are in order.",
      responses: [{ label: "Back", next: "root" }],
    },
    fire_success: {
      text: "Understood. I'll leave your ship better than I found it.",
      responses: [{ label: "Back", next: "root" }],
    },
  },

  "governor-caspian": {
    root: {
      text: "Governor Caspian Vale. Speak quickly. Administration does not pause for sentiment.",
      responses: [
        { label: "Give me the region's history.", next: "history" },
        { label: "What have ye learned about the dark fleet?", next: "fleet" },
        { label: "Talk about ship work.", next: "work_open" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    history: {
      text: "These ports were stitched from old fort harbors after the salt wars. Trade survived because armed escorts became law.",
      responses: [
        { label: "Who broke that peace?", next: "history_2" },
        { label: "Back", next: "root" },
      ],
    },
    history_2: {
      text: "Privateers first, then masked raiders. Now this dark fleet borrows from both.",
      responses: [{ label: "Back", next: "root" }],
    },
    fleet: {
      text: "They test defenses with fast cutters, then hit supply chains. Burned flags, no signatures, disciplined withdrawals.",
      responses: [
        { label: "Any weak points?", next: "fleet_2" },
        { label: "Back", next: "root" },
      ],
    },
    fleet_2: {
      text: "Their captains overcommit when denied clean exits. Trap them where reefs force narrow turns.",
      responses: [{ label: "Back", next: "root" }],
    },
    work_open: {
      text: "If this is a hiring pitch, spare us both the theater.",
      responses: [
        { label: "Would ye ever advise from aboard?", next: "work_terms" },
        { label: "Back", next: "root" },
      ],
    },
    work_terms: {
      text: "I govern ports, Captain. I do not sign under private command.",
      responses: [
        { label: "Ask him plain to sign aboard.", action: "probe_hire" },
        { label: "Back", next: "work_open" },
      ],
    },
    not_hireable: {
      text: "Caspian folds his hands. 'No. My duty is ashore. Try not to sink before my clerks finish your file.'",
      responses: [{ label: "Back", next: "root" }],
    },
  },

  "merrick-ash": {
    root: {
      text: "Merrick Ash. I cut wood. I keep moving. I don't waste words.",
      responses: [
        { label: "Where'd ye learn to cut like that?", next: "craft" },
        { label: "What keeps ye in these docks?", next: "past" },
        { label: "Talk about ship work.", next: "work_open" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    craft: {
      text: "Mountain camps. Winter quotas. If your swing was sloppy, families froze.",
      responses: [{ label: "Back", next: "root" }],
    },
    past: {
      text: "War took my kids. Trees don't lie about what they are, so I stayed with trees.",
      responses: [{ label: "Back", next: "root" }],
    },
    work_open: {
      text: "On a ship I'd route wood runs and keep the crew from wasting daylight.",
      responses: [
        { label: "How strict are ye with crew?", next: "work_rules" },
        { label: "What's the gain for me?", next: "work_terms" },
        { label: "Back", next: "root" },
      ],
    },
    work_rules: {
      text: "No wandering. No half loads. We cut, stack, and move.",
      responses: [{ label: "So what improves?", next: "work_terms" }],
    },
    work_terms: {
      text: "You'll pull wood faster and cleaner. Less drift, more usable haul.",
      responses: [
        { label: "Ask him plain to sign aboard.", action: "probe_hire" },
        { label: "Back", next: "work_open" },
      ],
    },
    hire_offer: {
      text: "Fine. I work. You steer.",
      responses: [
        { label: "Join my crew.", action: "hire" },
        { label: "Not today.", next: "root" },
      ],
    },
    hire_success: {
      text: "Point me at timber and stay out of my way.",
      responses: [{ label: "Back", next: "root" }],
    },
    hire_blocked: {
      text: "No berth, no axe on deck.",
      responses: [{ label: "Back", next: "root" }],
    },
    already_hired: {
      text: "Already aboard. Wood runs stay tight.",
      responses: [{ label: "Back", next: "root" }],
    },
    fire_success: {
      text: "Aye. I'll be with the lumber piles.",
      responses: [{ label: "Back", next: "root" }],
    },
  },

  "rafael-tide": {
    root: {
      text: "Rafael Tide. Fisher by instinct, flirt by reputation.",
      responses: [
        { label: "Ye flirt with everyone?", next: "flirt" },
        { label: "What makes a good fishing run?", next: "craft" },
        { label: "Talk about ship work.", next: "work_open" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    flirt: {
      text: "Only captains with good posture and better judgment. You might qualify.",
      responses: [
        { label: "Focus, charmer.", next: "craft" },
        { label: "Back", next: "root" },
      ],
    },
    craft: {
      text: "Read current lines, time the nets, and never chase dead water. Most crews fish where it's easy, not where it's rich.",
      responses: [{ label: "Back", next: "root" }],
    },
    work_open: {
      text: "Aboard your deck I'd tune routes and net timing. Less wandering, heavier holds.",
      responses: [
        { label: "And what do ye need from me?", next: "work_need" },
        { label: "How much better are we talkin'?", next: "work_terms" },
        { label: "Back", next: "root" },
      ],
    },
    work_need: {
      text: "Let me call fishing windows without second-guessing every turn.",
      responses: [{ label: "Alright, terms?", next: "work_terms" }],
    },
    work_terms: {
      text: "You'll gather fish faster with less wasted drift. Simple and pretty.",
      responses: [
        { label: "Ask him plain to sign aboard.", action: "probe_hire" },
        { label: "Back", next: "work_open" },
      ],
    },
    hire_offer: {
      text: "Then give me a berth and I'll make your nets famous.",
      responses: [
        { label: "Join my crew.", action: "hire" },
        { label: "Not yet.", next: "root" },
      ],
    },
    hire_success: {
      text: "Aye, Captain. We'll fill the hold and maybe your heart.",
      responses: [{ label: "Back", next: "root" }],
    },
    hire_blocked: {
      text: "No berth free? Cruel world. Make room and I'll come running.",
      responses: [{ label: "Back", next: "root" }],
    },
    already_hired: {
      text: "Already aboard, gorgeous. Professionally gorgeous.",
      responses: [{ label: "Back", next: "root" }],
    },
    fire_success: {
      text: "I'll miss your deck. Try not to miss me too loudly.",
      responses: [{ label: "Back", next: "root" }],
    },
  },

  "silas-quill": {
    root: {
      text: "Silas Quill. Sculptor first, stone worker second, reluctant sailor always.",
      responses: [
        { label: "Show me your sculptor side.", next: "art" },
        { label: "How do ye read stone seams?", next: "craft" },
        { label: "Talk about ship work.", next: "work_open" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    art: {
      text: "Marble remembers touch. I carve what I cannot keep. Mostly men I loved, mostly gone.",
      responses: [
        { label: "And mining pays for this?", next: "craft" },
        { label: "Back", next: "root" },
      ],
    },
    craft: {
      text: "I follow fault lines by sound. One wrong strike turns clean blocks into rubble.",
      responses: [{ label: "Back", next: "root" }],
    },
    work_open: {
      text: "If I sail with you, it is for precision, not adventure.",
      responses: [
        { label: "What would ye handle?", next: "work_terms" },
        { label: "What do ye require?", next: "work_need" },
        { label: "Back", next: "root" },
      ],
    },
    work_need: {
      text: "Patience. Proper tools. And no drunk swinging picks near my lines.",
      responses: [{ label: "Understood. Terms?", next: "work_terms" }],
    },
    work_terms: {
      text: "Your stone yield improves because each cut becomes intentional.",
      responses: [
        { label: "Ask him plain to sign aboard.", action: "probe_hire" },
        { label: "Back", next: "work_open" },
      ],
    },
    hire_offer: {
      text: "Very well. I'll help your stone runs, then return to my statues.",
      responses: [
        { label: "Join my crew.", action: "hire" },
        { label: "Not now.", next: "root" },
      ],
    },
    hire_success: {
      text: "Do not mistake my help for enthusiasm.",
      responses: [{ label: "Back", next: "root" }],
    },
    hire_blocked: {
      text: "No berth available. Then I return to my studio.",
      responses: [{ label: "Back", next: "root" }],
    },
    already_hired: {
      text: "Already aboard. Keep your crew disciplined around the quarry lines.",
      responses: [{ label: "Back", next: "root" }],
    },
    fire_success: {
      text: "At last. Stone dust was never my favorite perfume.",
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
