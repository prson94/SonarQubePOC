CREATE FUNCTION utility.GetBreadcrumbWrapper
(
	@Type varchar(50),
	@ID int
)
RETURNS XML
AS
BEGIN
	RETURN utility.GetBreadcrumb(@Type, @ID)
END