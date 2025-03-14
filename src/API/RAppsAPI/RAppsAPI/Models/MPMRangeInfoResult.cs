namespace RAppsAPI.Models.MPM
{
    public class MPMRangeInfoResult
    {
        public int SeriesCount { get; }
        public int CellId { get; }
        public int RowNum { get; }
        public int ColNum { get; }
        public string Value { get; }
        public string Formula { get; }
        public string Format { get; }
        public string Style { get; }
        public string Comment { get; }

        public MPMRangeInfoResult(int seriesCount, int cellId, int rowNum, int colNum, string value, string formula, string format, string style, string comment)
        {
            SeriesCount = seriesCount;
            CellId = cellId;
            RowNum = rowNum;
            ColNum = colNum;
            Value = value;
            Formula = formula;
            Format = format;
            Style = style;
            Comment = comment;
        }

        public override bool Equals(object? obj)
        {
            return obj is MPMRangeInfoResult other &&
                   SeriesCount == other.SeriesCount &&
                   CellId == other.CellId &&
                   RowNum == other.RowNum &&
                   ColNum == other.ColNum &&
                   Value == other.Value &&
                   Formula == other.Formula &&
                   Format == other.Format &&
                   Style == other.Style &&
                   Comment == other.Comment;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(SeriesCount);
            hash.Add(CellId);
            hash.Add(RowNum);
            hash.Add(ColNum);
            hash.Add(Value);
            hash.Add(Formula);
            hash.Add(Format);
            hash.Add(Style);
            hash.Add(Comment);
            return hash.ToHashCode();
        }
    }
}
