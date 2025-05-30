-- Save edit before processing. 
-- Caller must lock file on user's behalf first
-- Returns:
--   0 Success, edit was written
--  -1 File is not locked for edit
--  -2 File is locked for backup
-- 
-- Test:
-- UPDATE mpm.Locks SET Locked = 1 WHERE VFileID = 3 AND LockTypeID = 1   -- edit
-- UPDATE mpm.Locks SET Locked = 0 WHERE VFileID = 3 AND LockTypeID = 2   -- backup
-- select * from mpm.Locks
-- select * from mpm.LockTypes
-- EXEC mpm.spAddWBEdit 4, 3, '{}', 0, 1, ''
-- select * from mpm.Edits
-- select * from mpm.WBEventLogs
-- select * from mpm.WBEventTypes

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE mpm.spAddWBEdit(	
    @UserID UDT_ID,
	@VFileID UDT_ID,
	@Json nvarchar(max),
	@TrackingID UDT_ID,         -- Client side tracking id for this edit
	@Code UDT_ID,
	@Reason nvarchar(2048)
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
	
	-- Check if file locked for backup, cant proceed to register a new edit if so
	DECLARE @isFileLockedForBackup UDT_Bool = (SELECT Locked FROM mpm.Locks WHERE LockTypeID=@BACKUPINPROGRESS_LOCK_TYPE_ID AND VFileID = @VFileID)
	IF @isFileLockedForBackup = 1
	BEGIN
		SET @retCode = -2
		SET @message = 'File is locked for backup'
	END
	ELSE
	BEGIN
	    -- Check if file is locked by the given user for edit - file must be locked by caller for edit
		DECLARE @isFileLockedForEdit UDT_Bool = (SELECT Locked FROM mpm.Locks WHERE LockTypeID=@EDIT_LOCK_TYPE_ID AND VFileID = @VFileID)
		IF @isFileLockedForEdit = 1
		BEGIN
			-- Get the latest backup ID for the file
			DECLARE @latestBackupID UDT_ID = (SELECT LatestBackUpID FROM mpm.Workbooks WHERE VFileID = @VFileID)
			-- TODO check if 0 rows and indicate error
			-- Add to Edits table			
			INSERT INTO [mpm].[Edits]
					   ([VFileID]
					   ,[BackupID]
					   ,[Json]
					   ,[TrackingID]
					   ,[Code]
					   ,[Reason]
					   ,[CreatedBy]
					   ,[CreatedDate]
					   ,[LastUpdatedBy]
					   ,[LastUpdatedDate]
					   ,[RStatus])
			VALUES
			      (@VFileID
			 	  ,@latestBackupID
				  ,@Json
				  ,@TrackingID
				  ,@Code
				  ,@Reason
				  ,@UserID
				  ,GETDATE()
				  ,NULL, NULL, @ACTIVE_ROW_STATUS)
			-- Add to Event log for this workbook file
			DECLARE @lastEditID int = (SELECT SCOPE_IDENTITY())
			print @lastEditID
			INSERT INTO [mpm].[WBEventLogs]
					   ([VFileID]
					   ,[EventTypeID]
					   ,[BackupID]
					   ,[ID1]
					   ,[CreatedBy]
					   ,[CreatedDate]
					   ,[LastUpdatedBy]
					   ,[LastUpdatedDate]
					   ,[RStatus])
				 VALUES
					   (@VFileID
					   ,@EDIT_EVENT_TYPE_ID
					   ,@latestBackupID
					   ,@lastEditID
					   ,@UserID
					   ,GETDATE(),
					   NULL, NULL, @ACTIVE_ROW_STATUS)
		END
		ELSE
		BEGIN
			SET @retCode = -1
			SET @message = 'File is not locked for edit'
		END
	END	

	SELECT @retCode, @message
END
GO
