namespace RAppsAPI.Models.MPM
{
    public class MPMEditRequestDTO
    {
        public int ReqId { get; set; } // unique Id set by client to track req
        public int FileId { get; set; }
        public int TestRunTime { get; set; }
        public List<MPMWorkbookEditsSheet> EditedSheets { get; set; }
        public List<MPMWorkbookReadsSheet> ReadSheets { get; set; }
    }

    public class MPMWorkbookEditsSheet
    {
        public int SheetId { get; set; }  // sheet number
        public List<MPMWorkbookEditsRow> Rows { get; set; }
    }

    public class MPMWorkbookEditsRow
    {
        // RowNum is needed so that entire rows can be skipped if empty
        public int RN { get; set; }
        public List<MPMRichCell> Cells { get; set; }
    }

   

    
}
