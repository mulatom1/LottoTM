export interface WinningTicketResult {
  ticketId: number;
  matchingNumbers: number[];
}

export interface DrawsResult {
  drawId: number;
  drawDate: string; // YYYY-MM-DD
  drawSystemId: number;
  lottoType: string;
  drawNumbers: number[];
  ticketPrice: number | null;
  winPoolCount1: number | null;
  winPoolAmount1: number | null;
  winPoolCount2: number | null;
  winPoolAmount2: number | null;
  winPoolCount3: number | null;
  winPoolAmount3: number | null;
  winPoolCount4: number | null;
  winPoolAmount4: number | null;
  winningTicketsResult: WinningTicketResult[];
}

export interface TicketsResult {
  ticketId: number;
  groupName: string;
  ticketNumbers: number[];
}

export interface VerificationCheckResponse {
  executionTimeMs: number;
  drawsResults: DrawsResult[];
  ticketsResults: TicketsResult[];
}
  