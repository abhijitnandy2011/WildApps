-- Seed data

--SELECT NEWID()

---------------------------------------------------------------------------
-- ROLES 
-- drop constraints on this table first, restore later
INSERT INTO dbo.VRoles VALUES ('3A798A4B-6FF5-4570-B77B-D3FEF8FF1609', 'Unassigned', 'Unassigned', dbo.CONST('VUSERS_SYSTEM'), GETDATE(), NULL, NULL, dbo.CONST('RSTATUS_ACTIVE'))
INSERT INTO dbo.VRoles VALUES ('BD08482C-5CFF-4BDB-874C-0B25EA448E1B', 'Admin', 'Admin', dbo.CONST('VUSERS_SYSTEM'), GETDATE(), NULL, NULL, dbo.CONST('RSTATUS_ACTIVE'))
INSERT INTO dbo.VRoles VALUES ('D5CD880A-0D67-4356-B316-8610E6E80780', 'Visitor', 'Visitor',  dbo.CONST('VUSERS_SYSTEM'), GETDATE(), NULL, NULL, dbo.CONST('RSTATUS_ACTIVE'))



--------------------------------------------------------------------------
-- USERS
-- System, 1 to 100 all system related users
-- Admin - first real user
INSERT INTO dbo.VUsers VALUES (
dbo.CONST('VUSERS_SYSTEM'),
'System',
'System',
NULL,
'System',
'system@rapps.com',
1,
NULL,
'SYS',
'BD08482C-5CFF-4BDB-874C-0B25EA448E1B',
GETDATE(),
dbo.CONST('VUSERS_SYSTEM'),
GETDATE(),
NULL, 
NULL,
dbo.CONST('RSTATUS_ACTIVE')
)


-- Admin - first real user
INSERT INTO dbo.VUsers VALUES (
dbo.CONST('VUSERS_ADMIN'),
'Admin',
'Admin',
NULL,
'Admin',
'admin@rapps.com',
1,
NULL,
'BLR',
'BD08482C-5CFF-4BDB-874C-0B25EA448E1B',
GETDATE(),
dbo.CONST('VUSERS_SYSTEM'),
GETDATE(),
NULL, 
NULL,
dbo.CONST('RSTATUS_ACTIVE')
)

-- Test user, visitor role
INSERT INTO dbo.VUsers VALUES (
101,
'vis',
'Visitor',
NULL,
'Visitor',
'visitor@rapps.com',
1,
NULL,
'BLR',
'D5CD880A-0D67-4356-B316-8610E6E80780',
GETDATE(),
dbo.CONST('VUSERS_SYSTEM'),
GETDATE(),
NULL, 
NULL,
dbo.CONST('RSTATUS_ACTIVE')
)



------------------------------------------------------------------------
-- FOLDERS

-- Create root folder of RApp
INSERT INTO dbo.VFolders
(
Name,       
Attrs,
Path,
PathIDs,
CreatedBy,   
CreatedDate,
RStatus
)
VALUES (
'root',
'r',
'/',
'',
dbo.CONST('VUSERS_SYSTEM'),
GETDATE(),
dbo.CONST('RSTATUS_ACTIVE')
)


-- Create root system for System & Admin users to login to
DECLARE @RAppsFolderRootID int = (SELECT ID FROM dbo.VFolders WHERE name = 'root')
print(@RAppsFolderRootID)
INSERT INTO dbo.VSystems
(
Name,       
AssignedTo,
RootFolderID,
CreatedBy,   
CreatedDate,
RStatus
)
VALUES (
'rootsystem',
dbo.CONST('VUSERS_ADMIN'),
@RAppsFolderRootID,
dbo.CONST('VUSERS_SYSTEM'),
GETDATE(),
dbo.CONST('RSTATUS_ACTIVE')
)



-- RAPPS
-- RAppsRoot entry
DECLARE @RAppRootFolderID int = (SELECT ID FROM dbo.VFolders WHERE name = 'root')
print( @RAppRootFolderID)

INSERT INTO dbo.RAppsRoot
(
ID,
CompanyName,       
RootFolderID,
CreatedBy,   
CreatedDate,
RStatus
)
VALUES (
1,
'RApps',
@RAppRootFolderID,       -- fetched from VFolders, its created first
dbo.CONST('VUSERS_SYSTEM'),
GETDATE(),
dbo.CONST('RSTATUS_ACTIVE')
)


-- Create '/systems'
INSERT INTO dbo.VFolders
(
Name,       
Attrs,
Path,
PathIDs,
CreatedBy,   
CreatedDate,
RStatus
)
VALUES (
'systems',
'r',
'/systems',
'1',
dbo.CONST('VUSERS_SYSTEM'),
GETDATE(),
dbo.CONST('RSTATUS_ACTIVE')
)


-- Map '/systems' to root folder '/' & root system
INSERT INTO dbo.SystemFolderFiles
(
VSystemID,
VFileID, 
VFolderID,
Link,           
VParentFolderID,
CreatedBy,   
CreatedDate,
RStatus
)
VALUES (
1,    -- VSystemID     
NULL,	 -- VFileID
2,	  -- VFolderID      
0,	 -- Link           
1,	 -- VParentFolderID
dbo.CONST('VUSERS_SYSTEM'),
GETDATE(),
dbo.CONST('RSTATUS_ACTIVE')
)

--------------------------------------------------------------------------------
-- USER SYSTEMS
-- Create system 's1'
-- First create folder root for s1
-- Create '/systems/s1'
INSERT INTO dbo.VFolders
(
Name,       
Attrs,
Path,
PathIDs,
CreatedBy,   
CreatedDate,
RStatus
)
VALUES (
's1',
'r',
'/systems/s1',
'1,2',
dbo.CONST('VUSERS_SYSTEM'),
GETDATE(),
dbo.CONST('RSTATUS_ACTIVE')
)


-- Now create system 's1' with root folder '/systems/s1'
DECLARE @s1FolderRootID int = (SELECT ID FROM dbo.VFolders WHERE path = '/systems/s1')
print( @s1FolderRootID)
INSERT INTO dbo.VSystems
(
Name,       
AssignedTo,
RootFolderID,
CreatedBy,   
CreatedDate,
RStatus
)
VALUES (
's1',
dbo.CONST('VUSERS_ADMIN'),
@s1FolderRootID,
dbo.CONST('VUSERS_SYSTEM'),
GETDATE(),
dbo.CONST('RSTATUS_ACTIVE')
)

-- Map '/systems/s1' to folder '/systems' & system 's1'
INSERT INTO dbo.SystemFolderFiles
(
VSystemID,
VFileID,
VFolderID,       
Link,           
VParentFolderID,
CreatedBy,   
CreatedDate,
RStatus
)
VALUES (
1,  -- VSystemID,   
NULL,	 -- VFileID
3,	  -- VFolderID      
0,	 -- Link           
2,	 -- VParentFolderID
dbo.CONST('VUSERS_SYSTEM'),
GETDATE(),
dbo.CONST('RSTATUS_ACTIVE')
)





--------------------------------------------------------------------------------
-- APPS


-- Text
INSERT INTO dbo.VApps
(
Name,
Description,
Owner,
Settings,
CreatedBy,   
CreatedDate,
RStatus
)
VALUES (
'Notepad',
'Notepad app',  
dbo.CONST('VUSERS_ADMIN'),
'',
dbo.CONST('VUSERS_SYSTEM'),
GETDATE(),
dbo.CONST('RSTATUS_ACTIVE')
)


-- RSheets
INSERT INTO dbo.VApps
(
Name,
Description,
Owner,
Settings,
CreatedBy,   
CreatedDate,
RStatus
)
VALUES (
'RSheets',  
'RSheets app',  
dbo.CONST('VUSERS_ADMIN'),
'',
dbo.CONST('VUSERS_SYSTEM'),
GETDATE(),
dbo.CONST('RSTATUS_ACTIVE')
)

-- TODO: Add Car Manager app


--------------------------------------------------------------------------------
-- FILE TYPES

-- Text file type
INSERT INTO dbo.FileTypes
(
Name,       
Description,
CreatedBy,   
CreatedDate,
RStatus
)
VALUES (
'Text',   
'Text file',
dbo.CONST('VUSERS_SYSTEM'),
GETDATE(),
dbo.CONST('RSTATUS_ACTIVE')
)

-- RSheet file type
INSERT INTO dbo.FileTypes
(
Name,       
Description,
CreatedBy,   
CreatedDate,
RStatus
)
VALUES (
'RSheet',   
'RSheet file',
dbo.CONST('VUSERS_SYSTEM'),
GETDATE(),
dbo.CONST('RSTATUS_ACTIVE')
)



-------------------------------------------------------------------------------------------
-- FILE TYPE APPS


-- RSheet type can be opened by RSheets app
INSERT INTO dbo.FileTypeApps
(
VFileTypeID,       
VAppID,
CreatedBy,   
CreatedDate,
RStatus
)
VALUES (
1,
1,
dbo.CONST('VUSERS_SYSTEM'),
GETDATE(),
dbo.CONST('RSTATUS_ACTIVE')
)

-- RSheet type can be opened by RSheets app
INSERT INTO dbo.FileTypeApps
(
VFileTypeID,       
VAppID,
CreatedBy,   
CreatedDate,
RStatus
)
VALUES (
2,
2,
dbo.CONST('VUSERS_SYSTEM'),
GETDATE(),
dbo.CONST('RSTATUS_ACTIVE')
)




--------------------------------------------------------------------------------
-- FILES


-- ReadMe text file
INSERT INTO dbo.VFiles
(
Name,       
FileTypeID,
Attrs,
CreatedBy,   
CreatedDate,
RStatus
)
VALUES (
'ReadMe',
1,
'',
dbo.CONST('VUSERS_SYSTEM'),
GETDATE(),
dbo.CONST('RSTATUS_ACTIVE')
)

-- workbook1
INSERT INTO dbo.VFiles
(
Name,       
FileTypeID,
Attrs,
CreatedBy,   
CreatedDate,
RStatus
)
VALUES (
'Workbook1',
2,
'',
dbo.CONST('VUSERS_SYSTEM'),
GETDATE(),
dbo.CONST('RSTATUS_ACTIVE')
)



-- Map Readme text file to folder '/systems/s1'
INSERT INTO dbo.SystemFolderFiles
(
VSystemID,
VFileID,
VFolderID,       
Link,           
VParentFolderID,
CreatedBy,   
CreatedDate,
RStatus
)
VALUES (
1,  -- VSystemID,   
1,	 -- VFileID
NULL,	  -- VFolderID      
0,	 -- Link           
3,	 -- VParentFolderID
dbo.CONST('VUSERS_SYSTEM'),
GETDATE(),
dbo.CONST('RSTATUS_ACTIVE')
)

-- Map Workbook1 to folder '/systems/s1'
INSERT INTO dbo.SystemFolderFiles
(
VSystemID,
VFileID,
VFolderID,       
Link,           
VParentFolderID,
CreatedBy,   
CreatedDate,
RStatus
)
VALUES (
1,  -- VSystemID,   
2,	 -- VFileID
NULL,	  -- VFolderID      
0,	 -- Link           
3,	 -- VParentFolderID
dbo.CONST('VUSERS_SYSTEM'),
GETDATE(),
dbo.CONST('RSTATUS_ACTIVE')
)

























--------------------------------------------------------------------------------
-- TODO: Activate constraints and triggers(ENABLE/DISABLE TRIGGER) at end 
-- after root folder & system created
