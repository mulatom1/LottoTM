// ===================================================================
// CheckResults Component - Container for verification results
// ===================================================================

import { AccordionItem } from './accordion-item';
import type { TicketVerificationResult } from '../../services/contracts/verification-check-response';

interface CheckResultsProps {
  results: TicketVerificationResult[];
  totalTickets: number;
  totalDraws: number;
}

/**
 * Container component displaying verification results in accordion format
 * Groups tickets by draw, showing empty state when no results found
 */
export function CheckResults({ results, totalTickets, totalDraws }: CheckResultsProps) {
  // Transform results structure: group by draws instead of tickets
  // Backend returns: tickets -> draws
  // UI needs: draws -> tickets

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

  // Process results and group by draws
  results.forEach((ticketResult) => {
    ticketResult.draws.forEach((draw) => {
      const drawKey = `${draw.drawId}-${draw.drawDate}`;

      if (!drawsMap.has(drawKey)) {
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
      }

      const drawData = drawsMap.get(drawKey)!;
      drawData.tickets.push({
        ticketId: ticketResult.ticketId,
        groupName: ticketResult.groupName,
        ticketNumbers: ticketResult.ticketNumbers,
        hits: draw.hits,
        winningNumbers: draw.winningNumbers
      });
    });
  });

  // Convert Map to array and sort by date (newest first)
  const drawsArray = Array.from(drawsMap.values()).sort((a, b) =>
    b.drawDate.localeCompare(a.drawDate)
  );

  // Empty state
  if (drawsArray.length === 0) {
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
          {totalTickets === 0
            ? "Nie masz żadnych zestawów do weryfikacji. Dodaj zestawy na stronie Moje Zestawy."
            : totalDraws === 0
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
      {/* Results summary */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg px-6 py-4">
        <p className="text-sm text-blue-800">
          <span className="font-semibold">Znaleziono {drawsArray.length}</span>
          {' '}
          {drawsArray.length === 1 ? 'losowanie' :
           drawsArray.length < 5 ? 'losowania' : 'losowań'}
          {' '}z wygranymi dla {totalTickets}
          {' '}
          {totalTickets === 1 ? 'zestawu' : 'zestawów'}
        </p>
      </div>

      {/* Accordion list */}
      <div className="space-y-3">
        {drawsArray.map((draw, index) => (
          <AccordionItem
            key={draw.drawId}
            draw={{
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
              winPoolAmount4: draw.winPoolAmount4
            }}
            tickets={draw.tickets}
            defaultExpanded={index === 0} // First draw expanded by default
          />
        ))}
      </div>
    </div>
  );
}
