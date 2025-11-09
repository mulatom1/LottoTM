/**
 * Request contract for getting a draw by ID
 * GET /api/draws/{id}
 */
export interface DrawsGetByIdRequest {
  /**
   * The draw ID to retrieve
   */
  id: number;
}
