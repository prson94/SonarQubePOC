create FUNCTION utility.ObjectFields
(	
	-- Add the parameters for the function here
	@Object varchar(50),
	@ObjectID int
)
RETURNS TABLE 
AS
RETURN 
(
	SELECT	FT.Name as 'Field',
				F.FormattedValue as 'Value'
		FROM	Field F
				inner join FieldType FT on FT.ID = F.FieldTypeID and F.ObjectType = @Object and F.ObjectID = @ObjectID
)