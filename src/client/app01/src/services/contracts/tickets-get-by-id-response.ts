/**
 * Response interface for GET /api/tickets/{id}
 * Contains details of a single lottery ticket
 */
export interface TicketsGetByIdResponse {
  /**
   * ID of the ticket
   */
  id: number;

  /**
   * ID of the user who owns this ticket
   */
  userId: number;

  /**
   * Array of 6 lottery numbers (1-49) in order by position
   */
  numbers: number[];

  /**
   * UTC timestamp when the ticket was created
   */
  createdAt: string;
}
