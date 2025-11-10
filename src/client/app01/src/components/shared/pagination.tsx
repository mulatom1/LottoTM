import React from 'react';
import Button from './button';

export interface PaginationProps {
  currentPage: number;
  totalPages: number;
  totalCount: number;
  onPageChange: (page: number) => void;
}

/**
 * Pagination component for navigating through pages
 * Shows page numbers with Previous/Next buttons
 */
const Pagination: React.FC<PaginationProps> = ({
  currentPage,
  totalPages,
  totalCount,
  onPageChange,
}) => {
  // Calculate visible page numbers (max 5 centered around current page)
  const getVisiblePages = (): number[] => {
    const pages: number[] = [];
    const maxVisible = 5;

    if (totalPages <= maxVisible) {
      // Show all pages if total is less than max visible
      for (let i = 1; i <= totalPages; i++) {
        pages.push(i);
      }
    } else {
      // Calculate range centered around current page
      let start = Math.max(1, currentPage - 2);
      let end = Math.min(totalPages, currentPage + 2);

      // Adjust if at start or end
      if (currentPage <= 3) {
        start = 1;
        end = maxVisible;
      } else if (currentPage >= totalPages - 2) {
        start = totalPages - maxVisible + 1;
        end = totalPages;
      }

      for (let i = start; i <= end; i++) {
        pages.push(i);
      }
    }

    return pages;
  };

  const visiblePages = getVisiblePages();

  return (
    <div className="flex flex-col items-center gap-4 mt-6">
      {/* Desktop pagination */}
      <div className="hidden md:flex items-center gap-2">
        {/* Previous button */}
        <Button
          variant="secondary"
          onClick={() => onPageChange(currentPage - 1)}
          disabled={currentPage === 1}
          className="px-4 py-2 text-sm"
        >
          « Poprzednia
        </Button>

        {/* Page numbers */}
        {visiblePages.map((page) => (
          <button
            key={page}
            onClick={() => onPageChange(page)}
            disabled={page === currentPage}
            className={`
              px-4 py-2 rounded-lg font-medium text-sm
              transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500
              ${
                page === currentPage
                  ? 'bg-blue-600 text-white cursor-default'
                  : 'bg-gray-200 hover:bg-gray-300 text-gray-800'
              }
            `}
          >
            {page}
          </button>
        ))}

        {/* Next button */}
        <Button
          variant="secondary"
          onClick={() => onPageChange(currentPage + 1)}
          disabled={currentPage === totalPages}
          className="px-4 py-2 text-sm"
        >
          Następna »
        </Button>
      </div>

      {/* Mobile pagination (simplified) */}
      <div className="flex md:hidden items-center gap-2">
        <Button
          variant="secondary"
          onClick={() => onPageChange(currentPage - 1)}
          disabled={currentPage === 1}
          className="px-3 py-2 text-sm"
        >
          «
        </Button>

        <span className="text-sm text-gray-700 px-4">
          Strona {currentPage} z {totalPages}
        </span>

        <Button
          variant="secondary"
          onClick={() => onPageChange(currentPage + 1)}
          disabled={currentPage === totalPages}
          className="px-3 py-2 text-sm"
        >
          »
        </Button>
      </div>

      {/* Info text */}
      <p className="text-sm text-gray-600">
        Strona {currentPage} z {totalPages} ({totalCount} losowań)
      </p>
    </div>
  );
};

export default Pagination;
