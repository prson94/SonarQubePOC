CREATE procedure utility.GetFieldTypeLookupList
--declare 
	@type varchar(50), --= 'ArtifactType',
	@id int --= 1
as
begin
	select	type,
			value,
			title 
	from	utility.GetIntersectTypesByType(@type, @id)

	union

	select	'A' as type,
			'AttributeType|' + cast(ID as varchar) as value,
			Name as title
	from	AttributeType
	where	ParentID is null

	union

	select	'F' as type,
			'FusionAttributeType|' + cast(ID as varchar) as value,
			TextPath as title
	from	FusionAttributeType

	union

	SELECT	'L' as type,
			'Artifact|' + cast(ID as varchar) as value,
			'Artifact : ' + Name as title
	FROM	ArtifactType
	UNION
	SELECT	'L' as type,
			'ReferenceItemType|0'  as value,
			'Reference List' as title
	UNION
	SELECT	'L' as type,
			'ReferenceItem|' + cast(ID as varchar) as value,
			'Reference List Item: ' + Name as title
	FROM	ReferenceItemType
	UNION
	SELECT	'L' as type,
			'Resource|1' as value,
			'Resource : User' as title
	UNION
	SELECT	'L' as type,
			'Taxonomy|' + cast(ID as varchar) as value,
			'Information Model : ' + Name as title
	FROM	TaxonomyType
	UNION
	SELECT	'L' as type,
			'Lookup|' + cast(ID as varchar) as value,
			'Lookup : ' + Name as title
	FROM	LookupType

	union

	select	'FL' as type,
			'Lookup|' + cast(L.ID as varchar) as value,
			L.Name as title
	from	LookupType L
			cross apply (
						select	count(1) as [Count]
						from	FieldType
						where	Object = 'LookupType' 
								and ObjectID = L.ID
								and [Type] = 'Lookup'
								and LookupObjectType = REPLACE(@type, 'Type','') 
								and LookupObjectID = @id
						) F
	where	F.[Count] > 0
end