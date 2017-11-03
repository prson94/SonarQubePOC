CREATE FUNCTION [utility].[GetObjectHashWrapper]
(
	@Object varchar(50),
	@ObjectID int,
	@ObjectTypeID int,
	@KeyFieldOnly bit
)
RETURNS varchar(50)
AS
BEGIN
	return utility.GetObjectHash(@Object, @ObjectID, @ObjectTypeID, @KeyFieldOnly)
END