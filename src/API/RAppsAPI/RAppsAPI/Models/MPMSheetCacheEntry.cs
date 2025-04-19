namespace RAppsAPI.Models.MPM
{
   
    public class MPMSheetCacheEntry
    {
        public int FileId { get; set; }
        public int SheetNum { get; set; }
        public List<int> State { get; set; }   // should be a set
        public List<MPMWorkbookEditsRow> Rows { get; set; }
    }
    
}
