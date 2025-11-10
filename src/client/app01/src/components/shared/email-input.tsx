import React from 'react';

export interface EmailInputProps {
  label: string;
  value: string;
  error?: string;
  onChange: (value: string) => void;
  required?: boolean;
  placeholder?: string;
  autoFocus?: boolean;
}

const EmailInput: React.FC<EmailInputProps> = ({
  label,
  value,
  error,
  onChange,
  required = true,
  placeholder = 'twoj@email.com',
  autoFocus = false,
}) => {
  const inputId = 'email-input';
  const errorId = 'email-error';

  return (
    <div className="mb-4">
      <label
        htmlFor={inputId}
        className="block text-sm font-medium text-gray-700 mb-1"
      >
        {label}
      </label>
      <input
        id={inputId}
        type="email"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        required={required}
        placeholder={placeholder}
        autoFocus={autoFocus}
        {...(error && {
          "aria-invalid": "true",
          "aria-describedby": errorId
        })}
        className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-offset-2 transition-colors ${
          error
            ? 'border-red-500 focus:ring-red-500'
            : 'border-gray-300 focus:ring-blue-500'
        }`}
      />
      {error && (
        <span
          id={errorId}
          className="block text-sm text-red-600 mt-1"
          role="alert"
        >
          {error}
        </span>
      )}
    </div>
  );
};

export default EmailInput;
