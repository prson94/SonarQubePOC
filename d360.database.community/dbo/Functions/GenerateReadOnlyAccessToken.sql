CREATE FUNCTION [dbo].[GenerateReadOnlyAccessToken]
(
)
RETURNS varchar(50)
AS
BEGIN
	declare @Length int = 50
	declare @RandomID varchar(50) = ''
	declare @counter smallint = 1
	declare @RandomNumber float = 0
	declare @RandomNumberInt tinyint = 0
	declare @CurrentCharacter varchar(1) = ''
	declare @ValidCharacters varchar(255) = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789'
	declare @ValidCharactersLength int = len(@ValidCharacters)

	WHILE @counter <= (@Length)
	BEGIN
		SELECT @RandomNumber = r FROM RandomNumber
		SET @RandomNumberInt = Convert(tinyint, ((@ValidCharactersLength - 1) * @RandomNumber + 1))

		SELECT @CurrentCharacter = SUBSTRING(@ValidCharacters, @RandomNumberInt, 1)

		SET @counter = @counter + 1

		SET @RandomID = @RandomID + @CurrentCharacter
	END
	
	RETURN @RandomID
END