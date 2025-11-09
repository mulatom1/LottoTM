/**
 * Response contract for creating a lottery draw result
 * POST /api/draws - 201 Created
 */
export interface DrawsCreateResponse {
  /**
   * Success message confirming draw creation
   */
  message: string;
}
