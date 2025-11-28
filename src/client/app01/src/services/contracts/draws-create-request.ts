/**
 * Request contract for creating a new lottery draw result
 * POST /api/draws
 */
export interface DrawsCreateRequest {
  /**
   * Date of the lottery draw (YYYY-MM-DD format)
   * Requirements:
   * - Must not be in the future
   * - Must be unique globally (one draw per date and lottoType)
   */
  drawDate: string;

  /**
   * Type of lottery game
   * Allowed values: "LOTTO", "LOTTO PLUS"
   * Required field
   */
  lottoType: string;

  /**
   * Array of exactly 6 lottery numbers
   * Requirements:
   * - Exactly 6 numbers
   * - Each number must be in range 1-49
   * - All numbers must be unique
   */
  numbers: number[];

  /**
   * System identifier for the draw (required)
   */
  drawSystemId: number;

  /**
   * Ticket price for this draw (optional)
   */
  ticketPrice?: number;

  /**
   * Count of winners in tier 1 (6 matches) - optional
   */
  winPoolCount1?: number;

  /**
   * Prize amount for tier 1 (6 matches) - optional
   */
  winPoolAmount1?: number;

  /**
   * Count of winners in tier 2 (5 matches) - optional
   */
  winPoolCount2?: number;

  /**
   * Prize amount for tier 2 (5 matches) - optional
   */
  winPoolAmount2?: number;

  /**
   * Count of winners in tier 3 (4 matches) - optional
   */
  winPoolCount3?: number;

  /**
   * Prize amount for tier 3 (4 matches) - optional
   */
  winPoolAmount3?: number;

  /**
   * Count of winners in tier 4 (3 matches) - optional
   */
  winPoolCount4?: number;

  /**
   * Prize amount for tier 4 (3 matches) - optional
   */
  winPoolAmount4?: number;
}
