CREATE procedure [bulkload].[UpdateItemColumn]
	@id int,
	@globalTypeColumn int, 
	@typeColumn int, 
	@subjectAreaColumn int, 
	@itemColumn int
as
begin
	set nocount on;
	update	T
	set		T.LookupObject = TTT.Value,
			T.LookupObjectID = coalesce(A.ID, D.ID, I.ID, P.ID, R.ID, TA.ID)
	from	LoadItemColumn T
			inner join LoadItemColumn TT on TT.LoadID = T.LoadID and T.LoadID = @id and TT.RowIndex = T.RowIndex and TT.ColumnIndex = @typeColumn and T.ColumnIndex = @itemColumn
			inner join LoadItemColumn TS on TS.LoadID = T.LoadID and TS.RowIndex = T.RowIndex and TS.ColumnIndex = @subjectAreaColumn
			inner join LoadItemColumn TTT on TTT.LoadID = T.LoadID and TTT.RowIndex = T.RowIndex and TTT.ColumnIndex = @globalTypeColumn
			left join Artifact A on lower(A.TextPath) = lower(T.Value) and A.TaxonomyTypeID = TS.LookupObjectID and A.ArtifactTypeID = TT.LookupObjectID and TTT.Value = 'Artifact'
			left join Domain D on lower(D.Name) = lower(T.Value) and D.DomainTypeID = TT.LookupObjectID and TTT.Value = 'Domain'
			left join [Intersect] I on lower(I.Name) = lower(T.Value) and I.IntersectTypeID = TT.LookupObjectID and TTT.Value = 'Intersect'
			left join [Policy] P on lower(P.TextPath) = lower(T.Value) and P.PolicyTypeID = TT.LookupObjectID and TTT.Value = 'Policy'
			left join [Rule] R on lower(R.Name) = lower(T.Value) and R.RuleType = TT.LookupObjectID and TTT.Value = 'Rule'
			left join [Taxonomy] TA on lower(TA.TextPath) = lower(T.Value) and TA.TaxonomyTypeID = TT.LookupObjectID and TTT.Value = 'Taxonomy'
	where	coalesce(A.ID, D.ID, I.ID, P.ID, R.ID, TA.ID) is not null
end