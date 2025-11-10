import React, { useEffect } from 'react';

export interface ToastProps {
  message: string | null;
  onClose: () => void;
  duration?: number;
  variant?: 'success' | 'error' | 'info';
}

/**
 * Toast notification component
 * Auto-dismisses after specified duration
 */
const Toast: React.FC<ToastProps> = ({
  message,
  onClose,
  duration = 4000,
  variant = 'success',
}) => {
  useEffect(() => {
    if (message) {
      const timer = setTimeout(() => {
        onClose();
      }, duration);

      return () => clearTimeout(timer);
    }
  }, [message, duration, onClose]);

  if (!message) return null;

  const variantClasses = {
    success: 'bg-green-600 text-white',
    error: 'bg-red-600 text-white',
    info: 'bg-blue-600 text-white',
  };

  return (
    <div className="fixed bottom-4 right-4 z-50 animate-slide-in-up">
      <div
        className={`
          ${variantClasses[variant]}
          px-6 py-3 rounded-lg shadow-lg
          flex items-center gap-3
          max-w-md
        `}
        role="alert"
        aria-live="polite"
      >
        <p className="text-sm font-medium">{message}</p>
        <button
          onClick={onClose}
          className="ml-4 text-white hover:text-gray-200 focus:outline-none"
          aria-label="Zamknij powiadomienie"
        >
          <svg
            className="w-4 h-4"
            fill="none"
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth="2"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>
    </div>
  );
};

export default Toast;
