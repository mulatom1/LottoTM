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
  /** Type of lottery game: "LOTTO" or "LOTTO PLUS" */
  lottoType: string;
  /** Array of 6 numbers, empty string indicates empty field */
  numbers: (number | '')[];
  /** System identifier for the draw (required in backend, '' means empty field in UI) */
  drawSystemId: number | '';
  /** Ticket price for this draw (optional, '' means empty field in UI) */
  ticketPrice?: number | '';
  /** Count of winners in tier 1 (6 matches) - optional */
  winPoolCount1?: number | '';
  /** Prize amount for tier 1 (6 matches) - optional */
  winPoolAmount1?: number | '';
  /** Count of winners in tier 2 (5 matches) - optional */
  winPoolCount2?: number | '';
  /** Prize amount for tier 2 (5 matches) - optional */
  winPoolAmount2?: number | '';
  /** Count of winners in tier 3 (4 matches) - optional */
  winPoolCount3?: number | '';
  /** Prize amount for tier 3 (4 matches) - optional */
  winPoolAmount3?: number | '';
  /** Count of winners in tier 4 (3 matches) - optional */
  winPoolCount4?: number | '';
  /** Prize amount for tier 4 (3 matches) - optional */
  winPoolAmount4?: number | '';
}

/**
 * Validation errors for draw form
 */
export interface DrawFormErrors {
  /** Error message for draw date field */
  drawDate?: string;
  /** Error message for lotto type field */
  lottoType?: string;
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
  lottoType: string;
  numbers: number[];
  drawSystemId: number;
  ticketPrice: number | null;
  winPoolCount1: number | null;
  winPoolAmount1: number | null;
  winPoolCount2: number | null;
  winPoolAmount2: number | null;
  winPoolCount3: number | null;
  winPoolAmount3: number | null;
  winPoolCount4: number | null;
  winPoolAmount4: number | null;
  createdAt: string;
}
