/**
 * Response contract for getting a draw by ID
 * GET /api/draws/{id}
 */
export interface DrawsGetByIdResponse {
  /**
   * Draw identifier
   */
  id: number;

  /**
   * Date of the lottery draw (ISO 8601 format: YYYY-MM-DD)
   */
  drawDate: string;

  /**
   * Type of lottery game: "LOTTO" or "LOTTO PLUS"
   */
  lottoType: string;

  /**
   * Array of 6 drawn numbers (1-49), sorted by position
   */
  numbers: number[];

  /**
   * Timestamp when the draw was created in the system (ISO 8601 format)
   */
  createdAt: string;
}
