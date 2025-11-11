/**
 * Request contract for updating an existing lottery ticket
 * PUT /api/tickets/{id}
 */
export interface TicketsPutByIdRequest {
  /**
   * Optional group name for organizing tickets (max 100 chars)
   */
  groupName?: string;
  /**
   * Array of 6 unique numbers in range 1-49
   */
  numbers: number[];
}
