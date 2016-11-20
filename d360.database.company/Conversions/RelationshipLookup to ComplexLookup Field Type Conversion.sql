/*
{
	"Relations":[
		{"IntersectTypeID":2,"Object":"ArtifactType","ObjectID":2,"RelationType":1},
		{"IntersectTypeID":8269,"Object":"FusionAttributeType","ObjectID":205,"RelationType":2},
		{"IntersectTypeID":179,"Object":"FusionAttributeType","ObjectID":301,"RelationType":1}
	],
	"Fields":[
		{"Object":"ArtifactType","ObjectID":2,"FieldTypeID":0,"FieldTypeName":"Name","Filter":null,"OverrideDisplayName":null,"DisplayOrder":1,"SortOrder":0},
		{"Object":"ArtifactType","ObjectID":2,"FieldTypeID":0,"FieldTypeName":"Description","Filter":null,"OverrideDisplayName":null,"DisplayOrder":2,"SortOrder":0},
		{"Object":"ArtifactType","ObjectID":2,"FieldTypeID":52286,"FieldTypeName":"Relation.NumberTest","Filter":null,"OverrideDisplayName":null,"DisplayOrder":8,"SortOrder":0},
		{"Object":"FusionAttributeType","ObjectID":205,"FieldTypeID":51621,"FieldTypeName":"XmlTag","Filter":null,"OverrideDisplayName":null,"DisplayOrder":3,"SortOrder":0},
		{"Object":"FusionAttributeType","ObjectID":205,"FieldTypeID":51622,"FieldTypeName":"StarTag","Filter":null,"OverrideDisplayName":null,"DisplayOrder":5,"SortOrder":0},
		{"Object":"FusionAttributeType","ObjectID":205,"FieldTypeID":0,"FieldTypeName":"TextPath","Filter":null,"OverrideDisplayName":"Field","DisplayOrder":4,"SortOrder":2},
		{"Object":"FusionAttributeType","ObjectID":301,"FieldTypeID":0,"FieldTypeName":"Name","Filter":null,"OverrideDisplayName":"Mnemonic","DisplayOrder":2,"SortOrder":0}
	]
}
*/
declare @IDs table(ID int identity, FieldTypeID int, ReferenceType int, HideHeader bit, HideFooter bit)

insert into @IDs
	select	FieldTypeID,
			ReferenceType,
			HideHeader,
			HideFooter
	from	FieldTypeRelationLookupDefinition
	--where	ReferenceType = 2

declare @CurrentID int = 1,
		@MaxID int,
		@FieldTypeID int,
		@ReferenceType int,
		@Definition nvarchar(max),
		@HideHeader bit,
		@HideFooter bit,
		@HideFilter bit = 0
select @MaxID = max(ID) from @IDs

while @CurrentID <= @MaxID 
begin
	select	@FieldTypeID = FieldTypeID,
			@ReferenceType = ReferenceType,
			@HideHeader = HideHeader,
			@HideFooter = HideFooter
	from	@IDs
	where	ID = @CurrentID

	if @ReferenceType = 1
		begin
			set @Definition = (
				select	(
						select	D.IntersectTypeID,
								case when FT.Object = IT.Subject and FT.ObjectID = IT.SubjectID then IT.Object else IT.Subject end as Object,
								case when FT.Object = IT.Subject and FT.ObjectID = IT.SubjectID then IT.ObjectID else IT.SubjectID end as ObjectID,
								1 as RelationType
						from	FieldTypeRelationLookupDefinition D
								inner join FieldType FT on FT.ID = D.FieldTypeID 
								inner join [IntersectType] IT on IT.ID = D.IntersectTypeID
						where	D.FieldTypeID = @FieldTypeID
						for json path, INCLUDE_NULL_VALUES
						) as Relations,
						(
						select	case when FT.Object = IT.Subject and FT.ObjectID = IT.SubjectID then IT.Object else IT.Subject end as Object,
								case when FT.Object = IT.Subject and FT.ObjectID = IT.SubjectID then IT.ObjectID else IT.SubjectID end as ObjectID,
								F.FieldTypeID,
								F.FieldTypeName,
								F.FilterValue as [Filter],
								null as OverrideDisplayName,
								coalesce(F.SortOrder,1) as DisplayOrder,
								coalesce(F.SortOrder,0) as SortOrder
						from	FieldTypeRelationLookupDefinition D
								inner join FieldTypeRelationLookupDisplayField F on F.FieldTypeRelationLookupDefinitionID = D.ID and F.Show = 1
								inner join FieldType FT on FT.ID = D.FieldTypeID 
								inner join [IntersectType] IT on IT.ID = D.IntersectTypeID
						where	D.FieldTypeID = @FieldTypeID
						for json path, INCLUDE_NULL_VALUES
						) as Fields
				for	json path, WITHOUT_ARRAY_WRAPPER
			)
		end
	else
		begin
			set @Definition = (
				select	(
						select	*
						from	(
								select	D.IntersectTypeID,
										case when FT.Object = IT.Subject and FT.ObjectID = IT.SubjectID then IT.Object else IT.Subject end as Object,
										case when FT.Object = IT.Subject and FT.ObjectID = IT.SubjectID then IT.ObjectID else IT.SubjectID end as ObjectID,
										1 as RelationType
								from	FieldTypeRelationLookupDefinition D
										inner join FieldType FT on FT.ID = D.FieldTypeID 
										inner join [IntersectType] IT on IT.ID = D.IntersectTypeID
								where	D.FieldTypeID = @FieldTypeID
								union
								select	D.ChildIntersectTypeID as IntersectTypeID,
										IT.Object,
										IT. ObjectID,
										2 as RelationType
								from	FieldTypeRelationLookupDefinition D
										inner join [IntersectType] IT on IT.ID = D.ChildIntersectTypeID
								where	D.FieldTypeID = @FieldTypeID
								) O
						for json path, INCLUDE_NULL_VALUES
						) as Relations,
						(
						select	case when FT.Object = IT.Subject and FT.ObjectID = IT.SubjectID then IT.Object else IT.Subject end as Object,
								case when FT.Object = IT.Subject and FT.ObjectID = IT.SubjectID then IT.ObjectID else IT.SubjectID end as ObjectID,
								F.FieldTypeID,
								F.FieldTypeName,
								F.FilterValue as [Filter],
								null as OverrideDisplayName,
								coalesce(F.SortOrder,1) as DisplayOrder,
								coalesce(F.SortOrder,0) as SortOrder
						from	FieldTypeRelationLookupDefinition D
								inner join FieldTypeRelationLookupDisplayField F on F.FieldTypeRelationLookupDefinitionID = D.ID and F.Show = 1
								inner join FieldType FT on FT.ID = D.FieldTypeID 
								inner join [IntersectType] IT on IT.ID = D.IntersectTypeID
						where	D.FieldTypeID = @FieldTypeID
						for json path, INCLUDE_NULL_VALUES
						) as Fields
				for	json path, WITHOUT_ARRAY_WRAPPER
			)
		end

	--select @Definition
	update	FieldType
	set		[Type] = 'ComplexRelationLookup'
	where	ID = @FieldTypeID

	insert into FieldTypeLookup values (@FieldTypeID, @HideHeader, @HideFooter, 0, @Definition, @HideFilter)

	delete	T
	from	FieldTypeRelationLookupDisplayField T
			inner join FieldTypeRelationLookupDefinition S on S.ID = T.FieldTypeRelationLookupDefinitionID and S.FieldTypeID = @FieldTypeID

	delete	FieldTypeRelationLookupDefinition
	where	FieldTypeID = @FieldTypeID

	set @CurrentID = @CurrentID + 1
end