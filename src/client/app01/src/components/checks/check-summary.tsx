// ===================================================================
// CheckSummary Component - Summary statistics for verification results
// ===================================================================

import { useState } from 'react';
import type { DrawsResult, TicketsResult } from '../../services/contracts/verification-check-response';

interface CheckSummaryProps {
  drawsResults: DrawsResult[];
  ticketsResults: TicketsResult[];
}

interface WinStats {
  count: number;
  value: number;
}

/**
 * Component displaying summary statistics for ticket verification
 * - Number of draws and tickets
 * - Win statistics by level (3-6 hits)
 * - Total costs vs wins with profit/loss indicator
 * - Collapsible with toggle button
 */
export function CheckSummary({ drawsResults, ticketsResults }: CheckSummaryProps) {
  const [isExpanded, setIsExpanded] = useState<boolean>(true);

  // Calculate statistics
  const stats = calculateStats(drawsResults, ticketsResults);

  // Profit/loss calculation
  const profit = stats.totalWinValue - stats.totalCost;
  const profitEmoji = profit >= 0 ? '😊' : '😠';

  return (
    <div className="bg-gradient-to-r from-purple-50 to-blue-50 border border-purple-200 rounded-lg overflow-hidden">
      {/* Header with toggle */}
      <button
        onClick={() => setIsExpanded(!isExpanded)}
        className="w-full px-6 py-4 flex items-center justify-between hover:bg-white/50 transition-colors"
      >
        <h2 className="text-lg font-semibold text-gray-900 flex items-center gap-2">
          <svg
            className="w-5 h-5 text-purple-600"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
            />
          </svg>
          Podsumowanie wyników
        </h2>
        <svg
          className={`w-5 h-5 text-gray-600 transition-transform ${isExpanded ? 'rotate-180' : ''}`}
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
        </svg>
      </button>

      {/* Expandable content */}
      {isExpanded && (
        <div className="px-6 pb-6">
          {/* Grid with statistics */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {/* Row 1 */}
            <StatCard
              label="Liczba losowań"
              value={stats.drawsCount}
              icon="📅"
              color="blue"
            />
            <StatCard
              label="Liczba kuponów"
              value={stats.ticketsCount}
              icon="🎫"
              color="green"
            />
            <StatCard
              label="Suma nakładów"
              value={`${stats.totalCost.toFixed(2)} zł`}
              icon="💰"
              color="orange"
            />

            {/* Row 2 */}
            <WinStatCard
              label="Wygrane 1° (trójki)"
              stats={stats.wins3}
              color="yellow"
            />
            <WinStatCard
              label="Wygrane 2° (czwórki)"
              stats={stats.wins4}
              color="lime"
            />
            <WinStatCard
              label="Suma wygranych"
              stats={{ count: stats.totalWinCount, value: stats.totalWinValue }}
              color="indigo"
            />

            {/* Row 3 */}
            <WinStatCard
              label="Wygrane 3° (piątki)"
              stats={stats.wins5}
              color="emerald"
            />
            <WinStatCard
              label="Wygrane 4° (szóstki)"
              stats={stats.wins6}
              color="purple"
            />
            <div className={`rounded-lg p-4 ${profit >= 0 ? 'bg-green-100' : 'bg-red-100'}`}>
              <div className="text-sm font-medium text-gray-700 mb-1">
                Bilans {profitEmoji}
              </div>
              <div className={`text-2xl font-bold ${profit >= 0 ? 'text-green-700' : 'text-red-700'}`}>
                {profit >= 0 ? '+' : ''}{profit.toFixed(2)} zł
              </div>
              <div className="text-xs text-gray-600 mt-1">
                {profit >= 0 ? 'Zysk' : 'Strata'}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

/**
 * Calculate all statistics from draws and tickets results
 */
function calculateStats(drawsResults: DrawsResult[], ticketsResults: TicketsResult[]) {
  // Count unique draw dates (each date has both LOTTO and LOTTO PLUS)
  const uniqueDates = new Set(drawsResults.map(draw => draw.drawDate));
  const drawsCount = uniqueDates.size;
  const ticketsCount = ticketsResults.length;

  // Get ticket prices for LOTTO and LOTTO PLUS
  const lottoTypesPrices = new Map<string, number>();
  drawsResults.forEach(draw => {
    if (draw.ticketPrice !== null && !lottoTypesPrices.has(draw.lottoType)) {
      lottoTypesPrices.set(draw.lottoType, draw.ticketPrice);
    }
  });

  // Sum of LOTTO + LOTTO PLUS prices (cost per ticket per draw)
  const pricePerTicketPerDraw = Array.from(lottoTypesPrices.values()).reduce(
    (sum, price) => sum + price,
    0
  );

  // Total cost = draws × tickets × (LOTTO + LOTTO PLUS)
  const totalCost = drawsCount * ticketsCount * pricePerTicketPerDraw;

  // Initialize win stats
  const wins3: WinStats = { count: 0, value: 0 };
  const wins4: WinStats = { count: 0, value: 0 };
  const wins5: WinStats = { count: 0, value: 0 };
  const wins6: WinStats = { count: 0, value: 0 };

  // Process each draw to count wins
  drawsResults.forEach(draw => {
    draw.winningTicketsResult.forEach(winningTicket => {
      const hits = winningTicket.matchingNumbers.length;

      // Map hits to win levels and pool amounts
      switch (hits) {
        case 3:
          wins3.count++;
          wins3.value += draw.winPoolAmount4 ?? 0;
          break;
        case 4:
          wins4.count++;
          wins4.value += draw.winPoolAmount3 ?? 0;
          break;
        case 5:
          wins5.count++;
          wins5.value += draw.winPoolAmount2 ?? 0;
          break;
        case 6:
          wins6.count++;
          wins6.value += draw.winPoolAmount1 ?? 0;
          break;
      }
    });
  });

  const totalWinCount = wins3.count + wins4.count + wins5.count + wins6.count;
  const totalWinValue = wins3.value + wins4.value + wins5.value + wins6.value;

  return {
    drawsCount,
    ticketsCount,
    totalCost,
    wins3,
    wins4,
    wins5,
    wins6,
    totalWinCount,
    totalWinValue
  };
}

/**
 * Simple stat card component
 */
interface StatCardProps {
  label: string;
  value: string | number;
  icon: string;
  color: 'blue' | 'green' | 'orange' | 'purple' | 'indigo';
}

function StatCard({ label, value, icon, color }: StatCardProps) {
  const colorClasses = {
    blue: 'bg-blue-100',
    green: 'bg-green-100',
    orange: 'bg-orange-100',
    purple: 'bg-purple-100',
    indigo: 'bg-indigo-100'
  };

  return (
    <div className={`rounded-lg p-4 ${colorClasses[color]}`}>
      <div className="text-sm font-medium text-gray-700 mb-1">
        {icon} {label}
      </div>
      <div className="text-2xl font-bold text-gray-900">{value}</div>
    </div>
  );
}

/**
 * Win statistics card showing count and value
 */
interface WinStatCardProps {
  label: string;
  stats: WinStats;
  color: 'yellow' | 'lime' | 'emerald' | 'purple' | 'indigo';
}

function WinStatCard({ label, stats, color }: WinStatCardProps) {
  const colorClasses = {
    yellow: 'bg-yellow-100',
    lime: 'bg-lime-100',
    emerald: 'bg-emerald-100',
    purple: 'bg-purple-100',
    indigo: 'bg-indigo-100'
  };

  return (
    <div className={`rounded-lg p-4 ${colorClasses[color]}`}>
      <div className="text-sm font-medium text-gray-700 mb-1">{label}</div>
      <div className="flex items-baseline gap-2">
        <div className="text-2xl font-bold text-gray-900">{stats.count}</div>
        <div className="text-sm text-gray-600">|</div>
        <div className="text-lg font-semibold text-gray-700">{stats.value.toFixed(2)} zł</div>
      </div>
    </div>
  );
}
