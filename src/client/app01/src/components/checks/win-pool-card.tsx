// ===================================================================
// WinPoolCard Component - Single tier win pool statistics card
// ===================================================================

interface WinPoolCardProps {
  tier: 1 | 2 | 3 | 4;
  count: number | null;
  amount: number | null;
}

/**
 * Colored card displaying win pool statistics for a single tier
 * Each tier has a different color scheme
 */
export function WinPoolCard({ tier, count, amount }: WinPoolCardProps) {
  // Tier configuration: label, icon, colors
  const tierConfig = {
    1: {
      label: 'Stopień 1 (6 trafień)',
      icon: '6',
      borderColor: 'border-green-300',
      bgColor: 'bg-green-500',
      lightBg: 'bg-green-50'
    },
    2: {
      label: 'Stopień 2 (5 trafień)',
      icon: '5',
      borderColor: 'border-blue-300',
      bgColor: 'bg-blue-500',
      lightBg: 'bg-blue-50'
    },
    3: {
      label: 'Stopień 3 (4 trafienia)',
      icon: '4',
      borderColor: 'border-yellow-300',
      bgColor: 'bg-yellow-500',
      lightBg: 'bg-yellow-50'
    },
    4: {
      label: 'Stopień 4 (3 trafienia)',
      icon: '3',
      borderColor: 'border-orange-300',
      bgColor: 'bg-orange-500',
      lightBg: 'bg-orange-50'
    }
  };

  const config = tierConfig[tier];

  return (
    <div className={`border-2 ${config.borderColor} ${config.lightBg} rounded-lg p-4`}>
      {/* Icon with tier number */}
      <div className="flex items-center gap-3 mb-3">
        <div className={`${config.bgColor} text-white rounded-full w-10 h-10 flex items-center justify-center text-xl font-bold`}>
          {config.icon}
        </div>
        <div className="text-sm font-semibold text-gray-700">
          {config.label}
        </div>
      </div>

      {/* Count */}
      <div className="mb-2">
        <div className="text-xs text-gray-600 mb-1">Ilość:</div>
        <div className="text-sm font-semibold text-gray-900">
          {count !== null ? `${count.toLocaleString('pl-PL')} ${count === 1 ? 'osoba' : 'osób'}` : 'Brak danych'}
        </div>
      </div>

      {/* Amount */}
      <div>
        <div className="text-xs text-gray-600 mb-1">Kwota:</div>
        <div className="text-sm font-semibold text-gray-900">
          {amount !== null ? `${amount.toLocaleString('pl-PL', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} zł` : 'Brak danych'}
        </div>
      </div>
    </div>
  );
}
