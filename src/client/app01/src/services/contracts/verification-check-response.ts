export interface DrawVerificationResult {
    drawId: number;
    drawDate: string; // YYYY-MM-DD
    drawSystemId: number; // System identifier for the draw
    lottoType: string; // "LOTTO" or "LOTTO PLUS"
    drawNumbers: number[];
    hits: number;
    winningNumbers: number[];
    ticketPrice: number | null; // Price of the ticket (nullable)
    winPoolCount1: number | null; // Number of winners for tier 1 (6 matches)
    winPoolAmount1: number | null; // Prize amount for tier 1 (6 matches)
    winPoolCount2: number | null; // Number of winners for tier 2 (5 matches)
    winPoolAmount2: number | null; // Prize amount for tier 2 (5 matches)
    winPoolCount3: number | null; // Number of winners for tier 3 (4 matches)
    winPoolAmount3: number | null; // Prize amount for tier 3 (4 matches)
    winPoolCount4: number | null; // Number of winners for tier 4 (3 matches)
    winPoolAmount4: number | null; // Prize amount for tier 4 (3 matches)
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
  