-- Backup a file
-- Calling process must set a lock on the file & ensure all edits applied before the backup is initiated

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE mpm.spDoFileBackup(	
	@VFileID UDT_ID
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	-- Check if file has lock on it to prevent further edits while backup in progress


	-- Backup sheets

	-- Backup Products


	-- Backup Product Types

	-- Backup Cells

	-- Backup MSeries

	-- Backup MRanges

	-- Backup MTables
    
	
END
GO
