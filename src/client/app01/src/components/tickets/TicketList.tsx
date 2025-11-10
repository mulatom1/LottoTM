import React from 'react';
import TicketItem from './TicketItem';
import { Spinner } from '../shared';
import type { TicketListProps } from '../../types/tickets';

/**
 * Lista wszystkich zestawów użytkownika
 * Wyświetla Empty State jeśli brak zestawów
 */
const TicketList: React.FC<TicketListProps> = ({
  tickets,
  loading,
  onEdit,
  onDelete
}) => {
  if (loading) {
    return (
      <div className="flex flex-col justify-center items-center py-12 gap-3">
        <Spinner size="lg" />
        <p className="text-gray-600">Ładowanie zestawów...</p>
      </div>
    );
  }

  if (tickets.length === 0) {
    return (
      <div className="text-center py-12 text-gray-500">
        <p className="text-lg mb-2">Nie masz jeszcze żadnych zestawów.</p>
        <p className="text-sm">Dodaj swój pierwszy zestaw używając przycisków powyżej.</p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {tickets.map((ticket) => (
        <TicketItem
          key={ticket.id}
          ticket={ticket}
          onEdit={() => onEdit(ticket.id)}
          onDelete={() => onDelete(ticket.id)}
        />
      ))}
    </div>
  );
};

export default TicketList;
