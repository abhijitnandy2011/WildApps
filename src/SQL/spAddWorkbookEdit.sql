-- Save edit before processing. 
-- Caller must lock file on user's behalf first
-- Returns:
--   0 Success, edit was written
--  -1 Error
--   1 File is not locked for edit
--   2 File is locked for backup
-- 
-- Test:
-- UPDATE mpm.Locks SET Locked = 0 WHERE VFileID = 3 AND LockTypeID = 1
-- UPDATE mpm.Locks SET Locked = 0 WHERE VFileID = 3 AND LockTypeID = 2
-- select * from mpm.Locks
-- select * from mpm.LockTypes
-- EXEC mpm.spAddWorkbookEdit 4, 3, '{}', 0, 1, ''
--

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE mpm.spAddWorkbookEdit(	
    @UserID UDT_ID,
	@VFileID UDT_ID,
	@Json nvarchar(max),
	@ID1 UDT_ID,         -- Client side tracking id for this edit
	@AppliedCode UDT_ID,
	@Reason nvarchar(2048)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @retCode int = 0
	DECLARE @message nvarchar(200) = ''

	-- Check if file locked by user
	-- IMPORTANT: These should not change in mpm.LockTypes table, but querying it would be better
	DECLARE @EDIT_LOCK_TYPE_ID UDT_ID = 1   
	DECLARE @BACKUPINPROGRESS_LOCK_TYPE_ID UDT_ID = 2

	DECLARE @isFileLockedForBackup UDT_Bool = (SELECT Locked FROM mpm.Locks WHERE LockTypeID=@BACKUPINPROGRESS_LOCK_TYPE_ID AND VFileID = @VFileID)
	IF @isFileLockedForBackup = 1
	BEGIN
		SET @retCode = 2
		SET @message = 'File is locked for backup'
	END
	ELSE
	BEGIN
		DECLARE @isFileLockedForEdit UDT_Bool = (SELECT Locked FROM mpm.Locks WHERE LockTypeID=@EDIT_LOCK_TYPE_ID AND VFileID = @VFileID)
		IF @isFileLockedForEdit = 1
		BEGIN
			-- Get the latest backup ID for the file
			DECLARE @latestBackupID UDT_ID = (SELECT LatestBackUpID FROM mpm.Workbooks WHERE VFileID = @VFileID)
			-- TODO check if 0 rows and indicate error
			-- Add to Edits table
			INSERT INTO mpm.Edits VALUES(
				@VFileID, @latestBackupID, @Json, @ID1, @AppliedCode, @Reason, @UserID, GETDATE(),NULL, NULL, dbo.CONST('RSTATUS_ACTIVE'))
		END
		ELSE
		BEGIN
			SET @retCode = 1
			SET @message = 'File is not locked for edit'
		END
	END	

	SELECT @retCode, @message
END
GO
