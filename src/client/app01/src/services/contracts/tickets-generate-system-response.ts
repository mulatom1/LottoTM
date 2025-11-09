/**
 * Response from POST /api/tickets/generate-system endpoint
 * Contains details of 9 generated system tickets covering all numbers 1-49
 */
export interface TicketsGenerateSystemResponse {
  /**
   * Success message
   */
  message: string;

  /**
   * Number of tickets generated (always 9 for system generation)
   */
  generatedCount: number;

  /**
   * List of generated ticket details
   */
  tickets: TicketDto[];
}

/**
 * DTO representing a single generated ticket
 */
export interface TicketDto {
  /**
   * Unique ticket identifier
   */
  id: number;

  /**
   * 6 lottery numbers (sorted by position)
   */
  numbers: number[];

  /**
   * UTC timestamp of creation
   */
  createdAt: string;
}
