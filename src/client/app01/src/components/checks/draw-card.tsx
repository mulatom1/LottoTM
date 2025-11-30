// ===================================================================
// DrawCard Component - Single draw card with two expandable sections
// ===================================================================

import { useState } from 'react';
import { WinPoolStatsGrid } from './win-pool-stats-grid';
import { WinningTicketItem } from './winning-ticket-item';

interface DrawCardProps {
  draw: {
    drawId: number;
    drawDate: string;
    drawSystemId: number;
    lottoType: string;
    drawNumbers: number[];
    ticketPrice: number | null;
    winPoolCount1: number | null;
    winPoolAmount1: number | null;
    winPoolCount2: number | null;
    winPoolAmount2: number | null;
    winPoolCount3: number | null;
    winPoolAmount3: number | null;
    winPoolCount4: number | null;
    winPoolAmount4: number | null;
    winningTickets: Array<{
      ticketId: number;
      groupName: string;
      ticketNumbers: number[];
      hits: number;
      winningNumbers: number[];
    }>;
  };
}

/**
 * DrawCard component - represents a single draw with two expandable sections
 * Section 1: Ticket price and win pool statistics (tiers 1-4)
 * Section 2: List of winning tickets for this draw
 */
export function DrawCard({ draw }: DrawCardProps) {
  const [isSection1Expanded, setIsSection1Expanded] = useState(false);
  const [isSection2Expanded, setIsSection2Expanded] = useState(false);

  const section1Id = `section1-${draw.drawId}`;
  const section2Id = `section2-${draw.drawId}`;

  const toggleSection1 = () => setIsSection1Expanded(!isSection1Expanded);
  const toggleSection2 = () => setIsSection2Expanded(!isSection2Expanded);

  const handleKeyDownSection1 = (e: React.KeyboardEvent<HTMLButtonElement>) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      toggleSection1();
    }
  };

  const handleKeyDownSection2 = (e: React.KeyboardEvent<HTMLButtonElement>) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      toggleSection2();
    }
  };

  return (
    <div className="border border-gray-200 rounded-lg shadow-sm bg-white overflow-hidden">
      {/* Main Header (always visible, not clickable) */}
      <div className="px-6 py-4 bg-gradient-to-r from-blue-50 to-indigo-50 border-b border-gray-200">
        <div className="flex items-start justify-between flex-wrap gap-3">
          <div className="flex-1 min-w-0">
            {/* Date */}
            <div className="text-2xl font-bold text-gray-900 mb-2">
              {new Date(draw.drawDate).toLocaleDateString('pl-PL', {
                weekday: 'long',
                year: 'numeric',
                month: 'long',
                day: 'numeric'
              })}
            </div>

            <div className="flex items-center gap-3 flex-wrap">
              {/* Lotto Type Badge */}
              <span className={`inline-flex items-center px-3 py-1 rounded-full text-sm font-semibold ${
                draw.lottoType === 'LOTTO PLUS'
                  ? 'bg-purple-100 text-purple-700'
                  : 'bg-green-100 text-green-700'
              }`}>
                {draw.lottoType}
              </span>

              {/* Draw System ID */}
              <span className="text-sm text-gray-500">
                ID: {draw.drawSystemId}
              </span>
            </div>
          </div>

          {/* Draw Numbers */}
          <div className="flex gap-2 items-center">
            {draw.drawNumbers.map((num, index) => (
              <span
                key={index}
                className="inline-flex items-center justify-center w-10 h-10 rounded-full bg-blue-600 text-white text-sm font-bold shadow-md"
                aria-label={`Wylosowana liczba ${num}`}
              >
                {num}
              </span>
            ))}
          </div>
        </div>
      </div>

      {/* Expandable Section 1: Ticket Price & Win Pool Stats */}
      <div className="border-b border-gray-200">
        <button
          type="button"
          onClick={toggleSection1}
          onKeyDown={handleKeyDownSection1}
          aria-expanded={isSection1Expanded}
          aria-controls={section1Id}
          className="w-full flex items-center justify-between px-6 py-3 bg-white hover:bg-gray-50 transition-colors text-left focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-inset"
        >
          <span className="text-sm font-semibold text-gray-700">
            Koszt kuponu i statystyki wygranych
          </span>
          <span
            className="text-gray-400 transition-transform duration-200"
            style={{ transform: isSection1Expanded ? 'rotate(90deg)' : 'rotate(0deg)' }}
            aria-hidden="true"
          >
            ▶
          </span>
        </button>

        {isSection1Expanded && (
          <div
            id={section1Id}
            className="px-6 py-4 bg-gray-50"
            role="region"
            aria-labelledby={section1Id}
          >
            {/* Ticket Price */}
            <div className="mb-4">
              <span className="text-sm text-gray-600">Cena biletu: </span>
              <span className="text-sm font-semibold text-gray-900">
                {draw.ticketPrice !== null ? `${draw.ticketPrice.toFixed(2)} zł` : 'Brak danych'}
              </span>
            </div>

            {/* Win Pool Stats Grid */}
            <WinPoolStatsGrid
              winPoolCount1={draw.winPoolCount1}
              winPoolAmount1={draw.winPoolAmount1}
              winPoolCount2={draw.winPoolCount2}
              winPoolAmount2={draw.winPoolAmount2}
              winPoolCount3={draw.winPoolCount3}
              winPoolAmount3={draw.winPoolAmount3}
              winPoolCount4={draw.winPoolCount4}
              winPoolAmount4={draw.winPoolAmount4}
            />
          </div>
        )}
      </div>

      {/* Expandable Section 2: Winning Tickets */}
      <div>
        <button
          type="button"
          onClick={toggleSection2}
          onKeyDown={handleKeyDownSection2}
          aria-expanded={isSection2Expanded}
          aria-controls={section2Id}
          className="w-full flex items-center justify-between px-6 py-3 bg-white hover:bg-gray-50 transition-colors text-left focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-inset"
        >
          <span className="text-sm font-semibold text-gray-700">
            Ilość wygranych zestawów ({draw.winningTickets.length})
          </span>
          <span
            className="text-gray-400 transition-transform duration-200"
            style={{ transform: isSection2Expanded ? 'rotate(90deg)' : 'rotate(0deg)' }}
            aria-hidden="true"
          >
            ▶
          </span>
        </button>

        {isSection2Expanded && (
          <div
            id={section2Id}
            className="bg-gray-50 border-t border-gray-200"
            role="region"
            aria-labelledby={section2Id}
          >
            {draw.winningTickets.length > 0 ? (
              <div className="divide-y divide-gray-200">
                {draw.winningTickets.map((ticket) => (
                  <WinningTicketItem
                    key={ticket.ticketId}
                    ticket={ticket}
                  />
                ))}
              </div>
            ) : (
              <div className="px-6 py-8 text-center text-gray-500">
                Brak wygranych kuponów dla tego losowania
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
