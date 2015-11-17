CREATE FUNCTION utility.DeriveIntersectTypeNameWrapper
(
	@id int
)
RETURNS nvarchar(500)
AS
BEGIN
	RETURN utility.DeriveIntersectTypeName(@id)
END