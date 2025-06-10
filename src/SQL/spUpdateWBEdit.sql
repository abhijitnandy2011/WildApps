-- Update edit code and reason 
-- 
-- Test:
-- EXEC mpm.spUpdateWBEdit 4, 3, 1, 0, 'Done'
-- select * from mpm.Edits
--

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



ALTER PROCEDURE mpm.spUpdateWBEdit(	
    @UserID UDT_ID,
	@VFileID UDT_ID,
	@EditID UDT_ID,         -- ID from DB 
	@Code UDT_ID,
	@Reason nvarchar(2048)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	-- CONSTANTS
	DECLARE @ACTIVE_ROW_STATUS int = 2
			
	UPDATE [mpm].[Edits]
	SET 
		Code = @Code,
		Reason = @Reason,
		LastUpdatedBy = @UserID,
		LastUpdatedDate = GETDATE()
	WHERE
		VFileID=@VFileID AND EditID = @EditID AND RStatus=@ACTIVE_ROW_STATUS			
			
END
GO
