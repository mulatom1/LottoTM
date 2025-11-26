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
  onDelete,
  searchTerm,
  onClearSearch
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
    // Empty state z aktywnym filtrem
    if (searchTerm && searchTerm.trim() !== '') {
      return (
        <div className="text-center py-12 text-gray-500">
          <p className="text-lg mb-2">Brak wyników dla "{searchTerm}"</p>
          <p className="text-sm mb-4">Nie znaleziono zestawów pasujących do tego filtra.</p>
          {onClearSearch && (
            <button
              onClick={onClearSearch}
              className="px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors"
            >
              Wyczyść filtr
            </button>
          )}
        </div>
      );
    }

    // Empty state bez filtra
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
