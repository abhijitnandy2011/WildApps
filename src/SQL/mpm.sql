-- MPM related tables


-- TODO
-- Logging for operations done in DB e.g. in SPs
DROP TABLE mpm.DBLogs;
CREATE TABLE mpm.DBLogs(    
	ID            UDT_ID_BIG IDENTITY(1,1),    
	Module        UDT_Name,     
    Code          UDT_ID,
	Msg           UDT_Name_Med,      
	Description   UDT_LogDescription_Opt,  -- details with stack trace   
	ID1           UDT_ID_BIG_Opt,    
	ID2           UDT_ID_BIG_Opt, 
    CreatedBy     UDT_ID,    -- VUserID i.e. ID from the VUser table
    CreatedDate   UDT_DateTime,
	CONSTRAINT PK_MPM_DBLogs PRIMARY KEY (ID)	
)


-- TODO
DROP TABLE mpm.Workbooks;
CREATE TABLE mpm.Workbooks(
    ID               UDT_ID_BIG IDENTITY(1,1),
    VFileID          UDT_ID,    
    Name             UDT_Name,  -- same as VFile.Name
	LatestBackupID   UDT_ID_BIG, 
	LatestVersionID  UDT_ID_BIG,   -- version number may not be sequential
	LockHoldTimeInSecs     UDT_Number_Int,
	BackupFrequencyInDays  UDT_Number_Int,  -- 0 means no backups
    Settings         UDT_Settings_Opt,   -- Other settings go here in JSON string format - can be columnized if necessary later
    CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_Number_Int_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus          UDT_RowStatus,
    CONSTRAINT PK_MPM_WorkbookID PRIMARY KEY (ID),
);


-- TODO
DROP TABLE mpm.WBEventLogs;
CREATE TABLE mpm.WBEventLogs(
    ID               UDT_ID_BIG IDENTITY(1,1),   -- useful for knowing event order, imp for edits order when restoring
    VFileID          UDT_ID,    
    EventTypeID      UDT_ID,
	BackupID         UDT_ID, 
    ID1              UDT_ID_BIG,   -- ID relevant to event, like EditID, more could be added later 
    CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_Number_Int_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus          UDT_RowStatus,
    CONSTRAINT PK_MPM_WBEventLogID PRIMARY KEY (ID),
);


-- TODO
DROP TABLE mpm.WBEventTypes;
CREATE TABLE mpm.WBEventTypes(
    ID               UDT_ID,
    Name             UDT_Name,
    CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_MPM_WBEventTypeID PRIMARY KEY (ID),
);



-- TODO
DROP TABLE mpm.Edits;
CREATE TABLE mpm.Edits(
    ID               UDT_ID_BIG IDENTITY(1,1),
    VFileID          UDT_ID,
	BackupID         UDT_ID,
	Json             nvarchar(MAX),
	TrackingID       UDT_ID,          -- Tracking ID from client to support searching later    
	Code             UDT_ID,          -- negative for fail, 0 for success, 1 for pending, > 1 for warning
	Reason           nvarchar(2048),  -- fail or warn reason
    CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_Number_Int_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,    -- if Applied then applied datetime, else if failed then failed datetime, null if pending
    RStatus          UDT_RowStatus,
    CONSTRAINT PK_MPM_EditID PRIMARY KEY (ID),
);



-- TODO
DROP TABLE mpm.Locks;
CREATE TABLE mpm.Locks(
    VFileID          UDT_ID,
	LockTypeID       UDT_ID,               -- only 1 entry per lock type for a file
	Locked           UDT_Bool,
    CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_MPM_LockID PRIMARY KEY (VFileID, LockTypeID),
);


-- TODO
DROP TABLE mpm.LockTypes;
CREATE TABLE mpm.LockTypes(
    ID                UDT_ID,
    Name              UDT_Name,
    CreatedBy         UDT_ID,
    CreatedDate       UDT_DateTime,
    LastUpdatedBy     UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_MPM_LockTypeID PRIMARY KEY (ID),
);



DROP TABLE mpm.Sheets;
CREATE TABLE mpm.Sheets(
    VFileID          UDT_ID,
	SheetID          UDT_ID,
    Name             UDT_Name,
    SheetNum         UDT_Sequence,   -- separate from SheetID as the sheet order can be changed, wont change sheet id then
    Style            UDT_Style,     -- any colors or bold applied to the sheet name
    StartRowNum      UDT_CellRow,
    StartColNum      UDT_CellColumn,
    EndRowNum        UDT_CellRow,
    EndColNum        UDT_CellColumn,
	CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_MPM_SheetsID PRIMARY KEY (VFileID, SheetID),
);


DROP TABLE mpm.Products;
CREATE TABLE mpm.Products(
    VFileID          UDT_ID,
	ProductID        UDT_ID,
    Name             UDT_Name,
	SheetID          UDT_ID,
    CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_MPM_ProductsID PRIMARY KEY (VFileID, ProductID),
);


DROP TABLE mpm.ProductTypes;
CREATE TABLE mpm.ProductTypes(
    VFileID          UDT_ID,
	ProductTypeID    UDT_ID,
    Name             UDT_Name,
	ProductID        UDT_ID,
    CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_MPM_ProductTypesID PRIMARY KEY (VFileID, ProductTypeID),
);


-- TODO
DROP TABLE mpm.MRanges;
CREATE TABLE mpm.MRanges(
    VFileID          UDT_ID,
	RangeID          UDT_ID,    -- unique within file
    Name             UDT_Name,
	SheetID          UDT_ID,    -- in which sheet to export the range when downloading
	ProductID        UDT_ID,
	ProductTypeID    UDT_ID,
	HeaderTableID    UDT_ID,
    RangeNum         UDT_Sequence,   -- order of range within sheet, in same sheet there can be multiple ranges    
    CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_MPM_RangesID PRIMARY KEY (VFileID, RangeID),
);


DROP TABLE mpm.MSeries;
CREATE TABLE mpm.MSeries(
    VFileID          UDT_ID,
	SeriesID         UDT_ID, 
	Name             UDT_Name,
	RangeID          UDT_ID,
	SheetID          UDT_ID,
	HeaderTableID    UDT_ID,
	DetailTableID    UDT_ID,
    SeriesNum        UDT_Sequence,
    CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_MPM_SeriesID PRIMARY KEY (VFileID, SeriesID),
);


DROP TABLE mpm.MTables;
CREATE TABLE mpm.MTables(
    VFileID          UDT_ID,
	TableID          UDT_ID,       -- unique within a file
    Name             UDT_Name_med,
    NumRows          UDT_CellRow,    
	NumCols          UDT_CellColumn,
	StartRowNum      UDT_CellRow,     -- location within sheet
    StartColNum      UDT_CellColumn,
    EndRowNum        UDT_CellRow,
    EndColNum        UDT_CellColumn,  
	RangeID          UDT_ID_Opt,   -- if related to a Range, its ID will be here
	SeriesID         UDT_ID_Opt,    -- if related to a Series, its ID will be here
	SheetID          UDT_ID,       -- needed for master tables
	TableType        UDT_ID,          -- 1:Master, 2:Range header, 3:Series header, 100+:Series detail table types(min/max, fat etc)
    Style            UDT_CellStyle,   
    HeaderRow        UDT_Bool,
    TotalRow         UDT_Bool,
    BandedRows       UDT_Bool,
    BandedColumns    UDT_Bool,
    FilterButton     UDT_Bool,
    CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_MPM_MTablesID PRIMARY KEY (VFileID, TableID),
);


DROP TABLE mpm.Cells;
CREATE TABLE mpm.Cells(
    VFileID          UDT_ID,
	SheetID          UDT_ID,   -- unique only within a file
	CellID           UDT_ID,   -- unique only within a sheet, useful for formulas later when cell row/col changes but id doesnt
    RowNum           UDT_CellRow,   -- unique only within a table, its table scoped
    ColNum           UDT_CellColumn,  -- unique only within a table, its table scoped
    Value            UDT_CellValue,
    Formula          UDT_CellFormula,  -- maybe manage this in a separate formula table for optimum checking with dependencies of the formula(to decide when to call calculate())
    Format           UDT_CellFormat,
    Style            UDT_CellStyle,
	Comment          UDT_CellComment,
    CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_MPM_CellsID PRIMARY KEY (VFileID, SheetID, CellID)
);


------------------------------------------------------------

-- TODO
-- Backup data tables

DROP TABLE mpm.WBBackups;
CREATE TABLE mpm.WBBackups(
    VFileID          UDT_ID,
	BackupID         UDT_ID_BIG,
	Code             UDT_ID,          -- negative for fail, 0 for success, +ve for backed up with warning
	Reason           nvarchar(2048),  -- fail or warn reason
    CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_MPM_WBBackupID PRIMARY KEY (VFileID, BackupID),
);


DROP TABLE mpm.WBVersions;
CREATE TABLE mpm.WBVersions(
    VFileID          UDT_ID,
	VersionID        UDT_ID_BIG, 
	Description      UDT_Name_Big,
	BackupID         UDT_ID_BIG,    -- Backup on which the version was made	
    CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_MPM_WBVersionID PRIMARY KEY (VFileID, VersionID),
);



-- Any changes to mpm.Sheets needs to be done here too
DROP TABLE mpm.BackupSheets;
CREATE TABLE mpm.BackupSheets(
    VFileID          UDT_ID,
	BackupID         UDT_ID_BIG,
	SheetID          UDT_ID,
    Name             UDT_Name,
    SheetNum         UDT_Sequence,   -- separate from SheetID as the sheet order can be changed, wont change sheet id then
    Style            UDT_Style,     -- any colors or bold applied to the sheet name
    StartRowNum      UDT_CellRow,
    StartColNum      UDT_CellColumn,
    EndRowNum        UDT_CellRow,
    EndColNum        UDT_CellColumn,
	SheetCreatedBy         UDT_ID,
    SheetCreatedDate       UDT_DateTime,
    SheetLastUpdatedBy     UDT_ID_Opt,
    SheetLastUpdatedDate   UDT_DateTime_Opt,
    SheetRStatus           UDT_RowStatus,
	CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus          UDT_RowStatus,
    CONSTRAINT PK_MPM_BackupSheetID PRIMARY KEY (VFileID, BackupID, SheetID),
);


DROP TABLE mpm.BackupProducts;
CREATE TABLE mpm.BackupProducts(
    VFileID          UDT_ID,
	BackupID         UDT_ID_BIG,
	ProductID        UDT_ID,
    Name             UDT_Name,
	SheetID          UDT_ID,
	ProductCreatedBy        UDT_ID,
    ProductCreatedDate      UDT_DateTime,
    ProductLastUpdatedBy    UDT_ID_Opt,
    ProductLastUpdatedDate   UDT_DateTime_Opt,
    ProductRStatus           UDT_RowStatus,
    CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_MPM_BackupProductID PRIMARY KEY (VFileID, BackupID, ProductID),
);


DROP TABLE mpm.BackupProductTypes;
CREATE TABLE mpm.BackupProductTypes(
    VFileID          UDT_ID,
	BackupID         UDT_ID_BIG,
	ProductTypeID    UDT_ID,
    Name             UDT_Name,
	ProductID        UDT_ID,
	ProductTypeCreatedBy        UDT_ID,
    ProductTypeCreatedDate      UDT_DateTime,
    ProductTypeLastUpdatedBy    UDT_ID_Opt,
    ProductTypeLastUpdatedDate   UDT_DateTime_Opt,
    ProductTypeRStatus           UDT_RowStatus,
    CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_MPM_BackupProductTypeID PRIMARY KEY (VFileID, BackupID, ProductTypeID),
);


-- TODO
DROP TABLE mpm.BackupMRanges;
CREATE TABLE mpm.BackupMRanges(
    VFileID          UDT_ID,
	BackupID         UDT_ID_BIG,
	RangeID          UDT_ID,    -- unique within file
    Name             UDT_Name,
	SheetID          UDT_ID,    -- in which sheet to export the range when downloading
	ProductID        UDT_ID,
	ProductTypeID    UDT_ID,
	HeaderTableID    UDT_ID,
    RangeNum         UDT_Sequence,   -- order of range within sheet, in same sheet there can be multiple ranges    
    MRangeCreatedBy        UDT_ID,
    MRangeCreatedDate      UDT_DateTime,
    MRangeLastUpdatedBy    UDT_ID_Opt,
    MRangeLastUpdatedDate   UDT_DateTime_Opt,
    MRangeRStatus           UDT_RowStatus,
	CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_MPM_BackupMRangeID PRIMARY KEY (VFileID, BackupID, RangeID),
);


DROP TABLE mpm.BackupMSeries;
CREATE TABLE mpm.BackupMSeries(
    VFileID          UDT_ID,
	BackupID         UDT_ID_BIG,
	SeriesID         UDT_ID, 
	Name             UDT_Name,
	RangeID          UDT_ID,
	SheetID          UDT_ID,
	HeaderTableID    UDT_ID,
	DetailTableID    UDT_ID,
    SeriesNum        UDT_Sequence,
    MSeriesCreatedBy        UDT_ID,
    MSeriesCreatedDate      UDT_DateTime,
    MSeriesLastUpdatedBy    UDT_ID_Opt,
    MSeriesLastUpdatedDate   UDT_DateTime_Opt,
    MSeriesRStatus           UDT_RowStatus,
	CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_MPM_BackupMSeriesID PRIMARY KEY (VFileID, BackupID, SeriesID),
);


DROP TABLE mpm.BackupMTables;
CREATE TABLE mpm.BackupMTables(
    VFileID          UDT_ID,
	BackupID         UDT_ID_BIG,
	TableID          UDT_ID,       -- unique within a file
    Name             UDT_Name_med,
    NumRows          UDT_CellRow,    
	NumCols          UDT_CellColumn,
	StartRowNum      UDT_CellRow,     -- location within sheet
    StartColNum      UDT_CellColumn,
    EndRowNum        UDT_CellRow,
    EndColNum        UDT_CellColumn,  
	RangeID          UDT_ID_Opt,   -- if related to a Range, its ID will be here
	SeriesID         UDT_ID_Opt,    -- if related to a Series, its ID will be here
	SheetID          UDT_ID,       -- needed for master tables
	TableType        UDT_ID,          -- 1:Master, 2:Range header, 3:Series header, 100+:Series detail table types(min/max, fat etc)
    Style            UDT_CellStyle,   
    HeaderRow        UDT_Bool,
    TotalRow         UDT_Bool,
    BandedRows       UDT_Bool,
    BandedColumns    UDT_Bool,
    FilterButton     UDT_Bool,
	MTableCreatedBy        UDT_ID,
    MTableCreatedDate      UDT_DateTime,
    MTableLastUpdatedBy    UDT_ID_Opt,
    MTableLastUpdatedDate   UDT_DateTime_Opt,
    MTableRStatus           UDT_RowStatus,
	CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_MPM_BackupMTableID PRIMARY KEY (VFileID, BackupID, TableID),
);


-- No storage of meta columns like CreatedBy for Cells as too much space will be taken up
DROP TABLE mpm.BackupCells;
CREATE TABLE mpm.BackupCells(
    VFileID          UDT_ID,
	BackupID         UDT_ID_BIG,
	SheetID          UDT_ID,   -- unique only within a file
	CellID           UDT_ID,   -- unique only within a sheet, useful for formulas later when cell row/col changes but id doesnt
    RowNum           UDT_CellRow,   -- unique only within a table, its table scoped
    ColNum           UDT_CellColumn,  -- unique only within a table, its table scoped
    Value            UDT_CellValue,
    Formula          UDT_CellFormula,  -- maybe manage this in a separate formula table for optimum checking with dependencies of the formula(to decide when to call calculate())
    Format           UDT_CellFormat,
    Style            UDT_CellStyle,
	Comment          UDT_CellComment,
    CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_MPM_BackupCellID PRIMARY KEY (VFileID, BackupID, SheetID, CellID)
);

