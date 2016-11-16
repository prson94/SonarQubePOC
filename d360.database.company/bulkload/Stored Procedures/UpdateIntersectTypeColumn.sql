create procedure [bulkload].[UpdateIntersectTypeColumn]
	@id int,
	@column int
as
begin
	set nocount on;
	update	T
	set		T.LookupObject = 'IntersectType',
			T.LookupObjectID = S.ID
	from	LoadItemColumn T
			inner join IntersectType S on lower(S.Name) = lower(T.Value) and T.ColumnIndex = @column and T.LoadID = @id
end