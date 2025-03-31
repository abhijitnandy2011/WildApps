// GetRangeInfo response classes

export class MPMGetRangeInfoResponse {
  code: number;
  message: string;
  rangeInfo: MPMRangeInformation;
  seriesInfo: MPMSeriesInformation;
}

export class MPMRangeInformation {
  rangeId: number;
  numSeriesActual: number;
  fields: MPMRangeInfoField[];
}

export class MPMRangeInfoField {
  name: string;
  cells: MPMRichCell;
}

export class MPMSeriesInformation {
  series: MPMSeriesInfoRow[];
}

export class MPMSeriesInfoRow {
  seriesId: number;
  seriesNum: number;
  seriesHeader: MPMSeriesHeader;
  seriesDetail: MPMSeriesDetail;
}

export class MPMSeriesHeader {
  fields: MPMSeriesHeaderField[];
}

export class MPMSeriesHeaderField {
  name: string;
  Cells: MPMRichCell[];
}

export class MPMSeriesDetail {
  numRows: number;
  numCols: number;
  rows: MPMSeriesDetailRow[];
}

export class MPMSeriesDetailRow {
  rn: number;
  cells: MPMRichCell[];
}

export class MPMRichCell {
  cn: number;
  value?: string;
  vType?: string;
  formula?: string;
  format?: string;
  style?: string;
  comment?: string;
}
