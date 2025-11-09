/**
 * Response contract for DELETE /api/draws/{id}
 * Represents the response after successfully deleting a lottery draw
 */
export interface DrawsDeleteResponse {
  /**
   * Success message confirming draw deletion
   * Example: "Losowanie usunięte pomyślnie"
   */
  message: string;
}
