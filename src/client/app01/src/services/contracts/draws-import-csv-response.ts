/**
 * Response for Draws CSV import operation
 * POST /api/draws/import-csv
 */
export interface DrawsImportCsvResponse {
  /**
   * Number of successfully imported draws
   */
  imported: number;

  /**
   * Number of rejected draws due to validation errors
   */
  rejected: number;

  /**
   * List of import errors with row numbers and reasons
   */
  errors: DrawImportError[];
}

/**
 * Details of a single draw import error
 */
export interface DrawImportError {
  /**
   * Row number in CSV file (1-based, excluding header)
   */
  row: number;

  /**
   * Reason for rejection
   */
  reason: string;
}
