
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE dbo.logMsg
	@Module UDT_Name,
	@Code UDT_ID,
	@Msg UDT_Name_Med,
	@Description UDT_LogDescription = '',
	@User int = 2,
	@ObjectID1       UDT_ID_Opt = NULL,
	@ObjectID2       UDT_ID_Opt = NULL,
	@ObjectID3       UDT_ID_Opt = NULL
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	INSERT INTO dbo.SysLogs VALUES
	(
	@Module,
	@Code,
	@Msg,
	@Description,	
	@ObjectID1,
	@ObjectID2,
	@ObjectID3,
	@User,
	GETDATE()
	)

END
GO
