DROP VIEW [utility].[ArtifactAssetParent]
GO
DROP VIEW [utility].[ArtifactAssetParentIntermediate]
GO
DROP VIEW [utility].[IntersectAsset]
GO

ALTER TABLE [Intersect] SET ( SYSTEM_VERSIONING = OFF  )
GO
drop table Intersect_History
GO
ALTER TABLE [IntersectType] SET ( SYSTEM_VERSIONING = OFF  )
GO
drop table IntersectType_History
GO

ALTER TABLE [Intersect] DROP PERIOD FOR SYSTEM_TIME; 
alter table [Intersect] drop column [EffectiveStartDate]
alter table [Intersect] drop column [EffectiveEndDate]
--alter table [Intersect] add SubjectUid uniqueidentifier null
--alter table [Intersect] add ObjectUid uniqueidentifier null

ALTER TABLE [IntersectType] DROP PERIOD FOR SYSTEM_TIME; 
alter table [IntersectType] drop column [EffectiveStartDate]
alter table [IntersectType] drop column [EffectiveEndDate]
alter table [IntersectType] add SubjectUid uniqueidentifier null
alter table [IntersectType] add ObjectUid uniqueidentifier null
alter table [IntersectType] add [uid] uniqueidentifier constraint DF_IntersectType_uid default(newid()) not null

update	T
set		T.SubjectUid = S.[uid]
from	[IntersectType] T
		inner join AssetType S on S.Object = T.Subject and S.ObjectID = T.SubjectID
GO
update	T
set		T.ObjectUid = S.[uid]
from	[IntersectType] T
		inner join AssetType S on S.Object = T.Object and S.ObjectID = T.ObjectID
GO
update	IntersectType
set		SubjectUid = '0000000A-0000-0000-0000-000000000009' --reference
where	Subject = 'ReferenceItemType' and SubjectID = 0
GO
update	IntersectType
set		ObjectUid = '0000000A-0000-0000-0000-000000000009' --reference
where	Object = 'ReferenceItemType' and ObjectID = 0
GO

delete	IntersectType where SubjectUid is null and Subject <> 'IntersectType'
delete	IntersectType where ObjectUid is null and Object <> 'IntersectType'

--update	T
--set		T.SubjectUid = S.[uid]
--from	[Intersect] T
--		inner join Asset S on S.Object = T.Subject and S.ObjectID = T.SubjectID
--GO
--update	T
--set		T.ObjectUid = S.[uid]
--from	[Intersect] T
--		inner join Asset S on S.Object = T.Object and S.ObjectID = T.ObjectID
--GO
--update	T
--set		T.SubjectUid = S.[uid]
--from	[Intersect] T
--		inner join AssetType S on S.Object = T.Subject and S.ObjectID = T.SubjectID and T.Subject = 'ReferenceItemType'
--GO
--update	T
--set		T.ObjectUid = S.[uid]
--from	[Intersect] T
--		inner join AssetType S on S.Object = T.Object and S.ObjectID = T.ObjectID and T.Object = 'ReferenceItemType'
--GO

--delete	[Intersect] where SubjectUid is null and Subject <> 'Intersect'
--delete	[Intersect] where ObjectUid is null and Object <> 'Intersect'

--select Subject, SubjectID, Object, ObjectID, SubjectUid, ObjectUid from [Intersect] where SubjectUid is null or ObjectUid is null

ALTER TABLE [IntersectGroupItem] SET ( SYSTEM_VERSIONING = OFF  )
GO
drop table IntersectGroupItem_History
GO
ALTER TABLE [IntersectGroupItem] DROP PERIOD FOR SYSTEM_TIME; 
alter table [IntersectGroupItem] drop column [EffectiveStartDate]
alter table [IntersectGroupItem] drop column [EffectiveEndDate]
GO

ALTER TABLE [IntersectGroup] SET ( SYSTEM_VERSIONING = OFF  )
GO
drop table IntersectGroup_History
GO
ALTER TABLE [IntersectGroup] DROP PERIOD FOR SYSTEM_TIME; 
alter table [IntersectGroup] drop column [EffectiveStartDate]
alter table [IntersectGroup] drop column [EffectiveEndDate]
GO
--create VIEW [utility].[IntersectAsset] (get script)
GO
--CREATE VIEW [utility].[ArtifactAssetParentIntermediate] (get script)
GO
--create VIEW [utility].[ArtifactAssetParent] (get script)
GO

--Merge the rule types to the asset type table.
merge	AssetType as T
using	(
		select * from RuleType
		) S 
on		(T.Object = 'RuleType' and T.ObjectID = S.ID)
when not matched by target then
		insert (Name, Description, Class, DisplayFormat, [State], [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
		values (S.Name, S.Description, 7, coalesce(S.DisplayFormat, '{Name}'), 1, 0, 1, 'RuleType', S.ID, coalesce(S.UpdatedOn, getutcdate()), S.UpdatedBy, coalesce(S.UpdatedOn, getutcdate()), S.UpdatedBy);
GO

--Merge the org types to the asset type table.
merge	AssetType as T
using	(
		select * from OrganizationType
		) S 
on		(T.Object = 'OrganizationType' and T.ObjectID = S.ID)
when not matched by target then
		insert (Name, Description, Class, DisplayFormat, [State], [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
		values (S.Name, S.Description, 10, coalesce(S.DisplayFormat, '{Name}'), 1, 0, 1, 'OrganizationType', S.ID, coalesce(S.CreatedOn, getutcdate()), S.CreatedBy, coalesce(S.UpdatedOn, getutcdate()), S.UpdatedBy);
GO

ALTER procedure [metrics].[LoadFromStaging]
as
begin
	-- 1. Remove all except the most recent staging values, grouped by date (not time).
	/*
		insert into metrics.StagingResult values (52, '5/29/2018 3:11:00 PM', 2, 1, 0)
		insert into metrics.StagingResult values (53, '5/29/2018 3:11:00 PM', 2, 0, 0)
		insert into metrics.StagingResult values (54, '5/29/2018 3:11:00 PM', 2, 0, 0)
		insert into metrics.StagingResult values (55, '5/29/2018 3:11:00 PM', 2, 1, 0)

		insert into metrics.StagingResult values (52, '5/31/2018 3:11:00 PM', 2, 1, 0)
		insert into metrics.StagingResult values (53, '5/31/2018 3:11:00 PM', 2, 0, 0)
		insert into metrics.StagingResult values (54, '5/31/2018 3:11:00 PM', 2, 1, 0)
		insert into metrics.StagingResult values (55, '5/31/2018 3:11:00 PM', 2, 1, 0)
	*/
	set nocount on;

	DECLARE @TranName VARCHAR(20);  
	SELECT @TranName = 'UpdateScores';  
	begin transaction @TranName;

	begin try
		drop table if exists #gh

		create table #gh (GroupingID int, ID int, ParentID int null, Name nvarchar(250), Weight decimal(5,3), EffectiveStartDate datetime, EffectiveEndDate datetime, Level int, Type char(1));

		with g as (
			select	ID as GroupingID,
					ID,
					ParentID,
					Name,
					Weight,
					cast(null as datetime) as EffectiveStartDate,
					cast(null as datetime) as EffectiveEndDate,
					1 as Level,
					'G' as Type
			from	[metrics].[Group]
			where	ParentID is null
					and State = 1
			union all
			select	g.GroupingID,
					C.ID,
					C.ParentID,
					C.Name,
					C.Weight,
					cast(null as datetime) as EffectiveStartDate,
					cast(null as datetime) as EffectiveEndDate,
					g.Level+1 as Level,
					'G' as Type
			from	[metrics].[Group] C
					inner join g on g.ID = C.ParentID and C.State = 1
		)

		insert into #gh
			select * from g;

		--select * from #gh

		insert into #gh
			select	G.GroupingID,
					M.ID,
					G.ID,
					I.Name,
					M.Weight,
					M.EffectiveStartDate,
					M.EffectiveEndDate,
					G.Level + 1 as Level,
					'M' as Type
			from	#gh G 
					inner join [metrics].[Map] M on M.GroupID = G.ID
					inner join [metrics].[Item] I on I.ID = M.ItemID;


		update	#gh
		set		EffectiveEndDate = '12/31/9999'
		where	EffectiveStartDate is not null and EffectiveEndDate is null;

		--select * from #gh

		drop table if exists #a

		create table #a (AssetID bigint, Value bit, ID int, ParentID int null, Name nvarchar(250), Weight decimal(5,3), EffectiveDate datetime, Level int, Type char(1), New_ID varchar(250), New_ParentID varchar(250), Score decimal(5,3));

		insert into #a (AssetID, ID, EffectiveDate, Level, Type)
			select	distinct
					R.AssetID,
					0 as ID,
					R.EffectiveDate,
					0 as Level,
					'A' as Type
			from	[metrics].[StagingResult] R
					inner join #gh H on H.ID = R.MapID and H.Type = 'M' and R.EffectiveDate between H.EffectiveStartDate and H.EffectiveEndDate;

--select * from #a

		insert into #a (AssetID, Value, ID, ParentID, Name, Weight, EffectiveDate, Level, Type)
			select	G.AssetID,
					R.Value,
					H.ID,
					H.ParentID,
					H.Name,
					H.Weight,
					G.EffectiveDate,
					H.Level,
					H.Type
			from	#gh H
					inner join	(
								select	distinct
										R.AssetID,
										max(R.EffectiveDate) as EffectiveDate,
										H.GroupingID
								from	[metrics].[StagingResult] R
										inner join #gh H on H.ID = R.MapID and H.Type = 'M' and R.EffectiveDate between H.EffectiveStartDate and H.EffectiveEndDate
								group by R.AssetID,
										H.GroupingID
								) G on G.GroupingID = H.GroupingID
					left join [metrics].[StagingResult] R on R.AssetID = G.AssetID and cast(R.EffectiveDate as date) = cast(G.EffectiveDate as date) and R.MapID = H.ID and H.Type = 'M' ;

--select * from #a

		--Calculate parent/child concatenated IDs.
		update	#a
		set		New_ID = cast(format(EffectiveDate, 'yyyyMMddHHmmss', 'en-US') as varchar) + '.' + cast(AssetID as varchar) + '.' + cast(Type as varchar) + '.' + cast(ID as varchar);

		update	#a
		set		New_ParentID = cast(format(EffectiveDate, 'yyyyMMddHHmmss', 'en-US') as varchar) + '.' + cast(AssetID as varchar) + '.G.' + cast(ParentID as varchar)
		where	Type <> 'A'
				and ParentID is not null;

		update	#a
		set		New_ParentID = cast(format(EffectiveDate, 'yyyyMMddHHmmss', 'en-US') as varchar) + '.' + cast(AssetID as varchar) + '.A.0'
		where	Type <> 'A'
				and ParentID is null;

		--Now start calculating scores.
		update	#a
		set		Score = IIF(Value = 1, Weight, 0)
		where	Type = 'M';

		declare @level int
		select	@level = max(Level) 
		from	#a;

		while @level >= 0
		begin
			if @level > 0
				begin
					update	T
					set		T.Score = S.Score * T.Weight
					from	#a T
							cross apply (
								select	sum(Score) as Score
								from	#a
								where	New_ParentID = T.New_ID
							) S
					where	T.Type = 'G'	
							and T.Level = @level
				end
			else
				begin
					update	T
					set		T.Score = S.Score / C.[Count]
					from	#a T
							cross apply (
								select	sum(Score) as Score
								from	#a
								where	New_ParentID = T.New_ID
							) S
							cross apply (
								select	count(1) as [Count]
								from	#a
								where	New_ParentID = T.New_ID
							) C
					where	T.Type = 'A'
							and T.Level = @level
				end

			set @level = @level-1
		end

		--select * from #a

		/*
		delete	T
		from	metrics.StagingResult T
				left join	(
							select	MapID,
									max(EffectiveDate) as EffectiveDate,
									AssetID
							from	metrics.StagingResult
							group by	MapID, AssetID
							) S  on S.MapID = T.MapID and S.EffectiveDate = T.EffectiveDate and S.AssetID = T.AssetID
		where	S.MapID is null;
		*/

		-- 2. Update pre-existing scores
		update	T
		set		T.Value = S.Score
		from	metrics.Score T
				inner join (
							select		cast(R.EffectiveDate as date) as EffectiveDate, A.Object, A.ObjectID, R.Score 
							from		#a R
										inner join Asset A on A.ID = R.AssetID and R.Type = 'A'
							group by	cast(R.EffectiveDate as date), A.Object, A.ObjectID, R.Score 
							) S on S.EffectiveDate = T.EffectiveStartDate and S.Object = T.Object and S.ObjectID = T.ObjectID;

		-- 3. Insert new scores
		insert	metrics.Score
				select		A.Object, 
							A.ObjectID, 
							cast(R.EffectiveDate as date) as EffectiveDate, 
							case
								when M.EffectiveEndDate = cast('12/31/9999' as date) then M.EffectiveEndDate
								else DATEADD(d, -1, M.EffectiveEndDate)
							end as EffectiveEndDate, 
							R.Score 
				from		#a R
							inner join Asset A on A.ID = R.AssetID and R.Type = 'A'
							outer apply	(
										select	coalesce(min(EffectiveStartDate), cast('12/31/9999' as date)) as EffectiveEndDate
										from	metrics.Score
										where	Object = A.Object and ObjectID = A.ObjectID and EffectiveStartDate > cast(R.EffectiveDate as date)
										) M
							left join metrics.Score T on T.EffectiveStartDate = cast(R.EffectiveDate as date) and T.Object = A.Object and T.ObjectID = A.ObjectID
				where		T.ID is null
				group by	R.EffectiveDate, M.EffectiveEndDate, A.Object, A.ObjectID, R.Score;

		-- 4. Merge the metric results, updating existing and adding new ones.
		update	T
		set		T.Value = S.Value
		from	metrics.MapResult T
				inner join (
					select  distinct
							SR.ID,
							S.ID as ScoreID,
							coalesce(SR.Value, cast(0 as bit)) as Value
					from	#a SR
							inner join Asset A on A.ID = SR.AssetID and SR.Type = 'M'
							inner join metrics.Score S on S.Object = A.Object and S.ObjectID = A.ObjectID and cast(S.EffectiveStartDate as date) = cast(SR.EffectiveDate as date)			
				) S on S.ID = T.MapID and S.ScoreID = T.ScoreID;

--select * from #a

		insert into metrics.MapResult (MapID, ScoreID, [Value])
			select  SR.ID,
					S.ID as ScoreID,
					cast(max(coalesce(cast(SR.Value as int), cast(0 as int))) as bit) as Value
			from	#a SR
					inner join Asset A on A.ID = SR.AssetID and SR.Type = 'M'
					inner join metrics.Score S on S.Object = A.Object and S.ObjectID = A.ObjectID and cast(S.EffectiveStartDate as date) = cast(SR.EffectiveDate as date)
					left join metrics.MapResult E on E.MapID = SR.ID and E.ScoreID = S.ID
			where	E.MapID is null
			group by	SR.ID,
						S.ID;

		-- 5. End-date the older scores based on object and effective date comparisons.
		update	T
		set		T.EffectiveEndDate = DATEADD(d, -1, M.EffectiveStartDate)
		from	metrics.Score T
				inner join (
							select		MS.Object,
										MS.ObjectID,
										max(MS.EffectiveStartDate) as EffectiveStartDate 
							from		metrics.Score MS
										inner join (
													select		cast(R.EffectiveDate as date) as EffectiveDate, A.Object, A.ObjectID, Score 
													from		metrics.StagingResult R
																inner join Asset A on A.ID = R.AssetID
													group by	cast(R.EffectiveDate as date), A.Object, A.ObjectID, R.Score 
													) S on S.EffectiveDate = MS.EffectiveStartDate and S.Object = MS.Object and S.ObjectID = MS.ObjectID
							group by	MS.Object, 
										MS.ObjectID
							) M	on M.Object = T.Object and M.ObjectID = T.ObjectID and T.EffectiveStartDate < M.EffectiveStartDate and T.EffectiveEndDate = cast('12/31/9999' as date);

		-- 6. Backup staging table items that we are about to remove.
		insert into [metrics].[StagingResultArchive]
			select	T.[MapID],
					T.[EffectiveDate],
					T.[AssetID],
					T.[Value]
			from    metrics.StagingResult T
					inner join #a S on S.AssetID = T.AssetID and S.EffectiveDate = T.EffectiveDate and S.ID = T.MapID and S.Type = 'M';

		-- 6. Clear the staging table.
		delete	T
		from    metrics.StagingResult T
				inner join #a S on S.AssetID = T.AssetID and cast(S.EffectiveDate as date) = cast(T.EffectiveDate as date) and S.ID = T.MapID and S.Type = 'M';

		-- 7. Delete any possible dupes from score tables.
		delete	metrics.MapResult 
		where	ScoreID in	(
							select		T.ID
							from		metrics.Score T
										inner join	(
													select		max(ID) as ID,
																ObjectID,
																EffectiveStartDate,
																EffectiveEndDate
													from		metrics.Score 
													group by	ObjectID,
																EffectiveStartDate,
																EffectiveEndDate
													having		count(1) > 1
													) S on S.ID > T.ID and S.ObjectID = T.ObjectID and S.EffectiveStartDate = T.EffectiveStartDate and S.EffectiveEndDate = T.EffectiveEndDate
							);

		delete		T
		from		metrics.Score T
					inner join	(
								select		max(ID) as ID,
											ObjectID,
											EffectiveStartDate,
											EffectiveEndDate
								from		metrics.Score 
								group by	ObjectID,
											EffectiveStartDate,
											EffectiveEndDate
								having		count(1) > 1
								) S on S.ID > T.ID and S.ObjectID = T.ObjectID and S.EffectiveStartDate = T.EffectiveStartDate and S.EffectiveEndDate = T.EffectiveEndDate;

		commit transaction @TranName;
	end try
	begin catch
		rollback transaction @TranName;
	end catch
end
GO