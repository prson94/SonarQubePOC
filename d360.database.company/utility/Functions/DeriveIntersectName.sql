CREATE FUNCTION [utility].[DeriveIntersectName] 
(	
	@id int
)
RETURNS nvarchar(500)
AS
BEGIN
	DECLARE @result nvarchar(500)

	SET @result =	(
					SELECT	COALESCE(D.TextPath, '') + ' / '
					FROM	IntersectNode I
							INNER JOIN IntersectTypeNode IT							ON  I.IntersectTypeNodeID = IT.ID
							left join cache.ObjectDetails D on D.[Object] = I.ObjectType and D.ObjectID = I.ObjectID
					WHERE	I.IntersectID = @id
							AND EXISTS(SELECT 1 FROM IntersectNode WHERE IntersectID = @id)
							and @@NESTLEVEL < 6
					ORDER BY IT.[Order]
					FOR XML PATH('')
					)

	IF @Result IS NULL 
		SET @result = 'Name cannot be resolved'
	ELSE
		SET @result = SUBSTRING(@result, 1, LEN(@result) - 2)

	RETURN @result
END

