
create procedure bulkload.UpdateFusionConfigurationColumn
	@id int,
	@fusionConfigColumn int
as
begin
	set nocount on;
	update	T
	set		T.LookupObject = 'Fusion',
			T.LookupObjectID = S.ID
	from	LoadItemColumn T
			inner join Fusion S on lower(S.Name) = lower(T.Value) and T.ColumnIndex = @fusionConfigColumn and T.LoadID = @id
end