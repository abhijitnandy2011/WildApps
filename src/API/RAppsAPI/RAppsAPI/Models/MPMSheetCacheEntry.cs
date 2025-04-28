namespace RAppsAPI.Models.MPM
{

    public enum MPMCacheRowState
    {
        Building = 1,
        Temp = 2,
        DB = 3,
    }

    public class MPMSheetCacheEntry
    {
        public int FileId { get; set; }
        public int SheetId { get; set; }
        public List<int> EmptyRows { get; set; }
        public Dictionary<int, MPMSheetCacheRowEntry> RowNumberVsRowEntry { get; set; } 
    };

    public class MPMSheetCacheRowEntry
    {
        public MPMCacheRowState State { get; set; } // can be 1 - building, 2 - temp, 3 - db
        public MPMWorkbookEditsRow Row {get; set;}
    }
}
