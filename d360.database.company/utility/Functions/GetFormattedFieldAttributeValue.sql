CREATE FUNCTION utility.GetFormattedFieldAttributeValue
(
	@AttributeID int,
	@DisplayFormat nvarchar(250)	
)
RETURNS nvarchar(4000)
AS
BEGIN
	declare @formattedValue nvarchar(4000),
			@tDisplayFormat nvarchar(250)
	declare @tokens table(ID int identity(1,1), Token nvarchar(100), Field nvarchar(100))
	declare @fieldValues table(Field nvarchar(100), Value nvarchar(4000))

	set @formattedValue = @DisplayFormat
	SET @tDisplayFormat = @DisplayFormat
	
	WHILE(PATINDEX('%{%', @tDisplayFormat) > 0)
	BEGIN
		declare @txt nvarchar(100)
		SELECT @txt = SUBSTRING(@tDisplayFormat, PATINDEX('%{%', @tDisplayFormat), PATINDEX('%}%', @tDisplayFormat))
		IF NOT EXISTS(SELECT 1 FROM @tokens WHERE Token = @txt)
		BEGIN
			INSERT INTO @tokens VALUES (
				@txt,
				(select SUBSTRING(@txt, 2, LEN(@txt)-2))
			)
		END
		SET @tDisplayFormat = stuff(@tDisplayFormat, charindex(@txt, @tDisplayFormat), len(@txt), '')
	END

	insert into @fieldValues
		SELECT	Name,
				FormattedValue
		FROM	FieldWithRelation 
		WHERE	ObjectType = 'Attribute' 
				and ObjectID = @AttributeID

	declare @current int,
			@max int

	set @current = 1
	select @max = Max(ID) from @tokens

	while(@current <= @max)
	begin
		declare @currentToken nvarchar(100) = null,
				@currentField nvarchar(100) = null,
				@currentValue nvarchar(4000) = null,
				@lkpType nvarchar(250) = null, 
				@lkpID int = null, 
				@lkpFormat nvarchar(250) = null

		select	@currentField = Field, 
				@currentToken = Token 
		from	@tokens
		where	ID = @current

		select	@currentValue = Value
		from	@fieldValues 
		where	Field = @currentField

		if @currentValue is not null
		begin
			SET @formattedValue = REPLACE(@formattedValue, @currentToken, @currentValue)
		end

		SET @current = @current + 1
	end

	return @formattedValue
END