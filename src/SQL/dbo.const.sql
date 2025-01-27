-- ================================================
-- Template generated from Template Explorer using:
-- Create Scalar Function (New Menu).SQL
--
-- Use the Specify Values for Template Parameters 
-- command (Ctrl-Shift-M) to fill in the parameter 
-- values below.
--
-- This block of comments will not be included in
-- the definition of the function.
-- ================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
ALTER FUNCTION dbo.CONST
(
	-- Add the parameters for the function here
	@paramName nvarchar(100)
)
RETURNS int
AS
BEGIN
	-- Declare the return variable here
	DECLARE @constVal int

	-- Add the T-SQL statements to compute the return value here
	SELECT @constVal = case @paramName
		-- VUsers
		when 'VUSERS_SYSTEM'   then 1
		when 'VUSERS_ADMIN'    then 2

		-- RStatus
		when 'RSTATUS_INACTIVE'   then 1
		when 'RSTATUS_ACTIVE'     then 2

		-- SystemFolderFiles.VObjectType
		-- when 'VOBJECTTYPE_FOLDER'   then 1
		-- when 'VOBJECTTYPE_FILE'     then 2
		-- when 'VOBJECTTYPE_LINK'     then 3
		end

	-- Return the result of the function
	RETURN @constVal

END
GO

