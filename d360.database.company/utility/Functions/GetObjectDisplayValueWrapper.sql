CREATE FUNCTION utility.GetObjectDisplayValueWrapper
(
	@Object varchar(50),
	@ObjectID int,
	@ObjectTypeID int	
)
RETURNS nvarchar(max)
AS
BEGIN
	RETURN utility.GetObjectDisplayValue(@Object, @ObjectID, @ObjectTypeID)
END