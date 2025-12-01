// ===================================================================
// CheckResults Component - Container for verification results
// ===================================================================

import { DrawCard } from './draw-card';
import type { DrawsResult, TicketsResult } from '../../services/contracts/verification-check-response';

interface CheckResultsProps {
  drawsResults: DrawsResult[];
  ticketsResults: TicketsResult[];
  executionTimeMs: number;
  showNonWinningTickets: boolean;
  onShowNonWinningTicketsChange: (value: boolean) => void;
  show3Hits: boolean;
  onShow3HitsChange: (value: boolean) => void;
  show4Hits: boolean;
  onShow4HitsChange: (value: boolean) => void;
  show5Hits: boolean;
  onShow5HitsChange: (value: boolean) => void;
  show6Hits: boolean;
  onShow6HitsChange: (value: boolean) => void;
}

/**
 * Container component displaying verification results as Draw Cards
 * Each card has two expandable sections: ticket price/win pool stats and winning tickets list
 * Transforms API response (separate drawsResults and ticketsResults) into UI structure
 */
export function CheckResults({
  drawsResults,
  ticketsResults,
  showNonWinningTickets,
  onShowNonWinningTicketsChange,
  show3Hits,
  onShow3HitsChange,
  show4Hits,
  onShow4HitsChange,
  show5Hits,
  onShow5HitsChange,
  show6Hits,
  onShow6HitsChange
}: CheckResultsProps) {
  // Create a map of ticketId to ticket details for quick lookup
  const ticketsMap = new Map<number, TicketsResult>();
  ticketsResults.forEach(ticket => {
    ticketsMap.set(ticket.ticketId, ticket);
  });

  // Transform results structure: group winning tickets by draws
  const drawsMap = new Map<string, {
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
    tickets: Array<{
      ticketId: number;
      groupName: string;
      ticketNumbers: number[];
      hits: number;
      winningNumbers: number[];
    }>;
  }>();

  // Process draws and their winning tickets
  drawsResults.forEach((draw) => {
    const drawKey = `${draw.drawId}-${draw.drawDate}`;

    drawsMap.set(drawKey, {
      drawId: draw.drawId,
      drawDate: draw.drawDate,
      drawSystemId: draw.drawSystemId,
      lottoType: draw.lottoType,
      drawNumbers: draw.drawNumbers,
      ticketPrice: draw.ticketPrice,
      winPoolCount1: draw.winPoolCount1,
      winPoolAmount1: draw.winPoolAmount1,
      winPoolCount2: draw.winPoolCount2,
      winPoolAmount2: draw.winPoolAmount2,
      winPoolCount3: draw.winPoolCount3,
      winPoolAmount3: draw.winPoolAmount3,
      winPoolCount4: draw.winPoolCount4,
      winPoolAmount4: draw.winPoolAmount4,
      tickets: []
    });

    const drawData = drawsMap.get(drawKey)!;
    draw.winningTicketsResult.forEach((winningTicket) => {
      const ticket = ticketsMap.get(winningTicket.ticketId);
      if (ticket) {
        drawData.tickets.push({
          ticketId: ticket.ticketId,
          groupName: ticket.groupName,
          ticketNumbers: ticket.ticketNumbers,
          hits: winningTicket.matchingNumbers.length,
          winningNumbers: winningTicket.matchingNumbers
        });
      }
    });
  });

  // Convert Map to array and sort by date (newest first)
  const drawsArray = Array.from(drawsMap.values()).sort((a, b) =>
    b.drawDate.localeCompare(a.drawDate)
  );

  // Filter draws based on active hit filters
  // Collect active hit filters (checked checkboxes)
  const activeHits: number[] = [];
  if (show3Hits) activeHits.push(3);
  if (show4Hits) activeHits.push(4);
  if (show5Hits) activeHits.push(5);
  if (show6Hits) activeHits.push(6);

  // Apply filtering
  let filteredDraws = drawsArray;

  // Check if any filter is active
  const hasActiveFilters = showNonWinningTickets || activeHits.length > 0;

  if (!hasActiveFilters) {
    // No filters active - show nothing
    filteredDraws = [];
  } else {
    filteredDraws = drawsArray.filter(draw => {
      const hasWinningTickets = draw.tickets.some(ticket => activeHits.includes(ticket.hits));
      const hasOnlyNonWinningTickets = draw.tickets.length === 0;

      // Show draw if:
      // - showNonWinningTickets is checked and draw has no winning tickets (tickets.length === 0)
      // - any activeHits filter matches a ticket in the draw
      return (showNonWinningTickets && hasOnlyNonWinningTickets) || hasWinningTickets;
    });
  }

  // Results display
  return (
    <div className="space-y-4">
      {/* Results summary and filter - Always visible */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg px-6 py-4">
        <div className="flex flex-col gap-4">
          <p className="text-sm text-blue-800">
            <span className="font-semibold">Znaleziono {filteredDraws.length}</span>
            {' '}
            {filteredDraws.length === 1 ? 'losowanie' :
             filteredDraws.length < 5 ? 'losowania' : 'losowań'}
            {' '}(z {drawsArray.length} wszystkich)
          </p>

          {/* Filters section */}
          <div className="flex flex-col gap-3">
            <label className="flex items-center gap-2 text-sm text-blue-800 cursor-pointer">
              <input
                type="checkbox"
                checked={showNonWinningTickets}
                onChange={(e) => onShowNonWinningTicketsChange(e.target.checked)}
                className="w-4 h-4 text-blue-600 bg-white border-gray-300 rounded focus:ring-blue-500 focus:ring-2"
              />
              <span className="font-medium">Pokaż losowania bez dopasowań</span>
            </label>

            <label className="flex items-center gap-2 text-sm text-blue-800 cursor-pointer">
              <input
                type="checkbox"
                checked={show3Hits}
                onChange={(e) => onShow3HitsChange(e.target.checked)}
                className="w-4 h-4 text-blue-600 bg-white border-gray-300 rounded focus:ring-blue-500 focus:ring-2"
              />
              <span className="font-medium">Pokaż wylosowane trójki</span>
            </label>

            <label className="flex items-center gap-2 text-sm text-blue-800 cursor-pointer">
              <input
                type="checkbox"
                checked={show4Hits}
                onChange={(e) => onShow4HitsChange(e.target.checked)}
                className="w-4 h-4 text-blue-600 bg-white border-gray-300 rounded focus:ring-blue-500 focus:ring-2"
              />
              <span className="font-medium">Pokaż wylosowane czwórki</span>
            </label>

            <label className="flex items-center gap-2 text-sm text-blue-800 cursor-pointer">
              <input
                type="checkbox"
                checked={show5Hits}
                onChange={(e) => onShow5HitsChange(e.target.checked)}
                className="w-4 h-4 text-blue-600 bg-white border-gray-300 rounded focus:ring-blue-500 focus:ring-2"
              />
              <span className="font-medium">Pokaż wylosowane piątki</span>
            </label>

            <label className="flex items-center gap-2 text-sm text-blue-800 cursor-pointer">
              <input
                type="checkbox"
                checked={show6Hits}
                onChange={(e) => onShow6HitsChange(e.target.checked)}
                className="w-4 h-4 text-blue-600 bg-white border-gray-300 rounded focus:ring-blue-500 focus:ring-2"
              />
              <span className="font-medium">Pokaż wylosowane szóstki</span>
            </label>
          </div>
        </div>
      </div>

      {/* Empty state or Draw Cards list */}
      {filteredDraws.length === 0 ? (
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-8 text-center">
          <div className="text-gray-400 mb-4">
            <svg
              className="mx-auto h-12 w-12"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              aria-hidden="true"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
          </div>
          <h3 className="text-lg font-medium text-gray-900 mb-2">
            Brak wyników
          </h3>
          <p className="text-gray-600">
            {ticketsResults.length === 0
              ? "Nie masz żadnych zestawów do weryfikacji. Dodaj zestawy na stronie Moje Zestawy."
              : drawsResults.length === 0
              ? "Nie znaleziono losowań w wybranym zakresie dat."
              : activeHits.length === 0
              ? "Wszystkie filtry są wyłączone. Zaznacz przynajmniej jeden filtr, aby zobaczyć wyniki."
              : activeHits.length < 4
              ? "Nie znaleziono losowań z wybranymi trafieniami. Spróbuj zmienić filtry."
              : "Nie znaleziono wygranych w wybranym zakresie dat."
            }
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {filteredDraws.map((draw) => (
            <DrawCard
              key={`${draw.drawId}-${draw.drawDate}`}
              draw={{
                drawId: draw.drawId,
                drawDate: draw.drawDate,
                drawSystemId: draw.drawSystemId,
                lottoType: draw.lottoType,
                drawNumbers: draw.drawNumbers,
                ticketPrice: draw.ticketPrice,
                winPoolCount1: draw.winPoolCount1,
                winPoolAmount1: draw.winPoolAmount1,
                winPoolCount2: draw.winPoolCount2,
                winPoolAmount2: draw.winPoolAmount2,
                winPoolCount3: draw.winPoolCount3,
                winPoolAmount3: draw.winPoolAmount3,
                winPoolCount4: draw.winPoolCount4,
                winPoolAmount4: draw.winPoolAmount4,
                winningTickets: draw.tickets
              }}
            />
          ))}
        </div>
      )}
    </div>
  );
}
