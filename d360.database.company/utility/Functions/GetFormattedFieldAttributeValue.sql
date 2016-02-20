


CREATE FUNCTION [utility].[GetFormattedFieldAttributeValue]
(
--declare
	@AttributeID int,-- = 67,
	@DisplayFormat nvarchar(250)-- = '{Position1}, {Position2}, {Position3}, {Position4}'
)
RETURNS nvarchar(4000)
AS
BEGIN
	declare @formattedValue nvarchar(4000),
			@tDisplayFormat nvarchar(250)
	declare @tokens table(ID int identity(1,1), pos int, Token nvarchar(100), Field nvarchar(100))
	declare @fieldValues table(Field nvarchar(100), Value nvarchar(4000))

	set @formattedValue = @DisplayFormat
	SET @tDisplayFormat = @DisplayFormat

	Declare @pos int
	Declare @oldpos int
	Select @oldpos=0
	select @pos=patindex('%{%',@DisplayFormat) 
	while @pos > 0 and @oldpos<>@pos
	 begin
		declare @txt nvarchar(100)
		SELECT @txt = SUBSTRING(@tDisplayFormat, @pos, PATINDEX('%}%', @tDisplayFormat))

		insert into @tokens Values (@pos, @txt, SUBSTRING(@txt, 2, LEN(@txt)-2))
		Select @oldpos = @pos
		select @pos = patindex('%{%',Substring(@DisplayFormat, @pos + 1, len(@DisplayFormat))) + @pos
	end

	insert into @fieldValues
		SELECT	Name,
				FormattedValue
		FROM	FieldWithRelation 
		WHERE	ObjectType = 'Attribute' 
				and ObjectID = @AttributeID
--select * from @fieldValues
--select * from @tokens

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
		else
		begin
			SET @formattedValue = REPLACE(@formattedValue, @currentToken, '')
		end

		SET @current = @current + 1
	end

	return @formattedValue
END