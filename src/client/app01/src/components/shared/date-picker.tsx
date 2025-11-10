import React from 'react';

export interface DatePickerProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  error?: string;
  min?: string;
  max?: string;
  required?: boolean;
  className?: string;
}

/**
 * DatePicker component using native HTML5 date input
 * Supports inline validation and error display
 */
const DatePicker: React.FC<DatePickerProps> = ({
  label,
  value,
  onChange,
  error,
  min,
  max,
  required = false,
  className = '',
}) => {
  const inputId = `date-picker-${label.toLowerCase().replace(/\s+/g, '-')}`;
  const errorId = `${inputId}-error`;

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onChange(e.target.value);
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
        type="date"
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
        className={`
          border rounded px-3 py-2 text-sm
          focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent
          ${error ? 'border-red-500' : 'border-gray-300'}
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

export default DatePicker;
