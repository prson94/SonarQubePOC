CREATE FUNCTION utility.GetBreadcrumbStringWrapper
(
	@Type varchar(50),
	@ID int,
	@Delimiter varchar(10)
)
RETURNS nvarchar(1000)
AS
BEGIN
	RETURN utility.GetBreadcrumbString(@Type, @ID, @Delimiter)
END