----------------------------------------------------------
-- 22nd Jan
-- Creating all tables except shared & versions




------------------------------------
-- User defined types

CREATE TYPE dbo.UDT_ID FROM int NOT NULL;
CREATE TYPE dbo.UDT_ID_Opt FROM int NULL;

CREATE TYPE dbo.UDT_DateTime FROM datetime NOT NULL;
CREATE TYPE dbo.UDT_DateTime_Opt FROM datetime NULL;
CREATE TYPE dbo.UDT_Token FROM [nvarchar](40) NOT NULL;
CREATE TYPE dbo.UDT_Token_Opt FROM [nvarchar](40) NULL;
CREATE TYPE dbo.UDT_Name FROM [nvarchar](128) NOT NULL;
CREATE TYPE dbo.UDT_Name_Opt FROM [nvarchar](128) NULL;
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
CREATE TYPE dbo.UDT_ObjectType FROM smallint NOT NULL; 

CREATE TYPE dbo.UDT_CellRow FROM int NOT NULL;
CREATE TYPE dbo.UDT_CellColumn FROM int NOT NULL;
CREATE TYPE dbo.UDT_CellValue FROM [nvarchar](2048) NOT NULL;
CREATE TYPE dbo.UDT_CellFormula FROM [nvarchar](1024) NOT NULL;
CREATE TYPE dbo.UDT_CellFormat FROM [nvarchar](128) NOT NULL;
CREATE TYPE dbo.UDT_CellStyle FROM [nvarchar](1024) NOT NULL;

CREATE TYPE dbo.UDT_Bool FROM bit NOT NULL;

CREATE TYPE dbo.UDT_Path FROM [nvarchar](max) NOT NULL;
CREATE TYPE dbo.UDT_LogDescription FROM [nvarchar](max) NOT NULL;
CREATE TYPE dbo.UDT_Description FROM [nvarchar](max) NOT NULL;


-- Schema reqd later for app
CREATE SCHEMA rsa;


-- Creating the tables
DROP TABLE dbo.RAppsRoot;
CREATE TABLE dbo.RAppsRoot(   
    ID            UDT_ID,
    CompanyName   UDT_Name_Big,   -- TODO: make UNIQUE
	RootFolderID  UDT_ID,          -- entire file system's root
    [CreatedBy]      UDT_ID,
	[CreatedDate]    UDT_DateTime,
	[LastUpdatedBy]  UDT_ID_Opt,
	[LastUpdatedDate] UDT_DateTime_Opt,
	[RStatus] UDT_RowStatus,
	CONSTRAINT [PK_RAppsRoot] PRIMARY KEY (ID)
)



-- Once the Admin role & Admin user is created, no need to drop/re-create any more constraints
DROP TABLE [dbo].[VUsers];
CREATE TABLE [dbo].[VUsers](
	[ID] UDT_ID,
	[UserName] UDT_Name,   -- TODO: make UNIQUE
	[FirstName] UDT_Name,
	[LastName] UDT_Name_Opt,
	[FullName] UDT_Name_med,
	[Email] UDT_Name,      -- TODO: make UNIQUE
	[EmailConfirmed] [bit] NOT NULL,
	[EmailToken] UDT_Token_Opt,
	[Location] UDT_Name_med,
	[RoleID]    [uniqueidentifier] NOT NULL,
	[LastLoginDate]  UDT_DateTime_Opt,
	[CreatedBy]      UDT_ID,  -- self reference
	[CreatedDate]    UDT_DateTime,
	[LastUpdatedBy]  UDT_ID_Opt,      -- self reference
	[LastUpdatedDate] UDT_DateTime_Opt,
	[RStatus] UDT_RowStatus,
	CONSTRAINT [PK_VUsers] PRIMARY KEY (ID)
)


DROP TABLE [dbo].[VRoles];
CREATE TABLE [dbo].[VRoles](
	[ID] [uniqueidentifier] NOT NULL,
	[Name]   UDT_Name,
	[Description]  UDT_Name,
	[CreatedBy]      UDT_ID,
	[CreatedDate]    UDT_DateTime,
	[LastUpdatedBy]  UDT_ID_Opt,
	[LastUpdatedDate] UDT_DateTime_Opt,
	[RStatus] UDT_RowStatus,
	CONSTRAINT [PK_VRoles] PRIMARY KEY (ID)	
)


DROP TABLE [dbo].[VSystems];
CREATE TABLE dbo.VSystems(
    ID           UDT_ID IDENTITY(1,1),
    Name         UDT_Name_Big,  -- TODO: make UNIQUE, non-clustered index to ensure unique name
    AssignedTo    UDT_ID_Opt,   -- not assigned(NULL)/currently assigned to(VUserID)
	RootFolderID  UDT_ID,       -- TODO: must be UNIQUE, get the path by lookup in VFolders table
	[CreatedBy]      UDT_ID,
	[CreatedDate]    UDT_DateTime,
	[LastUpdatedBy]  UDT_ID_Opt,
	[LastUpdatedDate] UDT_DateTime_Opt,
	[RStatus] UDT_RowStatus,
    CONSTRAINT PK_VSystems PRIMARY KEY (ID),	
)


DROP TABLE dbo.VFolders;
CREATE TABLE dbo.VFolders(
    ID            UDT_ID IDENTITY(1,1),
    Name          UDT_Name_Big,   -- TODO: only one named 'root'
	Attrs  		  UDT_Name,    -- unix style 'xxx', so 'rw' means read & write allowed for all users
	Path          UDT_Path,      -- TODO: make UNIQUE, unix style path(TODO: INSERT/UPDATE TRIGGER to check the path components & whether they are in fact in the mentioned hierarchy)
	PathIDs       UDT_Path,      -- UNUSED for now, comma separated list of ancestor FolderIDs, oldest first(TODO: TRIGGERS to check the IDs)
    CreatedBy       UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus          UDT_RowStatus,
	CONSTRAINT PK_VFolders PRIMARY KEY (ID)
)


DROP TABLE dbo.VFiles;
CREATE TABLE dbo.VFiles(
    ID            UDT_ID IDENTITY(1,1),
    Name          UDT_Name_Big,     -- TODO: cant be 'root'
    FileTypeID    UDT_ID, -- to forward to proper webapp when opening or getting file info
    Attrs  		  UDT_Name,
    CreatedBy       UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus          UDT_RowStatus,
	CONSTRAINT PK_VFiles PRIMARY KEY (ID)
)


DROP TABLE dbo.VApps;
CREATE TABLE dbo.VApps(
    ID            UDT_ID IDENTITY(1,1),
    Name          UDT_Name_Big,
	Description  UDT_Name_big,
    Owner         UDT_ID,  -- which VUser owns/administers the app
    Settings       UDT_Name_Big,
    [CreatedBy]      UDT_ID,
	[CreatedDate]    UDT_DateTime,
	[LastUpdatedBy]  UDT_ID_Opt,
	[LastUpdatedDate] UDT_DateTime_Opt,
	[RStatus] UDT_RowStatus,
	CONSTRAINT PK_VApps PRIMARY KEY (ID)
)


DROP TABLE dbo.FileTypes;
CREATE TABLE dbo.FileTypes(
    ID        UDT_ID IDENTITY(1,1),    
    Name      UDT_Name,     -- Various file types in the system
	Description      UDT_Name_big,
    CreatedBy       UDT_ID,    -- VUserID i.e. ID from the VUser table
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus          UDT_RowStatus,
	CONSTRAINT PK_FileTypes PRIMARY KEY (ID)	
)


DROP TABLE dbo.AuthLogs;
CREATE TABLE dbo.AuthLogs(    
	ID            UDT_ID IDENTITY(1,1),    
	Module        UDT_Name,     -- Various file types in the system
    ErrorMsg      UDT_Name_Med,     -- Various file types in the system
	Description   UDT_LogDescription,     -- Various file types in the system
    CreatedBy       UDT_ID,    -- VUserID i.e. ID from the VUser table
    CreatedDate      UDT_DateTime,
	CONSTRAINT PK_AuthLogs PRIMARY KEY (ID)	
)



DROP TABLE dbo.SysLogs;
CREATE TABLE dbo.SysLogs(    
	ID            UDT_ID IDENTITY(1,1),    
	Module        UDT_Name,     -- Various file types in the system
    ErrorMsg      UDT_Name_Med,     -- Various file types in the system
	Description   UDT_LogDescription,     -- Various file types in the system
    CreatedBy       UDT_ID,    -- VUserID i.e. ID from the VUser table
    CreatedDate      UDT_DateTime,
	CONSTRAINT PK_SysLogs PRIMARY KEY (ID)	
)



-----------------------------------

-- RELATION join tables - dbo

-- User profiles in a system
DROP table dbo.SystemUsers;
CREATE TABLE dbo.SystemUsers(
    ID             UDT_ID IDENTITY(1,1),    
    VSystemID      UDT_ID,
    VUserID        UDT_ID,
    Profile        UDT_Name_Big, -- user settings on a specific system, maybe needed later, can be expanded to store settings as JSON later or more columns in this table
    CreatedBy        UDT_ID,
    CreatedDate       UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
	CONSTRAINT PK_SystemUsersID PRIMARY KEY (ID)    
)


-- Not needed currently, all apps available for all users in all systems
--DROP TABLE dbo.SystemUserApps;
--CREATE TABLE dbo.SystemUserApps(
--    ID               UDT_ID IDENTITY(1,1),    
--    VSystemID        UDT_ID,
--    VUserID          UDT_ID,
--    VAppID           UDT_ID,
--    Settings         UDT_Name_Big,
--    CreatedBy        UDT_ID,
--    CreatedDate       UDT_DateTime,
--    LastUpdatedBy    UDT_ID_Opt,
--    LastUpdatedDate   UDT_DateTime_Opt,
--    RStatus           UDT_RowStatus,
--	CONSTRAINT PK_SystemUserAppsID PRIMARY KEY (ID),
--)


DROP TABLE dbo.SystemFolderFiles;
CREATE TABLE dbo.SystemFolderFiles(
    ID               UDT_ID IDENTITY(1,1),    
    VSystemID        UDT_ID,
	VFileID          UDT_ID_Opt,    
	VFolderID        UDT_ID_Opt,    
	Link             UDT_Bool,   -- if false, VFileID/VFolderID is a child, else its a link to the file/folder
	VParentFolderID  UDT_ID,
	CheckedOutBy     UDT_ID_Opt,   -- NULL means not checked out
    CreatedBy        UDT_ID,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus          UDT_RowStatus,
	CONSTRAINT PK_SystemFolderFilesID PRIMARY KEY (ID)
)



-- Not all files will be created via upload so this is not in VFiles. 
-- Those which are will have their original file upload path saved here.
-- No need of VSystemID here as VFileID is unique across all systems.
DROP TABLE dbo.FileUploads;
CREATE TABLE dbo.FileUploads(
	ID            UDT_ID IDENTITY(1,1),
	VFileID       UDT_ID,
	VAppID        UDT_ID,    -- user selected app ID to indicate which app the file is meant for
	FileName      UDT_Name_med,
	Description   UDT_Description,
	FilePath      UDT_Path,
	CreatedBy        UDT_ID,
    CreatedDate       UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
	CONSTRAINT PK_FileUploadsID PRIMARY KEY (ID),
)


-- All Apps available for all users in all systems, for now
-- This app with file type association is global.
DROP TABLE dbo.FileTypeApps;
CREATE TABLE dbo.FileTypeApps(
    ID                UDT_ID IDENTITY(1,1),    
    VFileTypeID       UDT_ID,
    VAppID            UDT_ID,
    CreatedBy        UDT_ID,
    CreatedDate       UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
	CONSTRAINT PK_FileTypeAppsID PRIMARY KEY (ID),
)


---------------------------------------------

DROP TABLE rsa.Workbooks;
CREATE TABLE rsa.Workbooks(
    ID                UDT_ID IDENTITY(1,1),
    VFileID           UDT_ID,    
    Name              UDT_Name,  -- same as VFile.Name
    LastOpenedSheet   UDT_Sequence,
    LastFocusCellRow  UDT_CellRow,
    LastFocusCellCol  UDT_CellColumn,
    Settings          UDT_Settings_Opt,   -- Other settings go here in JSON string format - can be columnized if necessary later for searching/indexing faster
    CreatedBy        UDT_ID,
    CreatedDate       UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_RSA_WorkbooksID PRIMARY KEY (ID)
);


DROP TABLE rsa.Sheets;
CREATE TABLE rsa.Sheets(
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
    CreatedDate       UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_RSA_SheetID PRIMARY KEY (ID),
);


DROP TABLE rsa.XlTables;
CREATE TABLE rsa.XlTables(
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
    CreatedDate       UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_RSA_XlTableID PRIMARY KEY (ID),
);


DROP TABLE rsa.Cells;
CREATE TABLE rsa.Cells(
    SheetID          UDT_ID,
    RowNum           UDT_CellRow,
    ColNum           UDT_CellColumn,
    Value            UDT_CellValue,
    Formula          UDT_CellFormula,  -- maybe manage this in a separate formula table for optimum checking with dependencies of the formula(to decide when to call calculate())
    Format           UDT_CellFormat,
    Style            UDT_CellStyle,
    CreatedBy        UDT_ID,
    CreatedDate       UDT_DateTime,
    LastUpdatedBy    UDT_ID_Opt,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_RSA_CellID PRIMARY KEY (SheetID, RowNum, ColNum)
);



-------------------------------------------------------------------
-- Now ADD SEED DATA into tables, then activate constraints
-- See seed.sql

-------------------------------------------------------------------
-- dbo, add all non-PK constraints
-- see dboConstraints.sql

-----------------------------------------------------------
-- rsa, add constraints
-- see rsaConstraints.sql

-----------------------------------------------------------------------------------------------------
-- Drop all constraints
-- Query to generate drop constraints statements for all constraints(including PK ones)

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



-- dbo
  ALTER TABLE [dbo].[VSystems] DROP CONSTRAINT [FK_VSystems_VFolders_RootFolderID];
  ALTER TABLE [dbo].[VSystems] DROP CONSTRAINT [FK_VSystems_VUsers_AssignedTo];
  ALTER TABLE [dbo].[VSystems] DROP CONSTRAINT [FK_VSystems_VUsers_CreatedBy];
  ALTER TABLE [dbo].[VSystems] DROP CONSTRAINT [FK_VSystems_VUsers_LastUpdatedBy];
  ALTER TABLE [dbo].[VApps] DROP CONSTRAINT [FK_VApps_VUsers_Owner];
  ALTER TABLE [dbo].[VApps] DROP CONSTRAINT [FK_VApps_VUsers_CreatedBy];
  ALTER TABLE [dbo].[VApps] DROP CONSTRAINT [FK_VApps_VUsers_LastUpdatedBy];
  ALTER TABLE [dbo].[VFiles] DROP CONSTRAINT [FK_VFiles_FileTypes_FileTypeID];
  ALTER TABLE [dbo].[VFiles] DROP CONSTRAINT [FK_VFiles_VUsers_CreatedBy];
  ALTER TABLE [dbo].[VFiles] DROP CONSTRAINT [FK_VFiles_VUsers_LastUpdatedBy];
  ALTER TABLE [dbo].[FileTypes] DROP CONSTRAINT [FK_FileTypes_VUsers_CreatedBy];
  ALTER TABLE [dbo].[FileTypes] DROP CONSTRAINT [FK_FileTypes_VUsers_LastUpdatedBy];
  ALTER TABLE [dbo].[SystemUsers] DROP CONSTRAINT [FK_SystemUsers_VSystems_VSystemID];
  ALTER TABLE [dbo].[SystemUsers] DROP CONSTRAINT [FK_SystemUsers_VUsers_VUserID];
  ALTER TABLE [dbo].[SystemUsers] DROP CONSTRAINT [FK_SystemUsers_VUsers_CreatedBy];
  ALTER TABLE [dbo].[SystemUsers] DROP CONSTRAINT [FK_SystemUsers_VUsers_LastUpdatedBy];
  ALTER TABLE [dbo].[SystemUserApps] DROP CONSTRAINT [FK_SystemUserApps_VSystems_VSystemID];
  ALTER TABLE [dbo].[SystemUserApps] DROP CONSTRAINT [FK_SystemUserApps_VUsers_VUserID];
  ALTER TABLE [dbo].[SystemUserApps] DROP CONSTRAINT [FK_SystemUserApps_VApps_VAppID];
  ALTER TABLE [dbo].[SystemUserApps] DROP CONSTRAINT [FK_SystemUserApps_VUsers_CreatedBy];
  ALTER TABLE [dbo].[SystemUserApps] DROP CONSTRAINT [FK_SystemUserApps_VUsers_LastUpdatedBy];
  ALTER TABLE [dbo].[SystemFolderFiles] DROP CONSTRAINT [FK_SystemFolderFiles_VUsers_CheckedOutBy];
  ALTER TABLE [dbo].[SystemFolderFiles] DROP CONSTRAINT [FK_SystemFolderFiles_VUsers_CreatedBy];
  ALTER TABLE [dbo].[SystemFolderFiles] DROP CONSTRAINT [FK_SystemFolderFiles_VUsers_LastUpdatedBy];
  ALTER TABLE [dbo].[FileTypeApps] DROP CONSTRAINT [FK_FileTypeApps_FileTypes_VFileTypeID];
  ALTER TABLE [dbo].[FileTypeApps] DROP CONSTRAINT [FK_FileTypeApps_VApps_VAppID];
  ALTER TABLE [dbo].[FileTypeApps] DROP CONSTRAINT [FK_FileTypeApps_VUsers_CreatedBy];
  ALTER TABLE [dbo].[FileTypeApps] DROP CONSTRAINT [FK_FileTypeApps_VUsers_LastUpdatedBy];


-- rsa
  ALTER TABLE [rsa].[Workbooks] DROP CONSTRAINT [FK_RSA_Workbooks_VFiles_VFileID];
  ALTER TABLE [rsa].[Workbooks] DROP CONSTRAINT [FK_RSA_Workbooks_VUsers_CreatedBy];
  ALTER TABLE [rsa].[Workbooks] DROP CONSTRAINT [FK_RSA_Workbooks_VUsers_LastUpdatedBy];
  ALTER TABLE [rsa].[Sheets] DROP CONSTRAINT [FK_RSA_Sheets_Workbooks_WorkbookID];
  ALTER TABLE [rsa].[Sheets] DROP CONSTRAINT [FK_RSA_Sheets_VUsers_CreatedBy];
  ALTER TABLE [rsa].[Sheets] DROP CONSTRAINT [FK_RSA_Sheets_VUsers_LastUpdatedBy];
  ALTER TABLE [rsa].[XlTables] DROP CONSTRAINT [FK_RSA_XlTables_Sheets_SheetID];
  ALTER TABLE [rsa].[XlTables] DROP CONSTRAINT [FK_RSA_XlTables_VUsers_CreatedBy];
  ALTER TABLE [rsa].[XlTables] DROP CONSTRAINT [FK_RSA_XlTables_VUsers_LastUpdatedBy];
  ALTER TABLE [rsa].[Cells] DROP CONSTRAINT [FK_RSA_Cells_Sheets_SheetID];
  ALTER TABLE [rsa].[Cells] DROP CONSTRAINT [FK_RSA_Cells_VUsers_CreatedBy];
  ALTER TABLE [rsa].[Cells] DROP CONSTRAINT [FK_RSA_Cells_VUsers_LastUpdatedBy];
