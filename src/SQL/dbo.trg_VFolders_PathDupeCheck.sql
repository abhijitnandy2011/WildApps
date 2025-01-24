CREATE TRIGGER dbo.trg_VFolders_PathDupeCheck
ON dbo.VFolders
INSTEAD OF INSERT
AS
BEGIN
    DECLARE @inPath nvarchar(max) = (SELECT Path from inserted)
	--print(@inPath)

	DECLARE @existingCount int = (SELECT COUNT(*) FROM dbo.VFolders WHERE Path = @inPath)
	IF (@existingCount > 0)
	BEGIN
		DECLARE @msg nvarchar(max) = 'The path already exists:' + @inPath
		RAISERROR (@msg, 10,11)
	END
	ELSE
	BEGIN
		DECLARE @Name UDT_Name_Big                = (SELECT Name FROM inserted)
		DECLARE @Attrs UDT_Name                   = (SELECT Attrs FROM inserted)
		DECLARE @Path UDT_Path                    = (SELECT Path FROM inserted)
		DECLARE @PathIDs UDT_Path                 = (SELECT PathIDs FROM inserted)
		DECLARE @CreatedBy UDT_ID                 = (SELECT CreatedBy FROM inserted)
		DECLARE @CreatedDate UDT_DateTime         = (SELECT CreatedDate FROM inserted)
		DECLARE @LastUpdatedBy UDT_ID_Opt         = (SELECT LastUpdatedBy FROM inserted)
		DECLARE @LastUpdatedDate UDT_DateTime_Opt = (SELECT LastUpdatedDate FROM inserted)
		DECLARE @RStatus UDT_RowStatus            = (SELECT RStatus FROM inserted)

		INSERT INTO dbo.VFolders VALUES
		(
			@Name,
			@Attrs,
			@Path,
			@PathIDs,
			@CreatedBy,
			@CreatedDate,
			@LastUpdatedBy,
			@LastUpdatedDate,
			@RStatus
		)
	END

END