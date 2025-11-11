// ===================================================================
// WinBadge Component - Badge displaying number of hits
// ===================================================================

import { BADGE_VARIANTS, WIN_EMOJIS, WIN_LABELS, type WinLevel } from '../../types/verification';

interface WinBadgeProps {
  count: WinLevel;
}

/**
 * Badge component displaying win level with emoji and text
 * Color-coded based on number of matches
 */
export function WinBadge({ count }: WinBadgeProps) {
  const variant = BADGE_VARIANTS[count];
  const emoji = WIN_EMOJIS[count];
  const label = WIN_LABELS[count];

  // Color classes mapping
  const colorClasses = {
    green: 'bg-green-100 text-green-800',
    blue: 'bg-blue-100 text-blue-800',
    orange: 'bg-orange-100 text-orange-800',
    red: 'bg-red-100 text-red-800'
  };

  return (
    <span
      className={`inline-flex items-center px-3 py-1 rounded-full text-sm font-medium ${colorClasses[variant]}`}
      role="status"
      aria-label={label}
    >
      <span className="mr-1">{emoji}</span>
      {label}
    </span>
  );
}
