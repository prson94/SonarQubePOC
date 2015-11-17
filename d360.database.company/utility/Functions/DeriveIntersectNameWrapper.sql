CREATE FUNCTION utility.DeriveIntersectNameWrapper
(
	@id int
)
RETURNS nvarchar(500)
AS
BEGIN
	RETURN utility.DeriveIntersectName(@id)
END