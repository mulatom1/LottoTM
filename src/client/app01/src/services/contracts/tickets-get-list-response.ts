/**
 * DTO for a single ticket in the list
 */
export interface TicketDto {
  /**
   * Ticket ID
   */
  id: number;

  /**
   * User ID who owns this ticket
   */
  userId: number;

  /**
   * Array of 6 lottery numbers ordered by position (1-49)
   */
  numbers: number[];

  /**
   * UTC timestamp when ticket was created
   */
  createdAt: string; // ISO 8601 date string
}

/**
 * Response contract for GET /api/tickets
 * Contains list of tickets with pagination metadata
 */
export interface TicketsGetListResponse {
  /**
   * List of tickets belonging to the authenticated user
   */
  tickets: TicketDto[];

  /**
   * Total number of tickets for the user
   */
  totalCount: number;

  /**
   * Current page number (always 1 in MVP - no pagination)
   */
  page: number;

  /**
   * Number of items per page (equals totalCount in MVP)
   */
  pageSize: number;

  /**
   * Total number of pages (always 0 or 1 in MVP)
   */
  totalPages: number;

  /**
   * Maximum tickets allowed per user (100)
   */
  limit: number;
}
