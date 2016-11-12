
CREATE procedure [bulkload].[UpdateItemColumnByType]
	@id int,
	@ObjectType varchar(50), 
	@ObjectTypeID int,
	@subjectAreaColumn int, 
	@itemColumn int
as
begin
	set nocount on;
	update	T
	set		T.LookupObject = replace(@ObjectType, 'Type', ''),
			T.LookupObjectID = coalesce(A.ID, D.ID, F.ID, I.ID, P.ID, R.ID, TA.ID)
	from	LoadItemColumn T
			left join LoadItemColumn TS on TS.LoadID = T.LoadID and TS.RowIndex = T.RowIndex and TS.ColumnIndex = @subjectAreaColumn and T.ColumnIndex = @itemColumn
			left join Artifact A on lower(A.TextPath) = lower(T.Value) and A.TaxonomyTypeID = TS.LookupObjectID and A.ArtifactTypeID = @ObjectTypeID and @ObjectType = 'ArtifactType'
			left join Domain D on lower(D.Name) = lower(T.Value) and D.DomainTypeID = @ObjectTypeID and @ObjectType = 'DomainType'
			left join FusionAttribute F on lower(F.TextPath) = lower(T.Value) and F.FusionAttributeTypeID = @ObjectTypeID and @ObjectType = 'FusionAttributeType'
			left join [Intersect] I on lower(I.Name) = lower(T.Value) and I.IntersectTypeID = @ObjectTypeID and @ObjectType = 'IntersectType'
			left join [Policy] P on lower(P.TextPath) = lower(T.Value) and P.PolicyTypeID = @ObjectTypeID and @ObjectType = 'PolicyType'
			left join [Rule] R on lower(R.Name) = lower(T.Value) and R.RuleType = @ObjectTypeID and @ObjectType = 'RuleType'
			left join [Taxonomy] TA on lower(TA.TextPath) = lower(T.Value) and TA.TaxonomyTypeID = @ObjectTypeID and @ObjectType = 'TaxonomyType'
	where	T.LoadID = @id and coalesce(A.ID, D.ID, F.ID, I.ID, P.ID, R.ID, TA.ID) is not null
end
GO

