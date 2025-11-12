/**
 * Response from POST /api/tickets/generate-random
 * Contains generated numbers as a preview/proposal
 * User must manually save using POST /api/tickets to persist
 */
export interface TicketsGenerateRandomResponse {
  /**
   * Array of 6 generated lottery numbers (1-49)
   * @example [7, 19, 22, 33, 38, 45]
   */
  numbers: number[];
}
