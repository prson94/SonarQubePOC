
CREATE FUNCTION [utility].[DeriveIntersectTypeName] 
(
--declare
	@id int
--set @id = 67
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
									FA.Name,
									II.Name,
									G.Name,
									R.Name,
									P.Name,
									RE.Name,
									''
									) + ' / '
					FROM	IntersectTypeNode I
							LEFT OUTER JOIN ArtifactType A							ON	I.ObjectType = 'ArtifactType'			and A.ID = I.ObjectID
							LEFT OUTER JOIN [IntersectType] II						ON	I.ObjectType = 'IntersectType'			and II.ID = I.ObjectID and II.ID <> @id
							LEFT OUTER JOIN TaxonomyType T							ON	I.ObjectType = 'TaxonomyType'			and T.ID = I.ObjectID
							LEFT OUTER JOIN DomainType D							ON	I.ObjectType = 'DomainType'				and D.ID = I.ObjectID
							LEFT OUTER JOIN FusionAttributeType FA					ON  I.ObjectType = 'FusionAttributeType'	and FA.ID = I.ObjectID
							LEFT OUTER JOIN (select 'Group' as Name, 0 as ID) G		ON	I.ObjectType = 'Group'					and G.ID = I.ObjectID
							LEFT OUTER JOIN (select 'Resource' as Name, 1 as ID) RE	ON	I.ObjectType = 'Resource'				and RE.ID = I.ObjectID
							LEFT OUTER JOIN (
											select 1 as ID, 'Informational Rule' as Name
											union
											select 2 as ID, 'Quality Check Rule' as Name
											union
											select 3 as ID, 'Metric Rule' as Name
											union
											select 4 as ID, 'Profile Rule' as Name
											) R										ON	I.ObjectType = 'Rule'					and R.ID = I.ObjectID
							LEFT OUTER JOIN [PolicyType] P							ON	I.ObjectType = 'PolicyType'				and P.ID = I.ObjectID
					WHERE	I.IntersectTypeID = @id
							and @@NESTLEVEL < 6
					ORDER BY I.[Order]
					FOR XML PATH('')
					)

	IF @Result IS NULL 
		SET @result = 'Name cannot be resolved'
	ELSE
		SET @result = SUBSTRING(@result, 1, LEN(@result) - 2)

	RETURN @result
END
