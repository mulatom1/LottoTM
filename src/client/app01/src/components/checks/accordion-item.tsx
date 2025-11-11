// ===================================================================
// AccordionItem Component - Single draw with expandable ticket list
// ===================================================================

import { useState } from 'react';
import { ResultTicketItem } from './result-ticket-item';

interface AccordionItemProps {
  draw: {
    drawDate: string;
    lottoType: string;
    drawNumbers: number[];
  };
  tickets: Array<{
    ticketId: number;
    groupName: string;
    ticketNumbers: number[];
    hits: number;
    winningNumbers: number[];
  }>;
  defaultExpanded?: boolean;
}

/**
 * Accordion item component for displaying a single draw with expandable ticket results
 * Header shows draw date and numbers, content shows user's tickets with matches
 */
export function AccordionItem({ draw, tickets, defaultExpanded = false }: AccordionItemProps) {
  const [isExpanded, setIsExpanded] = useState(defaultExpanded);
  const contentId = `accordion-content-${draw.drawDate}`;

  const toggleExpand = () => {
    setIsExpanded(!isExpanded);
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLButtonElement>) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      toggleExpand();
    }
  };

  return (
    <div className="border border-gray-200 rounded-lg mb-3 overflow-hidden">
      {/* Accordion Header */}
      <button
        type="button"
        onClick={toggleExpand}
        onKeyDown={handleKeyDown}
        aria-expanded={isExpanded}
        aria-controls={contentId}
        className="w-full flex items-center justify-between px-6 py-4 bg-white hover:bg-gray-50 transition-colors text-left focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-inset"
      >
        <div className="flex items-center gap-4">
          {/* Expand/Collapse Icon */}
          <span
            className="text-gray-500 transition-transform duration-200"
            style={{ transform: isExpanded ? 'rotate(90deg)' : 'rotate(0deg)' }}
            aria-hidden="true"
          >
            ▶
          </span>

          {/* Draw Info */}
          <div>
            <div className="flex items-center gap-2">
              <div className="text-sm font-semibold text-gray-900">
                Losowanie {draw.drawDate}
              </div>
              <span className={`text-xs font-medium px-2 py-0.5 rounded ${
                draw.lottoType === 'LOTTO PLUS'
                  ? 'bg-purple-100 text-purple-700'
                  : 'bg-green-100 text-green-700'
              }`}>
                {draw.lottoType}
              </span>
            </div>
            <div className="flex gap-2 mt-2">
              {draw.drawNumbers.map((num, index) => (
                <span
                  key={index}
                  className="inline-flex items-center justify-center w-8 h-8 rounded-full bg-blue-600 text-white text-sm font-bold"
                  aria-label={`Wylosowana liczba ${num}`}
                >
                  {num}
                </span>
              ))}
            </div>
          </div>
        </div>

        {/* Tickets count badge */}
        <span className="text-sm text-gray-600 font-medium">
          {tickets.length} {tickets.length === 1 ? 'zestaw' : 'zestawy'}
        </span>
      </button>

      {/* Accordion Content */}
      {isExpanded && (
        <div
          id={contentId}
          className="bg-gray-50 border-t border-gray-200"
          role="region"
          aria-labelledby={contentId}
        >
          {tickets.length > 0 ? (
            <div className="divide-y divide-gray-200">
              {tickets.map((ticket) => (
                <ResultTicketItem key={ticket.ticketId} ticket={ticket} />
              ))}
            </div>
          ) : (
            <div className="px-6 py-8 text-center text-gray-500">
              Brak zestawów dla tego losowania
            </div>
          )}
        </div>
      )}
    </div>
  );
}
