

DROP TABLE mpm.Sheets;
CREATE TABLE mpm.Sheets(
    VFileID          UDT_ID,
	SheetID          UDT_ID,
    Name             UDT_Name,
    SheetNum         UDT_Sequence,   -- separate from SheetID as the sheet order can be changed, wont change sheet id then
    Style            UDT_Style,     -- any colors or bold applied to the sheet name
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
	RangeID          UDT_ID,
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
    InfoTable1ID     UDT_ID_Opt,
	InfoTable2ID     UDT_ID_Opt,
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
	TableID          UDT_ID,  
    Name             UDT_Name_med,
    NumRows          UDT_CellRow,    
	NumCols          UDT_CellColumn,
	RangeID          UDT_ID_Opt,   -- if related to a Range, its ID will be here
	SeriesID         UDT_ID_Opt,    -- if related to a Series, its ID will be here
	SheetID          UDT_ID_Opt,       -- needed for master tables
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
	TableID          UDT_ID, 
	CellID           UDT_ID,   -- unique only within a table, useful for formulas later when cell row/col changes but id doesnt
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
    CONSTRAINT PK_MPM_CellsID PRIMARY KEY (VFileID, TableID, CellID)
);



DROP TABLE mpm.FormulaRefs;
CREATE TABLE mpm.FormulaRefs(
Target, source
)
