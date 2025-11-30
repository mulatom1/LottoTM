// ===================================================================
// WinPoolStatsGrid Component - Grid of win pool statistics cards
// ===================================================================

import { WinPoolCard } from './win-pool-card';

interface WinPoolStatsGridProps {
  winPoolCount1: number | null;
  winPoolAmount1: number | null;
  winPoolCount2: number | null;
  winPoolAmount2: number | null;
  winPoolCount3: number | null;
  winPoolAmount3: number | null;
  winPoolCount4: number | null;
  winPoolAmount4: number | null;
}

/**
 * Grid component displaying win pool statistics for all 4 tiers
 * Responsive: 4 columns on desktop, 2 on tablet, 1 on mobile
 */
export function WinPoolStatsGrid({
  winPoolCount1,
  winPoolAmount1,
  winPoolCount2,
  winPoolAmount2,
  winPoolCount3,
  winPoolAmount3,
  winPoolCount4,
  winPoolAmount4
}: WinPoolStatsGridProps) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
      {/* Tier 1: 6 matches */}
      <WinPoolCard
        tier={1}
        count={winPoolCount1}
        amount={winPoolAmount1}
      />

      {/* Tier 2: 5 matches */}
      <WinPoolCard
        tier={2}
        count={winPoolCount2}
        amount={winPoolAmount2}
      />

      {/* Tier 3: 4 matches */}
      <WinPoolCard
        tier={3}
        count={winPoolCount3}
        amount={winPoolAmount3}
      />

      {/* Tier 4: 3 matches */}
      <WinPoolCard
        tier={4}
        count={winPoolCount4}
        amount={winPoolAmount4}
      />
    </div>
  );
}
