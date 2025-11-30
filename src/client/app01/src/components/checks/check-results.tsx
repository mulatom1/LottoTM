// ===================================================================
// CheckResults Component - Container for verification results
// ===================================================================

import { DrawCard } from './draw-card';
import type { DrawsResult, TicketsResult } from '../../services/contracts/verification-check-response';

interface CheckResultsProps {
  drawsResults: DrawsResult[];
  ticketsResults: TicketsResult[];
  executionTimeMs: number;
  showOnlyWins: boolean;
  onShowOnlyWinsChange: (value: boolean) => void;
}

/**
 * Container component displaying verification results as Draw Cards
 * Each card has two expandable sections: ticket price/win pool stats and winning tickets list
 * Transforms API response (separate drawsResults and ticketsResults) into UI structure
 */
export function CheckResults({ drawsResults, ticketsResults, showOnlyWins, onShowOnlyWinsChange }: CheckResultsProps) {
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

  // Filter draws based on showOnlyWins
  const filteredDraws = showOnlyWins
    ? drawsArray.filter(draw => draw.tickets.length > 0)
    : drawsArray;

  // Empty state
  if (filteredDraws.length === 0) {
    return (
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
          Brak wygranych
        </h3>
        <p className="text-gray-600">
          {ticketsResults.length === 0
            ? "Nie masz żadnych zestawów do weryfikacji. Dodaj zestawy na stronie Moje Zestawy."
            : drawsResults.length === 0
            ? "Nie znaleziono losowań w wybranym zakresie dat."
            : "Nie znaleziono wygranych w wybranym zakresie dat."
          }
        </p>
      </div>
    );
  }

  // Results display
  return (
    <div className="space-y-4">
      {/* Results summary and filter */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg px-6 py-4">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <p className="text-sm text-blue-800">
            <span className="font-semibold">Znaleziono {filteredDraws.length}</span>
            {' '}
            {filteredDraws.length === 1 ? 'losowanie' :
             filteredDraws.length < 5 ? 'losowania' : 'losowań'}
            {showOnlyWins && <span> z wygranymi kuponami</span>}
            {!showOnlyWins && (
              <>
                {' '}(z {drawsArray.length} wszystkich)
              </>
            )}
          </p>
          <label className="flex items-center gap-2 text-sm text-blue-800 cursor-pointer">
            <input
              type="checkbox"
              checked={showOnlyWins}
              onChange={(e) => onShowOnlyWinsChange(e.target.checked)}
              className="w-4 h-4 text-blue-600 bg-white border-gray-300 rounded focus:ring-blue-500 focus:ring-2"
            />
            <span className="font-medium">Pokaż tylko losowania z kuponami trafionymi</span>
          </label>
        </div>
      </div>

      {/* Draw Cards list */}
      <div className="space-y-4">
        {filteredDraws.map((draw) => (
          <DrawCard
            key={draw.drawId}
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
    </div>
  );
}
