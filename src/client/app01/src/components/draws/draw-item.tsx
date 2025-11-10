import React from 'react';
import Button from '../shared/button';

export interface DrawItemProps {
  draw: {
    id: number;
    drawDate: string;
    numbers: number[];
    createdAt: string;
  };
  isAdmin: boolean;
  onEdit: () => void;
  onDelete: () => void;
}

/**
 * Single draw item in the list
 * Displays draw date, numbers, creation timestamp, and admin actions
 */
const DrawItem: React.FC<DrawItemProps> = ({
  draw,
  isAdmin,
  onEdit,
  onDelete,
}) => {
  // Format date to YYYY-MM-DD
  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  };

  // Format ISO datetime to DD.MM.YYYY HH:MM
  const formatDateTime = (isoString: string): string => {
    const date = new Date(isoString);
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${day}.${month}.${year} ${hours}:${minutes}`;
  };

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4 hover:shadow-md transition-shadow">
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        {/* Left section: Date and numbers */}
        <div className="flex-1">
          {/* Draw date */}
          <h3 className="text-lg font-bold text-gray-900 mb-2">
            {formatDate(draw.drawDate)}
          </h3>

          {/* Numbers badges */}
          <div className="flex flex-wrap gap-2 mb-2">
            {draw.numbers.map((number, index) => (
              <span
                key={index}
                className="inline-flex items-center justify-center w-10 h-10 bg-blue-100 text-blue-800 font-bold rounded-full text-sm"
              >
                {number}
              </span>
            ))}
          </div>

          {/* Created at timestamp */}
          <p className="text-sm text-gray-600">
            Wprowadzono: {formatDateTime(draw.createdAt)}
          </p>
        </div>

        {/* Right section: Admin actions */}
        {isAdmin && (
          <div className="flex gap-2 md:flex-col lg:flex-row">
            <Button
              variant="secondary"
              onClick={onEdit}
              className="flex-1 md:flex-none text-sm px-4 py-2"
            >
              Edytuj
            </Button>
            <Button
              variant="danger"
              onClick={onDelete}
              className="flex-1 md:flex-none text-sm px-4 py-2"
            >
              Usuń
            </Button>
          </div>
        )}
      </div>
    </div>
  );
};

export default DrawItem;
