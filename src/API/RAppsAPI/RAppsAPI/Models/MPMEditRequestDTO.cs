// TODO: Unit testing needed of editing, adding, removing rows, sheets & tables
// Directly applying row data in between without accounting for rows added/removed
// will lead to data corruption. Same with cells/columns

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RAppsAPI.Models.MPM
{
    public class MPMEditRequestDTO
    {
        public int ReqId { get; set; } // unique Id set by client to track req
        public int FileId { get; set; }
        public int LastModifiedTime { get; set; } // The last modification time the front saw
                                                  // This is to check if backend has changes after front last took changes
        public int TestRunTime { get; set; } // DEBUG code
        public List<MPMWorkbookEditsSheet> EditedSheets { get; set; }
        public List<MPMWorkbookEditsSheet> AddedSheets { get; set; } // Added sheets must be put here
        public List<string> RemovedSheets { get; set; } // Names of removed sheets
        public List<MPMWorkbookReadsSheet> ReadSheets { get; set; }
    }

    public class MPMWorkbookEditsSheet
    {
        public string SheetName { get; set; }  // sheet name is unique
        public string NewSheetName { get; set; }  // If sheet was renamed
        public List<MPMWorkbookEditsRow> EditedRows { get; set; } // edited rows
        public List<int> AddedRows { get; set; }
        public List<int> RemovedRows { get; set; }
        public List<int> AddedColumns { get; set; }  // Column add/removes affect all rows, so must be applied first
        public List<int> RemovedColumns { get; set; }
        public List<int> ColumnWidths { get; set; }  // User may change column width
        public List<MPMWorkbookEditsTable> EditedTables { get; set; }  // edited tables - only resizes allowed
        public List<MPMWorkbookEditsTable> AddedTables { get; set; } // Added tables must be put here
        public List<string> RemovedTables { get; set; } // Names of removed tables
    }

    public class MPMWorkbookEditsRow
    {
        // RowNum is needed so that entire rows can be skipped if empty
        public int RN { get; set; }
        public List<MPMRichCell> Cells { get; set; }
        public int Height { get; set; }  // User may change row height
    }

    public class MPMWorkbookEditsTable
    {
        public string TableName { get; set; }  // table name is unique
        public int NumRows { get; set; }
        public int NumCols { get; set; }
        public int StartRowNum { get; set; }
        public int StartColNum { get; set; }
        public int EndRowNum { get; set; }
        public int EndColNum { get; set; }
        public int TableType { get; set; }  // Range/Series header/detail
        public string Style { get; set; } = null!;
        public bool HeaderRow { get; set; }
        public bool TotalRow { get; set; }
        public bool BandedRows { get; set; }
        public bool BandedColumns { get; set; }
        public bool FilterButton { get; set; }
    }   

}
