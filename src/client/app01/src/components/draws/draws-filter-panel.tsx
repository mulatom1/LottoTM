import React, { useState } from 'react';
import DatePicker from '../shared/date-picker';
import Button from '../shared/button';

export interface DrawsFilterPanelProps {
  onFilter: (dateFrom?: string, dateTo?: string) => void;
  onClearFilter: () => void;
  isFilterActive: boolean;
  initialDateFrom?: string;
  initialDateTo?: string;
}

/**
 * Filter panel for draws list
 * Allows filtering by date range (From/To)
 */
const DrawsFilterPanel: React.FC<DrawsFilterPanelProps> = ({
  onFilter,
  onClearFilter,
  isFilterActive,
  initialDateFrom = '',
  initialDateTo = '',
}) => {
  const [dateFrom, setDateFrom] = useState<string>(initialDateFrom);
  const [dateTo, setDateTo] = useState<string>(initialDateTo);
  const [validationError, setValidationError] = useState<string>('');

  const handleFilter = () => {
    // Clear previous validation error
    setValidationError('');

    // Inline validation: dateFrom must be <= dateTo
    if (dateFrom && dateTo && dateFrom > dateTo) {
      setValidationError("Data 'Od' musi być wcześniejsza lub równa 'Do'");
      return;
    }

    // Call parent handler with filter values (or undefined if empty)
    onFilter(dateFrom || undefined, dateTo || undefined);
  };

  const handleClear = () => {
    setDateFrom('');
    setDateTo('');
    setValidationError('');
    onClearFilter();
  };

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4 mb-4">
      <div className="flex flex-col md:flex-row gap-4 items-start md:items-end">
        {/* Date From */}
        <DatePicker
          label="Od"
          value={dateFrom}
          onChange={setDateFrom}
          className="flex-1"
        />

        {/* Date To */}
        <DatePicker
          label="Do"
          value={dateTo}
          onChange={setDateTo}
          className="flex-1"
        />

        {/* Action buttons */}
        <div className="flex gap-2 w-full md:w-auto">
          <Button
            variant="primary"
            onClick={handleFilter}
            className="flex-1 md:flex-none"
          >
            Filtruj
          </Button>

          {isFilterActive && (
            <Button
              variant="secondary"
              onClick={handleClear}
              className="flex-1 md:flex-none"
            >
              Wyczyść
            </Button>
          )}
        </div>
      </div>

      {/* Validation error message */}
      {validationError && (
        <p className="text-red-600 text-sm mt-3" role="alert">
          {validationError}
        </p>
      )}
    </div>
  );
};

export default DrawsFilterPanel;
