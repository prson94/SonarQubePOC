CREATE FUNCTION [workflow].[ConditionToPlainText] 
(
	@ConditionXml xml	
)
RETURNS varchar(500)
AS
BEGIN	
	DECLARE @PlainText varchar(500) = '';
	DECLARE @Value varchar(500) = '';
	DECLARE @Operator varchar(500) = '';
	DECLARE @FieldName varchar(500) = '';
	DECLARE @FieldTypeID int;

	SELECT 
		@FieldTypeID = Child.value('(Condition[1]/@FieldTypeID)', 'int'),
		@Operator = Child.value('(Condition[1])/@Operator', 'Varchar(50)'),
		@Value = Child.value('(Condition[1])/@Value', 'Varchar(50)')
	FROM
		@ConditionXml.nodes('/Conditions') AS N(Child);


	if (@FieldTypeID > 0)
	begin
		select @FieldName = FriendlyName from fieldtype where id = @FieldTypeID;
	end

	RETURN @FieldName + ' ' +  @Operator + ' ' + @Value;
END