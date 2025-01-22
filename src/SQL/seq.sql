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

CREATE TYPE dbo.UDT_CellRow FROM int NOT NULL;
CREATE TYPE dbo.UDT_CellColumn FROM int NOT NULL;
CREATE TYPE dbo.UDT_CellValue FROM [nvarchar](2048) NOT NULL;
CREATE TYPE dbo.UDT_CellFormula FROM [nvarchar](1024) NOT NULL;
CREATE TYPE dbo.UDT_CellFormat FROM [nvarchar](128) NOT NULL;
CREATE TYPE dbo.UDT_CellStyle FROM [nvarchar](1024) NOT NULL;

CREATE TYPE dbo.UDT_Bool FROM bit NOT NULL;


-- Schema reqd later for app
CREATE SCHEMA rsa;


-- Creating the tables
DROP TABLE dbo.RAppsRoot;
CREATE TABLE dbo.RAppsRoot(   
    ID            UDT_ID,
    CompanyName   UDT_Name_Big,
	RootFolderID  UDT_ID,
    [CreatedBy]      [uniqueidentifier] NOT NULL,
	[CreatedDate]    UDT_DateTime,
	[LastUpdatedBy]  [uniqueidentifier] NULL,
	[LastUpdatedDate] UDT_DateTime_Opt,
	[RStatus] UDT_RowStatus,
	CONSTRAINT [PK_RAppsRoot] PRIMARY KEY (ID)
)



-- Once the Admin role & Admin user is created, no need to drop/re-create any more constraints
DROP TABLE [dbo].[VUsers];
CREATE TABLE [dbo].[VUsers](
	[ID] [uniqueidentifier] NOT NULL,
	[UserName] UDT_Name,
	[FirstName] UDT_Name,
	[LastName] UDT_Name_Opt,
	[FullName] UDT_Name_med,
	[Email] UDT_Name,
	[EmailConfirmed] [bit] NOT NULL,
	[EmailToken] UDT_Token_Opt,
	[Location] UDT_Name_med,
	[RoleID]    [uniqueidentifier] NOT NULL,
	[LastLoginDate]  UDT_DateTime_Opt,
	[CreatedBy]      [uniqueidentifier] NOT NULL,  -- self reference
	[CreatedDate]    UDT_DateTime,
	[LastUpdatedBy]  [uniqueidentifier] NULL,      -- self reference
	[LastUpdatedDate] UDT_DateTime_Opt,
	[RStatus] UDT_RowStatus,
	CONSTRAINT [PK_VUsers] PRIMARY KEY (ID)
)


DROP TABLE [dbo].[VRoles];
CREATE TABLE [dbo].[VRoles](
	[ID] [uniqueidentifier] NOT NULL,
	[Name]   UDT_Name,
	[Description]  UDT_Name,
	[CreatedBy]      [uniqueidentifier] NOT NULL,
	[CreatedDate]    UDT_DateTime,
	[LastUpdatedBy]  [uniqueidentifier] NULL,
	[LastUpdatedDate] UDT_DateTime_Opt,
	[RStatus] UDT_RowStatus,
	CONSTRAINT [PK_VRoles] PRIMARY KEY (ID)	
)


DROP TABLE [dbo].[VSystems];
CREATE TABLE dbo.VSystems(
    ID           UDT_ID IDENTITY(1,1),
    Name         UDT_Name_Big,
    AssignedTo    [uniqueidentifier] NULL,   -- not assigned/currently assigned to - VUserID
    [CreatedBy]      [uniqueidentifier] NOT NULL,
	[CreatedDate]    UDT_DateTime,
	[LastUpdatedBy]  [uniqueidentifier] NULL,
	[LastUpdatedDate] UDT_DateTime_Opt,
	[RStatus] UDT_RowStatus,
    CONSTRAINT PK_VSystems PRIMARY KEY (ID),	
)


DROP TABLE dbo.VApps;
CREATE TABLE dbo.VApps(
    ID            UDT_ID IDENTITY(1,1),
    Name          UDT_Name_Big,
    Owner         [uniqueidentifier] NOT NULL,  -- which VUser owns/administers the app
    Settings       UDT_Name_Big,
    [CreatedBy]      [uniqueidentifier] NOT NULL,
	[CreatedDate]    UDT_DateTime,
	[LastUpdatedBy]  [uniqueidentifier] NULL,
	[LastUpdatedDate] UDT_DateTime_Opt,
	[RStatus] UDT_RowStatus,
	CONSTRAINT PK_VApps PRIMARY KEY (ID)
)




DROP TABLE dbo.VFolders;
CREATE TABLE dbo.VFolders(
    ID            UDT_ID IDENTITY(1,1),
    Name          UDT_Name_Big,
	Attrs  		  UDT_Name,
	Link          UDT_ID_Opt,        -- if shortcut, the pointed to folder ID, self ref
    CreatedBy       [uniqueidentifier] NOT NULL,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    [uniqueidentifier] NULL,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus          UDT_RowStatus,
	CONSTRAINT PK_VFolders PRIMARY KEY (ID)
)



DROP TABLE dbo.VFiles;
CREATE TABLE dbo.VFiles(
    ID            UDT_ID IDENTITY(1,1),
    Name          UDT_Name_Big,
    FileTypeID    UDT_ID, -- to forward to proper webapp when opening or getting file info
    Attrs  		  UDT_Name,
	Link          UDT_ID_Opt,        -- if shortcut, the pointed to file ID, self ref
    CreatedBy       [uniqueidentifier] NOT NULL,
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    [uniqueidentifier] NULL,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus          UDT_RowStatus,
	CONSTRAINT PK_VFiles PRIMARY KEY (ID)
)



DROP TABLE dbo.FileTypes;
CREATE TABLE dbo.FileTypes(
    ID        UDT_ID IDENTITY(1,1),    
    Name      UDT_Name,     -- Various file types in the system
    CreatedBy       [uniqueidentifier] NOT NULL,    -- VUserID i.e. ID from the VUser table
    CreatedDate      UDT_DateTime,
    LastUpdatedBy    [uniqueidentifier] NULL,
    LastUpdatedDate  UDT_DateTime_Opt,
    RStatus          UDT_RowStatus,
	CONSTRAINT PK_FileTypes PRIMARY KEY (ID)	
)


-----------------------------------

-- RELATION join tables - dbo

DROP table dbo.SystemUsers;
CREATE TABLE dbo.SystemUsers(
    ID             UDT_ID IDENTITY(1,1),    
    VSystemID      UDT_ID,
    VUserID        [uniqueidentifier] NOT NULL,
    Profile        UDT_Name_Big, -- user settings on a specific system, maybe needed later, can be expanded to store settings as JSON later.
    CreatedBy        [uniqueidentifier] NOT NULL,
    CreatedDate       UDT_DateTime,
    LastUpdatedBy    [uniqueidentifier] NULL,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
	CONSTRAINT PK_SystemUsersID PRIMARY KEY (ID)    
)


DROP TABLE dbo.SystemUserApps;
CREATE TABLE dbo.SystemUserApps(
    ID               UDT_ID IDENTITY(1,1),    
    VSystemID        UDT_ID,
    VUserID          [uniqueidentifier] NOT NULL,
    VAppID           UDT_ID,
    Settings         UDT_Name_Big,
    CreatedBy        [uniqueidentifier] NOT NULL,
    CreatedDate       UDT_DateTime,
    LastUpdatedBy    [uniqueidentifier] NULL,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
	CONSTRAINT PK_SystemUserAppsID PRIMARY KEY (ID),
)


DROP TABLE dbo.SystemFolderFiles;
CREATE TABLE dbo.SystemFolderFiles(
    ID               UDT_ID IDENTITY(1,1),    
    VSystemID        UDT_ID,
    VFolderID        UDT_ID,
    VFileID          UDT_ID,
    CreatedBy        [uniqueidentifier] NOT NULL,
    CreatedDate       UDT_DateTime,
    LastUpdatedBy    [uniqueidentifier] NULL,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
	CONSTRAINT PK_SystemFolderFilesID PRIMARY KEY (ID)
)


DROP TABLE dbo.FileTypeApps;
CREATE TABLE dbo.FileTypeApps(
    ID                UDT_ID IDENTITY(1,1),    
    VFileTypeID       UDT_ID,
    VAppID            UDT_ID,
    CreatedBy        [uniqueidentifier] NOT NULL,
    CreatedDate       UDT_DateTime,
    LastUpdatedBy    [uniqueidentifier] NULL,
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
    CreatedBy        [uniqueidentifier] NOT NULL,
    CreatedDate       UDT_DateTime,
    LastUpdatedBy    [uniqueidentifier] NULL,
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
    CreatedBy        [uniqueidentifier] NOT NULL,
    CreatedDate       UDT_DateTime,
    LastUpdatedBy    [uniqueidentifier] NULL,
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
    CreatedBy        [uniqueidentifier] NOT NULL,
    CreatedDate       UDT_DateTime,
    LastUpdatedBy    [uniqueidentifier] NULL,
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
    CreatedBy        [uniqueidentifier] NOT NULL,
    CreatedDate       UDT_DateTime,
    LastUpdatedBy    [uniqueidentifier] NULL,
    LastUpdatedDate   UDT_DateTime_Opt,
    RStatus           UDT_RowStatus,
    CONSTRAINT PK_RSA_CellID PRIMARY KEY (SheetID, RowNum, ColNum)
);



-------------------------------------------------------------------
-- Now ADD SEED DATA into tables, then activate constraints
-- See seed.sql



-------------------------------------------------------------------


-- dbo, add all non-PK constraints
ALTER TABLE dbo.RAppsRoot ADD CONSTRAINT [FK_RAppsRoot_VFolders_RootFolderID] FOREIGN KEY (RootFolderID) REFERENCES dbo.VFolders(ID)
ALTER TABLE dbo.RAppsRoot ADD CONSTRAINT FK_RAppsRoot_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.RAppsRoot ADD CONSTRAINT FK_RAppsRoot_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE dbo.VUsers ADD CONSTRAINT [FK_VUsers_VRoles_RoleId] FOREIGN KEY (RoleID) REFERENCES dbo.VRoles(ID)
ALTER TABLE [dbo].VUsers ADD CONSTRAINT FK_VUsers_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE	[dbo].VUsers ADD CONSTRAINT FK_VUsers_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE [dbo].[VRoles] ADD CONSTRAINT FK_VRoles_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE	[dbo].[VRoles] ADD CONSTRAINT FK_VRoles_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE dbo.VSystems ADD CONSTRAINT FK_VSystems_VUsers_AssignedTo FOREIGN KEY (AssignedTo) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.VSystems ADD CONSTRAINT FK_VSystems_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.VSystems ADD CONSTRAINT FK_VSystems_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE dbo.VApps ADD CONSTRAINT FK_VApps_VUsers_Owner FOREIGN KEY (Owner) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.VApps ADD CONSTRAINT FK_VApps_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.VApps ADD CONSTRAINT FK_VApps_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE dbo.VFolders ADD CONSTRAINT FK_VFolders_VFolders_Link FOREIGN KEY (Link) REFERENCES dbo.VFolders(ID)
ALTER TABLE dbo.VFolders ADD CONSTRAINT FK_VFolders_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.VFolders ADD CONSTRAINT FK_VFolders_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE dbo.VFiles ADD CONSTRAINT FK_VFiles_VFiles_Link FOREIGN KEY (Link) REFERENCES dbo.VFiles(ID)
ALTER TABLE dbo.VFiles ADD CONSTRAINT FK_VFiles_FileTypes_FileTypeID FOREIGN KEY (FileTypeID) REFERENCES dbo.FileTypes(ID) 
ALTER TABLE dbo.VFiles ADD CONSTRAINT FK_VFiles_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.VFiles ADD CONSTRAINT FK_VFiles_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE dbo.FileTypes ADD CONSTRAINT FK_FileTypes_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.FileTypes ADD CONSTRAINT FK_FileTypes_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

-- dbo join tables constraints
ALTER TABLE dbo.SystemUsers ADD CONSTRAINT FK_SystemUsers_VSystems_VSystemID FOREIGN KEY (VSystemID) REFERENCES dbo.VSystems(ID)
ALTER TABLE dbo.SystemUsers ADD CONSTRAINT FK_SystemUsers_VUsers_VUserID FOREIGN KEY (VUserID) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.SystemUsers ADD CONSTRAINT FK_SystemUsers_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.SystemUsers ADD CONSTRAINT FK_SystemUsers_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE dbo.SystemUserApps ADD CONSTRAINT FK_SystemUserApps_VSystems_VSystemID FOREIGN KEY (VSystemID) REFERENCES dbo.VSystems(ID)
ALTER TABLE dbo.SystemUserApps ADD CONSTRAINT FK_SystemUserApps_VUsers_VUserID FOREIGN KEY (VUserID) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.SystemUserApps ADD CONSTRAINT FK_SystemUserApps_VApps_VAppID FOREIGN KEY (VAppID) REFERENCES dbo.VApps(ID)
ALTER TABLE dbo.SystemUserApps ADD CONSTRAINT FK_SystemUserApps_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.SystemUserApps ADD CONSTRAINT FK_SystemUserApps_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE dbo.SystemFolderFiles ADD CONSTRAINT FK_SystemFolderFiles_VSystems_VSystemID FOREIGN KEY (VSystemID) REFERENCES dbo.VSystems(ID)
ALTER TABLE dbo.SystemFolderFiles ADD CONSTRAINT FK_SystemFolderFiles_VFolders_VFolderID FOREIGN KEY (VFolderID) REFERENCES dbo.VFolders(ID)
ALTER TABLE dbo.SystemFolderFiles ADD CONSTRAINT FK_SystemFolderFiles_VFiles_VFileID FOREIGN KEY (VFileID) REFERENCES dbo.VFiles(ID)
ALTER TABLE dbo.SystemFolderFiles ADD CONSTRAINT FK_SystemFolderFiles_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.SystemFolderFiles ADD CONSTRAINT FK_SystemFolderFiles_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE dbo.FileTypeApps ADD CONSTRAINT FK_FileTypeApps_FileTypes_VFileTypeID FOREIGN KEY (VFileTypeID) REFERENCES dbo.FileTypes(ID)
ALTER TABLE dbo.FileTypeApps ADD CONSTRAINT FK_FileTypeApps_VApps_VAppID FOREIGN KEY (VAppID) REFERENCES dbo.VApps(ID)
ALTER TABLE dbo.FileTypeApps ADD CONSTRAINT FK_FileTypeApps_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.FileTypeApps ADD CONSTRAINT FK_FileTypeApps_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)



-----------------------------------------------------------
-- rsa, add constraints
ALTER TABLE rsa.Workbooks ADD CONSTRAINT FK_RSA_Workbooks_VFiles_VFileID FOREIGN KEY (VFileID) REFERENCES dbo.VFiles(ID)
ALTER TABLE rsa.Workbooks ADD CONSTRAINT FK_RSA_Workbooks_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE rsa.Workbooks ADD CONSTRAINT FK_RSA_Workbooks_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE rsa.Sheets ADD CONSTRAINT FK_RSA_Sheets_Workbooks_WorkbookID FOREIGN KEY (WorkbookID) REFERENCES rsa.Workbooks(ID)
ALTER TABLE rsa.Sheets ADD CONSTRAINT FK_RSA_Sheets_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE rsa.Sheets ADD CONSTRAINT FK_RSA_Sheets_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE rsa.XlTables ADD CONSTRAINT FK_RSA_XlTables_Sheets_SheetID FOREIGN KEY (SheetID) REFERENCES rsa.Sheets(ID)
ALTER TABLE rsa.XlTables ADD CONSTRAINT FK_RSA_XlTables_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE rsa.XlTables ADD CONSTRAINT FK_RSA_XlTables_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE rsa.Cells ADD CONSTRAINT FK_RSA_Cells_Sheets_SheetID FOREIGN KEY (SheetID) REFERENCES rsa.Sheets(ID)
ALTER TABLE rsa.Cells ADD CONSTRAINT FK_RSA_Cells_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE rsa.Cells ADD CONSTRAINT FK_RSA_Cells_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)





-----------------------------------------------------------------------------------------------------

-- Drop all constraints
-- dbo
ALTER TABLE [dbo].[RAppsRoot] DROP CONSTRAINT [FK_RAppsRoot_VFolders_RootFolderID];
ALTER TABLE [dbo].[RAppsRoot] DROP CONSTRAINT [FK_RAppsRoot_VUsers_CreatedBy];
ALTER TABLE [dbo].[RAppsRoot] DROP CONSTRAINT [FK_RAppsRoot_VUsers_LastUpdatedBy];
ALTER TABLE [dbo].[VUsers] DROP CONSTRAINT [FK_VUsers_VRoles_RoleId];
ALTER TABLE [dbo].[VUsers] DROP CONSTRAINT [FK_VUsers_VUsers_CreatedBy];
ALTER TABLE [dbo].[VUsers] DROP CONSTRAINT [FK_VUsers_VUsers_LastUpdatedBy];
ALTER TABLE [dbo].[VRoles] DROP CONSTRAINT [FK_VRoles_VUsers_CreatedBy];
ALTER TABLE [dbo].[VRoles] DROP CONSTRAINT [FK_VRoles_VUsers_LastUpdatedBy];
ALTER TABLE [dbo].[VSystems] DROP CONSTRAINT [FK_VSystems_VUsers_AssignedTo];
ALTER TABLE [dbo].[VSystems] DROP CONSTRAINT [FK_VSystems_VUsers_CreatedBy];
ALTER TABLE [dbo].[VSystems] DROP CONSTRAINT [FK_VSystems_VUsers_LastUpdatedBy];
ALTER TABLE [dbo].[VFiles] DROP CONSTRAINT [FK_VFiles_FileTypes_FileTypeID];
ALTER TABLE [dbo].[VFiles] DROP CONSTRAINT [FK_VFiles_VUsers_CreatedBy];
ALTER TABLE [dbo].[VFiles] DROP CONSTRAINT [FK_VFiles_VUsers_LastUpdatedBy];
ALTER TABLE [dbo].[VFiles] DROP CONSTRAINT [FK_VFiles_VFiles_Link];
ALTER TABLE [dbo].[VFolders] DROP CONSTRAINT [FK_VFolders_VFolders_Link];
ALTER TABLE [dbo].[VFolders] DROP CONSTRAINT [FK_VFolders_VUsers_CreatedBy];
ALTER TABLE [dbo].[VFolders] DROP CONSTRAINT [FK_VFolders_VUsers_LastUpdatedBy];
ALTER TABLE [dbo].[FileTypes] DROP CONSTRAINT [FK_FileTypes_VUsers_CreatedBy];
ALTER TABLE [dbo].[FileTypes] DROP CONSTRAINT [FK_FileTypes_VUsers_LastUpdatedBy];
ALTER TABLE [dbo].[VApps] DROP CONSTRAINT [FK_VApps_VUsers_Owner];
ALTER TABLE [dbo].[VApps] DROP CONSTRAINT [FK_VApps_VUsers_CreatedBy];
ALTER TABLE [dbo].[VApps] DROP CONSTRAINT [FK_VApps_VUsers_LastUpdatedBy];

ALTER TABLE [dbo].[SystemUsers] DROP CONSTRAINT [FK_SystemUsers_VSystems_VSystemID];
ALTER TABLE [dbo].[SystemUsers] DROP CONSTRAINT [FK_SystemUsers_VUsers_VUserID];
ALTER TABLE [dbo].[SystemUsers] DROP CONSTRAINT [FK_SystemUsers_VUsers_CreatedBy];
ALTER TABLE [dbo].[SystemUsers] DROP CONSTRAINT [FK_SystemUsers_VUsers_LastUpdatedBy];

ALTER TABLE [dbo].[SystemUserApps] DROP CONSTRAINT [FK_SystemUserApps_VSystems_VSystemID];
ALTER TABLE [dbo].[SystemUserApps] DROP CONSTRAINT [FK_SystemUserApps_VUsers_VUserID];
ALTER TABLE [dbo].[SystemUserApps] DROP CONSTRAINT [FK_SystemUserApps_VApps_VAppID];
ALTER TABLE [dbo].[SystemUserApps] DROP CONSTRAINT [FK_SystemUserApps_VUsers_CreatedBy];
ALTER TABLE [dbo].[SystemUserApps] DROP CONSTRAINT [FK_SystemUserApps_VUsers_LastUpdatedBy];

ALTER TABLE [dbo].[FileTypeApps] DROP CONSTRAINT [FK_FileTypeApps_FileTypes_VFileTypeID];
ALTER TABLE [dbo].[FileTypeApps] DROP CONSTRAINT [FK_FileTypeApps_VApps_VAppID];
ALTER TABLE [dbo].[FileTypeApps] DROP CONSTRAINT [FK_FileTypeApps_VUsers_CreatedBy];
ALTER TABLE [dbo].[FileTypeApps] DROP CONSTRAINT [FK_FileTypeApps_VUsers_LastUpdatedBy];

ALTER TABLE [dbo].[SystemFolderFiles] DROP CONSTRAINT [FK_SystemFolderFiles_VSystems_VSystemID];
ALTER TABLE [dbo].[SystemFolderFiles] DROP CONSTRAINT [FK_SystemFolderFiles_VFolders_VFolderID];
ALTER TABLE [dbo].[SystemFolderFiles] DROP CONSTRAINT [FK_SystemFolderFiles_VFiles_VFileID];
ALTER TABLE [dbo].[SystemFolderFiles] DROP CONSTRAINT [FK_SystemFolderFiles_VUsers_CreatedBy];
ALTER TABLE [dbo].[SystemFolderFiles] DROP CONSTRAINT [FK_SystemFolderFiles_VUsers_LastUpdatedBy];


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


---------------------------------------------------------------------------------------------
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


