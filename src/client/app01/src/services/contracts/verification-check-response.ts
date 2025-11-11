export interface DrawVerificationResult {
    drawId: number;
    drawDate: string; // YYYY-MM-DD
    lottoType: string; // "LOTTO" or "LOTTO PLUS"
    drawNumbers: number[];
    hits: number;
    winningNumbers: number[];
  }

  export interface TicketVerificationResult {
    ticketId: number;
    groupName: string; // Group name for organizing tickets
    ticketNumbers: number[];
    draws: DrawVerificationResult[];
  }
  
  export interface VerificationCheckResponse {
    results: TicketVerificationResult[];
    totalTickets: number;
    totalDraws: number;
    executionTimeMs: number;
  }
  