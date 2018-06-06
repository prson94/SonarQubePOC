alter table [integration].[SynchedAssetType] add [RefreshIntervalOverride] int null
GO


declare @schema_name nvarchar(256)
declare @table_name nvarchar(256)
declare @Command  nvarchar(1000)

set @schema_name = N'reporting'
set @table_name = N'Dates'

select	@Command = 'sp_rename N''' + @schema_name + '.' + @table_name + '.' + d.name + ''', N''PK_ReportingDates'''
from	sys.tables t
	inner join sys.key_constraints d on d.type = 'PK' and d.parent_object_id = t.object_id
where	t.name = @table_name
	and t.schema_id = schema_id(@schema_name)

if @Command is not null
begin
	execute (@Command)
end
GO


declare @schema_name nvarchar(256)
declare @table_name nvarchar(256)
declare @Command  nvarchar(1000)

set @schema_name = N'dbo'
set @table_name = N'ContractAcceptance'

select	@Command = 'sp_rename N''' + @schema_name + '.' + @table_name + '.' + d.name + ''', N''PK_' + @table_name + ''''
from	sys.tables t
	inner join sys.key_constraints d on d.type = 'PK' and d.parent_object_id = t.object_id
where	t.name = @table_name
	and t.schema_id = schema_id(@schema_name)

if @Command is not null
begin
	execute (@Command)
end
GO

declare @schema_name nvarchar(256)
declare @table_name nvarchar(256)
declare @Command  nvarchar(1000)

set @schema_name = N'dbo'
set @table_name = N'CommentVote'

select	@Command = 'sp_rename N''' + @schema_name + '.' + @table_name + '.' + d.name + ''', N''PK_' + @table_name + ''''
from	sys.tables t
	inner join sys.key_constraints d on d.type = 'PK' and d.parent_object_id = t.object_id
where	t.name = @table_name
	and t.schema_id = schema_id(@schema_name)

if @Command is not null
begin
	execute (@Command)
end
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
	select * from g

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
			inner join [metrics].[Item] I on I.ID = M.ItemID
	--where	M.ObjectID = 2

update	#gh
set		EffectiveEndDate = '12/31/9999'
where	EffectiveStartDate is not null and EffectiveEndDate is null

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
			inner join #gh H on H.ID = R.MapID and H.Type = 'M' and R.EffectiveDate between H.EffectiveStartDate and H.EffectiveEndDate

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
								R.EffectiveDate,
								H.GroupingID
						from	[metrics].[StagingResult] R
								inner join #gh H on H.ID = R.MapID and H.Type = 'M' and R.EffectiveDate between H.EffectiveStartDate and H.EffectiveEndDate
						) G on G.GroupingID = H.GroupingID
			left join [metrics].[StagingResult] R on R.AssetID = G.AssetID and R.EffectiveDate = G.EffectiveDate and R.MapID = H.ID and H.Type = 'M' 

--Calculate parent/child concatenated IDs.
update	#a
set		New_ID = cast(format(EffectiveDate, 'yyyyMMddHHmmss', 'en-US') as varchar) + '.' + cast(AssetID as varchar) + '.' + cast(Type as varchar) + '.' + cast(ID as varchar)

update	#a
set		New_ParentID = cast(format(EffectiveDate, 'yyyyMMddHHmmss', 'en-US') as varchar) + '.' + cast(AssetID as varchar) + '.G.' + cast(ParentID as varchar)
where	Type <> 'A'
		and ParentID is not null

update	#a
set		New_ParentID = cast(format(EffectiveDate, 'yyyyMMddHHmmss', 'en-US') as varchar) + '.' + cast(AssetID as varchar) + '.A.0'
where	Type <> 'A'
		and ParentID is null

--Now start calculating scores.
update	#a
set		Score = IIF(Value = 1, Weight, 0)
where	Type = 'M'

declare @level int
select	@level = max(Level) 
from	#a

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
	merge   metrics.MapResult as T 
	using   ( 
			select  SR.ID,
					S.ID as ScoreID,
					coalesce(SR.Value, cast(0 as bit)) as Value
			from	#a SR
					inner join Asset A on A.ID = SR.AssetID and SR.Type = 'M'
					inner join metrics.Score S on S.Object = A.Object and S.ObjectID = A.ObjectID and S.EffectiveStartDate = cast(SR.EffectiveDate as date)
			) as S 
			on  (
				T.MapID = S.ID and T.ScoreID = S.ScoreID
				)
	when    matched then 
			update
				set
				T.Value = S.Value
	when    not matched by target then 
			insert (MapID, ScoreID, [Value]) 
			values (S.ID, S.ScoreID, S.Value);

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

	-- 6. Clear the staging table.
	delete	T
	from    metrics.StagingResult T
			inner join #a S on S.AssetID = T.AssetID and S.EffectiveDate = T.EffectiveDate and S.ID = T.MapID and S.Type = 'M';

end
GO

update	A
set		A.DisplayFormat = coalesce(F.Name, '')
--select *
from	AttributeType A
		outer apply (
			select	top 1 
					'{' + Name + '}' as Name
			from	FieldType 
			where	Object = 'AttributeType' and ObjectID = A.ID and IsListable = 1
		) F
where	DisplayFormat is null
GO

declare @schema_name nvarchar(256)
declare @table_name nvarchar(256)
declare @Command  nvarchar(1000)

set @schema_name = N'dbo'
set @table_name = N'CommentVote'

select	@Command = 'sp_rename N''' + d.name + ''', N''FK_CommentVote_Comment'''
from	sys.tables t
	inner join sys.foreign_keys d on d.parent_object_id = t.object_id --and d.type = 'FK'
where	t.name = @table_name
	and t.schema_id = schema_id(@schema_name)
--select @Command
if @Command is not null
begin
	execute (@Command)
end
GO


ALTER TRIGGER [dbo].[FieldType_AfterUpsert]
   ON  [dbo].[FieldType] 
   AFTER INSERT,UPDATE
AS 


		UPDATE	F
		set		F.FormattedValue = utility.GetFormattedFieldLookupValueWithMultiple(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value, FT.AllowMultipleValues)
		FROM	Field F
				inner join inserted FT on FT.ID = F.FieldTypeID and FT.LookupObjectType is not null

		update	FT	
		set		FT.defaultformattedvalue  = [utility].[GetFormattedFieldLookupValueWrapper](FT.[Type],FT.[LookupDisplayFormat],FT.[LookupObjectType],FT.[LookupObjectID],FT.[DefaultValue])
		from	FieldType FT
				inner join inserted ins on ins.ID = FT.ID and ins.LookupObjectType is not null
		
		--check insert vs update --  power(2, (25-1)) is 16777216
		IF (EXISTS (SELECT * FROM DELETED) AND ((COLUMNS_UPDATED() & 16777216)=16777216))
		begin
			INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
				select 'Update', [queue].WriteIndexXml('', Object, ObjectID, UpdatedBy), 'FieldType', ID from inserted;
		end
		ELSE IF (NOT EXISTS (SELECT * FROM DELETED))
		BEGIN
			INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
				select 'Add', [queue].WriteIndexXml('', Object, ObjectID, UpdatedBy), 'FieldType', ID from inserted;
		END
GO

ALTER procedure [metrics].[LoadFromStaging]
as
begin
	-- 1. Remove all except the most recent staging values, grouped by date (not time).
	/*
		insert into metrics.StagingResult values (52, '6/2/2018 3:11:00 PM', 2, 1, 0)
		insert into metrics.StagingResult values (53, '6/2/2018 3:11:00 PM', 2, 0, 0)
		insert into metrics.StagingResult values (54, '6/2/2018 3:11:00 PM', 2, 0, 0)
		insert into metrics.StagingResult values (55, '6/2/2018 3:11:00 PM', 2, 1, 0)

		insert into metrics.StagingResult values (52, '6/4/2018 3:11:00 PM', 2, 1, 0)
		insert into metrics.StagingResult values (53, '6/4/2018 3:11:00 PM', 2, 1, 0)
		insert into metrics.StagingResult values (54, '6/4/2018 3:11:00 PM', 2, 0, 0)
		insert into metrics.StagingResult values (55, '6/4/2018 3:11:00 PM', 2, 0, 0)
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
										R.EffectiveDate,
										H.GroupingID
								from	[metrics].[StagingResult] R
										inner join #gh H on H.ID = R.MapID and H.Type = 'M' and R.EffectiveDate between H.EffectiveStartDate and H.EffectiveEndDate
								) G on G.GroupingID = H.GroupingID
					left join [metrics].[StagingResult] R on R.AssetID = G.AssetID and R.EffectiveDate = G.EffectiveDate and R.MapID = H.ID and H.Type = 'M' ;

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
							inner join metrics.Score S on S.Object = A.Object and S.ObjectID = A.ObjectID and S.EffectiveStartDate = cast(SR.EffectiveDate as date)			
				) S on S.ID = T.MapID and S.ScoreID = T.ScoreID;

		insert into metrics.MapResult (MapID, ScoreID, [Value])
			select  distinct
					SR.ID,
					S.ID as ScoreID,
					coalesce(SR.Value, cast(0 as bit)) as Value
			from	#a SR
					inner join Asset A on A.ID = SR.AssetID and SR.Type = 'M'
					inner join metrics.Score S on S.Object = A.Object and S.ObjectID = A.ObjectID and S.EffectiveStartDate = cast(SR.EffectiveDate as date)
					left join metrics.MapResult E on E.MapID = SR.ID and E.ScoreID = S.ID
			where	E.MapID is null;

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

		-- 6. Clear the staging table.
		delete	T
		from    metrics.StagingResult T
				inner join #a S on S.AssetID = T.AssetID and S.EffectiveDate = T.EffectiveDate and S.ID = T.MapID and S.Type = 'M';

		commit transaction @TranName;
	end try
	begin catch
		rollback transaction @TranName;
	end catch

end
GO


alter table [integration].[SynchedAssetType] add [RefreshIntervalOverride] int null
--alter table [integration].[SynchedAssetType] drop column LastSuccessfulCount

CREATE TABLE [integration].[ExecutionRelationItem](
	[ID] [uniqueidentifier] NOT NULL,
	[ExecutionID] bigint NOT NULL,
	[SubjectSourceID] [nvarchar](250) NOT NULL,
	[ObjectSourceID] [nvarchar](250) NOT NULL,
	[IntersectTypeID] [int] NOT NULL,
	CONSTRAINT [PK_IntegrationExecutionRelationItem] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [integration].[ExecutionRelationItem] ADD  CONSTRAINT [DF_IntegrationExecutionRelationItem_ID]  DEFAULT (newid()) FOR [ID]
GO

ALTER TABLE [integration].[ExecutionRelationItem]  WITH CHECK ADD  CONSTRAINT [FK_IntegrationExecutionRelationItem_IntegrationExecution] FOREIGN KEY([ExecutionID]) REFERENCES [integration].[Execution] ([ID])
GO

ALTER TABLE [integration].[ExecutionRelationItem] CHECK CONSTRAINT [FK_IntegrationExecutionRelationItem_IntegrationExecution]
GO


--Script below addresses GOV-3977 (auto-assign read claim to configured roles)
merge into	[ResponsibilityTypeObjectClaim] T
using       (
			select	[ResponsibilityTypeID], 
					[ObjectType], 
					[ObjectID],
					1 as Claim,
					1 as ClaimObject
			from	[ResponsibilityTypeRelation]
			) S
on          (
				T.ResponsibilityTypeID = S.ResponsibilityTypeID and 
				T.ObjectType = S.ObjectType and 
				T.ObjectID = S.ObjectID
			)
when not matched then
	insert  (ResponsibilityTypeID, ObjectType, ObjectID, Claim, ClaimObject)
	values  (S.ResponsibilityTypeID, S.ObjectType, S.ObjectID, S.Claim, S.Claim);
GO
-----------------------------------


alter table [integration].[SynchedAssetTypeFieldItem] add ConsiderWhenDeleting bit constraint DF_IntegrationSynchedAssetTypeFieldItem_ConsiderWhenDeleting default(0) not null
GO

alter table [integration].[SynchedAssetTypeFieldItem] add constraint DF_IntegrationSynchedAssetTypeFieldItem_ConsiderWhenDeleting_Check CHECK (
	(ConsiderWhenDeleting=1 AND DefaultValue is not null) OR (ConsiderWhenDeleting=0 AND DefaultValue is null) OR (ConsiderWhenDeleting=0 AND DefaultValue is not null)
)
GO

ALTER procedure [integration].[ProcessDeletions]
as
begin
	DROP TABLE IF EXISTS #fullSynched

	create table #fullSynched (ExecutionID bigint, SynchedAssetTypeID int, CurrentSourceAssetCount int, SourceProcessedCount int)
	insert into #fullSynched
		select		E.ExecutionID,
					E.SynchedAssetTypeID,
					E.CurrentSourceAssetCount,
					count(1) as SourceProcessedCount
		from		integration.ExecutionAssetType E
					inner join	(
								select		Max(ExecutionID) as ExecutionID,
											SynchedAssetTypeID
								from		integration.ExecutionAssetType
								where		IsFullRefresh = 1
											and CompletedOn is not null
								group by	SynchedAssetTypeID
								) ME on ME.ExecutionID = E.ExecutionID and ME.SynchedAssetTypeID = E.SynchedAssetTypeID
					inner join integration.ExecutionAsset A on A.ExecutionID = E.ExecutionID and A.SynchedAssetTypeID = E.SynchedAssetTypeID and E.ProcessedDelete = 0
--where		E.SynchedAssetTypeID = 18
		group by	E.ExecutionID,
					E.SynchedAssetTypeID,
					E.CurrentSourceAssetCount
	/*
	select	* 
	from	#fullSynched
	*/

	-- Get the full list of assets, whether processed in the last full-synch executions or not.
	DROP TABLE IF EXISTS #targetAssets
	create table #targetAssets (ExecutionID bigint, SynchedAssetTypeID int, AssetID bigint, [Level] int)

	-- First, get ones where there is no level to deal with, AND have no default value field to worry about.
	insert into #targetAssets
		select		F.ExecutionID,
					F.SynchedAssetTypeID,
					A.ID,
					null
		from		#fullSynched F
					inner join integration.SynchedAssetType T on T.ID = F.SynchedAssetTypeID
					inner join Asset A on A.AssetTypeID = T.AssetTypeID
		where		T.[Level] is null
					and F.SynchedAssetTypeID not in (
						select	distinct 
								SynchedAssetTypeID
						from	integration.SynchedAssetTypeFieldItem 
						where	ConsiderWhenDeleting = 1
					)
		order by	F.SynchedAssetTypeID

	-- Next, get ones where there is no level to deal with, and HAVE a default value field to worry about.
	insert into #targetAssets
		select		F.ExecutionID,
					F.SynchedAssetTypeID,
					A.ID,
					null
		from		#fullSynched F
					inner join integration.SynchedAssetType T on T.ID = F.SynchedAssetTypeID
					inner join Asset A on A.AssetTypeID = T.AssetTypeID
					cross apply (
						select	IA.ID as AssetID
						from	Asset IA
								inner join integration.SynchedAssetTypeFieldItem FI on FI.ConsiderWhenDeleting = 1 and FI.SynchedAssetTypeID = F.SynchedAssetTypeID and IA.AssetTypeID = T.AssetTypeID
								inner join FieldType IFT on IFT.AssetTypeID = IA.AssetTypeID and IFT.Name = FI.TargetField
								inner join Field F on F.FieldTypeID = IFT.ID and F.Value = FI.DefaultValue and F.AssetID = IA.ID and IA.ID = A.ID
					) EF
		where		T.[Level] is null
					and F.SynchedAssetTypeID in (
						select	distinct 
								SynchedAssetTypeID
						from	integration.SynchedAssetTypeFieldItem 
						where	ConsiderWhenDeleting = 1
					)
		order by	F.SynchedAssetTypeID

	-- Next, get ones where there is a level to deal with, and no default value to consider.
	insert into #targetAssets
		select		F.ExecutionID,
					F.SynchedAssetTypeID,
					A.ID,
					L.Level
		from		#fullSynched F
					inner join integration.SynchedAssetType T on T.ID = F.SynchedAssetTypeID
					inner join Asset A on A.AssetTypeID = T.AssetTypeID
					cross apply dbo.GetAssetLevelById(A.ID) L
		where		L.[Level] = T.[Level]
					and F.SynchedAssetTypeID not in (
						select	distinct 
								SynchedAssetTypeID
						from	integration.SynchedAssetTypeFieldItem 
						where	ConsiderWhenDeleting = 1
					)
		order by	F.SynchedAssetTypeID,
					L.Level

	-- Last, get ones where there is a level to deal with, and HAS default value to consider.
	insert into #targetAssets
		select		F.ExecutionID,
					F.SynchedAssetTypeID,
					A.ID,
					L.Level
		from		#fullSynched F
					inner join integration.SynchedAssetType T on T.ID = F.SynchedAssetTypeID
					inner join Asset A on A.AssetTypeID = T.AssetTypeID
					cross apply dbo.GetAssetLevelById(A.ID) L
		where		L.[Level] = T.[Level]
					and F.SynchedAssetTypeID in (
						select	distinct 
								SynchedAssetTypeID
						from	integration.SynchedAssetTypeFieldItem 
						where	ConsiderWhenDeleting = 1
					)
		order by	F.SynchedAssetTypeID,
					L.Level

	--select * from #targetAssets

	-- Get the full list of assets that were not present in the last successful full synch, so we can delete them.
	DROP TABLE IF EXISTS #deletes
	create table #deletes (ID int identity, AssetID bigint, Object varchar(50), ObjectID int)

	--First, get the deletes where there is no level.
	insert into #deletes
		select		A.ID,
					A.Object,
					A.ObjectID
		from		#targetAssets T
					inner join Asset A on A.ID = T.AssetID
					left join integration.ExecutionAsset EA on EA.ExecutionID = T.ExecutionID and EA.SynchedAssetTypeID = T.SynchedAssetTypeID and A.SourceID = EA.SourceID
		where		T.Level is null
					and EA.SourceID is null

	--Next, get the deletes where there is a valid level.
	insert into #deletes
		select		A.ID,
					A.Object,
					A.ObjectID
		from		#targetAssets T
					inner join Asset A on A.ID = T.AssetID
					cross apply dbo.GetAssetLevelById(A.ID) L
					left join integration.ExecutionAsset EA on EA.ExecutionID = T.ExecutionID and EA.SynchedAssetTypeID = T.SynchedAssetTypeID and A.SourceID = EA.SourceID
		where		EA.SourceID is null
					and T.Level is not null
					and L.Level = T.Level
		order by	T.[Level] desc

	declare @current int = 1,
			@max int,
			@o varchar(50),
			@oID int
	select	@max = max(ID) from #deletes
	while	@current <= @max
	begin
		select	@o = Object, @oID = ObjectID from #deletes where ID  = @current
		exec DeleteObject @o, @oID, 0
		set		@current = @current + 1
	end

	--Finally, mark these full refreshed records as having been processed for deletes.
	update	T
	set		T.ProcessedDelete = 1
	from	integration.ExecutionAssetType T
			inner join #fullSynched S on S.ExecutionID = T.ExecutionID and S.SynchedAssetTypeID = T.SynchedAssetTypeID
end
GO

/*
ALTER TABLE [dbo].[AssetDataQualityImplementation]  WITH CHECK ADD  CONSTRAINT [FK_AssetDataQualityImplementation_Asset] FOREIGN KEY([AssetID]) REFERENCES [dbo].[Asset] ([ID]) ON DELETE CASCADE
ALTER TABLE [dbo].[AssetDataQualityImplementation] CHECK CONSTRAINT [FK_AssetDataQualityImplementation_Asset]
GO

ALTER TABLE [dbo].[AssetDataQualityImplementationResultQualifier] DROP CONSTRAINT [FK_AssetDataQualityImplementationResultQualifier_AssetDataQualityImplementationQualifierType]
GO

ALTER TABLE [dbo].[AssetDataQualityImplementationResultQualifier]  WITH CHECK ADD  CONSTRAINT [FK_AssetDataQualityImplementationResultQualifier_AssetDataQualityImplementationQualifierType] FOREIGN KEY([AssetDataQualityImplementationQualifierTypeID]) REFERENCES [dbo].[AssetDataQualityImplementationQualifierType] ([ID]) ON DELETE CASCADE
ALTER TABLE [dbo].[AssetDataQualityImplementationResultQualifier] CHECK CONSTRAINT [FK_AssetDataQualityImplementationResultQualifier_AssetDataQualityImplementationQualifierType]
GO


--ALTER TABLE [dbo].[AssetDataQualityImplementationResultQualifier] DROP CONSTRAINT [FK_AssetDataQualityImplementationResultQualifier_AssetDataQualityImplementationResult]
--GO

--ALTER TABLE [dbo].[AssetDataQualityImplementationResultQualifier]  WITH CHECK ADD  CONSTRAINT [FK_AssetDataQualityImplementationResultQualifier_AssetDataQualityImplementationResult] FOREIGN KEY([AssetDataQualityImplementationResultID]) REFERENCES [dbo].[AssetDataQualityImplementationResult] ([ID])
--ALTER TABLE [dbo].[AssetDataQualityImplementationResultQualifier] CHECK CONSTRAINT [FK_AssetDataQualityImplementationResultQualifier_AssetDataQualityImplementationResult]
--GO


ALTER TABLE [dbo].[AssetDataQualityProperty] DROP CONSTRAINT [FK_AssetDataQualityProperty_Asset]
GO

ALTER TABLE [dbo].[AssetDataQualityProperty]  WITH CHECK ADD  CONSTRAINT [FK_AssetDataQualityProperty_Asset] FOREIGN KEY([ID]) REFERENCES [dbo].[Asset] ([ID]) ON DELETE CASCADE
ALTER TABLE [dbo].[AssetDataQualityProperty] CHECK CONSTRAINT [FK_AssetDataQualityProperty_Asset]
GO


ALTER TABLE [dbo].[AssetDataQualityProperty] DROP CONSTRAINT [FK_AssetCalculated_AssetDataQualityDimension]
GO

ALTER TABLE [dbo].[AssetDataQualityProperty]  WITH CHECK ADD  CONSTRAINT [FK_AssetCalculated_AssetDataQualityDimension] FOREIGN KEY([AssetDataQualityDimensionID]) REFERENCES [dbo].[AssetDataQualityDimension] ([ID]) ON DELETE CASCADE
ALTER TABLE [dbo].[AssetDataQualityProperty] CHECK CONSTRAINT [FK_AssetCalculated_AssetDataQualityDimension]
GO
*/