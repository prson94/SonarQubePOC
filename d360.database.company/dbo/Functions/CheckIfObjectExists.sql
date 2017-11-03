CREATE function dbo.CheckIfObjectExists
(
	@ObjectType varchar(50), -- = 'ArtifactType'
	@ObjectTypeID int, -- = 1
	@ObjectID int, -- = 4651
	@Fields nvarchar(max) -- = '[{"id": 53072, "value":"Country Of Risk"}, {"id": 53096, "value":"Description for Country Of Risk"}]'
)
returns bit
as
begin
	declare @exists bit = 0

	declare @tbl table (ID int, Value nvarchar(max))

	insert into @tbl
		select	F.*
		from	openjson(@Fields) with (ID int 'strict $.ID', Value nvarchar(max) '$.Value') as F
				inner join FieldType T on T.ID = F.ID and T.Object = @ObjectType and T.ObjectID = @ObjectTypeID and T.IsPartOfKey = 1

	declare @results table (ID int, ObjectID int)
	insert into @results
		select	T.ID,
				F.ObjectID 
		from	@tbl T
				left join Field F on F.FieldTypeID = T.ID and F.Value = T.Value and ( (@ObjectID is null) OR (@ObjectID is not null and F.ObjectID <> @ObjectID) )

	if exists(select 1 from @results)
		begin
			if exists(select 1 from @results where ObjectID is null)
				begin
					set	@exists = 0
				end
			else
				begin
					set @exists = 1
				end
		end
	else
		begin
			set @exists = 0
		end

	return @exists
end