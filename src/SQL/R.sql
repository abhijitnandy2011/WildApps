ABCD FGGTRY
RETURN;


SELECT * from [Wild].[dbo].[Workbook]
SELECT * from [Wild].[dbo].[Sheet]
SELECT * from [Wild].[dbo].[Cell]
SELECT * from [Wild].[dbo].[XlTable]


--DELETE FROM dbo.Cell;
--DELETE FROM dbo.CellOldVer;
--DELETE FROM dbo.Sheet;
--DELETE FROM dbo.XlTable;


---------------------------------------------------
-- 12th Jan

 --delete from [dbo].[Users]


SELECT * FROM dbo.VUsers
SELECT * FROM dbo.VRoles

SELECT NEWID()
SELECT NEWID()
SELECT NEWID()



3A798A4B-6FF5-4570-B77B-D3FEF8FF1609
BD08482C-5CFF-4BDB-874C-0B25EA448E1B
D5CD880A-0D67-4356-B316-8610E6E80780


-- DELETE FROM dbo.SysLogs
exec dbo.logSysError 'MyModule', 'Error msg'

--DELETE FROM dbo.VSystems
--TRUNCATE TABLE dbo.VSystems


select * from dbo.VUsers


select * from dbo.VSystems
select * from dbo.VFolders
select * from dbo.VFiles
select * from dbo.SystemFolderFiles



select * from dbo.FileTypes
select * from dbo.VApps
select * from dbo.FileTypeApps
select * from dbo.VFolders
select * from dbo.VFiles
select * from dbo.SystemFolderFiles

select * from dbo.SystemUsers


--------------------------------------------------------------
-- 30th Jan 2025

-- Add folder

-- DROP TRIGGER [dbo].[trg_VFolders_PathDupeCheck]


select * from dbo.VFolders
select * from dbo.SystemFolderFiles

select * from dbo.SysLogs

exec dbo.logMsg 'MyModule', 0, 'Error msg'


-- TODO: Add description field in VSystems, VFolders & VFiles at end for now

--ALTER TABLE dbo.VSystems ADD Description UDT_Description_Small_Opt
--ALTER TABLE dbo.VFolders ADD Description   UDT_Description_Small_Opt
--ALTER TABLE dbo.VFiles ADD Description   UDT_Description_Small_Opt


-----------------------------------------

select * from mpm.Products
select * from mpm.ProductTypes
select * from mpm.Sheets
select * from mpm.MRanges
select * from mpm.MSeries
select * from mpm.MTables
select top 10 * from mpm.Cells

select * from mpm.Cells

-- Check there is no duplicate table name
select Name, count(*) from mpm.MTables group by Name order by count(*) desc


SELECT
p.Name,
pt.Name,
r.Name,
s.Name,
s.SeriesID
FROM
mpm.Products p JOIN mpm.ProductTypes pt ON pt.ProductID = p.ProductID
LEFT JOIN mpm.MRanges r ON r.ProductTypeID = pt.ProductTypeID
LEFT JOIN mpm.MSeries s ON s.RangeID = r.RangeID

SELECT * FROM mpm.MTables WHERE TableType = 3 AND RangeID IS NULL


select * from mpm.Products
select * from mpm.ProductTypes
select * from mpm.Sheets
select * from mpm.MRanges
select * from mpm.MSeries
select * from mpm.MTables
select top 10 * from mpm.Cells

select * from mpm.Cells

--   TRUNCATE TABLE mpm.Sheets
--   TRUNCATE TABLE mpm.Products
--   TRUNCATE TABLE mpm.ProductTypes
--   TRUNCATE TABLE mpm.MRanges
--   TRUNCATE TABLE mpm.MSeries
--   TRUNCATE TABLE mpm.MTables
--   TRUNCATE TABLE mpm.Cells


--UPDATE  mpm.MTables SET
--RangeID = s.RangeID
--FROM
--	mpm.MTables t JOIN mpm.MSeries s ON t.SeriesId = s.SeriesId
--WHERE
--    t.TableType = 3 AND t.RangeID IS NULL

----------------------------------------------------------------

-- 18th Feb
Select * from mpm.MRanges where VFileID = 3 and name like '%Mah%'
Select * from mpm.MSeries where VFileID = 3 and name like '%XUV%' and RangeID = 1

Select * from mpm.MTables

--Select * from mpm.Cells

-- See cell values as columns
Select
C1.Value AS "1",
C2.Value AS "2",
C3.Value AS "3",
C4.Value AS "4",
C5.Value AS "5",
C6.Value AS "6"
--C7.Value AS "7",
--C8.Value AS "8",
--C9.Value AS "9",
--C10.Value AS "10"
from
(select * from mpm.Cells where ColNum = 1  AND TableID = 4) AS C1,
(select * from mpm.Cells where ColNum = 2  AND TableID = 4) AS C2,
(select * from mpm.Cells where ColNum = 3  AND TableID = 4) AS C3,
(select * from mpm.Cells where ColNum = 4  AND TableID = 4) AS C4,
(select * from mpm.Cells where ColNum = 5  AND TableID = 4) AS C5,
(select * from mpm.Cells where ColNum = 6  AND TableID = 4) AS C6
--(select * from mpm.Cells where ColNum = 7  AND TableID = 4) AS C7,
--(select * from mpm.Cells where ColNum = 8  AND TableID = 4) AS C8,
--(select * from mpm.Cells where ColNum = 9  AND TableID = 4) AS C9,
--(select * from mpm.Cells where ColNum = 10 AND TableID = 4) AS C10
WHERE
C1.RowNum = C2.RowNum and
C1.RowNum = C3.RowNum and
C1.RowNum = C4.RowNum and
C1.RowNum = C5.RowNum and
C1.RowNum = C6.RowNum --and
--C1.RowNum = C7.RowNum and
--C1.RowNum = C8.RowNum and
--C1.RowNum = C9.RowNum and
--C1.RowNum = C10.RowNum


SELECT 
CONCAT('MAX(C', CellID, ') AS [', CellID, '],'),
CONCAT('case WHEN ColNum=', CellID, ' Then Value end AS C', CellID, ',')
FROM
mpm.Cells WHERE CellID < 20
--------------------
SELECT 
MAX(C1) AS [1],
MAX(C2) AS [2],
MAX(C3) AS [3],
MAX(C4) AS [4],
MAX(C5) AS [5],
MAX(C6) AS [6],
MAX(C7) AS [7],
MAX(C8) AS [8],
MAX(C9) AS [9],
MAX(C10) AS [10]
FROM
(
	Select
		RowNum,
case WHEN ColNum=1 Then Value end AS C1,
case WHEN ColNum=2 Then Value end AS C2,
case WHEN ColNum=3 Then Value end AS C3,
case WHEN ColNum=4 Then Value end AS C4,
case WHEN ColNum=5 Then Value end AS C5,
case WHEN ColNum=6 Then Value end AS C6,
case WHEN ColNum=7 Then Value end AS C7,
case WHEN ColNum=8 Then Value end AS C8,
case WHEN ColNum=9 Then Value end AS C9,
case WHEN ColNum=10 Then Value end AS C10
	from
	mpm.Cells 
	WHERE VFileID = 3 and TableID = 4
) t
GROUP BY RowNum


---------------------------
-- 26th Apr

select * from mpm.Cells where 
(RowNum >=1 and RowNum <=5 and ColNum >=1 and ColNum<=5) OR
(RowNum >=6 and RowNum <=10 and ColNum >=1 and ColNum<=5) 


DECLARE @minRow int = 1
DECLARE @minCol int = 1
DECLARE @maxCol int = 5
DECLARE @maxRow int = 8
DECLARE @sheetNameParam varchar(20) = 'Mahindra'
DECLARE @vFileIdParam int = 3
DECLARE @activeStatusParam int = 2

SELECT * FROM 
mpm.Cells AS c INNER JOIN mpm.Sheets as s ON s.SheetID = c.SheetID AND s.VFileID = c.VFileID AND s.RStatus = c.RStatus
WHERE 
(c.RowNum >= @minRow AND c.RowNum <= @maxRow AND c.ColNum >= @minCol AND c.ColNum <= @maxCol) 
AND s.Name=@sheetNameParam AND c.VFileID=@vFileIdParam AND c.RStatus=@activeStatusParam
ORDER BY RowNum, ColNum

SELECT * from mpm.MTables

SELECT MAX(ID) FROM dbo.VUsers

-- DELETE FROM dbo.VUsers WHERE ID = 0

-- Activate user
--UPDATE dbo.VUsers SET RStatus = dbo.CONST('RSTATUS_ACTIVE') WHERE ID = 102

-------------------------------


select * from mpm.Products
select * from mpm.ProductTypes
select * from mpm.Sheets
select * from mpm.MRanges
select * from mpm.MSeries
select * from mpm.MTables


select * from mpm.Workbooks

select * from dbo.VUsers
select * from mpm.Locks
select * from mpm.LockTypes

-- Locks:
-- Add edit lock for file - will be done by the relevant user
-- INSERT INTO mpm.Locks VALUES(3,1,1,4, GETDATE(), NULL, NULL, dbo.CONST('RSTATUS_ACTIVE'))
-- UPDATE mpm.Locks SET Locked = 0 WHERE VFileID = 3 AND LockTypeID = 1

-- Add backup lock but make it unlocked - will be applied by Admin
-- INSERT INTO mpm.Locks VALUES(3,2,0,2, GETDATE(), NULL, NULL, dbo.CONST('RSTATUS_ACTIVE'))
-- UPDATE mpm.Locks SET Locked = 1 WHERE VFileID = 3 AND LockTypeID = 2


-- Edits
-- TRUNCATE TABLE mpm.Edits
select * from mpm.Workbooks
select * from mpm.Edits

select * from mpm.WBEventLogs
select * from mpm.WBEventTypes
select * from mpm.WBBackups
select * from mpm.WBVersions

SELECT GETDATE()

SELECT * FROM mpm.Sheets
SELECT * FROM mpm.BackupSheets

TRUNCATE TABLE mpm.BackupSheets

INSERT INTO mpm.BackupSheets
SELECT VFileID, 1, SheetID, Name, SheetNum, Style, StartRowNum, StartColNum, EndRowNum, EndColNum, 
    CreatedBy, CreatedDate, LastUpdatedBy, LastUpdatedDate, RStatus,
	2, GETDATE(), NULL, NULL, 2
FROM mpm.Sheets WHERE VFileID = 3

SELECT LatestBackUpID+1 FROM mpm.Workbooks WHERE VFileID = 3

SELECT * FROM mpm.Sheets
SELECT * FROM mpm.BackupSheets

SELECT * FROM mpm.Products
SELECT * FROM mpm.BackupProducts

SELECT * FROM mpm.ProductTypes
SELECT * FROM mpm.BackupProductTypes

SELECT * FROM mpm.MRanges
SELECT * FROM mpm.BackupMRanges

SELECT * FROM mpm.MSeries
SELECT * FROM mpm.BackupMSeries

SELECT * FROM mpm.MTables
SELECT * FROM mpm.BackupMTables

SELECT * FROM mpm.Cells
SELECT * FROM mpm.BackupCells

-- TRUNCATE TABLE mpm.WBBackups

--BEGIN TRY  
--    -- Generate a divide-by-zero error.  
--    SELECT 1/0;  
--END TRY  
--BEGIN CATCH  
--    print 'Line ' + CAST(ERROR_LINE() AS nvarchar(5)) + ':'+ ERROR_MESSAGE()
--END CATCH;  
--GO


select * from mpm.DBLogs
-- INSERT INTO mpm.DBLogs VALUES('Backup', 1, 'Failed', NULL, NULL, NULL, 2, GETDATE())

--DECLARE @test nvarchar(5) = 'dsadasdass'
--print @test

DECLARE @locked UDT_bool
DECLARE @lastUpdatedBy UDT_ID
DECLARE @lastUpdatedDate UDT_DateTime
SELECT @locked=Locked, @lastUpdatedBy=LastUpdatedBy, @lastUpdatedDate=LastUpdatedDate FROM mpm.Locks 
	WHERE LockTypeID=1 AND VFileID = 3
print @locked
print @lastUpdatedBy
print @lastUpdatedDate
select * from mpm.Locks
select * from mpm.LockTypes

SELECT CASE WHEN DATEDIFF(ss, @lastUpdatedDate, GETDATE()) > 300 THEN 1 ELSE 0 END

DECLARE @id int = (SELECT MAX(EditID) + 1 FROM mpm.Edits WHERE VFileID = 3 GROUP BY VFileID)
SET @id = (SELECT ISNULL(@id,1))
print @id

select * from mpm.Edits

SELECT * FROM mpm.Sheets WHERE VFileID =3
SELECT * FROM mpm.BackupSheets WHERE VFileID =3

--DELETE FROM mpm.Sheets WHERE VFileID =