
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE dbo.logAuthError
	@Module UDT_Name,
	@ErrorMsg UDT_Name_Med,
	@Description UDT_LogDescription = '',
	@User int = 2
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	INSERT INTO dbo.AuthLogs VALUES
	(
	@Module,
	@ErrorMsg,
	@Description,
	@User,
	GETDATE()
	)

END
GO
