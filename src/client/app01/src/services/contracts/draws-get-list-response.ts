/**
 * Data Transfer Object for a single lottery draw
 */
export interface DrawDto {
  /**
   * Draw identifier
   */
  id: number;

  /**
   * Date of the lottery draw (ISO 8601 format)
   */
  drawDate: string;

  /**
   * Array of 6 drawn numbers (sorted by position, 1-49)
   */
  numbers: number[];

  /**
   * Timestamp when draw was created in the system (ISO 8601 format)
   */
  createdAt: string;
}

/**
 * Response for GET /api/draws endpoint
 * Contains paginated list of lottery draws with metadata
 */
export interface DrawsGetListResponse {
  /**
   * List of draw DTOs for the current page
   */
  draws: DrawDto[];

  /**
   * Total number of draws in the database
   */
  totalCount: number;

  /**
   * Current page number
   */
  page: number;

  /**
   * Number of items per page
   */
  pageSize: number;

  /**
   * Total number of pages available
   */
  totalPages: number;
}
