/**
 * Request contract for GET /api/tickets
 * Optional query parameters for filtering tickets
 * UserId is extracted from JWT token
 */
export interface TicketsGetListRequest {
  /**
   * Optional partial match filter by group name (case-sensitive, max 100 chars)
   * Uses LIKE/Contains search
   */
  groupName?: string;
}
