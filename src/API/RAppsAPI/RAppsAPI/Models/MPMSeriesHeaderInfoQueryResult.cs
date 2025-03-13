namespace RAppsAPI.Models.MPM
{
    internal class MPMSeriesHeaderInfoQueryResult
    {
        public int SeriesID { get; set; }
        public int SeriesNum { get; set; }
        public int RowNum { get; set; }
        public int ColNum { get; set; }
        public string Value { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string Style { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;

    }







}
