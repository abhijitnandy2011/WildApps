﻿using RAppsAPI.Models.MPM;

namespace RAppsAPI.Models.MPM
{

    public enum MPMCacheRowState
    {
        Temp = 1,
        DB = 2,
    }

    public class MPMSheetCacheEntry
    {
        public int FileId { get; set; }
        public string SheetName { get; set; }
        public HashSet<int> EmptyRows { get; set; } // NOTE: This is needed. If a row is 
                                                    // absent from RowNumberVsRowEntry that means its not cached,
                                                    // not that its empty. So adding it in EmptyRows will prevent
                                                    // backend from going to DB for it as its empty anyway.
                                                    // That may change over time, but entries will be invalidated after
                                                    // an edit, so when entries are recreated, the row will be
                                                    // absent from EmptyRows.
        public Dictionary<int, MPMSheetCacheRowEntry> RowNumberVsRowEntry { get; set; }
        public List<MPMSheetCacheTableEntry> Tables { get; set; } // TODO: should this be split as Added/Removed?
    };

    public class MPMSheetCacheRowEntry
    {
        public MPMCacheRowState State { get; set; } // can be 1 - building, 2 - temp, 3 - db
        public MPMWorkbookEditsRow Row { get; set; }
    }

    public class MPMSheetCacheTableEntry
    {
    }

    //------ User edit requests list ------
    public enum MPMUserEditReqState
    {
        Intermediate = 1,
        Done = 2,
    }

    public class MPMUserEditReqsCacheEntry
    {
        public Dictionary<int, int> ReqIdVsState { get; set; }
    }
}
