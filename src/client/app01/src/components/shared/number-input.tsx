import React from 'react';

export interface NumberInputProps {
  label: string;
  value: number | '';
  onChange: (value: number | '') => void;
  error?: string;
  min?: number;
  max?: number;
  required?: boolean;
  className?: string;
}

/**
 * NumberInput component with validation for range 1-49
 * Used in DrawFormModal and TicketFormModal
 */
const NumberInput: React.FC<NumberInputProps> = ({
  label,
  value,
  onChange,
  error,
  min = 1,
  max = 49,
  required = false,
  className = '',
}) => {
  const inputId = `number-input-${label.toLowerCase().replace(/\s+/g, '-')}`;
  const errorId = `${inputId}-error`;

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value;
    if (val === '') {
      onChange('');
    } else {
      const numVal = parseInt(val, 10);
      if (!isNaN(numVal)) {
        onChange(numVal);
      }
    }
  };

  return (
    <div className={`flex flex-col ${className}`}>
      <label
        htmlFor={inputId}
        className="text-sm font-medium text-gray-700 mb-1"
      >
        {label}
        {required && <span className="text-red-600 ml-1">*</span>}
      </label>
      <input
        type="number"
        id={inputId}
        value={value}
        onChange={handleChange}
        min={min}
        max={max}
        required={required}
        {...(error && {
          "aria-invalid": "true",
          "aria-describedby": errorId
        })}
        placeholder={`${min}-${max}`}
        className={`
          border rounded px-3 py-2 text-sm
          focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent
          ${error ? 'border-red-500' : value !== '' && !error ? 'border-green-500' : 'border-gray-300'}
        `}
      />
      {error && (
        <p id={errorId} className="text-red-600 text-sm mt-1" role="alert">
          {error}
        </p>
      )}
    </div>
  );
};

export default NumberInput;
