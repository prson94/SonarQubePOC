CREATE FUNCTION utility.GetResponsibilityContextHashWrapper
(
	@ID int
)
RETURNS varchar(50)
AS
BEGIN
	RETURN utility.GetResponsibilityContextHash(@ID)
END