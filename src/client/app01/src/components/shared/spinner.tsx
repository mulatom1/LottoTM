import React from 'react';

export interface SpinnerProps {
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

/**
 * Loading spinner component
 * Used to indicate loading state
 */
const Spinner: React.FC<SpinnerProps> = ({ size = 'md', className = '' }) => {
  const sizeClasses = {
    sm: 'w-4 h-4 border-2',
    md: 'w-8 h-8 border-3',
    lg: 'w-12 h-12 border-4',
  };

  return (
    <div className={`flex justify-center items-center ${className}`}>
      <div
        className={`
          ${sizeClasses[size]}
          border-blue-600 border-t-transparent
          rounded-full animate-spin
        `}
        role="status"
        aria-label="Ładowanie..."
      >
        <span className="sr-only">Ładowanie...</span>
      </div>
    </div>
  );
};

export default Spinner;
