// Shared utility functions used across multiple tabs and components.

export const BASE = import.meta.env.BASE_URL;

// Build a URL for an icon asset under the public/icons directory.
export function iconUrl(folder: "components" | "inventory", filename: string): string {
  return `${BASE}icons/${folder}/${filename}`;
}

// Map an item type name (e.g. "Wood") to its inventory icon URL.
export function inventoryIcon(itemType: string): string {
  const map: Record<string, string> = {
    Wood: "wood.png",
    Iron: "iron.png",
    Fish: "fish.png",
    Tea: "tea.png",
    Coin: "coin.png",
    CannonBall: "cannon_ball.png",
    Trophy: "trophy.png",
  };
  return iconUrl("inventory", map[itemType] ?? "coin.png");
}

// Convert a PascalCase stat name into a human-readable label.
// e.g. "MaxSpeed" → "Max Speed"
export function formatStatName(stat: string): string {
  return stat.replace(/([A-Z])/g, " $1").trim();
}

// Format a number for display: integers stay whole, decimals trim
// trailing zeroes to at most 2 decimal places.
export function fmt(n: number): string {
  const r = Math.round(n * 100) / 100;
  return r % 1 === 0 ? String(r) : r.toFixed(2).replace(/\.?0+$/, "");
}
