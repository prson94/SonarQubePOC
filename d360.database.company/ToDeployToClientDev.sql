-- lineage.GetByObject -----------------------------
ALTER procedure [lineage].[GetByObject]
--declare
	@Object varchar(50),-- = 'Artifact',
	@ObjectID int-- = 1101
as
begin
	--Hold the raw lineage records.
	declare @tbl table (IntersectID int, IntersectTypeID int, 
						Subject varchar(50), SubjectID int, Object varchar(50), ObjectID int, [State] int, 
						PredicateID int, PredicateName nvarchar(250), PredicateInverse nvarchar(250), PredicateType int, 
						IntersectGroupID int null
						)

	-- Get the direct lineage going backward from the provided object.
	insert into @tbl
		select	L.*,
				G.IntersectGroupID 
		from	lineage.GetTrailForObject(@Object, @ObjectID, 0) L	
				left join IntersectGroupItem G on G.IntersectID = L.IntersectID

	-- Get the direct lineage going foreward from the provided object.
	insert into @tbl
		select	L.*,
				G.IntersectGroupID 
		from	lineage.GetTrailForObject(@Object, @ObjectID, 1) L	
				left join IntersectGroupItem G on G.IntersectID = L.IntersectID

	-- Hold the intersect IDs that are part of an IntersectGroup from one of the retrieved intersects above.
	declare @groupIntersects table (IntersectGroupID int, IntersectID int)

	-- Get the intersects that are part of an IntersectGroup from one of intersects above, but not yet pulled back in the temp table (i.e. does not exist in the lineage)
	insert into @groupIntersects
		select	GI.IntersectGroupID,
				GI.IntersectID
		from	@tbl O
				inner join IntersectGroupItem GI on GI.IntersectGroupID = O.IntersectGroupID and GI.IntersectID not in (select IntersectID from @tbl)

	-- Get the intersect record itself, for each ID pulled back as part of the group query above.
	insert into @tbl
		select	P.*,
				G.IntersectGroupID
		from	PredicateIntersect P
				inner join @groupIntersects G on G.IntersectID = P.IntersectID

	-- Go back for each group intersectID retrieved above and get backward-facing lineage, that is not already present in the lineage @tbl
	insert into @tbl
		select	Src.*,
				null
		from	PredicateIntersect P
				inner join @groupIntersects G on G.IntersectID = P.IntersectID
				cross apply lineage.GetTrailForObject(P.Subject, P.SubjectID, 0) Src
		where	Src.IntersectID not in (select IntersectID from @tbl)


	-- Return the full results to the caller.
	select	distinct
			I.IntersectID,
			I.IntersectGroupID,
			T.IntersectTypeID,
			SA.ID as SubjectAssetID,
			I.Subject,
			I.SubjectID,
			utility.GetAssetDisplayValueWrapper(SA.ID) as SubjectName,
			SA.BackColor as SubjectBackColor,
			SA.ForeColor as SubjectForeColor,
			SA.TypeName as SubjectTypeName,
			SA.Type as SubjectType,
			SA.TypeID as SubjectTypeID,
			SA.AssetTypeID as SubjectAssetTypeID,

			OA.ID as ObjectAssetID,
			I.Object,
			I.ObjectID,
			utility.GetAssetDisplayValueWrapper(OA.ID) as ObjectName,
			OA.BackColor as ObjectBackColor,
			OA.ForeColor as ObjectForeColor,
			OA.TypeName as ObjectTypeName,
			OA.Type as ObjectType,
			OA.TypeID as ObjectTypeID,
			OA.AssetTypeID as ObjectAssetTypeID,

			I.[State],

			I.PredicateName as [Predicate]
	from	@tbl I
			inner join [Intersect] T on T.ID = I.IntersectID
			inner join AssetDetail SA on SA.Object = I.Subject and SA.ObjectID = I.SubjectID
			inner join AssetDetail OA on OA.Object = I.Object and OA.ObjectID = I.ObjectID
end

-------------------------------------------------------------------------------------------------------




--MapType needs an identity on ID------------
ALTER TABLE MapType NOCHECK CONSTRAINT ALL;
ALTER TABLE MapGroup NOCHECK CONSTRAINT ALL;
ALTER TABLE MapTypeOrder NOCHECK CONSTRAINT ALL;
ALTER TABLE Map NOCHECK CONSTRAINT ALL;

alter table MapTypeOrder drop constraint FK_MapTypeObject_MapType;
alter table Map drop constraint FK_Map_MapType

alter table MapType add ID2 int identity not null;
alter table MapType drop constraint PK_MapType;
alter table MapType drop column ID;

EXEC sp_rename 'dbo.MapType.ID2', 'ID', 'COLUMN'; 
go
ALTER TABLE [dbo].[MapType] ADD  CONSTRAINT [PK_MapType] PRIMARY KEY (ID);
go

ALTER TABLE [dbo].[MapTypeOrder]  WITH NOCHECK ADD  CONSTRAINT [FK_MapTypeObject_MapType] FOREIGN KEY([MapTypeID])
REFERENCES [dbo].[MapType] ([ID]);

ALTER TABLE [dbo].[Map]  WITH NOCHECK ADD  CONSTRAINT [FK_Map_MapType] FOREIGN KEY([MapTypeID])
REFERENCES [dbo].[MapType] ([ID]);

ALTER TABLE MapType CHECK CONSTRAINT ALL;
ALTER TABLE MapGroup CHECK CONSTRAINT ALL;
ALTER TABLE MapTypeOrder CHECK CONSTRAINT ALL;
ALTER TABLE Map CHECK CONSTRAINT ALL;
---------------------------------------------------------------


--BEGIN: Migrate Score Metric relationship XML--------------------------------------------------------------------------------------------------------------

if OBJECT_ID('tempdb..#tempScoreMetric') IS NOT NULL DROP TABLE #tempScoreMetric;
go

create table #tempScoreMetric (ID int, Object varchar(50), ObjectID int, Configuration XML);
go

insert into #tempScoreMetric 
select 
	s.ID,
	s.Object,
	s.ObjectID,
	s.Configuration
from 
	scoretypemetric s 
	outer apply s.Configuration.nodes('/fields/CheckObjects') as R(r)
where 
	s.checktype = 5 and s.deleted = 0 and s.Configuration.exist('/fields/CheckObjects/Object') = 1;

declare @i int;
select @i = count(*) from #tempScoreMetric;

while @i != 0
begin
	declare @config xml;
	declare @newConfig xml;
	declare @rowId int;
	
	select top 1 
		@config = Configuration, 
		@rowId = ID,
		@newConfig = ''
	from #tempScoreMetric;
	
	select 
		@newConfig = '<fields><CheckObjects>' + string_agg('<IntersectType>' + cast(T.ID as varchar) + '</IntersectType>', '') + '</CheckObjects></fields>'
	from IntersectType T
	inner join 
	(
		select 
		R.r.value('(Type/text())[1]', 'varchar(50)') as [Object], 
		R.r.value('(ID/text())[1]', 'int') as ObjectID 
		from @config.nodes('/fields/CheckObjects/*') as R(r)
	) obj on ((T.[Object] = obj.[Object] and T.ObjectID = obj.ObjectID) or (T.[Subject] = obj.[Object] and T.SubjectID = obj.ObjectID))
	inner join #tempScoreMetric m on m.ID = @rowId
	where ((T.[Object] = m.[Object] and T.objectID = m.ObjectID) or (T.[Subject] =  m.[Object] and T.SubjectID = m.ObjectID))

	update ScoreTypeMetric
	set Configuration = @newConfig
	where ID = @rowID;

	delete from #tempScoreMetric where ID = @rowID;

	select @i = count(*) from #tempScoreMetric;
end
--END: Migrate Score Metric relationship XML----------------------------------------------------------------------------------------------------------------

