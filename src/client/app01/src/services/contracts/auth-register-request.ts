/**
 * Request contract for user registration
 * POST /api/auth/register
 */
export interface AuthRegisterRequest {
  /**
   * User's email address (used as login)
   */
  email: string;

  /**
   * User's password (plain text, will be hashed on server)
   * Requirements:
   * - Minimum 8 characters
   * - At least 1 uppercase letter
   * - At least 1 digit
   * - At least 1 special character
   */
  password: string;

  /**
   * Password confirmation (must match password)
   */
  confirmPassword: string;
}
