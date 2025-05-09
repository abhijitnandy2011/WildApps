﻿namespace RAppsAPI.Models.MPM
{
    public class MPMGetRangeInfoResponseDTO
    {
        public int Code { get; set; } = -1;
        public string Message { get; set; } = string.Empty;
        public MPMRangeInformation RangeInfo { get; set; }
        public MPMSeriesInformation SeriesInfo { get; set; }
    }

    public class MPMRangeInformation
    {
        public int RangeId { get; set; }
        public int NumSeriesActual { get; set; } = -1;   // because UI can request less than complete series list
                                            // but would like to know how many are there(paginated series requests)
        public List<MPMRangeInfoField> Fields { get; set; }
    }

    public class MPMRangeInfoField
    {
        public string Name { get; set; } = string.Empty;
        public List<MPMRichCell> Cells { get; set; }  // Each value is actually a cell with formula,
                                                  // formatting, style etc
    }

    public class MPMSeriesInformation
    {
        public List<MPMSeriesInfoRow> Series { get; set; }
    }

    public class MPMSeriesInfoRow
    {
        public int SeriesId { get; set; }
        public int SeriesNum { get; set; }
        public MPMSeriesHeader SeriesHeader { get; set; }
        public MPMSeriesDetail SeriesDetail { get; set; }
    }


    public class MPMSeriesHeader
    {
        public List<MPMSeriesHeaderField> Fields { get; set; }
    }

    public class MPMSeriesHeaderField
    {
        public string Name { get; set; }
        public List<MPMRichCell> Cells { get; set; } // These are cells, have to each be richer
    }

    public class MPMSeriesDetail
    {
        public int NumRows { get; set; }
        public int NumCols { get; set; }
        public List<MPMSeriesDetailRow> Rows { get; set; }
    }

    public class MPMSeriesDetailRow
    {
        // RowNum is needed so that entire rows can be skipped if empty
        public int RN { get; set; } 
        public List<MPMRichCell> Cells { get; set; }
    }

    public class MPMRichCell
    {
        public int CN { get; set; }
        public string? Value { get; set; }  // empty cells can have color, formatting etc
        public string? VType { get; set; }
        public string? Formula { get; set; }
        public string? Format { get; set; }
        public string? Style { get; set; }
        public string? Comment { get; set; }
    }
}
