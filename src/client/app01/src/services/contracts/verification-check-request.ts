export interface VerificationCheckRequest {
    dateFrom: string; // YYYY-MM-DD
    dateTo: string;   // YYYY-MM-DD
    groupName?: string | null; // Optional group name filter
  }
  