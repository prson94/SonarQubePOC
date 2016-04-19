

--create procedure [dbo].[GetRelationships]
----declare
--	@ObjectType varchar(50),
--	@ObjectID int
----set @ObjectType = 'Artifact'
----set @ObjectID = 4651
--as
--begin
--	IF OBJECT_ID('tempdb..#Relates') IS NOT NULL
--		DROP TABLE #Relates;

--	create table #Relates (
--		IntersectID int, 
--		ObjectType varchar(50), 
--		ObjectID int, 
--		ObjectName nvarchar(1000),
--		TypeName nvarchar(250),
--		Url nvarchar(2000),
--		ConcatValue varchar(65)
--	);

--	CREATE NONCLUSTERED INDEX IX_TempRelates ON #Relates (ConcatValue ASC);

--	--Intersect loading
--	insert into #Relates
--		select	R.IntersectID,
--				R.TargetObject as ObjectType,
--				R.TargetObjectID as ObjectID,
--				coalesce(D.TextPath, R.TargetObjectName) as Name,
--				R.TargetTypeName as TypeName,
--				dbo.GenerateObjectUrl(R.TargetObject, R.TargetTypeID, R.TargetObjectID) Url,
--				R.TargetObject + cast(R.TargetObjectID as varchar(15))
--		from	cache.Relationships R
--				left join cache.ObjectDetails D on D.[Object] = R.TargetObject and D.ObjectID = R.TargetObjectID
--		where	R.SourceObject = @ObjectType
--				and R.SourceObjectID = @ObjectID
	
--	if (@ObjectType <> 'Intersect')
--	begin
--		--Source loading
--		insert into #Relates
--			select	NULL as IntersectID,
--					R.ResponsibleObjectType,
--					R.ResponsibleObjectID,
--					R.ResponsibleObjectName,
--					ROD.ObjectTypeName as TypeName,
--					ROD.Url,
--					NULL
--			from	SourcingResponsibilityDetail R
--					inner join cache.ObjectDetails ROD on ROD.[Object] = R.ResponsibleObjectType and ROD.ObjectID = R.ResponsibleObjectID --cross apply utility.ObjectDetail(R.ResponsibleObjectType, R.ResponsibleObjectID) ROD
--			where	R.ObjectType = @ObjectType 
--					and R.ObjectID = @ObjectID
--					and R.ResponsibleObjectType + cast(R.ResponsibleObjectID as varchar(15)) not in (select ObjectType + cast(ObjectID as varchar(15)) from #Relates)
--	end

--	-- Return the results to client.
--	select		IntersectID, 
--				ObjectType, 
--				ObjectID, 
--				ObjectName,
--				TypeName,
--				Url
--	from		#Relates
--	order by	TypeName,
--				ObjectName
--end

--GO


-- add column CreatedOn to artifact table not nullable default to current_timestamp
alter table [Artifact] add CreatedOn datetime not null constraint DF_Artifact_CreatedOn default(CURRENT_TIMESTAMP)
go

-- DISABLE TRIGGER SO WE DONT ADD A TON OF RECORDS TO UPDATE THINGS IN THE QUEUE
ALTER TABLE [Artifact] DISABLE TRIGGER Artifact_AfterUpdate
go

-- update all created on to 1/1/2011 so they all dont show up as new
update [Artifact] set CreatedOn = '1/1/2011';

-- update all createdon dates with the updatedon date if the exist
update [Artifact] set CreatedOn = UpdatedOn where UpdatedOn is not null;

-- go to audit table and get items created date and use this.
UPDATE
	artifact
SET
    artifact.CreatedOn = a.[date]
FROM
    [dbo].[artifact] at
INNER JOIN
    [reporting].[Global_Audit] a
ON 
    (a.[object] = 'Artifact' and a.actionobject = 'Artifact' and a.actionobjectid = at.id and a.objectid = at.id and a.[action] = 'Created');


-- REENABLE TRIGGER AFTER UPDATES

ALTER TABLE [Artifact] ENABLE TRIGGER Artifact_AfterUpdate
go


-- 4/19/16 Added dimension to rules
-- ALSO UPDATE - [dbo].[GetRenderedTemplateBody] PROC  and get create table script for [dbo].[RuleDimension]

-- add column RuleDimensionID to [dbo].[rule]
ALTER TABLE [dbo].[Rule] ADD RuleDimensionID int NULL

-- add fk so that rule dimension corresponds to an existing rule dimension
ALTER TABLE [dbo].[Rule]
add constraint FK_Rule_RuleDimension FOREIGN KEY ( [RuleDimensionID] ) references [dbo].[RuleDimension] ([ID])






