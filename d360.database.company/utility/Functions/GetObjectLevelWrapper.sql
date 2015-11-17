CREATE FUNCTION utility.GetObjectLevelWrapper
(
	@Type varchar(50),
	@ID int
)
RETURNS int
AS
BEGIN
	RETURN utility.GetObjectLevel(@Type, @ID)
END