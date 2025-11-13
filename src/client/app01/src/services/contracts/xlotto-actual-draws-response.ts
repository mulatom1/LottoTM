export interface XLottoActualDrawsResponse {
  success: boolean;
  data: string;
}

export interface XLottoDrawItem {
  DrawDate: string;
  GameType: string;
  Numbers: number[];
}

export interface XLottoDrawsData {
  Data: XLottoDrawItem[];
}
