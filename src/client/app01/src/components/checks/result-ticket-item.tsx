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
  drawData: {
    ticketPrice: number | null;
    winPoolCount1: number | null;
    winPoolAmount1: number | null;
    winPoolCount2: number | null;
    winPoolAmount2: number | null;
    winPoolCount3: number | null;
    winPoolAmount3: number | null;
    winPoolCount4: number | null;
    winPoolAmount4: number | null;
  };
}

/**
 * Component displaying a single ticket with highlighted matched numbers
 * Shows win badge for 3+ matches with detailed prize information box
 */
export function ResultTicketItem({ ticket, drawData }: ResultTicketItemProps) {
  const { groupName, ticketNumbers, hits, winningNumbers } = ticket;

  // Map hits to tier data (6 hits = tier 1, 5 hits = tier 2, 4 hits = tier 3, 3 hits = tier 4)
  const getTierData = (hits: number): { tier: number; count: number | null; amount: number | null } | null => {
    switch (hits) {
      case 6:
        return { tier: 1, count: drawData.winPoolCount1, amount: drawData.winPoolAmount1 };
      case 5:
        return { tier: 2, count: drawData.winPoolCount2, amount: drawData.winPoolAmount2 };
      case 4:
        return { tier: 3, count: drawData.winPoolCount3, amount: drawData.winPoolAmount3 };
      case 3:
        return { tier: 4, count: drawData.winPoolCount4, amount: drawData.winPoolAmount4 };
      default:
        return null;
    }
  };

  const tierData = getTierData(hits);

  return (
    <div className="py-4 px-6 hover:bg-gray-50 transition-colors">
      {/* Ticket info and numbers */}
      <div className="flex items-start justify-between mb-3">
        <div className="flex flex-col">
          <div className="flex items-center gap-2 mb-2">
            <span className="text-sm text-gray-600">Zestaw:</span>
            {groupName && (
              <span className="text-xs font-medium text-gray-500 bg-gray-100 px-2 py-0.5 rounded">
                {groupName}
              </span>
            )}
          </div>
          <div className="flex gap-2">
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

      {/* Win info box (only for hits >= 3) */}
      {hits >= 3 && tierData ? (
        <div className="mt-3 p-4 bg-green-50 border border-green-200 rounded-lg">
          {/* Win badge */}
          <div className="mb-3">
            <WinBadge count={hits as WinLevel} />
          </div>

          {/* Prize details */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 text-sm">
            <div>
              <div className="text-gray-600 mb-1">Koszt kuponu:</div>
              <div className="font-semibold text-gray-900">
                {drawData.ticketPrice !== null
                  ? `${drawData.ticketPrice.toFixed(2)} zł`
                  : 'Brak danych'}
              </div>
            </div>
            <div>
              <div className="text-gray-600 mb-1">Ilość wygranych:</div>
              <div className="font-semibold text-gray-900">
                {tierData.count !== null
                  ? `${tierData.count} ${tierData.count === 1 ? 'osoba' : 'osób'}`
                  : 'Brak danych'}
              </div>
            </div>
            <div>
              <div className="text-gray-600 mb-1">Wartość wygranej:</div>
              <div className="font-semibold text-green-700">
                {tierData.amount !== null
                  ? `${tierData.amount.toFixed(2)} zł`
                  : 'Brak danych'}
              </div>
            </div>
          </div>
        </div>
      ) : (
        <div className="mt-2">
          <span className="text-gray-500 text-sm" aria-label="Brak wygranej">
            Brak trafień
          </span>
        </div>
      )}
    </div>
  );
}
