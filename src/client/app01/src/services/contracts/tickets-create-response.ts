/**
 * Response contract for creating a new lottery ticket
 * POST /api/tickets
 */
export interface TicketsCreateResponse {
  /**
   * ID of the created ticket
   */
  id: number;

  /**
   * Success message
   */
  message: string;
}
