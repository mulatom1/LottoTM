export interface XLottoActualDrawsResponse {
  success: boolean;
  data: string;
}

export interface XLottoDrawItem {
  DrawDate: string;
  GameType: string;
  Numbers: number[];
  DrawSystemId: number;
  TicketPrice?: number | null;
  WinPoolCount1?: number | null;
  WinPoolAmount1?: number | null;
  WinPoolCount2?: number | null;
  WinPoolAmount2?: number | null;
  WinPoolCount3?: number | null;
  WinPoolAmount3?: number | null;
  WinPoolCount4?: number | null;
  WinPoolAmount4?: number | null;
}

export interface XLottoDrawsData {
  Data: XLottoDrawItem[];
}
