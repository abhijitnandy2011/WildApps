namespace RAppsAPI.Models.MPM
{
   
    public class MPMReadRequestDTO
    {
        public int ReqId { get; set; } // unique Id set by client to track req
        public int FileId { get; set; }
        public int TestRunTime { get; set; }  // DEBUG
        public List<int>? CheckCompletedEditReqIds { get; set; } // App will chk if these edit reqs have completed before reading
        public List<MPMWorkbookReadsSheet> Sheets { get; set; }
    }

    public class MPMWorkbookReadsSheet
    {
        public string SheetName { get; set; }
        public bool IncludeTableInfo { get; set; }
        public List<MPMWorkbookReadsRect> Rects { get; set; }
    }

    public class MPMWorkbookReadsRect
    {
        public int top { get; set; }
        public int left { get; set; }
        public int right { get; set; }
        public int bottom { get; set; }
    }


    //---------------------------------------------
    // Response

    public class MPMReadRequestResponseDTO
    {
        public int Code { get; set; } = -1;
        public string Message { get; set; } = string.Empty;
        public Dictionary<int, int> CompletedEditRequests { get; set; } // TODO: List of completed edit reqs as dictionary, will it convert to map<int, int>?
        public List<int> IncompleteEditRequests { get; set; }
        public Dictionary<int, MPMFailedEditInfo> FailedEditRequests { get; set; }
        public int ReqId { get; set; } // unique Id set by client to track req
        public int FileId { get; set; }
        public int NumSheets { get; set; }  // Add other workbook level settings here
        public List<MPMReadResponseSheet> Sheets { get; set; }        
    }

    public class MPMFailedEditInfo
    {
        public int ReqId { get; set; } // unique Id set by client to track req
        public int Code { get; set; } = -1;
        public string Message { get; set; } = string.Empty;        
        public int FileId { get; set; }
    }

    public class MPMReadResponseSheet
    {
        public string SheetName { get; set; }   // TODO: Sheet name format may be useful later
        public List<MPMReadResponseRow> Rows { get; set; }
        public List<MPMReadResponseTable> Tables { get; set; }
    }

    public class MPMReadResponseRow
    {
        // RowNum is needed so that entire rows can be skipped if empty
        public int RN { get; set; }
        public int State { get; set; } // 1 for DB, 2 for temp
        public List<MPMRichCell> Cells { get; set; }
    }

    public class MPMReadResponseTable
    {
        public string TableName { get; set; }  // table name is unique
        public int NumRows { get; set; }
        public int NumCols { get; set; }
        public int StartRowNum { get; set; }
        public int StartColNum { get; set; }
        public int EndRowNum { get; set; }
        public int EndColNum { get; set; }
        public int TableType { get; set; }   // Range/Series header/detail
        public string Style { get; set; } = null!;
        public bool HeaderRow { get; set; }
        public bool TotalRow { get; set; }
        public bool BandedRows { get; set; }
        public bool BandedColumns { get; set; }
        public bool FilterButton { get; set; }
    }

}
