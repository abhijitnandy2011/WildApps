namespace RAppsAPI.Models.MPM
{
   
    public class MPMReadRequestDTO
    {
        public int ReqId { get; set; } // unique Id set by client to track req
        public int FileId { get; set; }
        public int TestRunTime { get; set; }
        public List<MPMWorkbookReadsSheet> Sheets { get; set; }
    }

    public class MPMWorkbookReadsSheet
    {
        public int SheetId { get; set; } 
        public List<MPMWorkbookReadsRect> Rects { get; set; }
    }

    public class MPMWorkbookReadsRect
    {
        public int top { get; set; }
        public int left { get; set; }
        public int right { get; set; }
        public int bottom { get; set; }
    }

    public class MPMReadRequestResponseDTO
    {
        public int Code { get; set; } = -1;
        public string Message { get; set; } = string.Empty;
        public int ReqId { get; set; } // unique Id set by client to track req
        public int FileId { get; set; }        
        public List<MPMReadResponseSheet> Sheets { get; set; }
    }

    public class MPMReadResponseSheet
    {
        public int SheetId { get; set; }
        //public List<int> EmptyRows { get; set; }
        public List<MPMReadResponseRow> Rows { get; set; }
    }

    public class MPMReadResponseRow
    {
        // RowNum is needed so that entire rows can be skipped if empty
        public int RN { get; set; }
        public List<MPMRichCell> Cells { get; set; }
    }

}
