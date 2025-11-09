/**
 * Request contract for creating a new lottery ticket
 * POST /api/tickets
 */
export interface TicketsCreateRequest {
  /**
   * Array of 6 unique numbers in range 1-49
   */
  numbers: number[];
}
