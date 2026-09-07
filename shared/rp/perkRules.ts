// Temporary lab rules, not the final RP economy. Native trees remain in use.
export const PERK_POLICY_VERSION = 1;
export const INITIAL_PERK_POINTS = 3;
export const INITIAL_SKILLS: Record<string, number> = { Smithing: 30, Alchemy: 20, Enchanting: 20 };
export const PERK_RULES = [
  { id: 0x5218e, skill: 'Smithing', level: 20, requires: 0, name: 'Çelik işçiliği' },
  { id: 0xcb40f, skill: 'Smithing', level: 30, requires: 0x5218e, name: 'Cüce işçiliği' },
  { id: 0xbe127, skill: 'Alchemy', level: 15, requires: 0, name: 'Simya temeli' },
  { id: 0xc07ca, skill: 'Alchemy', level: 20, requires: 0xbe127, name: 'Simya uzmanlığı' },
  { id: 0xbee97, skill: 'Enchanting', level: 15, requires: 0, name: 'Efsunlama temeli' },
  { id: 0xc07ce, skill: 'Enchanting', level: 20, requires: 0xbee97, name: 'Efsunlama uzmanlığı' },
];

export function validatePerkSelection(data: { perks: number[]; perkPoints: number; skills: Record<string, number> }, id: unknown): string | null {
  if (!Number.isInteger(id) || typeof id !== 'number') return 'Geçersiz perk';
  const rule = PERK_RULES.find(item => item.id === id);
  if (!rule) return 'Bu perk geçici test kapsamında değil';
  if (data.perks.includes(id)) return null;
  if (!Number.isFinite(data.perkPoints) || data.perkPoints < 1) return 'Yetersiz perk puanı';
  if (!Number.isFinite(data.skills[rule.skill]) || data.skills[rule.skill] < rule.level) return 'Yetersiz yetenek seviyesi';
  if (rule.requires && !data.perks.includes(rule.requires)) return 'Önceki perk gerekli';
  return null;
}
