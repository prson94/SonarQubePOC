CREATE VIEW dbo.FieldTypeLookupValue
AS
	SELECT	'Artifact' as LookupObjectType,
			ID as LookupObjectID,
			Name--'Artifact : ' + Name as Name
	FROM	ArtifactType
	UNION
	SELECT	'Domain' as LookupObjectType,
			ID as LookupObjectID,
			Name --'Domain : ' + Name as Name
	FROM	DomainType
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
