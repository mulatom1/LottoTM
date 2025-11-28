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
   * Type of lottery game: "LOTTO" or "LOTTO PLUS"
   */
  lottoType: string;

  /**
   * Array of 6 drawn numbers (sorted by position, 1-49)
   */
  numbers: number[];

  /**
   * System identifier for the draw
   */
  drawSystemId: number;

  /**
   * Ticket price for this draw
   */
  ticketPrice: number | null;

  /**
   * Count of winners in tier 1 (6 matches)
   */
  winPoolCount1: number | null;

  /**
   * Prize amount for tier 1 (6 matches)
   */
  winPoolAmount1: number | null;

  /**
   * Count of winners in tier 2 (5 matches)
   */
  winPoolCount2: number | null;

  /**
   * Prize amount for tier 2 (5 matches)
   */
  winPoolAmount2: number | null;

  /**
   * Count of winners in tier 3 (4 matches)
   */
  winPoolCount3: number | null;

  /**
   * Prize amount for tier 3 (4 matches)
   */
  winPoolAmount3: number | null;

  /**
   * Count of winners in tier 4 (3 matches)
   */
  winPoolCount4: number | null;

  /**
   * Prize amount for tier 4 (3 matches)
   */
  winPoolAmount4: number | null;

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
