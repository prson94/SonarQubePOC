CREATE PROCEDURE [dbo].[GetIntersectTypeOptions]
AS
BEGIN
	SELECT		I.ID,
				I.Name,
				I.Type
	FROM		(
				SELECT	ID,
						'Artifact - ' + Name AS Name,
						'ArtifactType' AS Type
				FROM	ArtifactType
				UNION
				SELECT	ID,
						'Domain - ' + Name AS Name,
						'DomainType' AS Type
				FROM	DomainType
				UNION
				SELECT	A.ID,
						'Fusion Attribute - ' + A.TextPath AS Name,
						'FusionAttributeType' AS Type
				FROM	FusionAttributeType A
						INNER JOIN FusionType T ON A.FusionTypeID = T.ID
				UNION
				SELECT	1 as ID,
						'Group' as Name,
						'GroupType' as Type
				UNION
				SELECT	ID,
						'Model - ' + Name AS Name,
						'TaxonomyType' AS Type
				FROM	TaxonomyType
				UNION
				SELECT	0 as ID,
						'Policy' as Name,
						'Policy' as Type
				UNION
				SELECT	CAST(ID as int) ID,
						'Relationship Type - ' + Name AS Name,
						'IntersectType' AS Type
				FROM	IntersectType
				UNION
				SELECT	1 as ID,
						'Resource' as Name,
						'ResourceType' as Type
				UNION
				SELECT	0 as ID,
						'Rule' as Name,
						'Rule' as Type
				) I
	ORDER BY	I.Name
END
