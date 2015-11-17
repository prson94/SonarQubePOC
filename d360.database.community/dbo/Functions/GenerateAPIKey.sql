CREATE FUNCTION [dbo].[GenerateAPIKey]
(
    @Length int
)
RETURNS varchar(max)
AS
BEGIN
	--SET OPTION ON
	--SET NOCOUNT ON
	
	DECLARE @RandomID varchar(max)
	DECLARE @counter smallint
	DECLARE @RandomNumber float
	DECLARE @RandomNumberInt tinyint
	DECLARE @CurrentCharacter varchar(1)
	DECLARE @ValidCharacters varchar(255)
	SET @ValidCharacters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-=+$!'
	DECLARE @ValidCharactersLength int
	SET @ValidCharactersLength = len(@ValidCharacters)
	SET @CurrentCharacter = ''
	SET @RandomNumber = 0
	SET @RandomNumberInt = 0
	SET @RandomID = ''

	SET @counter = 1

	WHILE @counter < (@Length + 1)
		BEGIN
			SELECT @RandomNumber = r FROM RandomNumber
			SET @RandomNumberInt = Convert(tinyint, ((@ValidCharactersLength - 1) * @RandomNumber + 1))

			SELECT @CurrentCharacter = SUBSTRING(@ValidCharacters, @RandomNumberInt, 1)

			SET @counter = @counter + 1

			SET @RandomID = @RandomID + @CurrentCharacter
		END
	
	RETURN @RandomID
END