import React from 'react';
import { Button } from '../shared';
import type { TicketItemProps } from '../../types/tickets';
import { formatDate } from '../../utils/generators';

/**
 * Pojedynczy element listy zestawów
 * Wyświetla 6 liczb, datę utworzenia i przyciski akcji
 */
const TicketItem: React.FC<TicketItemProps> = ({ ticket, onEdit, onDelete }) => {
  return (
    <div className="border rounded-lg p-4 flex flex-col md:flex-row justify-between items-start md:items-center gap-4 hover:shadow-md transition-shadow">
      <div className="flex-1">
        {/* Nazwa grupy (jeśli istnieje) */}
        {ticket.groupName && (
          <div className="mb-2">
            <span className="inline-block px-2 py-1 bg-purple-100 text-purple-800 rounded text-xs font-medium">
              {ticket.groupName}
            </span>
          </div>
        )}

        {/* Liczby */}
        <div className="flex flex-wrap gap-2 mb-2">
          {ticket.numbers.map((num, index) => (
            <span
              key={index}
              className="px-3 py-1 bg-blue-100 text-blue-800 rounded-full font-semibold min-w-[2.5rem] text-center"
            >
              {num}
            </span>
          ))}
        </div>

        {/* Data utworzenia */}
        <p className="text-sm text-gray-500">
          Utworzono: {formatDate(ticket.createdAt)}
        </p>
      </div>
      <div className="flex gap-2">
        <Button onClick={onEdit} variant="secondary">
          Edytuj
        </Button>
        <Button onClick={onDelete} variant="danger">
          Usuń
        </Button>
      </div>
    </div>
  );
};

export default TicketItem;
