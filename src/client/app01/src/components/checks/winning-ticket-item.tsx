// ===================================================================
// WinningTicketItem Component - Single winning ticket display
// ===================================================================

import { WinBadge } from './win-badge';
import type { WinLevel } from '../../types/verification';

interface WinningTicketItemProps {
  ticket: {
    ticketId: number;
    groupName: string;
    ticketNumbers: number[];
    hits: number;
    winningNumbers: number[];
  };
}

/**
 * Component displaying a single winning ticket with highlighted matched numbers
 * Simplified version for use in Section 2 of DrawCard
 * Shows group name badge, win badge, and ticket numbers (gray for non-matched, blue bold for matched)
 */
export function WinningTicketItem({ ticket }: WinningTicketItemProps) {
  const { groupName, ticketNumbers, hits, winningNumbers } = ticket;

  return (
    <div className="py-4 px-6 hover:bg-white transition-colors">
      {/* Header: Group name badge and Win badge */}
      <div className="flex items-center gap-3 mb-3 flex-wrap">
        {/* Group name badge */}
        {groupName && (
          <span className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-gray-200 text-gray-700">
            {groupName}
          </span>
        )}

        {/* Win badge */}
        <WinBadge count={hits as WinLevel} />
      </div>

      {/* Ticket numbers */}
      <div className="flex gap-2 flex-wrap">
        {ticketNumbers.map((num, index) => {
          const isMatched = winningNumbers.includes(num);
          return (
            <span
              key={index}
              className={`
                inline-flex items-center justify-center w-9 h-9 rounded-full text-sm
                ${isMatched
                  ? 'font-bold bg-blue-600 text-white shadow-md'
                  : 'bg-gray-100 text-gray-700'
                }
              `}
              aria-label={isMatched ? `Liczba ${num} - trafiona` : `Liczba ${num}`}
            >
              {num}
            </span>
          );
        })}
      </div>
    </div>
  );
}
