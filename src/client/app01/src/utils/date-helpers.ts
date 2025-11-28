// ===================================================================
// Date Helper Functions
// ===================================================================

/**
 * Get default date "From" (-1 week from today)
 * @returns Date string in YYYY-MM-DD format
 */
export function getDefaultDateFrom(): string {
  const date = new Date();
  date.setDate(date.getDate() - 7);
  return date.toISOString().split('T')[0];
}

/**
 * Get default date "To" (today)
 * @returns Date string in YYYY-MM-DD format
 */
export function getDefaultDateTo(): string {
  return new Date().toISOString().split('T')[0];
}

/**
 * Validate date range
 * @param dateFrom - Start date in YYYY-MM-DD format
 * @param dateTo - End date in YYYY-MM-DD format
 * @returns Error message if validation fails, null if valid
 */
export function validateDateRange(dateFrom: string, dateTo: string): string | null {
  if (dateFrom > dateTo) {
    return "Data 'Od' musi być wcześniejsza lub równa 'Do'";
  }

  // Check if date range exceeds 3 years (optional frontend validation)
  const from = new Date(dateFrom);
  const to = new Date(dateTo);
  const diffDays = Math.ceil((to.getTime() - from.getTime()) / (1000 * 60 * 60 * 24));

  if (diffDays > 1095) { // 3 years ≈ 1095 days
    return "Zakres dat nie może przekraczać 3 lat";
  }

  return null; // Valid
}
