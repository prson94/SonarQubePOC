CREATE FUNCTION [utility].[GetFormattedFieldReferenceItemValue]
(
	@ReferenceItemID int,
	@ReferenceItemTypeID int	
)
RETURNS nvarchar(4000)
AS
BEGIN
	declare @formattedValue nvarchar(4000)
	declare @tokens table(ID int identity(1,1), Token nvarchar(100), Field nvarchar(100))
	declare @fieldValues table(Field nvarchar(100), Value nvarchar(4000))
	declare @displayFormat nvarchar(250)

	set @displayFormat = (select displayformat from [dbo].[ReferenceItemType] where ID = @ReferenceItemTypeID);

	set @formattedValue = @displayFormat

	while patindex('%{%',@formattedValue) > 0
	begin
		declare @txt nvarchar(100) = SUBSTRING(@formattedValue, patindex('%{%',@formattedValue), PATINDEX('%}%', @formattedValue))
		insert into @tokens Values (@txt, REPLACE(REPLACE(@txt,'{',''),'}',''))
		set @formattedValue = replace(@formattedValue, @txt, '')
	end

	insert into @fieldValues
		SELECT	Name,
				FormattedValue
		FROM	FieldWithRelation 
		WHERE	ObjectType = 'ReferenceItem' 
				and ObjectID = @ReferenceItemID

				
	insert into @fieldValues
		SELECT 'Code',
				Code
		FROM	ReferenceItem
		WHERE	ID = @ReferenceItemID

	declare @current int,
			@max int

	set @current = 1
	select @max = Max(ID) from @tokens
	set @formattedValue = @displayFormat

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



