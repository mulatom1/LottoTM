import React from 'react';
import type { NumberInputProps } from '../../types/tickets';

const NumberInput: React.FC<NumberInputProps> = ({
  label,
  value,
  onChange,
  error,
  min = 1,
  max = 49,
  required = false
}) => {
  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value;

    if (val === '') {
      onChange('');
      return;
    }

    const numValue = parseInt(val, 10);
    if (!isNaN(numValue)) {
      onChange(numValue);
    }
  };

  const borderColor = error ? 'border-red-500' : value !== '' ? 'border-gray-300' : 'border-gray-300';

  return (
    <div className="flex flex-col">
      <label
        htmlFor={label}
        className="text-sm font-medium text-gray-700 mb-1"
      >
        {label}
        {required && <span className="text-red-500 ml-1">*</span>}
      </label>
      <input
        id={label}
        type="number"
        min={min}
        max={max}
        value={value}
        onChange={handleChange}
        className={`px-3 py-2 border ${borderColor} rounded focus:outline-none focus:ring-2 focus:ring-blue-500 transition-colors`}
        aria-invalid={!!error}
        aria-describedby={error ? `${label}-error` : undefined}
        required={required}
      />
      {error && (
        <span id={`${label}-error`} className="text-red-600 text-sm mt-1">
          {error}
        </span>
      )}
    </div>
  );
};

export default NumberInput;
