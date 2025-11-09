/**
 * Response for DELETE /api/tickets/{id}
 * Confirms successful deletion of a lottery ticket
 */
export interface TicketsDeleteResponse {
  /**
   * Confirmation message
   * Example: "Zestaw usunięty pomyślnie"
   */
  message: string;
}
