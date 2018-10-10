if not exists (select 1 from [predicate] where name = 'Asset Owned For' and [Type] = 7 and IsSystem = 1)
begin
	insert into [predicate] values('Asset Owned For','Asset Owned By',7,1,0)
end
GO;

if not exists (select 1 from [predicate] where name = 'Validates' and [Type] = 7 and IsSystem = 1)
begin
    insert into [predicate] values('Validates','Is Validated By',7,1,0)
end
GO;


update ruleresultqualifiertype set name = name +cast(id as varchar(20)) where id in (
SELECT max(id)
FROM ruleresultqualifiertype
GROUP BY ruleimplementationid, name
HAVING COUNT(1) > 1)

ALTER TABLE ruleresultqualifiertype ADD CONSTRAINT DF_RuleResultQualifierType_RuleImplementationID_Name UNIQUE(RuleImplementationID, Name)
