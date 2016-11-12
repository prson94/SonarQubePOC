
CREATE procedure [bulkload].[MergeDynamicLookupFields]
	@id int,
	@startColumnIndex int,
	@endColumnIndex int
as
begin
	set nocount on;

	drop table if exists #Fields

	create table #Fields (
		FieldTypeID int,
		Object varchar(25),
		ObjectID int,
		Value nvarchar(max)
	)

	insert into #Fields
		select	distinct
				FT.ID as FieldTypeID,
				I.[Object],
				I.ObjectID,
				IC.LookupObjectID
		from	LoadItem I
				inner join [Load] L on L.ID = I.LoadID and I.LoadID = @id and I.ObjectID is not null
				inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex between @startColumnIndex and @endColumnIndex
				inner join LoadItemColumn IC on IC.LoadID = C.LoadID and I.RowIndex = IC.RowIndex and IC.ColumnIndex = C.ColumnIndex and IC.LookupObjectID is not null
				inner join FieldType FT on FT.[Object] = L.Object and FT.ObjectID = L.ObjectID and FT.Name = C.Name
	
	insert into #Fields
		select	distinct
				FT.ID as FieldTypeID,
				I.[Object],
				I.ObjectID,
				case 
					when FT.[Type] = 'Boolean' and LOWER(IC.Value) in ('y', 'yes', 'true', 't', '1') then 'true'
					when FT.[Type] = 'Boolean' and LOWER(IC.Value) not in ('y', 'yes', 'true', 't', '1') then 'false'
					else IC.Value
				end as Value
		from	LoadItem I
				inner join [Load] L on L.ID = I.LoadID and I.LoadID = @id and I.ObjectID is not null
				inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex between @startColumnIndex and @endColumnIndex
				inner join LoadItemColumn IC on IC.LoadID = C.LoadID and I.RowIndex = IC.RowIndex and IC.ColumnIndex = C.ColumnIndex and IC.LookupObjectID is null
				inner join FieldType FT on FT.[Object] = L.Object and FT.ObjectID = L.ObjectID and FT.Name = C.Name and FT.[Type] <> 'Lookup'

	--update existing fields
	update	T
	set		T.Value = S.Value
	from	Field T
			inner join #Fields S on S.FieldTypeID = T.FieldTypeID and S.Object = T.ObjectType and S.ObjectID = T.ObjectID;
	
	-- insert new fields
	insert into Field (ObjectType, ObjectID, FieldTypeID, Value)
		select	S.Object, S.ObjectID, S.FieldTypeID, max(S.Value)
		from	#Fields S
				left join Field T on T.FieldTypeID = S.FieldTypeID and T.ObjectType = S.Object and T.ObjectID = S.ObjectID
		where	T.FieldTypeID is null
		group by S.Object, S.ObjectID, S.FieldTypeID;
end