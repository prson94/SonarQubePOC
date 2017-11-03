
create FUNCTION [utility].[GenerateFormattedMultipleValue]
	-- Add the parameters for the stored procedure here	
	(@DisplayFormat nvarchar(250),
	@LookupObjectType varchar(25),
	@LookupObjectID int,
	@Value nvarchar(max))	
RETURNS nvarchar(max)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	
	declare @currentValue nvarchar(1000);	
	declare @FormattedValue nvarchar(max);

	--print 'Display Format is :' + @DisplayFormat;

	set @FormattedValue = '';

	-- split the values
	declare cursor1 cursor read_only for SELECT value FROM STRING_SPLIT(@Value, ',') WHERE RTRIM(value) <> '';  

	open cursor1

	fetch next from cursor1 into @currentValue;
	
	while @@fetch_status = 0
	begin
		--print @currentValue

		if @FormattedValue != ''
		begin
			set @FormattedValue = @FormattedValue + ',';
		end
		
		set @FormattedValue = @FormattedValue + utility.GetFormattedFieldLookupValueWithMultiple('Lookup', @DisplayFormat, @LookupObjectType, @LookupObjectID, @currentValue,0);

		fetch next from cursor1 into @currentValue
	end

	close cursor1

	deallocate cursor1

	--print @FormattedValue

	return @FormattedValue
	
END