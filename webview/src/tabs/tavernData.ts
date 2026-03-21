import type { ConversationTree } from "../components/ConversationPanel";
import type { StatChange, TavernCharacter } from "../types";

const DIALOGUE_BY_ID: Record<string, ConversationTree> = {
  "gideon-gearlock": {
    root: {
      text: "Gideon Gearlock. I watch prices, remember everything, and get suspicious whenever someone says they're 'just vibing the market.'",
      responses: [
        { label: "What markets do you watch?", next: "markets" },
        { label: "Any advice for a trader?", next: "advice" },
        { label: "Are you for hire aboard my ship?", next: "work_open" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    markets: {
      text: "Saint Johns usually pays well for food after storms. Krakenfall gets hungry for iron whenever the gunsmiths start pretending they're 'almost caught up.' They never are.",
      responses: [
        { label: "How do you read that so fast?", next: "methods" },
        { label: "Back", next: "root" },
      ],
    },
    methods: {
      text: "I watch dock delays, lamp oil orders, and which captains leave smiling for no obvious reason. Prices tell the story before the ledger catches up.",
      responses: [{ label: "Back", next: "root" }],
    },
    advice: {
      text: "Don't panic-sell. If the spread is weak, wait. A patient cargo hold makes more money than a captain who gets nervous and calls it strategy.",
      responses: [{ label: "Back", next: "root" }],
    },
    work_open: {
      text: "If I work aboard your ship, trust comes first and money comes second. I don't sail with captains who spend faster than they think.",
      responses: [
        { label: "What do you expect from a captain?", next: "work_expect" },
        { label: "What would you do on my deck?", next: "work_terms" },
        { label: "Back", next: "root" },
      ],
    },
    work_expect: {
      text: "Discipline. Clean logs. And no selling cargo just to feel rich for twelve seconds at the dock.",
      responses: [{ label: "And your terms?", next: "work_terms" }],
    },
    work_terms: {
      text: "I tighten up every sale. Small margins, repeated often. That's how you end up rich instead of merely loud.",
      responses: [
        { label: "Would you join my crew?", action: "probe_hire" },
        { label: "Back", next: "work_open" },
      ],
    },
    hire_offer: {
      text: "Then we understand each other. Give me a bunk and I'll make your sales cleaner.",
      responses: [
        { label: "Join my crew.", action: "hire" },
        { label: "Need more thought.", next: "root" },
      ],
    },
    hire_success: {
      text: "Good. I'll have your next unload priced before we're fully tied off.",
      responses: [{ label: "Back", next: "root" }],
    },
    hire_blocked: {
      text: "No berth, no bargain. Make room first.",
      responses: [{ label: "Back", next: "root" }],
    },
    already_hired: {
      text: "I'm already aboard. Listen when I say sell and we'll both sleep better.",
      responses: [{ label: "Back", next: "root" }],
    },
    fire_success: {
      text: "Then keep your own books. Try not to bleed coin all over them.",
      responses: [{ label: "Back", next: "root" }],
    },
  },

  "tommy-fuse": {
    root: {
      text: "Tommy Fuse. Fast hands, fast mouth, and a professional interest in not exploding by accident.",
      responses: [
        { label: "Where did you learn guns?", next: "origin" },
        { label: "How do you speed a shot?", next: "science" },
        { label: "Are you for hire aboard my ship?", next: "work_open" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    origin: {
      text: "I ran powder from battery to battery during the harbor wars. It was a terrible first job, honestly, but it taught me to move.",
      responses: [{ label: "Back", next: "root" }],
    },
    science: {
      text: "Dry powder, tighter pack, cleaner vent. Most crews throw away power before the ball even leaves the barrel, then act shocked about it.",
      responses: [
        { label: "Sounds dangerous.", next: "danger" },
        { label: "Back", next: "root" },
      ],
    },
    danger: {
      text: "It is dangerous. That's why I'm obsessed with drill. Fast only works when everyone knows what they're doing.",
      responses: [{ label: "Back", next: "root" }],
    },
    work_open: {
      text: "If I sail with you, I handle gun prep and firing rhythm. No panic shots. No random hero nonsense.",
      responses: [
        { label: "What rules do you keep?", next: "work_rules" },
        { label: "What do I gain from that?", next: "work_terms" },
        { label: "Back", next: "root" },
      ],
    },
    work_rules: {
      text: "The crew breathes between volleys, reloads in sequence, and never hands me wet powder unless they want a speech.",
      responses: [{ label: "Fine. Terms?", next: "work_terms" }],
    },
    work_terms: {
      text: "Your shots leave quicker and cleaner. Better launches, better pressure, fewer fights where everyone just hopes for the best.",
      responses: [
        { label: "Would you join my crew?", action: "probe_hire" },
        { label: "Back", next: "work_open" },
      ],
    },
    hire_offer: {
      text: "Say the word and I'll be in your powder room before sunset.",
      responses: [
        { label: "Join my crew.", action: "hire" },
        { label: "Not yet.", next: "root" },
      ],
    },
    hire_success: {
      text: "Yes. Your next broadside is going to feel a lot better.",
      responses: [{ label: "Back", next: "root" }],
    },
    hire_blocked: {
      text: "No bunk left. Make room and I'll make thunder.",
      responses: [{ label: "Back", next: "root" }],
    },
    already_hired: {
      text: "I'm already aboard. Just keep cannonballs stocked and we'll get along great.",
      responses: [{ label: "Back", next: "root" }],
    },
    fire_success: {
      text: "Alright. I'll find something else irresponsible to optimize.",
      responses: [{ label: "Back", next: "root" }],
    },
  },

  "elder-bertram": {
    root: {
      text: "Bertram. Old shipwright. I don't talk much, but the work tends to make the point for me.",
      responses: [
        { label: "What kind of hull work?", next: "craft" },
        { label: "Why so quiet?", next: "quiet" },
        { label: "Are you for hire aboard my ship?", next: "work_open" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    craft: {
      text: "Hidden braces. Tight seams. The kind of work you only notice when a hit should have sunk you and somehow didn't.",
      responses: [{ label: "Back", next: "root" }],
    },
    quiet: {
      text: "I used to speak more. Lost a crew once. After that, I decided timber was better company and usually made fewer bad arguments.",
      responses: [{ label: "Back", next: "root" }],
    },
    work_open: {
      text: "If I sign on, I stay below deck and work. I don't do deck speeches or dramatic pointing.",
      responses: [
        { label: "What do you need from me?", next: "work_need" },
        { label: "What would change on the ship?", next: "work_terms" },
        { label: "Back", next: "root" },
      ],
    },
    work_need: {
      text: "Time in port and no fool telling me to rush wet planks. That's how you build regrets.",
      responses: [{ label: "Understood. Terms?", next: "work_terms" }],
    },
    work_terms: {
      text: "I'll reinforce the places that take the worst strain. Your ship will hold a little longer when things go sideways.",
      responses: [
        { label: "Would you join my crew?", action: "probe_hire" },
        { label: "Back", next: "work_open" },
      ],
    },
    hire_offer: {
      text: "Alright. If you keep your word, I'll keep your hull alive.",
      responses: [
        { label: "Join my crew.", action: "hire" },
        { label: "Not yet.", next: "root" },
      ],
    },
    hire_success: {
      text: "I'll start with the ribs. Don't bother me unless we're sinking or on fire. Preferably not both.",
      responses: [{ label: "Back", next: "root" }],
    },
    hire_blocked: {
      text: "No berth free. I can wait.",
      responses: [{ label: "Back", next: "root" }],
    },
    already_hired: {
      text: "I'm already aboard. Work's underway.",
      responses: [{ label: "Back", next: "root" }],
    },
    fire_success: {
      text: "Understood. I'll return to shore quietly.",
      responses: [{ label: "Back", next: "root" }],
    },
  },

  "dorian-blackwake": {
    root: {
      text: "Dorian Blackwake. Say what you need, skip the pity, and don't confuse bluntness with a personality flaw.",
      responses: [
        { label: "Where did you earn that name?", next: "past" },
        { label: "What can you do to enemy ships?", next: "skills" },
        { label: "Are you for hire aboard my ship?", next: "work_open" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    past: {
      text: "Fleet mutiny. Smoke, fire, and one very long night. That's enough history for one drink, and probably enough for two.",
      responses: [
        { label: "Fair. Back.", next: "root" },
        { label: "Any lesson from it?", next: "lesson" },
      ],
    },
    lesson: {
      text: "Hesitation kills faster than cannon fire. Decide, then commit. Wobbling halfway through a choice is how people become cautionary tales.",
      responses: [{ label: "Back", next: "root" }],
    },
    skills: {
      text: "I train crews to hit harder with fewer wasted shots. Broadside discipline, not chaos, and definitely not whatever most tavern heroes think they're doing.",
      responses: [{ label: "Back", next: "root" }],
    },
    work_open: {
      text: "If you want me aboard, run a disciplined deck. I don't work for captains who confuse panic with intensity.",
      responses: [
        { label: "Define hard deck.", next: "work_rules" },
        { label: "What do I get if I do?", next: "work_terms" },
        { label: "Back", next: "root" },
      ],
    },
    work_rules: {
      text: "No screaming, no panic turns, no half-loaded guns. Orders get said once. If I have to repeat myself, something has already gone wrong.",
      responses: [{ label: "And the upside?", next: "work_terms" }],
    },
    work_terms: {
      text: "Your cannons hit with intent. Fewer lucky shots, more clean kills, less nonsense.",
      responses: [
        { label: "Would you join my crew?", action: "probe_hire" },
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
      text: "I'm already on your crew. Stop checking and go pick a fight worth having.",
      responses: [{ label: "Back", next: "root" }],
    },
    fire_success: {
      text: "Fine. The sea still remembers who taught your gunners.",
      responses: [{ label: "Back", next: "root" }],
    },
  },

  "valora-rumwhisper": {
    root: {
      text: "Valora Rumwhisper. I sell information, and unlike advice from strangers at the bar, mine is usually worth hearing.",
      responses: [
        { label: "Tell me a rumor about this port.", next: "port_rumor" },
        { label: "Tell me a rumor about tavern folk.", next: "crew_rumor" },
        { label: "Are you for hire aboard my ship?", next: "work_open" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    port_rumor: {
      text: "The governor audits manifests by moonlight. Move too much black powder and someone starts watching you by breakfast.",
      responses: [{ label: "Back", next: "root" }],
    },
    crew_rumor: {
      text: "Merrick counts axe strokes out loud when he's angry. Dorian drinks water before battle, never rum. Make of that what you like.",
      responses: [{ label: "Back", next: "root" }],
    },
    work_open: {
      text: "Work? I don't swab decks. I keep one ear in every dockside door and the other on who's lying badly.",
      responses: [
        { label: "Could you advise routes from shore?", next: "work_routes" },
        { label: "Would you sail with me at all?", next: "work_terms" },
        { label: "Back", next: "root" },
      ],
    },
    work_routes: {
      text: "Pay for rumors and I'll tell you where not to die. It's one of my more appreciated services.",
      responses: [{ label: "Back", next: "work_open" }],
    },
    work_terms: {
      text: "I trade in words, not berths. Boats sink. My table here has an excellent survival record.",
      responses: [
        { label: "Would you join my crew?", action: "probe_hire" },
        { label: "Back", next: "work_open" },
      ],
    },
    not_hireable: {
      text: "No. I stay where stories wash ashore. Buy a rumor and we both profit.",
      responses: [{ label: "Back", next: "root" }],
    },
  },

  "harlan-bentbeam": {
    root: {
      text: "Harlan Bentbeam. Master woodworker. I spend a lot of time convincing timber not to become firewood at the worst possible moment.",
      responses: [
        { label: "What makes your repairs different?", next: "craft" },
        { label: "Ever built warships?", next: "history" },
        { label: "Are you for hire aboard my ship?", next: "work_open" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    craft: {
      text: "Most people patch holes. I rebalance strain across the frame so small damage doesn't turn into a much more expensive conversation.",
      responses: [{ label: "Back", next: "root" }],
    },
    history: {
      text: "I built patrol cutters and grain barges. Warships taught me this: survival lives in the boring little joints people ignore until one fails.",
      responses: [{ label: "Back", next: "root" }],
    },
    work_open: {
      text: "On your ship I'd handle timber maintenance and emergency brace drills. Not glamorous, but very nice to have when things go bad.",
      responses: [
        { label: "What would you need from crew?", next: "work_need" },
        { label: "What do we gain from that?", next: "work_terms" },
        { label: "Back", next: "root" },
      ],
    },
    work_need: {
      text: "Dry storage, proper tools, and nobody hammering nails across the grain like they're improvising a coffin.",
      responses: [{ label: "And results?", next: "work_terms" }],
    },
    work_terms: {
      text: "You'll recover more steadily between fights. Not dramatic, but reliable beats dramatic more often than people admit.",
      responses: [
        { label: "Would you join my crew?", action: "probe_hire" },
        { label: "Back", next: "work_open" },
      ],
    },
    hire_offer: {
      text: "Yes. I'll keep your hull breathing.",
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
      text: "Governor Caspian Vale. Speak quickly. Administration has many flaws, but excessive free time is not one of them.",
      responses: [
        { label: "Give me the region's history.", next: "history" },
        { label: "What have you learned about the dark fleet?", next: "fleet" },
        { label: "Are you for hire aboard my ship?", next: "work_open" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    history: {
      text: "These ports were stitched together from old fort harbors after the salt wars. Trade survived because armed escorts stopped being optional and became policy.",
      responses: [
        { label: "Who broke that peace?", next: "history_2" },
        { label: "Back", next: "root" },
      ],
    },
    history_2: {
      text: "Privateers first, then masked raiders. Now this dark fleet borrows the worst habits of both.",
      responses: [{ label: "Back", next: "root" }],
    },
    fleet: {
      text: "They test defenses with fast cutters, then hit supply lines. Burned flags, no signatures, disciplined withdrawals. Annoyingly competent, in other words.",
      responses: [
        { label: "Any weak points?", next: "fleet_2" },
        { label: "Back", next: "root" },
      ],
    },
    fleet_2: {
      text: "Their captains overcommit when they lose a clean escape route. Trap them where reefs force narrow turns and they start making mistakes.",
      responses: [{ label: "Back", next: "root" }],
    },
    work_open: {
      text: "If this is a hiring pitch, spare both of us the theater.",
      responses: [
        { label: "Would you ever advise from aboard?", next: "work_terms" },
        { label: "Back", next: "root" },
      ],
    },
    work_terms: {
      text: "I govern ports, Captain. I do not sign under private command.",
      responses: [
        { label: "Would you join my crew?", action: "probe_hire" },
        { label: "Back", next: "work_open" },
      ],
    },
    not_hireable: {
      text: "No. My duty is ashore. Try not to sink before my clerks finish your paperwork.",
      responses: [{ label: "Back", next: "root" }],
    },
  },

  "merrick-ash": {
    root: {
      text: "Merrick Ash. I cut wood, keep moving, and try not to waste words on people who confuse talking with working.",
      responses: [
        { label: "Where did you learn to cut like that?", next: "craft" },
        { label: "What keeps you in these docks?", next: "past" },
        { label: "Are you for hire aboard my ship?", next: "work_open" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    craft: {
      text: "Mountain camps. Winter quotas. If your swing was sloppy, families froze. It was a great system if you enjoy trauma as training.",
      responses: [{ label: "Back", next: "root" }],
    },
    past: {
      text: "War took my kids. Trees don't lie about what they are, so I stayed with trees. They've been better company than most people.",
      responses: [{ label: "Back", next: "root" }],
    },
    work_open: {
      text: "On a ship I'd route wood runs and keep the crew from wasting daylight pretending to be busy.",
      responses: [
        { label: "How strict are you with crew?", next: "work_rules" },
        { label: "What's the gain for me?", next: "work_terms" },
        { label: "Back", next: "root" },
      ],
    },
    work_rules: {
      text: "No wandering. No half loads. We cut, stack, move, and save the complaints for later.",
      responses: [{ label: "So what improves?", next: "work_terms" }],
    },
    work_terms: {
      text: "You'll pull wood faster and cleaner. Less drift, more usable haul, fewer trips where everyone acts surprised by math.",
      responses: [
        { label: "Would you join my crew?", action: "probe_hire" },
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
      text: "Yes. I'll be with the lumber piles.",
      responses: [{ label: "Back", next: "root" }],
    },
  },

  "rafael-tide": {
    root: {
      text: "Rafael Tide. Fisher by instinct, flirt by reputation, and yes, I am aware of both brands.",
      responses: [
        { label: "You flirt with everyone?", next: "flirt" },
        { label: "What makes a good fishing run?", next: "craft" },
        { label: "Are you for hire aboard my ship?", next: "work_open" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    flirt: {
      text: "Only the ones with good posture and better judgment. You might qualify. Jury's still out.",
      responses: [
        { label: "Focus, charmer.", next: "craft" },
        { label: "Back", next: "root" },
      ],
    },
    craft: {
      text: "Read the current lines, time the nets, and never chase dead water. Most crews fish where it's easy, then wonder why dinner feels disappointing.",
      responses: [{ label: "Back", next: "root" }],
    },
    work_open: {
      text: "On your deck I'd tune routes and net timing. Less wandering, heavier holds, fewer sad little fishing runs.",
      responses: [
        { label: "And what do you need from me?", next: "work_need" },
        { label: "How much better are we talking?", next: "work_terms" },
        { label: "Back", next: "root" },
      ],
    },
    work_need: {
      text: "Let me call the fishing windows without second-guessing every turn and we'll get along fine.",
      responses: [{ label: "Alright, terms?", next: "work_terms" }],
    },
    work_terms: {
      text: "You'll gather fish faster with less wasted drift. Simple, efficient, and honestly kind of elegant.",
      responses: [
        { label: "Would you join my crew?", action: "probe_hire" },
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
      text: "Excellent. We'll fill the hold first and workshop the rest later.",
      responses: [{ label: "Back", next: "root" }],
    },
    hire_blocked: {
      text: "No berth free? Cruel world. Make room and I'll come running.",
      responses: [{ label: "Back", next: "root" }],
    },
    already_hired: {
      text: "I'm already aboard. Very professional of you to keep checking, though.",
      responses: [{ label: "Back", next: "root" }],
    },
    fire_success: {
      text: "I'll miss your deck. Try not to miss me too loudly.",
      responses: [{ label: "Back", next: "root" }],
    },
  },

  "silas-quill": {
    root: {
      text: "Silas Quill. Sculptor first, stone worker second, reluctant sailor always. The sea and I remain politely unconvinced by each other.",
      responses: [
        { label: "Show me your sculptor side.", next: "art" },
        { label: "How do you read stone seams?", next: "craft" },
        { label: "Are you for hire aboard my ship?", next: "work_open" },
        { label: "Back to tavern", next: "root" },
      ],
    },
    art: {
      text: "Marble remembers touch. I carve what I can't keep. Mostly people I loved, mostly gone. It's healthier than drinking, probably.",
      responses: [
        { label: "And mining pays for this?", next: "craft" },
        { label: "Back", next: "root" },
      ],
    },
    craft: {
      text: "I follow fault lines by sound. One bad strike turns clean blocks into rubble, which is a nice metaphor and an expensive mistake.",
      responses: [{ label: "Back", next: "root" }],
    },
    work_open: {
      text: "If I sail with you, it's for precision, not adventure. Adventure is usually just poor planning with better branding.",
      responses: [
        { label: "What would you handle?", next: "work_terms" },
        { label: "What do you require?", next: "work_need" },
        { label: "Back", next: "root" },
      ],
    },
    work_need: {
      text: "Patience. Proper tools. And nobody drunk swinging picks near my lines unless they want to become a story.",
      responses: [{ label: "Understood. Terms?", next: "work_terms" }],
    },
    work_terms: {
      text: "Your stone yield improves because every cut becomes intentional. It's shocking how much that helps.",
      responses: [
        { label: "Would you join my crew?", action: "probe_hire" },
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
      text: "Do not mistake my help for enthusiasm. Those are still separate things.",
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
      text: `I'm ${character.name}. What would you like to know?`,
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
