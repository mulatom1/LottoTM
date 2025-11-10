/**
 * Validates email format using regex pattern
 * @param email - Email string to validate
 * @returns true if email format is valid, false otherwise
 */
export const validateEmail = (email: string): boolean => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
};
