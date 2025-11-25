/**
 * Response for CSV import operation
 * POST /api/tickets/import-csv
 */
export interface TicketsImportCsvResponse {
  /**
   * Number of successfully imported tickets
   */
  imported: number;

  /**
   * Number of rejected tickets due to validation errors
   */
  rejected: number;

  /**
   * List of import errors with row numbers and reasons
   */
  errors: ImportError[];
}

/**
 * Details of a single import error
 */
export interface ImportError {
  /**
   * Row number in CSV file (1-based, excluding header)
   */
  row: number;

  /**
   * Reason for rejection
   */
  reason: string;
}
