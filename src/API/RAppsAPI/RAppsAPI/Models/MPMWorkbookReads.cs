namespace RAppsAPI.Models.MPM
{
   
    public class MPMReadRequestDTO
    {
        public int ReqId { get; set; } // unique Id set by client to track req
        public List<MPMWorkbookReadsSheet> Sheets { get; set; }
    }

    public class MPMWorkbookReadsSheet
    {
        public int SheetNum { get; set; }  // sheet number
        public List<MPMWorkbookReadsRect> Rects { get; set; }
    }

    public class MPMWorkbookReadsRect
    {
        public int top { get; set; }
        public int left { get; set; }
        public int right { get; set; }
        public int bottom { get; set; }
    }
    
}
