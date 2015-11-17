CREATE procedure [dbo].[GetAllocationOptions]
as
begin
	select	*
	from	(
			select	'ArtifactType' as ObjectType,
					ID as ObjectTypeID, 
					'Artifacts :: ' + Name as Name
			from	ArtifactType
			union
			select	'DomainType' as ObjectType,
					ID as ObjectTypeID, 
					'Domains :: ' + Name as Name
			from	DomainType
			union
			select	'TaxonomyType' as ObjectType,
					ID as ObjectTypeID, 
					'Information Models :: ' + Name as Name
			from	TaxonomyType
			union
			select	'IntersectType' as ObjectType,
					ID as ObjectTypeID, 
					'Relationships :: ' + Name as Name
			from	IntersectType
			union
			select	'FusionType' as ObjectType,
					ID as ObjectTypeID, 
					'Fusion Types :: ' + Name as Name
			from	FusionType
			union
			select	'FusionAttributeType' as ObjectType,
					ID as ObjectTypeID, 
					'Fusion Attributes :: ' + TextPath as Name
			from	FusionAttributeType
			union
			select	'PolicyType' as ObjectType,
					0 as ObjectTypeID, 
					'Policies and Rules' as Name
			) O
	order by Name
end

