/**
 * Request contract for updating an existing lottery ticket
 * PUT /api/tickets/{id}
 */
export interface TicketsPutByIdRequest {
  /**
   * Array of 6 unique numbers in range 1-49
   */
  numbers: number[];
}
