CREATE view [dbo].[StatisticTypeCheckOption]
as
	select	'ArtifactType' as ObjectType,
			ID AS ObjectID,
			Name,
			'Artifact' as NamePrefix
	from	ArtifactType
	union
	select	'AttributeType' as ObjectType,
			ID AS ObjectID,
			Name,
			'Attribute' as NamePrefix
	from	AttributeType
	union
	select	'ReferenceItemType' as ObjectType,
			0 AS ObjectID,
			'Reference List',
			'ReferenceItemType' as NamePrefix
	union
	select	'IntersectType' as ObjectType,
			ID AS ObjectID,
			Name,
			'Relationship' as NamePrefix
	from	IntersectType
	union
	select	'ResponsibilityType' as ObjectType,
			ID AS ObjectID,
			Name,
			'Ownership' as NamePrefix
	from	ResponsibilityType
	union
	select	'TaxonomyType' as ObjectType,
			ID AS ObjectID,
			Name,
			'Information Model' as NamePrefix
	from	TaxonomyType