/**
 * Response for deleting all user tickets
 * DELETE /api/tickets/all
 */
export interface TicketsDeleteAllResponse {
  /**
   * Success message
   */
  message: string;

  /**
   * Number of tickets deleted
   */
  deletedCount: number;
}
