CREATE procedure [bulkload].[UpdateItemColumnByType]
	@id int,
	@ObjectType varchar(50), 
	@ObjectTypeID int,
	@subjectAreaColumn int, 
	@itemColumn int
as
begin
	set nocount on;

	if @ObjectType = 'ArtifactType'
	begin
			update	T
			set		T.LookupObject = replace(@ObjectType, 'Type', ''),
					T.LookupObjectID = S.ID
			from	LoadItemColumn T
					inner join LoadItemColumn TS on TS.LoadID = T.LoadID and TS.RowIndex = T.RowIndex and TS.ColumnIndex = @subjectAreaColumn
					inner join Artifact S on lower(S.TextPath) = lower(T.Value) and S.TaxonomyTypeID = TS.LookupObjectID and S.ArtifactTypeID = @ObjectTypeID
			where	T.LoadID = @id
	end
	if @ObjectType = 'FusionAttributeType'
	begin
		update	T
		set		T.LookupObject = replace(@ObjectType, 'Type', ''),
				T.LookupObjectID = S.ID
		from	LoadItemColumn T
				left join FusionAttribute S on lower(S.TextPath) = lower(T.Value) and S.FusionAttributeTypeID = @ObjectTypeID
		where	T.LoadID = @id
				and T.ColumnIndex = @itemColumn
	end
	if @ObjectType = 'IntersectType'
	begin
		update	T
		set		T.LookupObject = replace(@ObjectType, 'Type', ''),
				T.LookupObjectID = S.ID
		from	LoadItemColumn T
				left join [Intersect] S on lower(S.Name) = lower(T.Value) and S.IntersectTypeID = @ObjectTypeID
		where	T.LoadID = @id
				and T.ColumnIndex = @itemColumn
	end
	if @ObjectType = 'MapType'
	begin
		update	T
		set		T.LookupObject = replace(@ObjectType, 'Type', ''),
				T.LookupObjectID = S.ID
		from	LoadItemColumn T
				left join [Map] S on lower(S.Name) = lower(T.Value) and S.MapTypeID = @ObjectTypeID
		where	T.LoadID = @id
				and T.ColumnIndex = @itemColumn
	end
	if @ObjectType = 'PolicyType'
	begin
		update	T
		set		T.LookupObject = replace(@ObjectType, 'Type', ''),
				T.LookupObjectID = S.ID
		from	LoadItemColumn T
				left join [Policy] S on lower(S.TextPath) = lower(T.Value) and S.PolicyTypeID = @ObjectTypeID
		where	T.LoadID = @id
				and T.ColumnIndex = @itemColumn
	end
	if @ObjectType = 'ReferenceItemType' and @ObjectTypeID = 0
	begin
		update	T
		set		T.LookupObject = @ObjectType,
				T.LookupObjectID = S.ID
		from	LoadItemColumn T
				left join ReferenceItemType S on lower(S.Name) = lower(T.Value)
		where	T.LoadID = @id
				and T.ColumnIndex = @itemColumn
	end
	if @ObjectType = 'ReferenceItemType' and @ObjectTypeID > 0
	begin
		update	T
		set		T.LookupObject = replace(@ObjectType, 'Type', ''),
				T.LookupObjectID = S.ID
		from	LoadItemColumn T
				left join ReferenceItem S on lower(S.DisplayValue) = lower(T.Value) and S.ReferenceItemTypeID = @ObjectTypeID
		where	T.LoadID = @id
				and T.ColumnIndex = @itemColumn
	end
	if @ObjectType = 'RuleType'
	begin
		update	T
		set		T.LookupObject = replace(@ObjectType, 'Type', ''),
				T.LookupObjectID = S.ID
		from	LoadItemColumn T
				left join [Rule] S on lower(S.Name) = lower(T.Value) and S.RuleTypeID = @ObjectTypeID
		where	T.LoadID = @id
				and T.ColumnIndex = @itemColumn
	end
	if @ObjectType = 'TaxonomyType'
	begin
		update	T
		set		T.LookupObject = replace(@ObjectType, 'Type', ''),
				T.LookupObjectID = S.ID
		from	LoadItemColumn T
				left join Taxonomy S on lower(S.TextPath) = lower(T.Value) and S.TaxonomyTypeID = @ObjectTypeID
		where	T.LoadID = @id
				and T.ColumnIndex = @itemColumn
	end
end
GO

