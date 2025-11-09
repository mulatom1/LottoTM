/**
 * Request interface for user authentication
 * POST /api/auth/login
 */
export interface AuthLoginRequest {
  /**
   * User's email address
   */
  email: string;

  /**
   * User's password (plain text)
   */
  password: string;
}
