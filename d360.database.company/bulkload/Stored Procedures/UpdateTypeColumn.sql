
CREATE procedure [bulkload].[UpdateTypeColumn]
	@id int,
	@typeColumn int,
	@typeNameColumn int
as
begin
	set nocount on;
	update	T2
	set		T2.LookupObject = T1.Value + 'Type',
			T2.LookupObjectID = coalesce(A.ID, D.ID, P.ID, T.ID, R.ID)
	from	LoadItemColumn T2
			inner join LoadItemColumn T1 on T1.LoadID = T2.LoadID and T1.RowIndex = T2.RowIndex and T1.ColumnIndex = @typeColumn and T2.LoadID = @id and T2.ColumnIndex = @typeNameColumn
			left join ArtifactType A on lower(A.Name) = lower(T2.Value) and T1.Value = 'Artifact'
			left join DomainType D on lower(D.Name) = lower(T2.Value) and T1.Value = 'Domain'
			left join IntersectType I on lower(I.Name) = lower(T2.Value) and T1.Value = 'Intersect'
			left join PolicyType P on lower(P.Name) = lower(T2.Value) and T1.Value = 'Policy'
			left join TaxonomyType T on lower(T.Name) = lower(T2.Value) and T1.Value = 'Taxonomy'
			left join	(
						select 1 as ID, 'informational' as Name
						union
						select 2 as ID, 'quality check' as Name
						union
						select 3 as ID, 'metric' as Name
						union
						select 4 as ID, 'profile' as Name
						) R on lower(R.Name) = lower(T2.Value) and T1.Value = 'Rule'	
end