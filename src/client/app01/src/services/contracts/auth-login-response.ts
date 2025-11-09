/**
 * Response interface for successful user authentication
 * Contains JWT token and user information
 */
export interface AuthLoginResponse {
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
