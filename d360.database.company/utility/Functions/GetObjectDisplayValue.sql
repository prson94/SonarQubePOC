CREATE FUNCTION utility.GetObjectDisplayValue
(
	@Object varchar(50),
	@ObjectID int,
	@ObjectTypeID int	
)
RETURNS nvarchar(max)
AS
BEGIN
	declare @formattedValue nvarchar(max)
	declare @tokens table(ID int identity(1,1), Token nvarchar(100), Field nvarchar(100))
	declare @fieldValues table(Field nvarchar(100), Value nvarchar(max))
	declare @displayFormat nvarchar(250)

	if @Object = 'Artifact'
	begin
		set @displayFormat = (select DisplayFormat from [ArtifactType] where ID = @ObjectTypeID);
	end
	if @Object = 'Attribute'
	begin
		set @displayFormat = (select DisplayFormat from [AttributeType] where ID = @ObjectTypeID);
	end
	if @Object = 'FusionQueryAttribute'
	begin
		set @displayFormat = (select DisplayFormat from FusionQueryAttributeType where ID = @ObjectTypeID);
	end
	if @Object = 'Policy'
	begin
		set @displayFormat = (select DisplayFormat from [PolicyType] where ID = @ObjectTypeID);
	end
	if @Object = 'ReferenceItem'
	begin
		set @displayFormat = (select DisplayFormat from [ReferenceItemType] where ID = @ObjectTypeID);

		insert into @fieldValues
			SELECT 'Code',
					Code
			FROM	ReferenceItem
			WHERE	ID = @ObjectID
	end
	if @Object = 'Rule'
	begin
		set @displayFormat = (select DisplayFormat from [RuleType] where ID = @ObjectTypeID);
	end
	if @Object = 'Taxonomy'
	begin
		set @displayFormat = (select DisplayFormat from [TaxonomyType] where ID = @ObjectTypeID);
	end

	set @formattedValue = @displayFormat

	while patindex('%{%',@formattedValue) > 0
	begin
		declare @txt nvarchar(100) = SUBSTRING(@formattedValue, patindex('%{%',@formattedValue), PATINDEX('%}%', @formattedValue))
		insert into @tokens Values (@txt, REPLACE(REPLACE(@txt,'{',''),'}',''))
		set @formattedValue = replace(@formattedValue, @txt, '')
	end

	insert into @fieldValues
		SELECT	FT.Name,
				F.FormattedValue
		FROM	Field F
				inner join FieldType FT on FT.ID = F.FieldTypeID and F.ObjectType = @Object and F.ObjectID = @ObjectID
	
	declare @current int,
			@max int

	set @current = 1
	select @max = Max(ID) from @tokens
	set @formattedValue = @displayFormat

	while(@current <= @max)
	begin
		declare @currentToken nvarchar(100) = null,
				@currentField nvarchar(100) = null,
				@currentValue nvarchar(max) = null,
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