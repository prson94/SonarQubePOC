CREATE FUNCTION [utility].[DeriveIntersectTypeName] 
(
--declare
	@id int
--set @id = 17
)
RETURNS nvarchar(500)
AS
BEGIN
	DECLARE @result nvarchar(500)

	SET @result =	(
					SELECT	COALESCE(S.TextPath, '') + ' / ' + COALESCE(O.TextPath, '')
					FROM	IntersectType I
							left join cache.ObjectDetails S on S.Object = I.Subject and S.ObjectID = I.SubjectID
							left join cache.ObjectDetails O on O.Object = I.Object and O.ObjectID = I.ObjectID
					WHERE	I.ID = @id
					FOR XML PATH('')
					)

	RETURN @result
END
