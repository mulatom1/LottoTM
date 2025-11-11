// ===================================================================
// ResultTicketItem Component - Single ticket in verification results
// ===================================================================

import { WinBadge } from './win-badge';
import type { WinLevel } from '../../types/verification';

interface ResultTicketItemProps {
  ticket: {
    ticketId: number;
    groupName: string;
    ticketNumbers: number[];
    hits: number;
    winningNumbers: number[];
  };
}

/**
 * Component displaying a single ticket with highlighted matched numbers
 * Shows win badge for 3+ matches, "Brak trafień" for <3 matches
 */
export function ResultTicketItem({ ticket }: ResultTicketItemProps) {
  const { groupName, ticketNumbers, hits, winningNumbers } = ticket;

  return (
    <div className="flex items-center justify-between py-3 px-4 hover:bg-gray-50 transition-colors">
      {/* Ticket numbers */}
      <div className="flex items-center gap-3">
        <div className="flex flex-col">
          <div className="flex items-center gap-2">
            <span className="text-sm text-gray-600">Zestaw:</span>
            {groupName && (
              <span className="text-xs font-medium text-gray-500 bg-gray-100 px-2 py-0.5 rounded">
                {groupName}
              </span>
            )}
          </div>
          <div className="flex gap-2 mt-1">
            {ticketNumbers.map((num, index) => {
              const isMatched = winningNumbers.includes(num);
              return (
                <span
                  key={index}
                  className={`
                    inline-flex items-center justify-center w-8 h-8 rounded-full text-sm
                    ${isMatched
                      ? 'font-bold bg-blue-600 text-white'
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
      </div>

      {/* Win badge or "Brak trafień" */}
      <div>
        {hits >= 3 ? (
          <WinBadge count={hits as WinLevel} />
        ) : (
          <span className="text-gray-500 text-sm" aria-label="Brak wygranej">
            Brak trafień
          </span>
        )}
      </div>
    </div>
  );
}
