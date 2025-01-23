-- Seed data

--SELECT NEWID()

-- Roles(drop constraints on this table first, restore later)
INSERT INTO dbo.VRoles VALUES ('3A798A4B-6FF5-4570-B77B-D3FEF8FF1609', 'Unassigned', 'Unassigned', dbo.CONST('VUSERS_SYSTEM'), GETDATE(), NULL, NULL, dbo.CONST('RSTATUS_ACTIVE'))
INSERT INTO dbo.VRoles VALUES ('BD08482C-5CFF-4BDB-874C-0B25EA448E1B', 'Admin', 'Admin', dbo.CONST('VUSERS_SYSTEM'), GETDATE(), NULL, NULL, dbo.CONST('RSTATUS_ACTIVE'))
INSERT INTO dbo.VRoles VALUES ('D5CD880A-0D67-4356-B316-8610E6E80780', 'Visitor', 'Visitor',  dbo.CONST('VUSERS_SYSTEM'), GETDATE(), NULL, NULL, dbo.CONST('RSTATUS_ACTIVE'))


-- Users
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



-- TODO

-- Create root folder and readme file
INSERT INTO dbo.VFolders
(
Name,       
Attrs,  		
CreatedBy,   
CreatedDate,
RStatus
)
VALUES (
'root',
'',
dbo.CONST('VUSERS_SYSTEM'),
GETDATE(),
dbo.CONST('RSTATUS_ACTIVE')
)



