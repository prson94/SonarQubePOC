CREATE FUNCTION [dbo].[GenerateAPIKeyWrapper]
(
    @Length int
)
RETURNS varchar(max)
AS
BEGIN
	DECLARE @RandomID varchar(max)
	SELECT @RandomID = dbo.GenerateAPIKey(@Length)
	RETURN @RandomID
END