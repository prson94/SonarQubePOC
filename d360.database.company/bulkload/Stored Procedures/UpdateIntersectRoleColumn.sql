
create procedure bulkload.UpdateIntersectRoleColumn
	@id int,
	@roleColumn int
as
begin
	set nocount on;
	update	T
	set		T.LookupObject = 'IntersectRole',
			T.LookupObjectID = S.ID
	from	LoadItemColumn T
			inner join IntersectRole S on lower(S.Name) = lower(T.Value) and T.ColumnIndex = @roleColumn and T.LoadID = @id
end