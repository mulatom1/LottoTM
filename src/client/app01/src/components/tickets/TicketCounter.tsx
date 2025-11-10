import React from 'react';
import type { TicketCounterProps } from '../../types/tickets';
import { getCounterColor } from '../../utils/generators';

/**
 * Licznik zestawów użytkownika w formacie "X/100"
 * Z progresywną kolorystyką (zielony/żółty/czerwony)
 */
const TicketCounter: React.FC<TicketCounterProps> = ({ count, max = 100 }) => {
  const colorClass = getCounterColor(count, max);

  return (
    <span className={`text-lg font-semibold ${colorClass}`}>
      [{count}/{max}]
    </span>
  );
};

export default TicketCounter;
