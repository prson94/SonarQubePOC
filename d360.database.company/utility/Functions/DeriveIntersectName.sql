CREATE FUNCTION [utility].[DeriveIntersectName] 
(	
	@id int
)
RETURNS nvarchar(500)
AS
BEGIN
	DECLARE @result nvarchar(500)

	SET @result =	(
					SELECT	COALESCE(
									A.Name,
									T.Name,
									D.Name, 
									E.Name,
									FA.Name,
									II.Name,
									G.Name,
									R.Name,
									P.Name,
									RE.LastName + ', ' + RE.FirstName,
									''
									) + ' / '
					FROM	IntersectNode I
							INNER JOIN IntersectTypeNode IT							ON  I.IntersectTypeNodeID = IT.ID
							LEFT OUTER JOIN Artifact A								ON	I.ObjectType = 'Artifact'			and A.ID = I.ObjectID
							LEFT OUTER JOIN [Intersect] II							ON	I.ObjectType = 'Intersect'			and II.ID = I.ObjectID and II.ID <> @id
							LEFT OUTER JOIN Taxonomy T								ON	I.ObjectType = 'Taxonomy'			and T.ID = I.ObjectID
							LEFT OUTER JOIN Domain D								ON	I.ObjectType = 'Domain'				and D.ID = I.ObjectID
							LEFT OUTER JOIN [EventType] E							ON  I.ObjectType = 'EventType'			and E.ID = I.ObjectID  --EventType is used here b/c this is the actual object you create an intersect to.
							LEFT OUTER JOIN FusionAttribute FA						ON  I.ObjectType = 'FusionAttribute'	and FA.ID = I.ObjectID
							LEFT OUTER JOIN [Group] G								ON	I.ObjectType = 'Group'				and G.ID = I.ObjectID
							LEFT OUTER JOIN reporting.Global_Resource RE			ON	I.ObjectType = 'Resource'			and RE.ResourceID = I.ObjectID
							LEFT OUTER JOIN [Rule] R								ON	I.ObjectType = 'Rule'				and R.ID = I.ObjectID
							LEFT OUTER JOIN [Policy] P								ON	I.ObjectType = 'Policy'				and P.ID = I.ObjectID
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

