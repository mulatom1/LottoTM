/**
 * Request contract for updating an existing lottery draw
 * PUT /api/draws/{id}
 */
export interface DrawsUpdateRequest {
  /**
   * Draw date in YYYY-MM-DD format (cannot be in the future)
   */
  drawDate: string;

  /**
   * Type of lottery game
   * Allowed values: "LOTTO", "LOTTO PLUS"
   * Required field
   */
  lottoType: string;

  /**
   * Array of 6 drawn numbers (1-49, unique)
   */
  numbers: number[];
}
