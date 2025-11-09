/**
 * Response from POST /api/tickets/generate-random
 * Contains success message and generated ticket ID
 */
export interface TicketsGenerateRandomResponse {
  /**
   * Success message
   * @example "Zestaw wygenerowany pomyślnie"
   */
  message: string;

  /**
   * ID of the generated ticket
   * @example 123
   */
  ticketId: number;
}
