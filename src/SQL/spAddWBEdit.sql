-- Save edit before processing. 
-- 
-- Returns:
--   0 Success, edit was written
--   1 Success, lock was transferred from another user
--  -1 File is not locked for edit
--  -2 File is locked for backup
-- 
-- Test:
-- UPDATE mpm.Locks SET Locked = 0 WHERE VFileID = 3 AND LockTypeID = 1   -- edit
-- UPDATE mpm.Locks SET Locked = 0, LastUpdatedBy=2, LastUpdatedDate=GETDATE() WHERE VFileID = 3 AND LockTypeID = 2   -- backup
-- select * from mpm.Locks
-- select * from mpm.LockTypes
-- EXEC mpm.spAddWBEdit 4, 3, '{}', 0, 1, ''
-- EXEC mpm.spAddWBEdit 3, 3, '{}', 0, 1, ''
-- select * from mpm.Edits
-- select * from mpm.WBEventLogs
-- select * from mpm.WBEventTypes

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



ALTER PROCEDURE mpm.spAddWBEdit(	
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
	DECLARE @lastEditID int = -1
	DECLARE @lockedByUser UDT_ID
	DECLARE @lastLockUpdatedTime UDT_DateTime

	-- CONSTANTS
	DECLARE @ACTIVE_ROW_STATUS int = 2
	-- LockType consts
	-- IMPORTANT: These should not change in mpm.LockTypes table, but querying it would be better
	DECLARE @EDIT_LOCK_TYPE_ID UDT_ID = 1   
	DECLARE @BACKUPINPROGRESS_LOCK_TYPE_ID UDT_ID = 2
	-- EventType consts
	DECLARE @EDIT_EVENT_TYPE_ID UDT_ID = 3   
	
	-- Check if file locked for backup, cant proceed to register a new edit if so
	DECLARE @lastUpdatedBy UDT_ID
	DECLARE @lastUpdatedDate UDT_DateTime
	DECLARE @isFileLockedForBackup UDT_Bool
	SELECT @isFileLockedForBackup=Locked, @lastUpdatedBy=LastUpdatedBy, @lastUpdatedDate=LastUpdatedDate
					FROM mpm.Locks WHERE LockTypeID=@BACKUPINPROGRESS_LOCK_TYPE_ID 
											AND VFileID=@VFileID AND RStatus=@ACTIVE_ROW_STATUS
	IF @isFileLockedForBackup = 1
	BEGIN
		SET @retCode = -2
		SET @message = 'File is locked for backup'
		SET @lockedByUser = @lastUpdatedBy
		SET @lastLockUpdatedTime = @lastUpdatedDate
	END
	ELSE
	BEGIN
	    -- Check if file is locked by the given user for edit, try to get the lock if not
		DECLARE @isFileLockedForEdit UDT_Bool = 0
		DECLARE @locked UDT_bool
		SELECT @locked=Locked, @lastUpdatedBy=LastUpdatedBy, @lastUpdatedDate=LastUpdatedDate FROM mpm.Locks 
			WHERE LockTypeID=@EDIT_LOCK_TYPE_ID AND VFileID=@VFileID AND RStatus=@ACTIVE_ROW_STATUS
		print @locked
		print @lastUpdatedBy
		print @lastUpdatedDate
		IF @locked = 0
		BEGIN
			-- File is not locked, give this user the lock rightaway
			EXEC mpm.spUpdateWBLock @UserID, @VFileID, @EDIT_LOCK_TYPE_ID, 1
			SET @isFileLockedForEdit = 1
			SET @lockedByUser = @UserID
			SET @lastLockUpdatedTime = GETDATE()
		END
		ELSE
		BEGIN
			-- File is locked
			-- Check if this user has the lock
			IF @lastUpdatedBy = @UserID
			BEGIN
				-- Need to lock again just to update the time so lock hold time is reset
				EXEC mpm.spUpdateWBLock @UserID, @VFileID, @EDIT_LOCK_TYPE_ID, 1
				SET @isFileLockedForEdit = 1
				SET @lockedByUser = @UserID
				SET @lastLockUpdatedTime = GETDATE()
			END
			ELSE
			BEGIN
				-- Some other user has the lock, check if lock can be transferred to this user
				DECLARE @lockHoldTime UDT_Number_int = (SELECT LockHoldTimeInSecs FROM mpm.Workbooks WHERE VFileID=@VFileID AND RStatus=@ACTIVE_ROW_STATUS)
				DECLARE @hasLockLapsed UDT_bool = (SELECT CASE WHEN DATEDIFF(ss, @lastUpdatedDate, GETDATE()) > @lockHoldTime THEN 1 ELSE 0 END )
				-- Has sufficient time elapsed?
				IF @hasLockLapsed = 1
				BEGIN
					-- Yes lock time has elapsed, so transfer the lock to this user
					EXEC mpm.spUpdateWBLock @UserID, @VFileID, @EDIT_LOCK_TYPE_ID, 1
					SET @isFileLockedForEdit = 1
					SET @lockedByUser = @UserID
					SET @lastLockUpdatedTime = GETDATE()
					SET @retCode = 1
					SET @message = 'Lock was transferred from user:' + CAST(@lastUpdatedBy AS nvarchar(10))
				END
				ELSE
				BEGIN
					-- Lock time has not elapsed yet, cant lock
					SET @isFileLockedForEdit = 0
					SET @lockedByUser = @lastUpdatedBy
					SET @lastLockUpdatedTime = @lastUpdatedDate
				END				
			END			
		END		
		
		-- Check if above code was able to lock the file finally or not
		IF @isFileLockedForEdit = 1
		BEGIN
			-- Get the latest backup ID for the file
			DECLARE @latestBackupID UDT_ID = (SELECT LatestBackUpID FROM mpm.Workbooks WHERE VFileID = @VFileID)
			SET @lastEditID = (SELECT MAX(EditID) + 1 FROM mpm.Edits WHERE VFileID = 3 GROUP BY VFileID)
			SET @lastEditID = (SELECT ISNULL(@lastEditID,1))
			print @lastEditID
			-- TODO check if 0 rows and indicate error
			-- Add to Edits table			
			INSERT INTO [mpm].[Edits]
					   ([VFileID]
					   ,[EditID]
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
				  ,@lastEditID
			 	  ,@latestBackupID
				  ,@Json
				  ,@TrackingID
				  ,@Code
				  ,@Reason
				  ,@UserID
				  ,GETDATE()
				  ,NULL, NULL, @ACTIVE_ROW_STATUS)
			
			-- Add to Event log for this workbook file
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
			SET @message = 'File could not be locked for edit'
		END
	END	

	SELECT @lastEditID AS EditID,
	       @lockedByUser AS LockedBy, 
		   @lastLockUpdatedTime AS LastLockedTime,
		   @retCode AS RetCode, 
		   @message AS Message
END
GO
