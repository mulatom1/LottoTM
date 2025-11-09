/**
 * Request contract for GET /api/tickets
 * No parameters needed - returns all tickets for authenticated user
 * UserId is extracted from JWT token
 */
// eslint-disable-next-line @typescript-eslint/no-empty-object-type
export interface TicketsGetListRequest {
  // Empty interface - no parameters needed
  // The endpoint uses JWT authentication to identify the user
}
