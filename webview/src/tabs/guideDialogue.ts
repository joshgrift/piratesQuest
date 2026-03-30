import type { ConversationNode } from "../components/ConversationPanel";
import type { TavernCharacter } from "../types";

export const SCARLETT_CHARACTER: TavernCharacter = {
  id: "scarlett",
  name: "Scarlett",
  role: "First Mate",
  portrait: "character2.png",
  hireable: false,
  statChanges: [],
};

export function buildScarlettDialogue(
  hasAvailableQuest: boolean,
): Record<string, ConversationNode> {
  return {
    root: {
      text: "Ahoy, captain. What can I help ye with today?",
      responses: [
        { label: "Where do I keep track of quests?", next: "quests_log_intro" },
        { label: "I have some questions about ports.", next: "ports_intro" },
        { label: "How do I finish quests?", next: "quests_turnin_root" },
        { label: "Teach me about sailing and combat.", next: "sailing_intro" },
        { label: "What are those AI ships doing out there?", next: "ai_ships_intro" },
        {
          label: "How do I grow stronger out here?",
          next: "progression_intro",
        },
        { label: "Explain resources and trading.", next: "trade_intro" },
        { label: "What if I sink or get overloaded?", next: "danger_intro" },
        {
          label: "If I impress you, do I at least get a smile?",
          next: "flirt_intro",
        },
      ],
    },

    quests_log_intro: {
      text: "Use the Quests button at the top left when ye need a reminder. It shows your active job, what's finished, and what's still waiting on your attention.",
      responses: [
        { label: "How do I finish quests?", next: "quests_turnin_root" },
        { label: "Tell me about sailing and combat.", next: "sailing_intro" },
      ],
    },

    quests_turnin_root: {
      text: "Most quests have two parts: finish the checklist, then come back to the quest giver's port to wrap it up.",
      responses: [
        { label: "Quiz me on that.", next: "quests_turnin_quiz" },
        { label: "Back to the main menu.", next: "root" },
      ],
    },
    quests_turnin_quiz: {
      text: "Right then, captain. If Gideon gives you a quest and every objective is green, what actually finishes it?",
      responses: [
        { label: "Docking back at Saint Johns.", next: "quests_turnin_right" },
        { label: "Just opening the Quests tab.", next: "quests_turnin_wrong" },
        { label: "Talking to any random sailor.", next: "quests_turnin_wrong" },
      ],
    },
    quests_turnin_right: {
      text: "Exactly. Return to the right port and it closes right away.",
      responses: [{ label: "Back to the main menu.", next: "root" }],
    },
    quests_turnin_wrong: {
      text: "Not quite. Finish the steps first, then return to the port where the quest giver is. That's what closes most quests.",
      responses: [{ label: "Back to the main menu.", next: "root" }],
    },

    ports_intro: {
      text: "Ports are your reset button. Sell cargo, buy supplies, patch the hull, and get organized before you head back out. What part do you want to go over?",
      responses: [
        {
          label: "What should I handle first when I dock?",
          next: "ports_basics",
        },
        {
          label: "Explain the shipyard, components, and stats.",
          next: "ports_shipyard",
        },
        { label: "What about the market and the vault?", next: "ports_market" },
        {
          label: "How do tavern crew and quest talks fit in?",
          next: "ports_people",
        },
      ],
    },
    ports_basics: {
      text: "First check whether you're damaged, broke, or carrying way too much junk. Then sell what you don't need, repair if the hull got chewed up, and leave with coin and cannonballs instead of a bad feeling.",
      responses: [
        { label: "Tell me about the shipyard.", next: "ports_shipyard" },
        { label: "And the market and vault?", next: "ports_market" },
      ],
    },
    ports_shipyard: {
      text: "The shipyard is for repairs and upgrades. Components change stats like speed, cargo space, damage, and gathering power. The Stats panel shows the final result after your gear and crew bonuses are all added together.",
      responses: [
        { label: "How do I pick good upgrades?", next: "ports_upgrades" },
        { label: "What about the market and vault?", next: "ports_market" },
      ],
    },
    ports_upgrades: {
      text: "Think in roles. More cargo helps trading runs, stronger damage and range help in fights, and harvesting tools help gathering trips. You only have so many slots, so build for the job you're about to do, not the imaginary perfect ship in your head.",
      responses: [{ label: "Back to port questions.", next: "ports_intro" }],
    },
    ports_market: {
      text: "The market is where you buy low and sell high. The vault is where you stash valuables so one bad trip doesn't ruin your whole evening. If you're about to do something risky, bank the good stuff first.",
      responses: [{ label: "Back to port questions.", next: "ports_intro" }],
    },
    ports_people: {
      text: "Ports are also where you get context. Taverns give you recruits, quest givers, and the kind of local gossip that sounds useless right up until it's very useful. The questline points you at the basics, and the people in port fill in the gaps.",
      responses: [{ label: "Back to port questions.", next: "ports_intro" }],
    },

    sailing_intro: {
      text: "At sea, momentum matters. A heavy ship handles worse, so plan turns early and don't charge into a bad angle unless you really mean it. Which part do you want first?",
      responses: [
        { label: "Give me the sailing basics.", next: "sailing_basics" },
        { label: "How does combat work?", next: "combat_basics" },
        { label: "Any survival advice?", next: "sailing_survival" },
        { label: "Remind me about cannon controls.", next: "combat_controls" },
      ],
    },
    sailing_basics: {
      text: "W moves you forward, S slows you down, and A or D turns the ship. Cargo weight matters too. When the hold gets too full, the ship feels sluggish and tight turns start feeling like bad ideas.",
      responses: [
        { label: "Where do I keep track of quests?", next: "quests_log_intro" },
        { label: "How does combat work?", next: "combat_basics" },
        { label: "Any survival advice?", next: "sailing_survival" },
      ],
    },
    combat_basics: {
      text: "Q fires the left side and E fires the right. Every volley costs cannonballs, so keep some in the hold. If a fight starts going badly, remember that ports are safe and ego does not repair hull damage.",
      responses: [
        { label: "Any survival advice?", next: "sailing_survival" },
        { label: "Remind me about cannon controls.", next: "combat_controls" },
        { label: "Back to sea lessons.", next: "sailing_intro" },
      ],
    },
    combat_controls: {
      text: "Q is port, the left side. E is starboard, the right side. Get that wrong once and you'll remember forever, mostly because it feels embarrassing.",
      responses: [{ label: "Back to sea lessons.", next: "sailing_intro" }],
    },
    sailing_survival: {
      text: "Don't sail blind. Watch your hull, keep enough goods for repairs, and don't start a dangerous run while already overloaded. A lot of smart wins come from saying 'not this fight' and leaving.",
      responses: [{ label: "Back to sea lessons.", next: "sailing_intro" }],
    },

    ai_ships_intro: {
      text: "Those are sea rogues, captain. They roam, watch for nearby ships, and try to swing into broadside range. They do not care about quests, and they do not call a port safe just because you do.",
      responses: [
        { label: "Quiz me on that.", next: "ai_ships_quiz" },
        { label: "Back to the main menu.", next: "root" },
      ],
    },
    ai_ships_quiz: {
      text: "Say a raider follows you right up to the harbor mouth. What should you assume?",
      responses: [
        { label: "The port aura makes it harmless too.", next: "ai_ships_wrong" },
        { label: "It might still shoot if it has the angle.", next: "ai_ships_right" },
        { label: "It will stop because AI ships collect resources instead.", next: "ai_ships_wrong" },
      ],
    },
    ai_ships_right: {
      text: "Aye. Player ports are safe for captains, not a holy oath for every rogue hull on the sea. Keep your angle tidy.",
      responses: [{ label: "Back to the main menu.", next: "root" }],
    },
    ai_ships_wrong: {
      text: "Not this time. AI ships ignore the port truce, so treat them like danger until you've actually shaken them or sunk them.",
      responses: [{ label: "Back to the main menu.", next: "root" }],
    },

    trade_intro: {
      text: "Trade and gathering keep everything else running. Goods pay for upgrades, repairs, cannonballs, and every other bad decision you want to fund. What do you want to know?",
      responses: [
        { label: "How does trading work between ports?", next: "trade_routes" },
        { label: "What are resources actually for?", next: "trade_resources" },
        { label: "How do I gather faster?", next: "trade_collecting" },
        {
          label: "What does the stats panel help me read?",
          next: "trade_stats",
        },
      ],
    },
    trade_routes: {
      text: "Buy where prices are low, haul to a port where prices are better, and try not to leave yourself so loaded that you handle like a brick. Profit feels great right up until a full cargo hold gets you trapped somewhere stupid.",
      responses: [
        { label: "What are resources actually for?", next: "trade_resources" },
        { label: "How do I gather faster?", next: "trade_collecting" },
      ],
    },
    trade_resources: {
      text: "Wood and Fish help with repairs. Iron and Coin feed upgrades. Tea is a solid trade good, and cannonballs keep your arguments persuasive. Ideally every haul helps set up the next one.",
      responses: [
        { label: "How do I gather faster?", next: "trade_collecting" },
        { label: "Back to trade questions.", next: "trade_intro" },
      ],
    },
    trade_collecting: {
      text: "Look for the red harvest circles in the world, then sail close enough for the collection marker to appear. Hold position while it fills and pays out. Gathering tools help, but a full hold will still cut the trip short.",
      responses: [
        {
          label: "What does the stats panel help me read?",
          next: "trade_stats",
        },
        { label: "Back to trade questions.", next: "trade_intro" },
      ],
    },
    trade_stats: {
      text: "The Stats panel is where you check whether your build is actually doing something. It's useful for separating 'this sounds cool' from 'this is measurably better.'",
      responses: [{ label: "Back to trade questions.", next: "trade_intro" }],
    },

    progression_intro: {
      text: "Progress mostly comes from three things: quests that unlock systems, crew and gear that improve the ship, and smart trips that bring back more than they risk. Which part do you want me to break down?",
      responses: [
        { label: "Tell me about quests.", next: "quests_intro" },
        { label: "How do crew and ship growth work?", next: "crew_intro" },
        {
          label: "What happens if I sink or want to track progress?",
          next: "progress_tracking",
        },
        { label: "How do ship class upgrades fit in?", next: "ship_tiers" },
      ],
    },
    quests_intro: {
      text: hasAvailableQuest
        ? "Quests are the easiest way to learn the game. I have a starter job ready, and finishing it unlocks more of the port."
        : "Quests teach systems, unlock features, and point you at the next useful thing to learn. Most of them also expect you to return to the giver's port once the checklist is done.",
      responses: hasAvailableQuest
        ? [
            {
              label: "Give me that starter job.",
              action: "accept_scarlett_quest",
            },
            { label: "What does it unlock?", next: "quests_unlocks" },
            { label: "How do turn-ins work?", next: "quests_turnin_root" },
          ]
        : [
            { label: "What do quests usually unlock?", next: "quests_unlocks" },
            { label: "How do turn-ins work?", next: "quests_turnin_root" },
          ],
    },
    quests_unlocks: {
      text: "A quest might unlock a feature, a new lead, or just a better habit. The important part is that the questline teaches the essentials, and I'm here for the follow-up questions the UI doesn't answer.",
      responses: [
        { label: "Back to progression topics.", next: "progression_intro" },
      ],
    },
    quest_accept_success: {
      text: "Nice. Quest accepted. Check the Quests tab, follow the steps, and come back if you want help with any of it.",
      responses: [
        { label: "Back to progression topics.", next: "progression_intro" },
      ],
    },
    quests_already_started: {
      text: "You're already working on that one. Open the Quests tab and it'll show what's active, what's finished, and what's left.",
      responses: [
        { label: "Back to progression topics.", next: "progression_intro" },
      ],
    },
    crew_intro: {
      text: "Crew give bonuses and let you specialize. Hire people who match the kind of trip you want to run, then check the Crew and Stats panels to see what actually changed.",
      responses: [
        { label: "How do ship class upgrades fit in?", next: "ship_tiers" },
        { label: "Back to progression topics.", next: "progression_intro" },
      ],
    },
    ship_tiers: {
      text: "Ship class upgrades are a bigger commitment than components. They change the hull itself and usually give you room for more gear. Make sure your income can support the jump before you buy one and then immediately regret it.",
      responses: [
        { label: "Back to progression topics.", next: "progression_intro" },
      ],
    },
    progress_tracking: {
      text: "If you sink, bad planning gets expensive fast, so don't risk more than the trip is worth. For long-term progress, the Stats panel and Hall of Captains help you see how your ship and your captain are improving over time.",
      responses: [
        { label: "Back to progression topics.", next: "progression_intro" },
      ],
    },

    danger_intro: {
      text: "This is the part people ignore until the game smacks them with it. Weight, damage, and greed all punish sloppy planning. What do you want spelled out?",
      responses: [
        {
          label: "What happens when I'm overburdened?",
          next: "danger_overburdened",
        },
        {
          label: "What should I remember before risky runs?",
          next: "danger_preparation",
        },
        { label: "What happens when I die?", next: "danger_death" },
      ],
    },
    danger_overburdened: {
      text: "An overloaded ship handles worse, turns slower, and gives problems more time to catch up to you. If a run might get dangerous, don't leave port already sailing like a refrigerator.",
      responses: [
        { label: "What happens when I die?", next: "danger_death" },
        { label: "Back to danger questions.", next: "danger_intro" },
      ],
    },
    danger_preparation: {
      text: "Before a risky run, patch the hull, stock cannonballs, carry repair goods, and stash anything you really don't want to lose. Most disasters start back in port with someone saying, 'eh, this is probably fine.'",
      responses: [
        { label: "What happens when I die?", next: "danger_death" },
        { label: "Back to danger questions.", next: "danger_intro" },
      ],
    },
    danger_death: {
      text: "Sinking is expensive because you lose gear, time, and momentum all at once. That's why smart players use the vault, pay attention to what they're carrying, and only risk what the trip can justify.",
      responses: [{ label: "Back to danger questions.", next: "danger_intro" }],
    },

    flirt_intro: {
      text: "Bold opener. Keep going. I like confidence as long as it comes with at least one good decision.",
      responses: [
        {
          label: "You have a dangerous amount of confidence, Scarlett.",
          next: "flirt_smooth",
        },
        {
          label: "I'd like to hear you laugh when I'm not sinking.",
          next: "flirt_confident",
        },
        {
          label: "So this is the part where I'm supposed to be charming?",
          next: "flirt_playful",
        },
      ],
    },
    flirt_smooth: {
      text: "That's pretty smooth. But charm without common sense is how people end up underwater, so I'd like both.",
      responses: [
        {
          label: "Then tell me the smart plan and I'll follow it.",
          next: "flirt_advice",
        },
        { label: "Maybe you just like hearing me try.", next: "flirt_tease" },
      ],
    },
    flirt_confident: {
      text: "Reckless and kind of sweet. Dangerous mix. I like confidence, but I like stocked cannonballs more.",
      responses: [
        {
          label: "So what's the smart move before a fight?",
          next: "flirt_advice",
        },
        {
          label: "Then save me a seat and watch me work.",
          next: "flirt_tease",
        },
      ],
    },
    flirt_playful: {
      text: "A little, yes. Be careful or I'll start expecting decent timing and decent banter every time you dock.",
      responses: [
        {
          label: "Then give me a lesson worth remembering.",
          next: "flirt_advice",
        },
        {
          label: "I'll return with better loot and better lines.",
          next: "flirt_tease",
        },
      ],
    },
    flirt_advice: {
      text: "Before you go chasing glory, stash your valuables, patch the hull, and sail with a plan. Smart, prepared, and a little dangerous looks a lot better than dramatic and sunk.",
      responses: [
        { label: "Now that sounds like approval.", next: "flirt_end" },
        {
          label: "I'll handle business first and come back charming later.",
          next: "flirt_end",
        },
      ],
    },
    flirt_tease: {
      text: "Maybe. But if you come back empty-handed and half-sunk, I'm laughing first and complimenting you second.",
      responses: [
        { label: "Cruel. I adore it.", next: "flirt_end" },
        { label: "Then I'll return with profit and proof.", next: "flirt_end" },
      ],
    },
    flirt_end: {
      text: "Better. Keep your crew alive, keep your plans tighter than your swagger, and maybe next time I dock I'll look genuinely impressed.",
      responses: [{ label: "Ask another question.", next: "root" }],
    },
  };
}
