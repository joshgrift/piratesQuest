import type { TavernCharacter } from "../types";

export const SCARLETT_CHARACTER_ID = "scarlett";

export function getRandomTalkPhrase(character: TavernCharacter): string {
  if (character.talkPhrases.length === 0) {
    return `${character.name} studies you for a moment, but has nothing to add just now.`;
  }

  const index = Math.floor(Math.random() * character.talkPhrases.length);
  return character.talkPhrases[index] || character.talkPhrases[0] || `${character.name} stays quiet for a beat.`;
}

export function getFirePrompt(character: TavernCharacter): string {
  return character.fireText?.trim() || `${character.name} waits for your decision.`;
}
