-- Restore a workbook(WB)
-- Calling process must set a backup lock on the file
-- Returns:
--   0 Success, edit was written
--  -1 Exception while running backup
--  -2 WB has edit lock, needs backup-in-progress lock
--  -3 WB needs backup-in-progress lock
--  -4 WB has pending edits
--  -5 No backup exists with the given BackupID
--  -6 Failed to remove backup-in-progress lock
--
-- Test:
-- UPDATE mpm.Locks SET Locked = 0 WHERE VFileID = 3 AND LockTypeID = 1   -- edit
-- UPDATE mpm.Locks SET Locked = 1 WHERE VFileID = 3 AND LockTypeID = 2   -- backup-in-progress
-- select * from mpm.Locks
-- select * from mpm.LockTypes
--
-- TRUNCATE TABLE mpm.BackupSheets
-- TRUNCATE TABLE mpm.BackupProducts
-- EXEC mpm.spDoWBBackup 2, 3
--
-- select * from mpm.Edits
-- select * from mpm.WBEventLogs
-- select * from mpm.WBEventTypes
-- select * from mpm.WBBackups


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE mpm.spDoWBRestore(
	@UserID UDT_ID,
	@VFileID UDT_ID,
	@BackupID UDT_ID  -- BackupID to restore from
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @retCode int = 0
	DECLARE @message nvarchar(200) = ''

	-- CONSTANTS
	DECLARE @ACTIVE_ROW_STATUS int = 2
	-- LockType consts
	-- IMPORTANT: These should not change in mpm.LockTypes table, but querying it would be better
	DECLARE @EDIT_LOCK_TYPE_ID UDT_ID = 1   
	DECLARE @BACKUPINPROGRESS_LOCK_TYPE_ID UDT_ID = 2
	-- EventType consts
	DECLARE @EDIT_EVENT_TYPE_ID UDT_ID = 3 
	-- Edit status
	DECLARE @EDIT_PENDING_STATUS_CODE UDT_ID = 1 

BEGIN TRY	
	-- Check if file has edit lock on it, cant do restore then
	DECLARE @isFileLockedForEdit UDT_Bool = (SELECT Locked FROM mpm.Locks WHERE LockTypeID=@EDIT_LOCK_TYPE_ID AND VFileID = @VFileID)
	IF @isFileLockedForEdit = 1
	BEGIN
		SET @retCode = -2
		SET @message = 'File has edit lock. Needs backup-in-progress lock'
		GOTO LABEL_RETURN
	END

	-- Check if file has backup-in-progress lock on it, cant do backup without it
	DECLARE @isFileLockedForBackup UDT_Bool = (SELECT Locked FROM mpm.Locks WHERE LockTypeID=@BACKUPINPROGRESS_LOCK_TYPE_ID AND VFileID = @VFileID)
	IF @isFileLockedForBackup != 1
	BEGIN
		SET @retCode = -3
		SET @message = 'File needs backup-in-progress lock'
		GOTO LABEL_RETURN
	END

	-- WB has backup lock only, can proceed with restore. Further edits are blocked.
	
	-- Check if any pending edits for this WB. Caller needs to ensure that all pending edits for the WB are failed
	-- before doing a restore. Cannot have the edits come later.
	DECLARE @numPendingEdits int = (SELECT COUNT(*) FROM mpm.Edits as e JOIN mpm.Workbooks as w ON e.VFileID = w.VFileID
		WHERE e.Code = @EDIT_PENDING_STATUS_CODE AND e.Code = w.LatestBackupID AND e.VFileID = @VFileID)

	DECLARE @debugmsg nvarchar(200) = 'Num pending edits:' + CAST(@numPendingEdits AS nvarchar(5))
	print @debugmsg

	IF @numPendingEdits > 0
	BEGIN
		SET @retCode = -4
		SET @message = 'File has pending edits'
		GOTO LABEL_RETURN
	END

	-- Check if a backup exists with the given id for this file
	DECLARE @numExistingBackupsWithNewID UDT_ID = (SELECT COUNT(*) FROM mpm.BackupSheets WHERE BackupID = @BackupID AND VFileID = @VFileID)
	IF @numExistingBackupsWithNewID = 0
	BEGIN
		SET @retCode = -5
		SET @message = 'No backup exists with id:' + @BackupID
		GOTO LABEL_RETURN
	END

	-- All changes now must be atomic
	BEGIN TRAN

	-- Remove existing data from the main data tables for the given file
	DELETE FROM mpm.Sheets WHERE VFileID = @VFileId
	DELETE FROM mpm.Products WHERE VFileID = @VFileId
	DELETE FROM mpm.ProductTypes WHERE VFileID = @VFileId
	DELETE FROM mpm.MRanges WHERE VFileID = @VFileId
	DELETE FROM mpm.MSeries WHERE VFileID = @VFileId
	DELETE FROM mpm.MTables WHERE VFileID = @VFileId
	DELETE FROM mpm.Cells WHERE VFileID = @VFileId

	-- Restore sheets
	INSERT INTO [mpm].[Sheets]
           ([VFileID]
           ,[SheetID]
           ,[Name]
           ,[SheetNum]
           ,[Style]
           ,[StartRowNum]
           ,[StartColNum]
           ,[EndRowNum]
           ,[EndColNum]           
           ,[CreatedBy]
           ,[CreatedDate]
           ,[LastUpdatedBy]
           ,[LastUpdatedDate]
           ,[RStatus])
		SELECT VFileID, SheetID, Name, SheetNum, Style, StartRowNum, StartColNum, EndRowNum, EndColNum
		   ,[SheetCreatedBy]
           ,[SheetCreatedDate]
           ,[SheetLastUpdatedBy]
           ,[SheetLastUpdatedDate]
           ,[SheetRStatus]		
		FROM mpm.[BackupSheets] WHERE VFileID=@VFileID AND BackupID=@BackupID

	-- Restore Products
	INSERT INTO [mpm].[Products]
           ([VFileID]
           ,[ProductID]
           ,[Name]
           ,[SheetID]          
           ,[CreatedBy]
           ,[CreatedDate]
           ,[LastUpdatedBy]
           ,[LastUpdatedDate]
           ,[RStatus])
		SELECT VFileID, ProductID, Name, SheetID
		   ,[ProductCreatedBy]
           ,[ProductCreatedDate]
           ,[ProductLastUpdatedBy]
           ,[ProductLastUpdatedDate]
           ,[ProductRStatus]
		FROM mpm.[BackupProducts] WHERE VFileID=@VFileID AND BackupID=@BackupID


	-- Restore Product Types
	INSERT INTO [mpm].ProductTypes
           ([VFileID]
           ,[ProductTypeID]
           ,[Name]
           ,[ProductID]           
           ,[CreatedBy]
           ,[CreatedDate]
           ,[LastUpdatedBy]
           ,[LastUpdatedDate]
           ,[RStatus])
		SELECT VFileID, ProductTypeID, Name, ProductID
		   ,[ProductTypeCreatedBy]
           ,[ProductTypeCreatedDate]
           ,[ProductTypeLastUpdatedBy]
           ,[ProductTypeLastUpdatedDate]
           ,[ProductTypeRStatus]
		FROM mpm.[BackupProductTypes] WHERE VFileID=@VFileID AND BackupID=@BackupID

	-- Restore MSeries
	INSERT INTO [mpm].MSeries
           ([VFileID]
           ,[SeriesID]
           ,[Name]
           ,[RangeID]
           ,[SheetID]
           ,[HeaderTableID]
           ,[DetailTableID]
           ,[SeriesNum]          
           ,[CreatedBy]
           ,[CreatedDate]
           ,[LastUpdatedBy]
           ,[LastUpdatedDate]
           ,[RStatus])
		SELECT VFileID, SeriesID, Name, RangeID, SheetID, HeaderTableID, DetailTableID, SeriesNum
		   ,[MSeriesCreatedBy]
           ,[MSeriesCreatedDate]
           ,[MSeriesLastUpdatedBy]
           ,[MSeriesLastUpdatedDate]
           ,[MSeriesRStatus]
		FROM mpm.[BackupMSeries] WHERE VFileID=@VFileID AND BackupID=@BackupID

	-- Restore MRanges
	INSERT INTO [mpm].MRanges
           ([VFileID]
           ,[RangeID]
           ,[Name]
           ,[SheetID]
           ,[ProductID]
           ,[ProductTypeID]
           ,[HeaderTableID]
           ,[RangeNum]
           ,[CreatedBy]
           ,[CreatedDate]
           ,[LastUpdatedBy]
           ,[LastUpdatedDate]
           ,[RStatus])
		SELECT VFileID, RangeID, Name, SheetID, ProductID, ProductTypeID, HeaderTableID, RangeNum
		   ,[MRangeCreatedBy]
           ,[MRangeCreatedDate]
           ,[MRangeLastUpdatedBy]
           ,[MRangeLastUpdatedDate]
           ,[MRangeRStatus]
		FROM mpm.[BackupMRanges] WHERE VFileID=@VFileID AND BackupID=@BackupID

	-- Restore MTables
	INSERT INTO [mpm].MTables
           ([VFileID]
           ,[TableID]
           ,[Name]
           ,[NumRows]
           ,[NumCols]
           ,[StartRowNum]
           ,[StartColNum]
           ,[EndRowNum]
           ,[EndColNum]
           ,[RangeID]
           ,[SeriesID]
           ,[SheetID]
           ,[TableType]
           ,[Style]
           ,[HeaderRow]
           ,[TotalRow]
           ,[BandedRows]
           ,[BandedColumns]
           ,[FilterButton]
           ,[CreatedBy]
           ,[CreatedDate]
           ,[LastUpdatedBy]
           ,[LastUpdatedDate]
           ,[RStatus])
		SELECT VFileID
		   ,[TableID]
           ,[Name]
           ,[NumRows]
           ,[NumCols]
		   ,[StartRowNum]
           ,[StartColNum]
           ,[EndRowNum]
           ,[EndColNum]
           ,[RangeID]
           ,[SeriesID]
           ,[SheetID]
           ,[TableType]
           ,[Style]
           ,[HeaderRow]
           ,[TotalRow]
           ,[BandedRows]
           ,[BandedColumns]
           ,[FilterButton]
		   ,[MTableCreatedBy]
           ,[MTableCreatedDate]
           ,[MTableLastUpdatedBy]
           ,[MTableLastUpdatedDate]
           ,[MTableRStatus]
		FROM mpm.[BackupMTables] WHERE VFileID=@VFileID AND BackupID=@BackupID

	-- Restore Cells - NOTE, creation, lastupdated dates, rstatus is not preserved
	INSERT INTO [mpm].Cells
           ([VFileID]
           ,[SheetID]
           ,[CellID]
           ,[RowNum]
           ,[ColNum]
           ,[Value]
           ,[Formula]
           ,[Format]
           ,[Style]
           ,[Comment]
           ,[CreatedBy]
           ,[CreatedDate]
           ,[LastUpdatedBy]
           ,[LastUpdatedDate]
           ,[RStatus])
		SELECT VFileID
		   ,[SheetID]
           ,[CellID]
           ,[RowNum]
           ,[ColNum]
           ,[Value]
           ,[Formula]
           ,[Format]
           ,[Style]
           ,[Comment]
		   ,[CreatedBy]
           ,[CreatedDate]
           ,[LastUpdatedBy]
           ,[LastUpdatedDate]
           ,[RStatus]
		FROM mpm.[BackupCells] WHERE VFileID=@VFileID AND BackupID=@BackupID

	-- Note restore event success in db log
	INSERT INTO mpm.DBLogs VALUES('Restore', 0, 'Success', '', NULL, NULL, 2, GETDATE())

	IF @@TRANCOUNT > 0
		COMMIT TRAN	
    
	LABEL_RETURN:
	-- Done
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0
		ROLLBACK TRANSACTION	
	-- Set return code
	SET @retCode = -1
	SET @message = 'Line ' + CAST(ERROR_LINE() AS nvarchar(5)) + ':'+ ERROR_MESSAGE()
	-- Note error in db log while doing backup
	INSERT INTO mpm.DBLogs VALUES('Restore', @retCode, 'Failed', @message, NULL, NULL, 2, GETDATE())
END CATCH

	-- Remove backup-in-progress lock
	DECLARE @RC int
	EXECUTE @RC = mpm.spUpdateWBLock @UserID, @VFileID, @BACKUPINPROGRESS_LOCK_TYPE_ID, 0
	IF @RC != 0
	BEGIN 
		SET @retCode = -5
		SET @message = @message + 'Failed to remove backup-in-progress lock'  -- because an error msg maybe there already so keep it
	END

	SELECT @retCode, @message

END
GO
