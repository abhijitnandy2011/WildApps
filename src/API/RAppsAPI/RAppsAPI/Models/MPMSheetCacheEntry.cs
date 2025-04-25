namespace RAppsAPI.Models.MPM
{

    public class MPMSheetCacheEntry
    {
        public int FileId { get; set; }
        public int SheetId { get; set; }
        public int SheetNum { get; set; }
        public Dictionary<int, MPMSheetCacheRowEntry> RowNumberVsRowEntry { get; set; } 
    };

    public class MPMSheetCacheRowEntry
    {
        int state; // can be 1 - building, 2 - temp, 3 - db
        MPMWorkbookEditsRow row {get; set;}
    }
}
