import React, { useEffect } from 'react';
import Button from './button';

export interface ErrorModalProps {
  isOpen: boolean;
  onClose: () => void;
  errors: string[] | string;
}

const ErrorModal: React.FC<ErrorModalProps> = ({ isOpen, onClose, errors }) => {
  // Handle Escape key to close modal
  useEffect(() => {
    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && isOpen) {
        onClose();
      }
    };

    if (isOpen) {
      document.addEventListener('keydown', handleEscape);
      // Prevent body scroll when modal is open
      document.body.style.overflow = 'hidden';
    }

    return () => {
      document.removeEventListener('keydown', handleEscape);
      document.body.style.overflow = 'unset';
    };
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  const errorList = Array.isArray(errors) ? errors : [errors];
  const modalTitleId = 'error-modal-title';

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center"
      role="dialog"
      aria-modal="true"
      aria-labelledby={modalTitleId}
    >
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-black bg-opacity-50 transition-opacity"
        onClick={onClose}
        aria-hidden="true"
      />

      {/* Modal content */}
      <div className="relative bg-white rounded-lg shadow-xl max-w-md w-full mx-4 p-6 z-10">
        {/* Header */}
        <div className="flex items-center justify-between mb-4">
          <h2
            id={modalTitleId}
            className="text-xl font-bold text-gray-900"
          >
            Błąd
          </h2>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 focus:outline-none focus:ring-2 focus:ring-blue-500 rounded"
            aria-label="Zamknij modal"
          >
            <svg
              className="w-6 h-6"
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

        {/* Body - Error list */}
        <div className="mb-6">
          <ul className="list-disc list-inside space-y-2">
            {errorList.map((error, index) => (
              <li key={index} className="text-red-600 text-sm">
                {error}
              </li>
            ))}
          </ul>
        </div>

        {/* Footer */}
        <div className="flex justify-end">
          <Button
            variant="primary"
            onClick={onClose}
            type="button"
          >
            Zamknij
          </Button>
        </div>
      </div>
    </div>
  );
};

export default ErrorModal;
