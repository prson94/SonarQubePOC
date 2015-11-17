CREATE FUNCTION [utility].[DeriveIntersectTypeName] 
(
--declare
	@id int
--set @id = 1
)
RETURNS nvarchar(500)
AS
BEGIN
	DECLARE @result nvarchar(500)

	select	@result = case N.ObjectType when 'Rule' then 'Rule' else D.Name end 
	from	IntersectTypeNode N 
			left join cache.ObjectDetails D on D.[Object] = N.ObjectType and D.ObjectID = N.ObjectID
	where	N.IntersectTypeID = @id and N.[Order] = 1

	select	@result = @result + '/' + case N.ObjectType when 'Rule' then 'Rule' else D.Name end
	from	IntersectTypeNode N 
			left join cache.ObjectDetails D on D.[Object] = N.ObjectType and D.ObjectID = N.ObjectID
	where	N.IntersectTypeID = @id and N.[Order] = 2

	IF @Result IS NULL 
		SET @result = 'Name cannot be resolved'

	RETURN @result
END
