export interface DrawVerificationResult {
    drawId: number;
    drawDate: string; // YYYY-MM-DD
    drawNumbers: number[];
    hits: number;
    winningNumbers: number[];
  }
  
  export interface TicketVerificationResult {
    ticketId: number;
    ticketNumbers: number[];
    draws: DrawVerificationResult[];
  }
  
  export interface VerificationCheckResponse {
    results: TicketVerificationResult[];
    totalTickets: number;
    totalDraws: number;
    executionTimeMs: number;
  }
  