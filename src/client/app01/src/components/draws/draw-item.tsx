import React from 'react';
import Button from '../shared/button';

export interface DrawItemProps {
  draw: {
    id: number;
    drawDate: string;
    lottoType: string;
    numbers: number[];
    drawSystemId: number;
    ticketPrice: number | null;
    winPoolCount1: number | null;
    winPoolAmount1: number | null;
    winPoolCount2: number | null;
    winPoolAmount2: number | null;
    winPoolCount3: number | null;
    winPoolAmount3: number | null;
    winPoolCount4: number | null;
    winPoolAmount4: number | null;
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

  // Format currency (PLN)
  const formatCurrency = (amount: number | null): string => {
    if (amount === null || amount === undefined) return '-';
    return `${amount.toFixed(2)} zł`;
  };

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4 hover:shadow-md transition-shadow">
      <div className="flex flex-col gap-4">
        {/* Header: Date, lotto type, and admin actions */}
        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
          {/* Draw date and lotto type */}
          <div className="flex items-center gap-2 flex-wrap">
            <h3 className="text-lg font-bold text-gray-900">
              {formatDate(draw.drawDate)}
            </h3>
            <span
              className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                draw.lottoType === 'LOTTO'
                  ? 'bg-blue-100 text-blue-800'
                  : 'bg-purple-100 text-purple-800'
              }`}
            >
              {draw.lottoType}
            </span>
            {draw.drawSystemId && (
              <span className="text-xs text-gray-500">
                ID: {draw.drawSystemId}
              </span>
            )}
          </div>

          {/* Admin actions */}
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

        {/* Numbers badges */}
        <div className="flex flex-wrap gap-2">
          {draw.numbers.map((number, index) => (
            <span
              key={index}
              className="inline-flex items-center justify-center w-10 h-10 bg-blue-100 text-blue-800 font-bold rounded-full text-sm"
            >
              {number}
            </span>
          ))}
        </div>

        {/* Ticket price */}
        {draw.ticketPrice !== null && draw.ticketPrice !== undefined && (
          <div className="text-sm text-gray-600">
            <span>Cena biletu: {formatCurrency(draw.ticketPrice)}</span>
          </div>
        )}

        {/* Win pools information - Enhanced display */}
        {(draw.winPoolCount1 !== null || draw.winPoolCount2 !== null ||
          draw.winPoolCount3 !== null || draw.winPoolCount4 !== null ||
          draw.winPoolAmount1 !== null || draw.winPoolAmount2 !== null ||
          draw.winPoolAmount3 !== null || draw.winPoolAmount4 !== null) && (
          <div className="bg-gradient-to-br from-gray-50 to-gray-100 rounded-xl border-2 border-gray-300 p-4 mt-2">
            <div className="flex items-center gap-2 mb-3">
              <div className="w-1 h-6 bg-gradient-to-b from-green-500 to-orange-500 rounded-full"></div>
              <h4 className="text-sm font-bold text-gray-800">Informacje o wygranych</h4>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
              {/* Tier 1 - 6 matches */}
              <div className="bg-white rounded-lg p-3 border-2 border-green-300 shadow-sm">
                <div className="flex items-center gap-2 mb-2">
                  <div className="w-8 h-8 bg-green-500 rounded-full flex items-center justify-center text-white font-bold text-xs">
                    6
                  </div>
                  <p className="text-xs font-semibold text-green-800">trafionych</p>
                </div>
                <div className="space-y-1">
                  <p className="text-xs text-gray-600">Ilość wygranych:</p>
                  <p className="text-lg font-bold text-green-900">{draw.winPoolCount1 ?? 0}</p>
                  <p className="text-xs text-gray-600">Kwota:</p>
                  <p className="text-sm font-semibold text-green-800">{formatCurrency(draw.winPoolAmount1)}</p>
                </div>
              </div>

              {/* Tier 2 - 5 matches */}
              <div className="bg-white rounded-lg p-3 border-2 border-blue-300 shadow-sm">
                <div className="flex items-center gap-2 mb-2">
                  <div className="w-8 h-8 bg-blue-500 rounded-full flex items-center justify-center text-white font-bold text-xs">
                    5
                  </div>
                  <p className="text-xs font-semibold text-blue-800">trafionych</p>
                </div>
                <div className="space-y-1">
                  <p className="text-xs text-gray-600">Ilość wygranych:</p>
                  <p className="text-lg font-bold text-blue-900">{draw.winPoolCount2 ?? 0}</p>
                  <p className="text-xs text-gray-600">Kwota:</p>
                  <p className="text-sm font-semibold text-blue-800">{formatCurrency(draw.winPoolAmount2)}</p>
                </div>
              </div>

              {/* Tier 3 - 4 matches */}
              <div className="bg-white rounded-lg p-3 border-2 border-yellow-300 shadow-sm">
                <div className="flex items-center gap-2 mb-2">
                  <div className="w-8 h-8 bg-yellow-500 rounded-full flex items-center justify-center text-white font-bold text-xs">
                    4
                  </div>
                  <p className="text-xs font-semibold text-yellow-800">trafione</p>
                </div>
                <div className="space-y-1">
                  <p className="text-xs text-gray-600">Ilość wygranych:</p>
                  <p className="text-lg font-bold text-yellow-900">{draw.winPoolCount3 ?? 0}</p>
                  <p className="text-xs text-gray-600">Kwota:</p>
                  <p className="text-sm font-semibold text-yellow-800">{formatCurrency(draw.winPoolAmount3)}</p>
                </div>
              </div>

              {/* Tier 4 - 3 matches */}
              <div className="bg-white rounded-lg p-3 border-2 border-orange-300 shadow-sm">
                <div className="flex items-center gap-2 mb-2">
                  <div className="w-8 h-8 bg-orange-500 rounded-full flex items-center justify-center text-white font-bold text-xs">
                    3
                  </div>
                  <p className="text-xs font-semibold text-orange-800">trafione</p>
                </div>
                <div className="space-y-1">
                  <p className="text-xs text-gray-600">Ilość wygranych:</p>
                  <p className="text-lg font-bold text-orange-900">{draw.winPoolCount4 ?? 0}</p>
                  <p className="text-xs text-gray-600">Kwota:</p>
                  <p className="text-sm font-semibold text-orange-800">{formatCurrency(draw.winPoolAmount4)}</p>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default DrawItem;
