/**
 * Response contract for user registration
 * POST /api/auth/register - 201 Created
 */
export interface AuthRegisterResponse {
  /**
   * Success message confirming registration
   */
  message: string;
}
