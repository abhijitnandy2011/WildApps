-- Backup a workbook(WB)
-- Calling process must set a lock on the file & ensure all edits applied before the backup is initiated
-- Returns:
--   0 Success, edit was written
--  -1 Exception while running backup
--  -2 WB has edit lock, needs backup-in-progress lock
--  -3 WB needs backup-in-progress lock
--  -4 WB has pending edits
--  -5 Backup exists already with latest(next) id
--  -6 Failed to remove backup-in-progress lock
--
-- Test:
-- UPDATE mpm.Locks SET Locked = 0 WHERE VFileID = 3 AND LockTypeID = 1   -- edit
-- UPDATE mpm.Locks SET Locked = 1 WHERE VFileID = 3 AND LockTypeID = 2   -- backup-in-progress
-- select * from mpm.Locks
-- select * from mpm.LockTypes
-- 
-- TRUNCATE TABLE mpm.Edits
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
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
ALTER PROCEDURE mpm.spDoWBBackup(
	@UserID UDT_ID,
	@VFileID UDT_ID
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
	-- Check if file has edit lock on it, cant do backup then
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

	-- WB has backup lock only, can proceed. We assume all the registered edits are complete & written to the 
	-- current WB and further edits are blocked. 
	
	-- Check if any pending edits for this WB. Caller needs to ensure that all pending edits for the WB are complete.
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

	-- Decide the new backup ID
	DECLARE @latestBackupID UDT_ID = (SELECT LatestBackUpID+1 FROM mpm.Workbooks WHERE VFileID = @VFileID)

	-- TODO: Check if there is any backup with this new id for this file already(should not be as backup failures are rolled back)
	DECLARE @numExistingBackupsWithNewID UDT_ID = (SELECT COUNT(*) FROM mpm.BackupSheets WHERE BackupID = @latestBackupID AND VFileID = @VFileID)
	IF @numExistingBackupsWithNewID > 0
	BEGIN
		SET @retCode = -5
		SET @message = 'Backup exists already with id:' + @latestBackupID
		GOTO LABEL_RETURN
	END

	-- All changes now must be atomic
	BEGIN TRAN

	-- Backup sheets
	INSERT INTO [mpm].[BackupSheets]
           ([VFileID]
           ,[BackupID]
           ,[SheetID]
           ,[Name]
           ,[SheetNum]
           ,[Style]
           ,[StartRowNum]
           ,[StartColNum]
           ,[EndRowNum]
           ,[EndColNum]
           ,[SheetCreatedBy]
           ,[SheetCreatedDate]
           ,[SheetLastUpdatedBy]
           ,[SheetLastUpdatedDate]
           ,[SheetRStatus]
           ,[CreatedBy]
           ,[CreatedDate]
           ,[LastUpdatedBy]
           ,[LastUpdatedDate]
           ,[RStatus])
		SELECT VFileID, @latestBackupID, SheetID, Name, SheetNum, Style, StartRowNum, StartColNum, EndRowNum, EndColNum, 
			CreatedBy, CreatedDate, LastUpdatedBy, LastUpdatedDate, RStatus,
			@UserID, GETDATE(), NULL, NULL, @ACTIVE_ROW_STATUS
		FROM mpm.Sheets WHERE VFileID = @VFileID

	-- Backup Products
	INSERT INTO [mpm].[BackupProducts]
           ([VFileID]
           ,[BackupID]
           ,[ProductID]
           ,[Name]
           ,[SheetID]
           ,[ProductCreatedBy]
           ,[ProductCreatedDate]
           ,[ProductLastUpdatedBy]
           ,[ProductLastUpdatedDate]
           ,[ProductRStatus]
           ,[CreatedBy]
           ,[CreatedDate]
           ,[LastUpdatedBy]
           ,[LastUpdatedDate]
           ,[RStatus])
		SELECT VFileID, @latestBackupID, ProductID, Name, SheetID,
			CreatedBy, CreatedDate, LastUpdatedBy, LastUpdatedDate, RStatus,
			@UserID, GETDATE(), NULL, NULL, @ACTIVE_ROW_STATUS
		FROM mpm.Products WHERE VFileID = @VFileID


	-- Backup Product Types
	INSERT INTO [mpm].[BackupProductTypes]
           ([VFileID]
           ,[BackupID]
           ,[ProductTypeID]
           ,[Name]
           ,[ProductID]
           ,[ProductTypeCreatedBy]
           ,[ProductTypeCreatedDate]
           ,[ProductTypeLastUpdatedBy]
           ,[ProductTypeLastUpdatedDate]
           ,[ProductTypeRStatus]
           ,[CreatedBy]
           ,[CreatedDate]
           ,[LastUpdatedBy]
           ,[LastUpdatedDate]
           ,[RStatus])
		SELECT VFileID, @latestBackupID, ProductTypeID, Name, ProductID,
			CreatedBy, CreatedDate, LastUpdatedBy, LastUpdatedDate, RStatus,
			@UserID, GETDATE(), NULL, NULL, @ACTIVE_ROW_STATUS
		FROM mpm.ProductTypes WHERE VFileID = @VFileID

	-- Backup MSeries
	INSERT INTO [mpm].[BackupMSeries]
           ([VFileID]
           ,[BackupID]
           ,[SeriesID]
           ,[Name]
           ,[RangeID]
           ,[SheetID]
           ,[HeaderTableID]
           ,[DetailTableID]
           ,[SeriesNum]
           ,[MSeriesCreatedBy]
           ,[MSeriesCreatedDate]
           ,[MSeriesLastUpdatedBy]
           ,[MSeriesLastUpdatedDate]
           ,[MSeriesRStatus]
           ,[CreatedBy]
           ,[CreatedDate]
           ,[LastUpdatedBy]
           ,[LastUpdatedDate]
           ,[RStatus])
		SELECT VFileID, @latestBackupID, SeriesID, Name, RangeID, SheetID, HeaderTableID, DetailTableID, SeriesNum,
			CreatedBy, CreatedDate, LastUpdatedBy, LastUpdatedDate, RStatus,
			@UserID, GETDATE(), NULL, NULL, @ACTIVE_ROW_STATUS
		FROM mpm.MSeries WHERE VFileID = @VFileID

	-- Backup MRanges
	INSERT INTO [mpm].[BackupMRanges]
           ([VFileID]
           ,[BackupID]
           ,[RangeID]
           ,[Name]
           ,[SheetID]
           ,[ProductID]
           ,[ProductTypeID]
           ,[HeaderTableID]
           ,[RangeNum]
           ,[MRangeCreatedBy]
           ,[MRangeCreatedDate]
           ,[MRangeLastUpdatedBy]
           ,[MRangeLastUpdatedDate]
           ,[MRangeRStatus]
           ,[CreatedBy]
           ,[CreatedDate]
           ,[LastUpdatedBy]
           ,[LastUpdatedDate]
           ,[RStatus])
		SELECT VFileID, @latestBackupID, RangeID, Name, SheetID, ProductID, ProductTypeID, HeaderTableID, RangeNum,
			CreatedBy, CreatedDate, LastUpdatedBy, LastUpdatedDate, RStatus,
			@UserID, GETDATE(), NULL, NULL, @ACTIVE_ROW_STATUS
		FROM mpm.MRanges WHERE VFileID = @VFileID

	-- Backup MTables
	INSERT INTO [mpm].[BackupMTables]
           ([VFileID]
           ,[BackupID]
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
           ,[CreatedBy]
           ,[CreatedDate]
           ,[LastUpdatedBy]
           ,[LastUpdatedDate]
           ,[RStatus])
		SELECT VFileID, @latestBackupID
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
		   ,CreatedBy, CreatedDate, LastUpdatedBy, LastUpdatedDate, RStatus,
			@UserID, GETDATE(), NULL, NULL, @ACTIVE_ROW_STATUS
		FROM mpm.MTables WHERE VFileID = @VFileID

	-- Backup Cells = NOTE, creation, lastupdated dates, rstatus is not preserved
	INSERT INTO [mpm].[BackupCells]
           ([VFileID]
           ,[BackupID]
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
		SELECT VFileID, @latestBackupID
		   ,[SheetID]
           ,[CellID]
           ,[RowNum]
           ,[ColNum]
           ,[Value]
           ,[Formula]
           ,[Format]
           ,[Style]
           ,[Comment]
		   ,@UserID, GETDATE(), NULL, NULL, @ACTIVE_ROW_STATUS
		FROM mpm.Cells WHERE VFileID = @VFileID

	-- Update/Insert mpm.Backups table with state of backup as success
	-- NOTE: This has to be a UPDATE or INSERT as an earlier Backup attempt might have inserted a row
	IF EXISTS (SELECT 1 FROM mpm.WBBackups WHERE BackupID = @latestBackupID AND VFileID = @VFileID)
	BEGIN 
		UPDATE mpm.WBBackups SET 
			Code = 0,
			Reason = '',
			CreatedBy = @UserID,
			CreatedDate = GETDATE()      -- NOTE: Will not change the RStatus field, so if inactive will stay that way
		WHERE BackupID = @latestBackupID AND VFileID = @VFileID
	END
	ELSE
	BEGIN
		INSERT INTO [mpm].[WBBackups]
           ([VFileID]
           ,[BackupID]
           ,[Code]
           ,[Reason]
           ,[CreatedBy]
           ,[CreatedDate]
           ,[LastUpdatedBy]
           ,[LastUpdatedDate]
           ,[RStatus])
		VALUES(
			@VFileID,
			@latestBackupID,
			0,
			'',
			@UserID, GETDATE(), NULL, NULL, @ACTIVE_ROW_STATUS)
	END

	-- Update latest backup id in mpm.Workbooks table, edits will now use this new backup id
	UPDATE mpm.Workbooks SET LatestBackupID = @latestBackupID WHERE VFileID = @VFileID

	-- Note Backup event success in db log
	INSERT INTO mpm.DBLogs VALUES('Backup', 0, 'Success', '', NULL, NULL, 2, GETDATE())

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
	INSERT INTO mpm.DBLogs VALUES('Backup', @retCode, 'Failed', @message, NULL, NULL, 2, GETDATE())
	-- Update mpm.Backups table with state of backup & reason
	IF EXISTS (SELECT 1 FROM mpm.WBBackups WHERE BackupID = @latestBackupID AND VFileID = @VFileID)
	BEGIN 
		UPDATE mpm.WBBackups SET 
			Code = @retCode,
			Reason = @message,
			CreatedBy = @UserID,
			CreatedDate = GETDATE()      -- NOTE: Will not change the RStatus field, so if inactive will stay that way
		WHERE BackupID = @latestBackupID AND VFileID = @VFileID
	END
	ELSE
	BEGIN
		INSERT INTO [mpm].[WBBackups]
           ([VFileID]
           ,[BackupID]
           ,[Code]
           ,[Reason]
           ,[CreatedBy]
           ,[CreatedDate]
           ,[LastUpdatedBy]
           ,[LastUpdatedDate]
           ,[RStatus])
		VALUES(
			@VFileID,
			@latestBackupID,
			@retCode,
			@message,
			@UserID, GETDATE(), NULL, NULL, @ACTIVE_ROW_STATUS)
	END

END CATCH

	-- Remove backup-in-progress lock
	DECLARE @RC int
	EXECUTE @RC = mpm.spUpdateWBLock @UserID, @VFileID, @BACKUPINPROGRESS_LOCK_TYPE_ID, 0
	IF @RC != 0
	BEGIN 
		SET @retCode = -6
		SET @message = @message + 'Failed to remove backup-in-progress lock'  -- because an error msg maybe there already so keep it
	END

	SELECT @retCode, @message

END
GO
