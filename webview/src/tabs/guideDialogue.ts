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

export function buildScarlettDialogue(hasAvailableQuest: boolean): Record<string, ConversationNode> {
  return {
    root: {
      text: "Ahoy, captain. What can I help ye with today?",
      responses: [
        { label: "I have some questions about ports.", next: "ports_intro" },
        { label: "Teach me about sailing and combat.", next: "sailing_intro" },
        { label: "How do I grow stronger out here?", next: "progression_intro" },
        { label: "Explain resources and trading.", next: "trade_intro" },
        { label: "What if I sink or get overloaded?", next: "danger_intro" },
        { label: "If I impress ye, do I earn a smile too?", next: "flirt_intro" },
      ],
    },

    ports_intro: {
      text: "Ports are where ye reset the voyage. Sell cargo, buy supplies, patch the hull, and sort the ship before ye cast off again. What part do ye want?",
      responses: [
        { label: "What should I handle first when I dock?", next: "ports_basics" },
        { label: "Explain the shipyard, components, and stats.", next: "ports_shipyard" },
        { label: "What about the market and the vault?", next: "ports_market" },
        { label: "How do tavern crew and quest talks fit in?", next: "ports_people" },
      ],
    },
    ports_basics: {
      text: "First, check whether ye're hurt, broke, or stuffed with cargo. Then sell what ye don't need, repair if the hull took a beating, and make sure ye leave with coin and cannonballs instead of regrets.",
      responses: [
        { label: "Tell me about the shipyard.", next: "ports_shipyard" },
        { label: "And the market and vault?", next: "ports_market" },
      ],
    },
    ports_shipyard: {
      text: "The shipyard is for repairs and upgrades. Components change ship stats like speed, cargo space, damage, and collection power. The Stats panel shows the final numbers after all yer gear and crew bonuses are counted together.",
      responses: [
        { label: "How do I pick good upgrades?", next: "ports_upgrades" },
        { label: "What about the market and vault?", next: "ports_market" },
      ],
    },
    ports_upgrades: {
      text: "Think in roles. More cargo helps trading runs, stronger damage and range help fights, and harvesting tools help gathering trips. Ye've only got so many slots, so build the ship for the job ye plan to do next.",
      responses: [
        { label: "Back to port questions.", next: "ports_intro" },
      ],
    },
    ports_market: {
      text: "The market is where ye buy low and sell high. The vault is where ye stash valuables for safer keeping. If ye're heading into trouble, storing coin and spare goods before ye sail is a sharp captain's habit.",
      responses: [
        { label: "Back to port questions.", next: "ports_intro" },
      ],
    },
    ports_people: {
      text: "Ports are also where ye gather knowledge. Taverns bring ye recruits, quest givers, and local gossip. The questline should nudge ye toward what matters first, and the folk in port help fill in the rest.",
      responses: [
        { label: "Back to port questions.", next: "ports_intro" },
      ],
    },

    sailing_intro: {
      text: "At sea, momentum matters. A heavy ship handles worse, so plan turns early and don't charge into a bad lane unless ye mean it. Which lesson do ye want first?",
      responses: [
        { label: "Give me the sailing basics.", next: "sailing_basics" },
        { label: "How does combat work?", next: "combat_basics" },
        { label: "Any survival advice?", next: "sailing_survival" },
        { label: "Remind me about cannon controls.", next: "combat_controls" },
      ],
    },
    sailing_basics: {
      text: "W drives ye forward, S reins the ship in, and A or D swings the bow. Cargo weight matters too. When the hold gets too full, the ship feels sluggish and tight turns get riskier.",
      responses: [
        { label: "How does combat work?", next: "combat_basics" },
        { label: "Any survival advice?", next: "sailing_survival" },
      ],
    },
    combat_basics: {
      text: "Q fires port side and E fires starboard. Every volley spends cannonballs, so keep the hold stocked. If a fight turns ugly, remember that ports are safety and pride won't plug a leaking hull.",
      responses: [
        { label: "Any survival advice?", next: "sailing_survival" },
        { label: "Remind me about cannon controls.", next: "combat_controls" },
        { label: "Back to sea lessons.", next: "sailing_intro" },
      ],
    },
    combat_controls: {
      text: "Q is port, left side. E is starboard, right side. Keep that straight and ye'll waste fewer volleys lookin' foolish broadside to the wrong horizon.",
      responses: [
        { label: "Back to sea lessons.", next: "sailing_intro" },
      ],
    },
    sailing_survival: {
      text: "Don't sail blind. Watch hull health, keep enough goods for repairs, and avoid staying overburdened before a dangerous run. Smart captains win plenty of fights by choosing when not to take one.",
      responses: [
        { label: "Back to sea lessons.", next: "sailing_intro" },
      ],
    },

    trade_intro: {
      text: "Trade and gathering keep the whole voyage afloat. Goods pay for upgrades, repairs, cannonballs, and whatever glory ye can afford. Which part do ye want?",
      responses: [
        { label: "How does trading work between ports?", next: "trade_routes" },
        { label: "What are resources actually for?", next: "trade_resources" },
        { label: "How do I gather faster?", next: "trade_collecting" },
        { label: "What does the stats panel help me read?", next: "trade_stats" },
      ],
    },
    trade_routes: {
      text: "Buy where prices are low, haul where prices are rich, and sell with enough cargo room left to stay nimble. Profit sounds glamorous till a fat hold gets ye cornered in the wrong waters.",
      responses: [
        { label: "What are resources actually for?", next: "trade_resources" },
        { label: "How do I gather faster?", next: "trade_collecting" },
      ],
    },
    trade_resources: {
      text: "Wood and Fish keep repairs flowing. Iron and Coin feed upgrades. Tea is a fine trade good, and cannonballs keep ye from negotiating with empty guns. Every haul should support the next run somehow.",
      responses: [
        { label: "How do I gather faster?", next: "trade_collecting" },
        { label: "Back to trade questions.", next: "trade_intro" },
      ],
    },
    trade_collecting: {
      text: "Sail into collection spots and let the crew work, but don't forget the ship build behind it. Gathering tools and cargo upgrades matter, and a full hold cuts the run short no matter how rich the node is.",
      responses: [
        { label: "What does the stats panel help me read?", next: "trade_stats" },
        { label: "Back to trade questions.", next: "trade_intro" },
      ],
    },
    trade_stats: {
      text: "The Stats panel is the ship's truth-teller. Use it to see whether crew, components, and upgrades are actually pushing the numbers ye care about instead of just sounding fancy in the shop.",
      responses: [
        { label: "Back to trade questions.", next: "trade_intro" },
      ],
    },

    progression_intro: {
      text: "Progress comes from three things: quests that unlock systems, crew and gear that sharpen the ship, and smart voyages that bring home more than they risk. Which part do ye want me to break down?",
      responses: [
        { label: "Tell me about quests.", next: "quests_intro" },
        { label: "How do crew and ship growth work?", next: "crew_intro" },
        { label: "What happens if I sink or want to track progress?", next: "progress_tracking" },
        { label: "How do ship class upgrades fit in?", next: "ship_tiers" },
      ],
    },
    quests_intro: {
      text: hasAvailableQuest
        ? "Quests are the cleanest way to learn the ropes. I've got a starter job ready for ye, and finishing it opens more of the game."
        : "Quests teach systems, unlock features, and push ye toward the next thing worth learnin'. If the log looks quiet, check what you've already started or finished in the Quests tab.",
      responses: hasAvailableQuest
        ? [
            { label: "Give me that starter job.", action: "accept_scarlett_quest" },
            { label: "What does it unlock?", next: "quests_unlocks" },
          ]
        : [
            { label: "What do quests usually unlock?", next: "quests_unlocks" },
          ],
    },
    quests_unlocks: {
      text: "A quest might unlock a feature, a new lead, or just point ye toward the next useful habit. The important bit is this: the questline should teach the essentials, and I fill in the details when ye need a refresher.",
      responses: [
        { label: "Back to progression topics.", next: "progression_intro" },
      ],
    },
    quest_accept_success: {
      text: "Good. Quest accepted. Check the Quests tab, follow the steps, and come back if ye want the finer points explained.",
      responses: [
        { label: "Back to progression topics.", next: "progression_intro" },
      ],
    },
    quests_already_started: {
      text: "That lesson's already underway, captain. Open the Quests tab and it'll show ye what's active, what's finished, and what still needs doing.",
      responses: [
        { label: "Back to progression topics.", next: "progression_intro" },
      ],
    },
    crew_intro: {
      text: "Crew add bonuses and give ye specialists to build around. Hire people who match the voyage ye want, then check the Crew panel and Stats panel to see how the whole ship changes once everyone's aboard.",
      responses: [
        { label: "How do ship class upgrades fit in?", next: "ship_tiers" },
        { label: "Back to progression topics.", next: "progression_intro" },
      ],
    },
    ship_tiers: {
      text: "Ship class upgrades are bigger commitments than components. They change the hull itself, usually giving ye room for more gear and a stronger long-term build. Make sure yer money flow can support the jump before ye buy it.",
      responses: [
        { label: "Back to progression topics.", next: "progression_intro" },
      ],
    },
    progress_tracking: {
      text: "If ye sink, bad planning gets expensive in a hurry, so don't carry more risk than the run is worth. For the long view, the Stats panel and Hall of Captains help ye see how the ship and the captain are growin' over time.",
      responses: [
        { label: "Back to progression topics.", next: "progression_intro" },
      ],
    },

    danger_intro: {
      text: "That's the part captains ignore till the sea teaches it the hard way. Weight, damage, and greed all punish sloppy planning. What do ye want spelled out?",
      responses: [
        { label: "What happens when I'm overburdened?", next: "danger_overburdened" },
        { label: "What should I remember before risky runs?", next: "danger_preparation" },
        { label: "What happens when I die?", next: "danger_death" },
      ],
    },
    danger_overburdened: {
      text: "An overloaded ship handles worse, turns slower, and gives trouble more chances to catch up. If a run might turn violent, don't leave port already sailing like a brick in the tide.",
      responses: [
        { label: "What happens when I die?", next: "danger_death" },
        { label: "Back to danger questions.", next: "danger_intro" },
      ],
    },
    danger_preparation: {
      text: "Before a risky run, patch the hull, stock cannonballs, carry repair goods, and stash what ye can't bear to lose. Most disasters start back in port with a captain who said, 'eh, good enough.'",
      responses: [
        { label: "What happens when I die?", next: "danger_death" },
        { label: "Back to danger questions.", next: "danger_intro" },
      ],
    },
    danger_death: {
      text: "Sinking is expensive because the sea collects her debt in gear and momentum. That's why smart captains use the vault, think about what they're carrying, and only risk what the voyage can justify.",
      responses: [
        { label: "Back to danger questions.", next: "danger_intro" },
      ],
    },

    flirt_intro: {
      text: "Well now, bold of ye. Keep talking, captain. Just know I admire a pirate with charm and sense in equal measure.",
      responses: [
        { label: "Your smile could calm a storm, Scarlett.", next: "flirt_smooth" },
        { label: "I'd sail through cannon fire just to hear ye laugh.", next: "flirt_confident" },
        { label: "Steal my heart if ye must, just leave me enough for rum.", next: "flirt_playful" },
      ],
    },
    flirt_smooth: {
      text: "Mmm. Smooth as polished teak. But sweet talk alone won't keep a ship afloat, so impress me with a little sense too.",
      responses: [
        { label: "Then tell me the smart plan, and I'll follow it.", next: "flirt_advice" },
        { label: "Maybe ye just like hearin' me try.", next: "flirt_tease" },
      ],
    },
    flirt_confident: {
      text: "Reckless and romantic. Dangerous blend. I like a pirate with backbone, but I'd like him better if he remembered to stock cannonballs first.",
      responses: [
        { label: "So what's the smart move before a fight?", next: "flirt_advice" },
        { label: "Then save me a seat and watch me work.", next: "flirt_tease" },
      ],
    },
    flirt_playful: {
      text: "Cheeky too? Careful, captain. I might start expectin' both profits and poetry whenever ye dock.",
      responses: [
        { label: "Then give me a lesson worth rememberin'.", next: "flirt_advice" },
        { label: "I'll return with better loot and better lines.", next: "flirt_tease" },
      ],
    },
    flirt_advice: {
      text: "Before ye chase glory, stash valuables, patch the hull, and sail with purpose. Smart, prepared, and a little dangerous? That's a far better look than sinkin' pretty.",
      responses: [
        { label: "Now that sounds like approval.", next: "flirt_end" },
        { label: "I'll handle business first and come back charming later.", next: "flirt_end" },
      ],
    },
    flirt_tease: {
      text: "Ha. Maybe I do. But if ye come back empty-handed and half-sunk, I'll laugh at the wreck before I praise the line.",
      responses: [
        { label: "Cruel. I adore it.", next: "flirt_end" },
        { label: "Then I'll return with profit and proof.", next: "flirt_end" },
      ],
    },
    flirt_end: {
      text: "That's more like it. Keep yer crew alive, keep yer plans tighter than yer swagger, and maybe next time ye dock I'll save ye a smile meant just for you.",
      responses: [
        { label: "Ask another question.", next: "root" },
      ],
    },
  };
}
