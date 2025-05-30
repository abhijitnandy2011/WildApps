-- Update workbook lock 
-- Lock entries for a wb are created when a workbook is created
-- Returns:
--   0 Success, edit was written
--  -1 Error
-- 
-- Test:
-- UPDATE mpm.Locks SET Locked = 1 WHERE VFileID = 3 AND LockTypeID = 1   -- edit
-- UPDATE mpm.Locks SET Locked = 0 WHERE VFileID = 3 AND LockTypeID = 2   -- backup
-- select * from mpm.Locks
-- select * from mpm.LockTypes
-- EXEC mpm.spUpdateWBLock UserID=4, 3, 1, 0
--

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE mpm.spUpdateWBLock(	
    @UserID UDT_ID,
	@VFileID UDT_ID,
	@LockTypeID UDT_ID,
	@Lock bit -- 1 to lock, 0 to unlock
)
AS
BEGIN
	SET NOCOUNT ON;

	UPDATE mpm.Locks SET 
		Locked = @Lock,
		LastUpdatedBy = @UserID,
		LastUpdatedDate = GETDATE()
	WHERE VFileID = @VFileID AND LockTypeID = @LockTypeID 

	SELECT 0
END
GO
