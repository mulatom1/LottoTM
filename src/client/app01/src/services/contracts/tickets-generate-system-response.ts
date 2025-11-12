/**
 * Response from POST /api/tickets/generate-system endpoint
 * Contains 9 generated tickets as preview/proposal covering all numbers 1-49
 * User must manually save each ticket using POST /api/tickets to persist
 */
export interface TicketsGenerateSystemResponse {
  /**
   * List of 9 generated tickets (proposals)
   */
  tickets: TicketDto[];
}

/**
 * DTO representing a single generated ticket (preview only)
 */
export interface TicketDto {
  /**
   * 6 lottery numbers (1-49)
   * @example [7, 15, 23, 31, 39, 47]
   */
  numbers: number[];
}
