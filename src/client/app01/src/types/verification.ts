// ===================================================================
// Verification Types - Helper types for Checks Page
// ===================================================================

/**
 * Win level type - number of matched numbers (3-6)
 */
export type WinLevel = 3 | 4 | 5 | 6;

/**
 * Badge variant for win badges
 */
export type BadgeVariant = 'green' | 'blue' | 'orange' | 'red';

/**
 * Mapping WinLevel to BadgeVariant
 */
export const BADGE_VARIANTS: Record<WinLevel, BadgeVariant> = {
  3: 'green',
  4: 'blue',
  5: 'orange',
  6: 'red'
};

/**
 * Mapping WinLevel to label text
 */
export const WIN_LABELS: Record<WinLevel, string> = {
  3: 'Wygrana 3 (trójka)',
  4: 'Wygrana 4 (czwórka)',
  5: 'Wygrana 5 (piątka)',
  6: 'Wygrana 6 (szóstka)'
};

/**
 * Mapping WinLevel to emoji
 */
export const WIN_EMOJIS: Record<WinLevel, string> = {
  3: '🏆',
  4: '🏆',
  5: '🏆',
  6: '🎉'
};
