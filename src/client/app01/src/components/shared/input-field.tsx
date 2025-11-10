import { useId } from 'react';

interface InputFieldProps {
  label: string;
  type: 'text' | 'email' | 'password';
  value: string;
  onChange: (value: string) => void;
  onBlur?: () => void;
  error?: string;
  placeholder?: string;
  required?: boolean;
  autoFocus?: boolean;
}

function InputField({
  label,
  type,
  value,
  onChange,
  onBlur,
  error,
  placeholder,
  required = false,
  autoFocus = false
}: InputFieldProps) {
  const inputId = useId();
  const errorId = useId();

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onChange(e.target.value);
  };

  const borderClass = error
    ? 'border-red-500 focus:ring-red-500 focus:border-red-500'
    : 'border-gray-300 focus:ring-blue-500 focus:border-blue-500';

  return (
    <div className="mb-4">
      <label
        htmlFor={inputId}
        className="block text-sm font-medium text-gray-700 mb-1"
      >
        {label}
        {required && <span className="text-red-500 ml-1">*</span>}
      </label>
      <input
        id={inputId}
        type={type}
        value={value}
        onChange={handleChange}
        onBlur={onBlur}
        placeholder={placeholder}
        required={required}
        autoFocus={autoFocus}
        className={`
          w-full px-3 py-2 border rounded-md shadow-sm
          focus:outline-none focus:ring-2 focus:ring-offset-2
          transition-colors duration-200
          ${borderClass}
        `}
        {...(error && {
          "aria-invalid": "true",
          "aria-describedby": errorId
        })}
      />
      {error && (
        <p
          id={errorId}
          className="text-red-600 text-sm mt-1"
          role="alert"
        >
          {error}
        </p>
      )}
    </div>
  );
}

export default InputField;
