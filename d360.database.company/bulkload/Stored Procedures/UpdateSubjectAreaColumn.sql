
create procedure bulkload.UpdateSubjectAreaColumn
	@id int,
	@subjectAreaColumn int
as
begin
	set nocount on;
	update	T
	set		T.LookupObject = 'TaxonomyType',
			T.LookupObjectID = S.ID
	from	LoadItemColumn T
			inner join TaxonomyType S on lower(S.Name) = lower(T.Value) and T.ColumnIndex = @subjectAreaColumn and T.LoadID = @id
end