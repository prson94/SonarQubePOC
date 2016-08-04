CREATE FUNCTION [utility].[DeriveIntersectName] 
(	
	@id int
)
RETURNS nvarchar(500)
AS
BEGIN
	DECLARE @result nvarchar(500)

	SET @result =	(
					SELECT	COALESCE(S.TextPath, '') + ' / ' + COALESCE(O.TextPath, '')
					FROM	[Intersect] I
							left join cache.ObjectDetails S on S.[Object] = I.Subject and S.ObjectID = I.SubjectID
							left join cache.ObjectDetails O on O.[Object] = I.Object and O.ObjectID = I.ObjectID
					WHERE	I.ID = @id
					FOR XML PATH('')
					)

	RETURN @result
END

