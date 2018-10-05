if not exists (select 1 from [predicate] where name = 'Asset Owned For' and [Type] = 7 and IsSystem = 1)
begin
	insert into [predicate] values('Asset Owned For','Asset Owned By',7,1,0)
end
GO

if not exists (select 1 from [predicate] where name = 'Validates' and [Type] = 7 and IsSystem = 1)
begin
    insert into [predicate] values('Validates','Is Validated By',7,1,0)
end
GO