-- Basic DB script


------------------------------------
-- User defined types

CREATE TYPE dbo.UDT_ID FROM int NOT NULL;

CREATE TYPE dbo.UDT_DateTime FROM datetime NOT NULL;
CREATE TYPE dbo.UDT_DateTime_Opt FROM datetime NULL;
CREATE TYPE dbo.UDT_Name FROM [nvarchar](128) NOT NULL;
CREATE TYPE dbo.UDT_Name_med FROM [nvarchar](512) NOT NULL;
CREATE TYPE dbo.UDT_Name_Big FROM [nvarchar](1024) NOT NULL;
CREATE TYPE dbo.UDT_Settings FROM [nvarchar](2048) NOT NULL;
CREATE TYPE dbo.UDT_Settings_Opt FROM [nvarchar](2048) NULL;
CREATE TYPE dbo.UDT_Style FROM [nvarchar](2048) NOT NULL;

CREATE TYPE dbo.UDT_FileType FROM [int] NOT NULL;

CREATE TYPE dbo.UDT_RowStatus FROM [tinyint] NOT NULL;

CREATE TYPE dbo.UDT_Number_Int FROM int NOT NULL;
CREATE TYPE dbo.UDT_Number_Int_Opt FROM int NULL;
CREATE TYPE dbo.UDT_Sequence FROM smallint NOT NULL; 

CREATE TYPE dbo.UDT_CellRow FROM int NOT NULL;
CREATE TYPE dbo.UDT_CellColumn FROM int NOT NULL;
CREATE TYPE dbo.UDT_CellValue FROM [nvarchar](512) NOT NULL;
CREATE TYPE dbo.UDT_CellFormula FROM [nvarchar](1024) NOT NULL;
CREATE TYPE dbo.UDT_CellFormat FROM [nvarchar](128) NOT NULL;
CREATE TYPE dbo.UDT_CellStyle FROM [nvarchar](1024) NOT NULL;

CREATE TYPE dbo.UDT_Bool FROM bit NOT NULL;














-----------------------------------

In namespace dbo:

-- Entities
-- Only one entry - each row can be a property instead of each column
CREATE TABLE WildRoot(   
    ID 
    CompanyName
    CreatedBy
    CreatedDate
    LastUpdatedBy    UDT_Number_Int_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus
)

-- Basic entities have V infront, to prevent name clashes in queries
-- Users 1 to 1000 are various system related users & admins
-- From 1001, we have real users
CREATE TABLE VUser(
    ID
    Name
    Roles    
    LastLoggedIn
    CreatedBy
    CreatedDate
    LastUpdatedBy    UDT_Number_Int_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus
)


-- Pretty simple system now, assigned to 1 person, no multiple logins.
-- Systems can be shared and an user could own multiple systems - so many to many.
-- Used to manage files/folders, set storage limits, what file types can be created,
-- provide default set of apps & files
-- Also manage desktop settings. App settings are managed by the app itself in its own tables.
CREATE TABLE VSystem(
    ID  int NOT NULL IDENTITY(1,1),
    Name [nvarchar](1024) NULL,
    AssignedTo int NULL,   -- currently assigned to - VUserID
    CreatedBy int NULL,
    CreatedDate datetime NOT NULL,	
    LastUpdatedBy    UDT_Number_Int_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus smallint NOT NULL,
    CONSTRAINT PK_VSystem PRIMARY KEY (Id)
)

-- An app can be installed in many systems
-- Has unique settings for each system.
-- Default apps like desktop & File Browser maintain their settings here in JSON or
-- in their own tables by System/User.
CREATE TABLE VApp(
    ID,
    Name,
    Owner,
    Settings
    CreatedBy
    CreatedDate
    LastUpdatedBy    UDT_Number_Int_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus
)

CREATE TABLE VFolder(
    ID,
    Name,
    CreatedBy
    CreatedDate
    LastUpdatedBy    UDT_Number_Int_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus
)

-- File can be edited by multiple users after sharing, but has one owning VSystem.
-- The user to whom the system is assigned, owns the file.
-- Users can leave, but the system continues owning the file.
-- Various types of files, spreadsheets, docs, text files, shortcuts to other files in same
-- or another system(shared file)
-- File contents are managed in one or more DB tables, like spreadsheets will need Row/Column, tables, etc
CREATE TABLE VFile(
    ID,
    Name,
    VFileTypeID,
    CurrentVersionID,    -- current file version from VFileVersion table
    IsShared,
    CreatedBy
    CreatedDate
    LastUpdatedBy    UDT_Number_Int_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus
)


-- Various file types in the System, default types & App installed types.
-- Shared file is not a type - its a fundamental attribute of the file diff from type.
CREATE TABLE FileType(
    ID,    
    Name           -- Various file types in the system
    CreatedBy    -- VUserID i.e. ID from the VUser table
    CreatedDate
    LastUpdatedBy    UDT_Number_Int_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus
)


-- Lists all versions for all files
CREATE TABLE FileVersion(
    ID,    
    VFileID,
    Version,
    CreatedBy    -- VUserID i.e. ID from the VUser table
    CreatedDate
    LastUpdatedBy    UDT_Number_Int_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus
)

-- Audit tables
-- Various actions and objects in the audit entry needs to be clickable.
-- So the involved object names have to be stored in a special way.
-- The line has to be in a specific format with placeholders for the objects e.g. {object0} etc
-- Audits can happen at various object levels 
-- System level for file/folder/App changes
-- App level for files opened/closed etc
-- Folder level, files created/deleted/renamed/shared etc
-- File level for versioning, file type specific changes based on the app used to open it.
--   Versioning is tied to file(not folder), when file moved to another folder, retains the version info.


-------------------------------

-- Relations

-- Multiple assignments possible - many to many
-- An user could own multiple systems.
-- All user specific settings would be replicated based on the user folder/profile
-- Shared data can also be a user of the VSystem.
CREATE TABLE SystemUser(
    ID,    
    VSystemID,
    VUserID,
    Profile     -- user settings on a specific system, maybe needed later
    CreatedBy
    CreatedDate
    LastUpdatedBy    UDT_Number_Int_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus
)

-- Which system has which apps for which user
-- Apps store user settings here or mapped in their own tables.
-- File Browser app stores settings here as JSON. WildSheets might use its own tables.
-- Apps should not have direct access to this table as other apps settings will be visible!
CREATE TABLE SystemUserApp(
    ID,   
    VSystemID
    VUserID
    VAppID    
    Settings,
    CreatedBy
    CreatedDate
    LastUpdatedBy    UDT_Number_Int_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus
)

-- No need for 2 tables, this has the rel between Syste, Folders & File
-- Which System has which folder & file etc.
CREATE TABLE SystemFolderFile(
    ID,
    VSystemID
    VFolderID 
    VFileID   
    CreatedBy
    CreatedDate
    LastUpdatedBy    UDT_Number_Int_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus
)


-- Shared files - which files are shared with which users.
-- Files are shared with a user, not a system. If user logs in to another system, files
-- will be visible there too - if file is unshared, it will be gone from all systems.
-- Any user can find files shared with him & also find files which he shared.
-- Separate folders to see this are there & in the original folder will show 'shared' icon
CREATE TABLE SharedFileUser(
    ID,    
    VFileID   
    VUserID     
    CreatedBy
    CreatedDate
    LastUpdatedBy    UDT_Number_Int_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus
)


-- A file type could be related/opened by many apps
CREATE TABLE FileTypeApp(
    ID,    
    VFileTypeID
    VAppID
    CreatedBy
    CreatedDate
    LastUpdatedBy    UDT_Number_Int_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus
)

-----------------------------------------

-- Wild Sheets App(Wild Platform, spreadsheet app)
-- Has app specific tables to manage its files
-- Maybe own namespace 'wsa'
-- Each object type has its own tables
-- Sheet, Cell, Table is needed now. Later PivotTable, Chart, Image, Macro etc.

CREATE NAMESPACE wsa;

-- Workbook level settings like which sheet was last opened, which cell was last in focus
-- Last saved etc. These settings are not stored in VFile as that has only File generic stuff.
-- When a WildSheet file is created using the WildSheets app, it will make an entry here to store its data.
-- Its similar to making an entry in the file system for a file - without this there is no data persistence.
-- This is the root for the data storage of the WildSheet file. Root of the WildSheet file data model.
-- Other apps will have other models in other structs optimized for their own data.
-- A workbook is in a file.
DROP TABLE Workbook;
CREATE TABLE Workbook(
    ID               UDT_ID IDENTITY(1,1),
    VFileID          UDT_ID,    
    Name             UDT_Name,  -- same as VFile.Name
    LastOpenedSheet  UDT_Sequence,
    LastFocusCellRow UDT_CellRow,
    LastFocusCellCol UDT_CellColumn,
    Settings         UDT_Settings_Opt,   -- Other settings go here in JSON string format - can be columnized if necessary later for searching/indexing faster
    CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_Number_Int_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus          UDT_RowStatus,
    CONSTRAINT PK_WorkbookID PRIMARY KEY NONCLUSTERED (ID),
);


DROP TABLE Sheet;
CREATE TABLE Sheet(
    ID               UDT_ID IDENTITY(1,1),
    WorkbookID       UDT_ID,
    Name             UDT_Name,
    SheetNum         UDT_Sequence,
    Style            UDT_Style,
    StartRowNum      UDT_CellRow,
    StartColNum      UDT_CellColumn,
    EndRowNum        UDT_CellRow,
    EndColNum        UDT_CellColumn,
    CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_Number_Int_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus          UDT_RowStatus,
    CONSTRAINT PK_SheetID PRIMARY KEY NONCLUSTERED (ID),
    CONSTRAINT FK_Workbook_Sheet FOREIGN KEY (WorkbookID) REFERENCES Workbook(ID)
);


-- Excel tables for all files in the system
DROP TABLE XlTable;
CREATE TABLE XlTable(
    ID               UDT_ID IDENTITY(1,1),
    SheetID          UDT_ID,   
    Name             UDT_Name_med,
    StartRowNum      UDT_CellRow,
    StartColNum      UDT_CellColumn,
    EndRowNum        UDT_CellRow,
    EndColNum        UDT_CellColumn,    
    Style            UDT_CellStyle,   
    HeaderRow        UDT_Bool,
    TotalRow         UDT_Bool,
    BandedRows       UDT_Bool,
    BandedColumns    UDT_Bool,
    FilterButton     UDT_Bool,
    CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_Number_Int_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus          UDT_RowStatus
    CONSTRAINT PK_XlTableID PRIMARY KEY NONCLUSTERED (ID),
    CONSTRAINT FK_Sheet_XlTable FOREIGN KEY (SheetID) REFERENCES Sheet(ID)
);


-- Cell data for current version
-- Only cell data is split between current version and older versions as its a lot.
-- Querying easier with less data.
DROP TABLE Cell;
CREATE TABLE Cell(
    ID               UDT_ID IDENTITY(1,1),
    SheetID          UDT_ID,
    RowNum           UDT_CellRow,
    ColNum           UDT_CellColumn,
    Value            UDT_CellValue,
    Formula          UDT_CellFormula,  -- maybe manage this in a separate formula table for optimum checking with dependencies of the formula(to decide when to call calculate())
    Format           UDT_CellFormat,
    Style            UDT_CellStyle,
    CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_Number_Int_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus          UDT_RowStatus
    CONSTRAINT PK_CellID PRIMARY KEY NONCLUSTERED (ID),
    CONSTRAINT FK_Sheet_Cell FOREIGN KEY (SheetID) REFERENCES Sheet(ID)
);

-- Cell data for older versions. This table will get huge so we dont want to regularly query this.
-- More of historical data.
DROP TABLE CellOldVer;
CREATE TABLE CellOldVer(
    ID               UDT_ID IDENTITY(1,1),    -- this can grow really big later as it will have cells for all workbooks ever, outgrowing int
    VersionID        UDT_ID,   
    SheetID          UDT_ID,
    RowNum           UDT_CellRow,
    ColNum           UDT_CellColumn,
    Value            UDT_CellValue,
    Formula          UDT_CellFormula,  -- maybe manage this in a separate formula table for optimum checking with dependencies of the formula(to decide when to call calculate())
    Format           UDT_CellFormat,
    Style            UDT_CellStyle,
    CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_Number_Int_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus          UDT_RowStatus
    CONSTRAINT PK_CellOldVerID PRIMARY KEY NONCLUSTERED (ID),
    CONSTRAINT FK_Sheet_CellOldVer FOREIGN KEY (SheetID) REFERENCES Sheet(ID)
);


--------------------------------------------------------------------------------
-- Recreating tables

-- Clearing all table data(except Workbooks)
DELETE FROM dbo.Cell;
DELETE FROM dbo.CellOldVer;
DELETE FROM dbo.Sheet;
DELETE FROM dbo.XlTable;

-- Dropping all constraints
-- Usually not needed, only dropping the constraints for the tables being modified are often enough
ALTER TABLE [dbo].[Sheet] DROP CONSTRAINT [FK_Workbook_Sheet];
ALTER TABLE [dbo].[XlTable] DROP CONSTRAINT [FK_Sheet_XlTable];
ALTER TABLE [dbo].[Cell] DROP CONSTRAINT [FK_Sheet_Cell];
ALTER TABLE [dbo].[CellOldVer] DROP CONSTRAINT [FK_Sheet_CellOldVer];
ALTER TABLE [dbo].[CellOldVer] DROP CONSTRAINT [PK_CellOldVerID];
ALTER TABLE [dbo].[Cell] DROP CONSTRAINT [PK_CellID];
ALTER TABLE [dbo].[XlTable] DROP CONSTRAINT [PK_XlTableID];
ALTER TABLE [dbo].[Workbook] DROP CONSTRAINT [PK_WorkbookID];
ALTER TABLE [dbo].[Sheet] DROP CONSTRAINT [PK_SheetID];

--------------------------

DECLARE @sql NVARCHAR(MAX);
SET @sql = N'';

SELECT @sql = @sql + N'
  ALTER TABLE ' + QUOTENAME(s.name) + N'.'
  + QUOTENAME(t.name) + N' DROP CONSTRAINT '
  + QUOTENAME(c.name) + ';'
FROM sys.objects AS c
INNER JOIN sys.tables AS t
ON c.parent_object_id = t.[object_id]
INNER JOIN sys.schemas AS s 
ON t.[schema_id] = s.[schema_id]
WHERE c.[type] IN ('D','C','F','PK','UQ')
ORDER BY c.[type];

PRINT @sql;


