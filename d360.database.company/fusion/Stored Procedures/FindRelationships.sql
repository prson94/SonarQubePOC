CREATE PROCEDURE [Fusion].[FindRelationships]
as
begin
	set NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	exec [Fusion].[FindEagleToDBRelationships]
	
end
