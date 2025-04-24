using EFCore_DBLibrary;

namespace WildExcelLoader.models
{
    // Used for assigning RangeId to MSeries and a SeriesId from a series within the range
    // to series detail MTable.
    // Also needed for adding the MRange to db later as a List<MRangeWithRow> will be maintained.
    internal class MRangeWithRow
    {
        public MRange DBRange { get; set; }
        // Range header table's row position, used to decide this range's RangeNum
        // & assign multiple series to it.
        public int Row { get; set; }
        // Got from the Range header
        // After a series is confirmed by row distance, check here & remove the series name
        // TODO: If any are left after all series are done, its an error
        public HashSet<string> setSeriesNames { get; set; } = new();
    }

    // Used for assigning RangeID to a MSeries. List<MSeriesWithRow> will be maintained.
    // Also to add MSeries to db later.
    internal class MSeriesWithRow
    {
        public MSeries DBSeries { get; set; }
        // Series header's row position - used for row distance calc to find the MRange for above
        // MSeries entry. The Series Header MTable is already updated with the SeriesId, so not 
        // needed here.
        public int Row { get; set; }
    }

    // Used for locating the range and then assigning the correct SeriesId within that range
    // to Series Detail MTable(looks up dictSeriesNameVsID in class MRangeWithRow).
    // List<MTableWithRow> will be maintained.
    internal class MTableWithRow
    {
        public MTable DBTable;
        // After a series is confirmed by row distance, check here & remove the series name
        public int Row;        
    }

    internal class CellStyle
    {
        public string bg  {get; set; }
        public CellFont font {get; set; }
    };

    internal class CellFont
    {
        public string c{get; set; }
        public string n{get; set; }
        public string b{get; set; }
        public string i {get; set; }
        public string u {get; set; }
        public string s {get; set; }
    };

    public enum RStatus
    {
        Inactive = 1,
        Active = 2
    };

    enum TableTypes
    {
        MASTER = 1,
        RANGE_HEADER = 2,
        RANGE_SERIES_HEADER = 3,
        RANGE_SERIES_DETAIL_MINMAX = 100
    };


    enum ParseRetCode
    {
        SUCCESS = 0,
        INVALID_RANGE_HEADER_DIMS,
        INVALID_RANGE_HEADER_TABLE_NAME,
        INVALID_RANGE_SERIES_HEADER_DIMS,
        INVALID_PRODUCT,
        INVALID_PRODUCT_TYPE_OR_RANGE,
        INVALID_SERIES_HEADER_TABLE_NAME,
        INVALID_SERIES_HEADER_SERIES_NAME,
        INVALID_SERIES_HEADER_DIMS,
        INVALID_SERIES_DETAIL_TABLE_NAME,
        INVALID_SERIES_DETAIL_SERIES_NAME,
        INVALID_SERIES_DETAIL_DIMS,
        // Create file for download, return codes
        RANGE_HEADER_MTABLES_NOT_FOUND,
        MTABLE_RANGE_ID_MISMATCH,
        MTABLE_TYPE_MISMATCH,
        MTABLE_LESS_CELLS,
        MTABLE_CELL_INVALID_ROW,
        MTABLE_CELL_INVALID_COL,
        // File comparison
        COMPARE_SHEET_ABSENT,
        COMPARE_TABLE_ROW_COL_MISMATCH,
        COMPARE_TABLE_NOT_FOUND,
        COMPARE_CELL_VALUE_MISMATCH
    };

    //------------------- download related types -------------------------
    internal class OutRangeInfo
    {
        public int RangeId { get; set; }
        public int HeaderTableId { get; set; }
    }

    internal class OutSeriesInfo
    {
        public int HeaderTableId { get; set; }
        public int DetailTableId { get; set; }
    }


    // TODO:
    // Checks on sheet data
    // Does the table have right dimensions to contain all necessary fields?
    // No field or field value should be empty, like Table Name, Product etc
    // 
}
