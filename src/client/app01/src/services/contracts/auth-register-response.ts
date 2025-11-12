/**
 * Response contract for user registration
 * POST /api/auth/register - 201 Created
 * Backend automatically logs in the user and returns JWT token
 */
export interface AuthRegisterResponse {
  /**
   * JWT bearer token for authentication
   */
  token: string;

  /**
   * Unique user identifier
   */
  userId: number;

  /**
   * User's email address
   */
  email: string;

  /**
   * Flag indicating if user has admin privileges
   */
  isAdmin: boolean;

  /**
   * Token expiration timestamp (ISO 8601 format, UTC)
   */
  expiresAt: string;
}
