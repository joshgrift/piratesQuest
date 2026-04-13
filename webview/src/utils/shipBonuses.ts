import type { ComponentData, PortState } from "../types";

export interface StatImpactBreakdown {
  additive: number;
  multiplicative: number;
}

export function getCrewImpact(state: PortState): Record<string, number> {
  const hired = new Set(state.crew.hiredCharacterIds);
  const totals: Record<string, number> = {};

  for (const character of state.crew.characters) {
    if (!hired.has(character.id)) continue;

    for (const change of character.statChanges) {
      if (change.modifier !== "Additive") continue;
      totals[change.stat] = (totals[change.stat] ?? 0) + change.value;
    }
  }

  return totals;
}

export function getEquippedComponentImpact(
  equipped: Array<{ count: number; data: ComponentData }>,
): Record<string, StatImpactBreakdown> {
  const totals: Record<string, StatImpactBreakdown> = {};

  for (const { count, data } of equipped) {
    for (const change of data.statChanges) {
      if (!totals[change.stat]) {
        totals[change.stat] = { additive: 0, multiplicative: 1 };
      }

      const current = totals[change.stat]!;
      if (change.modifier === "Additive") {
        current.additive += change.value * count;
      } else {
        current.multiplicative *= Math.pow(change.value, count);
      }
    }
  }

  return totals;
}
