CREATE VIEW [dbo].[FieldTypeLookupValue]
AS
	SELECT	'Artifact' as LookupObjectType,
			ID as LookupObjectID,
			Name--'Artifact : ' + Name as Name
	FROM	ArtifactType
	UNION
	SELECT	'ReferenceItemType' as LookupObjectType,
			ID as LookupObjectID,
			Name --'Domain : ' + Name as Name
	FROM	ReferenceItemType
	UNION
	SELECT	'Taxonomy' as LookupObjectType,
			ID as LookupObjectID,
			Name --'Information Model : ' + Name as Name
	FROM	TaxonomyType
	UNION
	SELECT	'Lookup' as LookupObjectType,
			ID as LookupObjectID,
			Name --'Lookup : ' + Name as Name
	FROM	LookupType
