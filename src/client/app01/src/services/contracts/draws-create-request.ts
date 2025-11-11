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
}
