/**
 * Response contract for /api/config endpoint
 */
export interface ConfigResponse {
  /**
   * Maximum number of days allowed for verification date range
   */
  verificationMaxDays: number;
}
