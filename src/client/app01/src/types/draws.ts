/**
 * ViewModel types for Draws Page
 * These types are used for UI state management and component props
 */

/**
 * State for date range filter
 */
export interface FilterState {
  /** Start date in YYYY-MM-DD format or empty string */
  dateFrom: string;
  /** End date in YYYY-MM-DD format or empty string */
  dateTo: string;
  /** Whether filter is currently active (any filter applied) */
  isActive: boolean;
}

/**
 * State for pagination
 */
export interface PaginationState {
  /** Current page number (1-indexed) */
  currentPage: number;
  /** Total number of pages */
  totalPages: number;
  /** Number of items per page (fixed at 20 in MVP) */
  pageSize: number;
  /** Total count of draws in database */
  totalCount: number;
}

/**
 * Form data for adding or editing a draw
 */
export interface DrawFormData {
  /** Draw date in YYYY-MM-DD format */
  drawDate: string;
  /** Array of 6 numbers, empty string indicates empty field */
  numbers: (number | '')[];
}

/**
 * Validation errors for draw form
 */
export interface DrawFormErrors {
  /** Error message for draw date field */
  drawDate?: string;
  /** Array of 6 error messages, one per number field */
  numbers?: string[];
}

/**
 * State for modals in DrawsPage
 */
export interface ModalState {
  /** Whether draw form modal is open */
  isFormOpen: boolean;
  /** Mode of form modal: 'add' or 'edit' */
  formMode: 'add' | 'edit';
  /** Draw being edited (null for add mode) */
  editingDraw: DrawDto | null;
  /** Whether delete confirmation modal is open */
  isDeleteOpen: boolean;
  /** Draw being deleted (null when modal closed) */
  deletingDraw: DrawDto | null;
  /** Whether error modal is open */
  isErrorOpen: boolean;
  /** List of error messages to display in error modal */
  errorMessages: string[];
}

/**
 * Query parameters for GET /api/draws endpoint
 */
export interface DrawsQueryParams {
  /** Page number (1-indexed) */
  page: number;
  /** Number of items per page */
  pageSize: number;
  /** Field to sort by */
  sortBy: 'drawDate' | 'createdAt';
  /** Sort order: ascending or descending */
  sortOrder: 'asc' | 'desc';
  /** Optional filter: start date (YYYY-MM-DD) */
  dateFrom?: string;
  /** Optional filter: end date (YYYY-MM-DD) */
  dateTo?: string;
}

/**
 * Re-export DrawDto from API contracts for convenience
 */
export interface DrawDto {
  id: number;
  drawDate: string;
  numbers: number[];
  createdAt: string;
}
