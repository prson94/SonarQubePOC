CREATE procedure [bulkload].[UpdateItemColumnByIntersectType]
	@id int,
	@intersectTypeColumn int, 
	@isSubject bit,
	@subjectAreaColumn int, 
	@itemColumn int
as
begin
	set nocount on;
	update	T
	set		T.LookupObject = replace(case when @isSubject = 1 then IT.Subject else IT.Object end, 'Type', ''),
			T.LookupObjectID = coalesce(A.ID, D.ID, F.ID, I.ID, P.ID, R.ID, TA.ID)
	from	LoadItemColumn T
			inner join LoadItemColumn TI on TI.LoadID = T.LoadID and TI.RowIndex = T.RowIndex and TI.ColumnIndex = @intersectTypeColumn and T.ColumnIndex = @itemColumn
			inner join IntersectType IT on TI.LookupObject = 'IntersectType' and IT.ID = TI.LookupObjectID
			left join LoadItemColumn TS on TS.LoadID = T.LoadID and TS.RowIndex = T.RowIndex and TS.ColumnIndex = @subjectAreaColumn
			left join Artifact A on lower(A.TextPath) = lower(T.Value) and A.TaxonomyTypeID = TS.LookupObjectID and A.ArtifactTypeID = case when @isSubject = 1 then IT.SubjectID else IT.ObjectID end and 'ArtifactType' = case when @isSubject = 1 then IT.Subject else IT.Object end
			left join Domain D on lower(D.Name) = lower(T.Value) and D.DomainTypeID = case when @isSubject = 1 then IT.SubjectID else IT.ObjectID end and 'DomainType' = case when @isSubject = 1 then IT.Subject else IT.Object end
			left join FusionAttribute F on lower(F.TextPath) = lower(T.Value) and F.FusionAttributeTypeID = case when @isSubject = 1 then IT.SubjectID else IT.ObjectID end and 'FusionAttributeType' = case when @isSubject = 1 then IT.Subject else IT.Object end
			left join [Intersect] I on lower(I.Name) = lower(T.Value) and I.IntersectTypeID = case when @isSubject = 1 then IT.SubjectID else IT.ObjectID end and 'IntersectType' = case when @isSubject = 1 then IT.Subject else IT.Object end
			left join [Policy] P on lower(P.TextPath) = lower(T.Value) and P.PolicyTypeID = case when @isSubject = 1 then IT.SubjectID else IT.ObjectID end and 'PolicyType' = case when @isSubject = 1 then IT.Subject else IT.Object end
			left join [Rule] R on lower(R.Name) = lower(T.Value) and R.RuleType = case when @isSubject = 1 then IT.SubjectID else IT.ObjectID end and 'RuleType' = case when @isSubject = 1 then IT.Subject else IT.Object end
			left join [Taxonomy] TA on lower(TA.TextPath) = lower(T.Value) and TA.TaxonomyTypeID = case when @isSubject = 1 then IT.SubjectID else IT.ObjectID end and 'TaxonomyType' = case when @isSubject = 1 then IT.Subject else IT.Object end
	where	T.LoadID = @id and coalesce(A.ID, D.ID, F.ID, I.ID, P.ID, R.ID, TA.ID) is not null
end