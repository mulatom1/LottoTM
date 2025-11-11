import React from 'react';
import DrawItem from './draw-item';
import Spinner from '../shared/spinner';
import type { DrawDto } from '../../services/contracts/draws-get-list-response';

export interface DrawListProps {
  draws: DrawDto[];
  isAdmin: boolean;
  onEdit: (drawId: number) => void;
  onDelete: (drawId: number) => void;
  loading: boolean;
}

/**
 * List of lottery draws with empty state and loading state
 */
const DrawList: React.FC<DrawListProps> = ({
  draws,
  isAdmin,
  onEdit,
  onDelete,
  loading,
}) => {
  // Loading state
  if (loading) {
    return (
      <div className="flex justify-center items-center py-12">
        <Spinner size="lg" />
      </div>
    );
  }

  // Empty state
  if (draws.length === 0) {
    return (
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-8 text-center">
        <svg
          className="mx-auto h-12 w-12 text-gray-400 mb-4"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
          aria-hidden="true"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
          />
        </svg>
        <h3 className="text-lg font-medium text-gray-900 mb-2">
          Brak wyników losowań
        </h3>
        <p className="text-sm text-gray-600">
          {isAdmin
            ? 'Dodaj pierwszy wynik losowania klikając przycisk "+ Dodaj wynik"'
            : 'Nie znaleziono żadnych wyników losowań w bazie danych'}
        </p>
      </div>
    );
  }

  // List of draws
  return (
    <div className="space-y-4">
      {draws.map((draw) => (
        <DrawItem
          key={draw.id}
          draw={draw}
          isAdmin={isAdmin}
          onEdit={() => onEdit(draw.id)}
          onDelete={() => onDelete(draw.id)}
        />
      ))}
    </div>
  );
};

export default DrawList;
