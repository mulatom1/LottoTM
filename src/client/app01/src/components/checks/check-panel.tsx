// ===================================================================
// CheckPanel Component - Form for date range selection
// ===================================================================

import { useState, useEffect } from 'react';
import DatePicker from '../shared/date-picker';
import { validateDateRange } from '../../utils/date-helpers';

interface CheckPanelProps {
  dateFrom: string;
  dateTo: string;
  groupName: string;
  onDateFromChange: (value: string) => void;
  onDateToChange: (value: string) => void;
  onGroupNameChange: (value: string) => void;
  onSubmit: (dateFrom: string, dateTo: string, groupName: string) => void;
  isLoading: boolean;
}

/**
 * Panel component with date range form, optional group name filter, and submit button
 * - Validates date range inline and disables submit when invalid
 * - Group name filter uses partial match (case-insensitive)
 */
export function CheckPanel({
  dateFrom,
  dateTo,
  groupName,
  onDateFromChange,
  onDateToChange,
  onGroupNameChange,
  onSubmit,
  isLoading
}: CheckPanelProps) {
  const [dateError, setDateError] = useState<string | null>(null);

  // Validate dates on change
  useEffect(() => {
    if (dateFrom && dateTo) {
      const error = validateDateRange(dateFrom, dateTo);
      setDateError(error);
    }
  }, [dateFrom, dateTo]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    // Final validation before submit
    const error = validateDateRange(dateFrom, dateTo);
    if (error) {
      setDateError(error);
      return;
    }

    onSubmit(dateFrom, dateTo, groupName);
  };

  const isSubmitDisabled = isLoading || !!dateError || !dateFrom || !dateTo;

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
      <h2 className="text-lg font-semibold text-gray-900 mb-4">
        Wybierz zakres dat
      </h2>

      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Date pickers container */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <DatePicker
            label="Data Od"
            value={dateFrom}
            onChange={onDateFromChange}
            max={dateTo || undefined}
            required
          />

          <DatePicker
            label="Data Do"
            value={dateTo}
            onChange={onDateToChange}
            min={dateFrom || undefined}
            error={dateError || undefined}
            required
          />
        </div>

        {/* Group name filter (optional) */}
        <div>
          <label htmlFor="groupName" className="block text-sm font-medium text-gray-700 mb-1">
            Nazwa grupy (opcjonalnie)
          </label>
          <input
            type="text"
            id="groupName"
            value={groupName}
            onChange={(e) => onGroupNameChange(e.target.value)}
            placeholder="np. Ulubione"
            className="
              w-full px-3 py-2 border border-gray-300 rounded-lg
              text-sm text-gray-900 placeholder-gray-400
              focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent
              transition-colors duration-200
            "
          />
          <p className="mt-1 text-xs text-gray-500">
            Wyszukiwanie częściowe - wpisz fragment nazwy grupy (np. 'ulu' znajdzie 'Ulubione')
          </p>
        </div>

        {/* Submit button */}
        <div className="flex items-center justify-end pt-2">
          <button
            type="submit"
            disabled={isSubmitDisabled}
            className={`
              px-6 py-2.5 rounded-lg font-medium text-sm
              transition-all duration-200
              focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500
              ${isSubmitDisabled
                ? 'bg-gray-300 text-gray-500 cursor-not-allowed'
                : 'bg-blue-600 text-white hover:bg-blue-700 active:bg-blue-800'
              }
            `}
            aria-label="Sprawdź wygrane w wybranym zakresie dat"
          >
            {isLoading ? (
              <span className="flex items-center gap-2">
                <svg
                  className="animate-spin h-4 w-4 text-gray-500"
                  xmlns="http://www.w3.org/2000/svg"
                  fill="none"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <circle
                    className="opacity-25"
                    cx="12"
                    cy="12"
                    r="10"
                    stroke="currentColor"
                    strokeWidth="4"
                  />
                  <path
                    className="opacity-75"
                    fill="currentColor"
                    d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                  />
                </svg>
                Sprawdzanie...
              </span>
            ) : (
              'Sprawdź wygrane'
            )}
          </button>
        </div>
      </form>

      {/* Info text */}
      <p className="text-xs text-gray-500 mt-4">
        Zakres dat może obejmować maksymalnie 3 lata
      </p>
    </div>
  );
}
