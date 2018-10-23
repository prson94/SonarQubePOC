if not exists (select 1 from [predicate] where name = 'Asset Owned For' and [Type] = 7 and IsSystem = 1)
begin
	insert into [predicate] (Name, Inverse,[Type],IsSystem,Code, [Uid]) values('Asset Owned For','Asset Owned By',7,1,0, '2A7FA12D-63AA-4595-83D0-CFA98AAC2AA4')
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

-- BEGIN GOV-5718 DUPLICATED COMMENTS DUE TO DUPLICATED USERS IN ASSET TABLE
-- delete duplicated resources in asset table
with R as (
select *, row_number() over(partition by [object], objectid order by (select null)) as rn
from asset where [object] = 'Resource'
)
delete R
where rn > 1;
go

-- add constraint
ALTER TABLE Asset ADD CONSTRAINT UC_Asset_Object_ObjectID UNIQUE ([Object],[ObjectID]);
go
-- END GOV-5718 DUPLICATED COMMENTS DUE TO DUPLICATED USERS IN ASSET TABLE