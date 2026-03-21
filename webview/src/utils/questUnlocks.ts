const UNLOCK_DESCRIPTIONS: Record<string, string> = {
  SellGoods: "the ability to sell goods at the market",
  TavernTalk: "the ability to interact with folks at the tavern",
  BuyGoods: "the ability to buy goods at the market",
  ShipyardComponents: "access to ship components in the shipyard",
  ShipTierUpgrades: "ship class upgrades in the shipyard",
  Vault: "access to the vault for storing valuables",
};

export function describeQuestUnlocks(unlocks: string[]): string {
  if (unlocks.length === 0) {
    return "Nothing new yet.";
  }

  const descriptions = unlocks.map((unlock) => UNLOCK_DESCRIPTIONS[unlock] ?? unlock);

  if (descriptions.length === 1) {
    return `Unlocks ${descriptions[0]}.`;
  }

  if (descriptions.length === 2) {
    return `Unlocks ${descriptions[0]} and ${descriptions[1]}.`;
  }

  const allButLast = descriptions.slice(0, -1).join(", ");
  const last = descriptions[descriptions.length - 1];
  return `Unlocks ${allButLast}, and ${last}.`;
}
