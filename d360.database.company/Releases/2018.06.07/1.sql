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

-- Added 06/07/18 -----------------------------------------
CREATE NONCLUSTERED INDEX [IX_FusionAttribute_Fusion_Include]
    ON [dbo].[FusionAttribute]([FusionAttributeTypeID] ASC, [FusionID] ASC, [Deleted] ASC)
    INCLUDE([Name], [ParentID]);
GO

alter table metrics.Map add [State] INT CONSTRAINT [DF_MetricMap_State] DEFAULT ((1)) NOT NULL
GO

alter procedure [dbo].[DeleteObject]
 @ObjTemp varchar(50),
 @ObjectIDTemp int,
 @ResourceIDTemp int
as 
begin
	set nocount on
	declare    @Obj varchar(50) = @ObjTemp,
		@ObjectID int = @ObjectIDTemp,
		@ResourceID int = @ResourceIDTemp
	
	declare    @Object varchar(50) = @Obj,
		@CurrentDate datetime = getutcdate(),
		@predicateType int = 0,
		@trans varchar(25) = 'Trans',
		@current int = 1,
		@max int,
		@IsType bit = 0

	declare @h table (IntersectID int, ID bigint, ObjectID int, Processed bit null)
	declare @ht table (IntersectTypeID int, ID int, ObjectID int, Processed bit null)

	declare @ClearAttributes bit = 0,
			@ClearComments bit = 0,
			@ClearIntersects bit = 0,
			@ClearFavorites bit = 0,
			@ClearFields bit = 0,
			@ClearFollows bit = 0,
			@ClearIssues bit = 0,
			@ClearNyms bit = 0,
			@ClearResponsibilities bit = 0,
			@ClearSiteNav bit = 0,
			@ClearPromotion bit = 0

	if charindex('Type', @Object) > 0
	begin
		set @IsType = 1
	end

	begin try
		begin transaction @trans

		if @Obj = 'Artifact' or @Obj = 'ArtifactType' or @Obj = 'FusionAttribute' or @Obj = 'FusionAttributeType' or @Obj = 'ReferenceItem' or @Obj = 'ReferenceItemType'
		begin
			set @predicateType = 3
		end
		if @Obj = 'Policy' or @Obj = 'PolicyType' or @Obj = 'Taxonomy' or @Obj = 'TaxonomyType'
		begin
			set @predicateType = 4
		end

		if @predicateType > 0
		begin
			if @IsType = 1
				begin
					insert into @ht
						select	null,
								ID,
								ObjectID,
								0
						from	AssetType
						where	[Object] = @Obj and ObjectID = @ObjectID

					while exists(select 1 from @ht where Processed = 0)
					begin
						insert into @ht
							select	I.ID,
									C.ID,
									C.ObjectID,
									null
							from	AssetType C
									inner join IntersectType I on I.Subject = @Obj and I.Object = @Obj and I.ObjectID = C.ObjectID
									inner join [Predicate] PR on PR.ID = I.PredicateID and PR.[Type] = @predicateType
									inner join AssetType P on P.Object = I.Subject and P.ObjectID = I.SubjectID
									inner join @ht T on T.ID = P.ID and T.Processed = 0

						update	@ht set Processed = 1 where Processed = 0
						update	@ht set Processed = 0 where Processed is null
					end

					-- Get all assets based on the types found above.
					insert into @h 
						select null, ID, ObjectID, 1 from Asset where AssetTypeID in (select ID from @ht)
				end
			else
				begin
					insert into @h
						select	null,
								ID,
								ObjectID,
								0
						from	Asset
						where	[Object] = @Obj and ObjectID = @ObjectID

					while exists(select 1 from @h where Processed = 0)
					begin
						insert into @h
							select	I.IntersectID,
									C.ID,
									C.ObjectID,
									null
							from	Asset C
									inner join PredicateIntersect I on I.PredicateType = @predicateType and I.Subject = @Obj and I.Object = @Obj and I.ObjectID = C.ObjectID
									inner join Asset P on P.Object = I.Subject and P.ObjectID = I.SubjectID
									inner join @h T on T.ID = P.ID and T.Processed = 0

						update	@h set Processed = 1 where Processed = 0
						update	@h set Processed = 0 where Processed is null
					end
				end
		end
		
		-- INDEX
		INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
			select	'ObjectIndex', 
					'D',
					O.Object, 
					O.ObjectID
			from	Asset O
					inner join @h I on O.ID = I.ID

		-- AUDIT
		insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
			select	O.Object, 
					O.ObjectID, 
					O.DisplayValue, 
					@ResourceID, 
					@CurrentDate, 
					'Deleted', 
					O.Object, 
					O.ObjectID, 
					O.TypeName, 
					O.DisplayValue, 
					'This asset has been removed.' 
			from	AssetDetail O
					inner join @h I on O.ID = I.ID
			union
			select	O.Object, 
					O.ObjectID, 
					O.Name, 
					@ResourceID, 
					@CurrentDate, 
					'Deleted', 
					O.Object, 
					O.ObjectID, 
					O.Name, 
					O.Name, 
					'This asset type has been removed.' 
			from	AssetType O
					inner join @ht I on O.ID = I.ID

		-- WORKFLOW

		if @Object = 'Artifact'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearPromotion = 1

			delete Artifact where ID in (select ObjectID from @h)
		end

		if @Object = 'ArtifactType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearSiteNav = 1
			
			delete	T
			from	ArtifactTypeExportTemplate T
					inner join @ht h on h.ObjectID = T.ID

			delete	Artifact
			where	ID in (select ObjectID from @h)

			delete	ArtifactType			
			where	ID in (select ObjectID from @ht)
		end

		if @Object = 'AttributeType'
		begin
			declare @at table (ID int)
			declare @a table (ID int);

			with ht as	(
						select	ID, 
								ParentID
						from	AttributeType
						where	ID = @ObjectID
						union all
						select	C.ID,
								C.ParentID
						from	AttributeType C
								inner join ht P on P.ID = C.ParentID
						)

			insert into @at 
				select ID from ht

			insert into @a
				select ID from Attribute where AttributeTypeID in (select ID from @at)

			-- AUDIT
			insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
				select	A.Object, 
						A.ObjectID, 
						A.DisplayValue, 
						@ResourceID, 
						@CurrentDate, 
						'Deleted', 
						'Attribute', 
						O.ID, 
						O.Name, 
						O.FormattedValue, 
						'This attribute has been removed.' 
				from	AttributeDetail O
						inner join AssetDetail A on A.Object = O.ObjectType and A.ObjectID = O.ObjectID
						inner join @a I on O.ID = I.ID
				union
				select	A.Object, 
						A.ObjectID, 
						A.Name, 
						@ResourceID, 
						@CurrentDate, 
						'Deleted', 
						'AttributeType', 
						O.ID, 
						'Attribute Type', 
						O.Name, 
						'This attribute type has been removed.' 
				from	AttributeType O
						inner join @at I on O.ID = I.ID
						inner join AttributeTypeRelation R on R.AttributeTypeID = O.ID
						inner join AssetType A on A.Object = R.ObjectType and A.ObjectID = R.ObjectID

			update	T
			set		T.UpdatedBy = @ResourceID,
					T.UpdatedOn = @CurrentDate
			from	Asset T
					inner join [Attribute] S on S.ObjectType = T.Object and S.ObjectID = T.ObjectID and S.ID in (select ID from @a)

			update	T
			set		T.UpdatedBy = @ResourceID,
					T.UpdatedOn = @CurrentDate
			from	AssetType T
					inner join AttributeTypeRelation S on S.ObjectType = T.Object and S.ObjectID = T.ObjectID 
					inner join AttributeType A on A.ID = S.AttributeTypeID and A.ID in (select ID from @at)

			delete Field					where ObjectType = 'Attribute' and ObjectID in (select ID from @a)
			delete Attribute				where ID in (select ID from @a)
			delete FieldType				where Object = 'AttributeType' and ObjectID in (select ID from @at)
			delete AttributeTypeRelation	where AttributeTypeID in (select ID from @at)
			delete AttributeType			where ID in (select ID from @at)
		end

		if @Object = 'FieldType'
		begin
			-- AUDIT
			insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
				select	A.Object, 
						A.ObjectID, 
						A.DisplayValue, 
						@ResourceID, 
						@CurrentDate, 
						'Deleted', 
						A.Object, 
						A.ObjectID, 
						T.Name, 
						O.FormattedValue, 
						'This field has been removed.' 
				from	Field O
						inner join FieldType T on T.ID = O.FieldTypeID and T.ID = @ObjectID
						inner join AssetDetail A on A.Object = O.ObjectType and A.ObjectID = O.ObjectID
				union
				select	A.Object, 
						A.ObjectID, 
						A.Name, 
						@ResourceID, 
						@CurrentDate, 
						'Deleted', 
						'FieldType', 
						O.ID, 
						'Field Type', 
						O.Name, 
						'This field type has been removed.' 
				from	FieldType O
						inner join AssetType A on A.Object = O.Object and A.ObjectID = O.ObjectID and O.ID = @ObjectID

			update	T
			set		T.UpdatedBy = @ResourceID,
					T.UpdatedOn = @CurrentDate
			from	Asset T
					inner join Field S on S.ObjectType = T.Object and S.ObjectID = T.ObjectID and S.FieldTypeID = @ObjectID

			update	T
			set		T.UpdatedBy = @ResourceID,
					T.UpdatedOn = @CurrentDate
			from	AssetType T
					inner join FieldType S on S.Object = T.Object and S.ObjectID = T.ObjectID and S.ID = @ObjectID

			delete	Field 
			where	FieldTypeID = @ObjectID
			
			update	FieldType 
			set		ParentFieldTypeID = null 
			where	ParentFieldTypeID = @ObjectID

			delete	FieldType 
			where	ID = @ObjectID
		end

		if @Object = 'FusionAttribute'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearPromotion = 1

			delete FusionAttribute where ID in (select ObjectID from @h)
		end

		if @Object = 'FusionAttributeType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			delete FusionAttribute		where ID in (select ObjectID from @h)
			delete FusionAttributeType	where ID in (select ObjectID from @ht)
		end

		if @Object = 'Fusion'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			insert into @h
				select	I.ID, null, F.ID, null 
				from	[IntersectDetail] I
						inner join FusionAttribute F on I.Subject = 'FusionAttribute' 
														and I.Object = 'FusionAttribute' 
														and (F.ID = I.SubjectID OR F.ID = I.ObjectID) 
														and F.FusionID = @ObjectID
														and I.PredicateType = 3

			delete FusionAttribute where FusionID = @ObjectID
			delete Fusion where ID = @ObjectID
		end

		if @Object = 'FusionType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			insert into @ht
				select	ID, null, null, null
				from	IntersectType
				where	Subject = 'FusionAttributeType' 
						and Object = 'FusionAttributeType' 
						and (
							SubjectID in (select ID from FusionAttributeType where FusionTypeID = @ObjectID)
							or ObjectID in (select ID from FusionAttributeType where FusionTypeID = @ObjectID)
							)

			insert into @h
				select ID, null, null, null from [Intersect] where IntersectTypeID in (select IntersectTypeID from @ht)

			delete FusionAttribute where FusionAttributeTypeID in (select ID from FusionAttributeType where FusionTypeID = @ObjectID)
			delete Fusion where FusionTypeID = @ObjectID
			delete FusionAttributeType where FusionTypeID = @ObjectID
			delete FusionType where ID = @ObjectID
		end

		if @Object = 'IntersectType'
		begin
			set @ClearAttributes = 1
			set @ClearFields = 1

			delete [Intersect] where IntersectTypeID = @ObjectID
			delete IntersectType where ID = @ObjectID
		end

		if @Object = 'LookupType'
		begin
			set @ClearFields = 1

			delete [Lookup] where LookupTypeID = @ObjectID
			delete  LookupType where ID=@ObjectID
		end

		if @Object = 'Policy'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearPromotion = 1

			delete [Policy] where ID in (select ObjectID from @h)
		end

		if @Object = 'PolicyType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearSiteNav = 1

			delete [Policy] where PolicyTypeID in (select ObjectID from @ht)
			delete PolicyTypeLevel where PolicyTypeID in (select ObjectID from @ht)
			delete PolicyType where ID in (select ObjectID from @ht)
		end

		if @Object = 'ReferenceItem'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			delete ReferenceItem where ID = @ObjectID			
		end

		if @Object = 'ReferenceItemType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			delete ReferenceItem where ReferenceItemTypeID = @ObjectID
			delete ReferenceItemType where ID = @ObjectID
		end

		if @Object = 'ResponsibilityType'
		begin
			delete ResponsibilityTypeRelationOverrideItem where ResponsibilityTypeID = @ObjectID
			delete ResponsibilityTypeRelationItem where ResponsibilityTypeID = @ObjectID
			delete ResponsibilityTypeRelationRule where ResponsibilityTypeID = @ObjectID
			delete ResponsibilityType where ID = @ObjectID
		end

		if @Object = 'Rule'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			-- DELETE
			delete T
			from   RuleResultQualifierType T
				   inner join RuleImplementation I on I.ID = T.RuleImplementationID and I.RuleID = @ObjectID

			delete	Q
			from	RuleResultQualifier Q 
					inner join RuleResult S on S.ID = Q.RuleResultID
					inner join RuleImplementation I on I.ID = S.RuleImplementationID and I.RuleID = @ObjectID

			delete	F
			from	RuleResultFusionAttribute F
					inner join RuleResult S on S.ID = F.RuleResultID
					inner join RuleImplementation I on I.ID = S.RuleImplementationID and I.RuleID = @ObjectID

			delete	RuleImplementation where RuleID = @ObjectID

			delete	[Rule] where ID = @ObjectID
		end

		if @Object = 'RuleType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			delete	Q
			from	RuleResultQualifier Q 
					inner join RuleResult S on S.ID = Q.RuleResultID
					inner join RuleImplementation I on I.ID = S.RuleImplementationID
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID

			delete	F
			from	RuleResultQualifierType F
					inner join RuleImplementation I on I.ID = F.RuleImplementationID
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID

			delete	F
			from	RuleResultFusionAttribute F
					inner join RuleResult S on S.ID = F.RuleResultID
					inner join RuleImplementation I on I.ID = S.RuleImplementationID
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID

			delete	S
			from	RuleResult S
					inner join RuleImplementation I on I.ID = S.RuleImplementationID
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID

			delete [RuleImplementation] where RuleID in (select ID from [Rule] where RuleTypeID = @ObjectID)

			delete [Rule] where RuleTypeID = @ObjectID

			delete RuleType where ID = @ObjectID
		end

		if @Object = 'SurveyType'
		begin
			delete Survey where SurveyTypeID = @ObjectID
			delete SurveyType where ID = @ObjectID
		end

		if @Object = 'Taxonomy'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearPromotion = 1

			delete Taxonomy where ID in (select ObjectID from @h)
		end

		if @Object = 'TaxonomyType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearSiteNav = 1

			delete Taxonomy where TaxonomyTypeID = @ObjectID
			delete TaxonomyTypeLevel where TaxonomyTypeID = @ObjectID
			delete TaxonomyType where ID = @ObjectID
		end

		-- DELETE (Supporting Tables) ---------------------------------------------------------

		-- Attribute deletion
		IF @ClearAttributes = 1 AND @IsType = 0
		BEGIN
			delete Field where ObjectType = 'Attribute' and ObjectID in (select ID from Attribute where ObjectType = @Object and ObjectID in (select ObjectID from @h))
			delete Attribute where ObjectType = @Object and ObjectID in (select ObjectID from @h)
		END

		-- Intersect deletion
		IF @ClearIntersects = 1
		BEGIN
			DECLARE @tblIntersectIDs table (ID int)

			INSERT INTO @tblIntersectIDs
				SELECT	ID
				FROM	[Intersect]
				WHERE	(Subject = @Object and SubjectID in (select ObjectID from @h)) OR (Object = @Object and ObjectID in (select ObjectID from @h))

			--delete	MapItem 
			--where	SourceIntersectID in (select ID from @tblIntersectIDs) OR
			--		TargetIntersectID in (select ID from @tblIntersectIDs)

			delete [Intersect] where ID in (select ID from @tblIntersectIDs)
		END

		-- Comment deletion
		IF @ClearComments = 1 AND @IsType = 0
		BEGIN
			delete	CommentRelation
			where	ObjectType = @Object 
					and ObjectID in (select ObjectID from @h)

			delete	CommentVote
			where	CommentID in (
								select	ID
								from	Comment
								where	OwnerObjectType = @Object 
										and OwnerObjectID in (select ObjectID from @h)			
								)

			delete	Comment
			where	OwnerObjectType = @Object 
					and OwnerObjectID in (select ObjectID from @h)
		END

		-- Site menu deletion
		IF @ClearSiteNav = 1
		BEGIN
			delete sitenav where objectid = @ObjectID and [object] = @Object
		END

		IF @ClearPromotion = 1
		BEGIN
			delete from fusion.rulepromotion where objecttype = @Object and objectid = @ObjectID
		END 


		-- Favorite deletion
		IF @ClearFavorites = 1
		BEGIN
			IF @IsType = 1
				BEGIN
					delete	Favorite
					where	Object = @Object 
							and ObjectID in (select ObjectID from @ht)
				END
			ELSE
				BEGIN 
					delete	Favorite
					where	Object = @Object
							and ObjectID in (select ObjectID from @h)
				END
		END

		-- Field deletion
		IF @ClearFields = 1
		BEGIN
			IF @IsType = 1
				BEGIN
					delete	FieldType
					where	[Object] = @Object 
							and ObjectID in (select ObjectID from @ht)
				END
			ELSE
				BEGIN
					delete	Field
					where	ObjectType = @Object 
							and ObjectID in (select ObjectID from @h)
				END
		END

		-- Follow deletion
		IF @ClearFollows = 1
		BEGIN
			IF @IsType = 1
				BEGIN
					delete	Follow
					where	ObjectType = @Object 
							and ObjectID in (select ObjectID from @ht)
				END
			ELSE
				BEGIN 
					delete	Follow
					where	ObjectType = @Object
							and ObjectID in (select ObjectID from @h)
				END
		END

		-- Issue deletion
		IF @ClearIssues = 1 AND @IsType = 0
		BEGIN
			delete	Issue
			where	Object = @Object 
					and ObjectID in (select ObjectID from @h)
		END

		-- Nym deletion
		IF @ClearNyms = 1 AND @IsType = 0
		BEGIN
			IF @IsType = 1
				BEGIN 
					delete	NymRelation
					where	Object = @Object 
							and ObjectID in (select ObjectID from @ht)			
				END
			ELSE
				BEGIN
					delete	Nym
					where	Object = @Object 
							and ObjectID in (select ObjectID from @h)
				END
		END

		-- Responsibility deletion
		IF @ClearResponsibilities = 1 AND @IsType = 0
		BEGIN
			IF @IsType = 1
				BEGIN
					delete ResponsibilityTypeRelation		where ObjectType = @Obj and ObjectID in (select ObjectID from @ht)
					delete ResponsibilityTypeObjectClaim	where ObjectType = @Obj and ObjectID in (select ObjectID from @ht)
				END
			ELSE
				BEGIN
					delete	T
					from	ResponsibilityTypeRelationOverrideItem T
							inner join Asset A on A.ID = T.AssetID and A.ID in (select ID from @h)

					delete	T
					from	ResponsibilityTypeRelationItem T
							inner join Asset A on A.ID = T.AssetID and A.ID in (select ID from @h)
				END
		END
		---------------------------------------------------------------------------------------

		if @IsType = 1
		begin
			delete AttributeTypeRelation			where ObjectType = @Obj AND ObjectID in (select ObjectID from @ht)
			delete IntersectType					where (Object = @Obj and ObjectID in (select ObjectID from @ht)) OR (Subject = @Obj and SubjectID in (select ObjectID from @ht))
			delete ObjectStyle						where ObjectType = @Obj AND ObjectID in (select ObjectID from @ht)
		end
		
		commit transaction @trans
	end try
	begin catch
		DECLARE @ErrorMessage NVARCHAR(4000)
		DECLARE @ErrorSeverity INT
	    DECLARE @ErrorState INT

		SELECT 
			@ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE()

		-- Use RAISERROR inside the CATCH block to return error
		-- information about the original error that caused
		-- execution to jump to the CATCH block.
		RAISERROR (@ErrorMessage, -- Message text.
				   @ErrorSeverity, -- Severity.
				   @ErrorState -- State.
				   )

		rollback transaction @trans
	end catch
end
GO


alter procedure [dbo].[GetLineage]
--declare 
	@type varchar(50),
	@id int,
	@view int = 1,
	@usageOnly bit = 0,
	@rows LineageTable readonly,
	@technicalRows LineageTechnicalTable readonly

--set @type = 'Artifact'
--set @id = 550
--set @view = 1
as
begin
	declare @links table ([from] varchar(250), [to] varchar(250), category varchar(50))
	declare @nodes table (
		assetId int,
		[key] varchar(250), 
		obj varchar(50), [objid] int, [type] varchar(50), typeName nvarchar(250), name nvarchar(500), shortname nvarchar(500),
		back varchar(7), fore varchar(7), template varchar(50), other varchar(500),

		HasSourceRules bit
		)
	declare @objects table (Type varchar(50), ID int)
	declare @currentDepth int = 0;
	declare @maxDepth int = 6;
	declare @maxItems int = 500;
	declare @itemCount int = 0;
	
	if @view in (0, 1, 2)
	begin
		insert into @objects values (@type, @id)

		if not exists(
			select	MI.ID
			from	MapItem MI
					inner join [Intersect] SI on SI.ID = MI.SourceIntersectID
					inner join [Intersect] TI ON TI.ID = MI.TargetIntersectID
			where 	( (SI.Subject = @type and SI.SubjectID = @id) OR (SI.Object = @type and SI.ObjectID = @id)  )
					OR ( (TI.Subject = @type and TI.SubjectID = @id) OR (TI.Object = @type and TI.ObjectID = @id)  )
		)
		begin
			insert into @objects
				select	case 
							when I.Subject = @type and I.SubjectID = @id then I.Object
							else I.Subject
						end,
						case 
							when I.Subject = @type and I.SubjectID = @id then I.ObjectID 
							else I.SubjectID 
						end
				from	[Intersect] I
						inner join IntersectType T on T.ID = I.IntersectTypeID 
						inner join Predicate P on P.ID = T.PredicateID and P.Type = 6
				where	(I.Subject = @type and I.SubjectID = @id) or (I.Object = @type and I.ObjectID = @id)
		end

		IF OBJECT_ID('tempdb..#points') IS NOT NULL DROP TABLE #points;
		create table #points ( ID int, SourceIntersectID int, TargetIntersectID int, Depth int );
		declare @forwardPoints table ( ID int, SourceIntersectID int, TargetIntersectID int );

		-- get all items directly tied to the focal object.
		insert into #points
			select	top (@maxItems)
				MI.ID, MI.SourceIntersectID, MI.TargetIntersectID, 0
			from	MapItem MI
					inner join [Intersect] SI on SI.ID = MI.SourceIntersectID
					inner join [Intersect] TI ON TI.ID = MI.TargetIntersectID
					inner join @objects O on	( (SI.Subject = O.Type and SI.SubjectID = O.ID) OR (SI.Object = O.Type and SI.ObjectID = O.ID)  ) OR 
												( (TI.Subject = O.Type and TI.SubjectID = O.ID) OR (TI.Object = O.Type and TI.ObjectID = O.ID)  )
			where not exists (select 1 from @rows R where R.Deleting = 1 and R.ID = MI.ID)

			set @maxItems = @maxItems - (select count(*) from #points);

		-- get all items not directly tied to the focal object, but still tied to maps involved above.
		if (@maxItems > 0)
		begin
			insert into #points
				select	top (@maxItems)
					MI.ID, MI.SourceIntersectID, MI.TargetIntersectID, 0
				from	MapItem MI
						inner join	(
									select	ID.MapItemID
									from	MapItemMap DM
											inner join #points D on D.ID = DM.MapItemID
											inner join MapItemMap ID on ID.MapID = DM.MapID and ID.MapItemID not in (
																													select ID from #points
																													)
									) O on O.MapItemID = MI.ID
				where not exists (select 1 from @rows R where R.Deleting = 1 and R.ID = MI.ID);

				set @maxItems = @maxItems - (select count(*) from #points);
		end

		insert into @forwardPoints
			select ID,SourceIntersectID,TargetIntersectID from #points

			--join editor rows to any existing intersects
			if exists(select 1 from @rows)
			begin
				insert into #points
					select	R.ID,
							D1.ID as SourceIntersectID,
							D2.ID as TargetIntersectID,
							0
					from	@rows R
							inner join [Intersect] D1 on 
								R.SourceSubject = D1.[Subject] AND 
								R.SourceObject = D1.[Object] AND 
								R.SourceSubjectID = D1.SubjectID AND 
								R.SourceObjectID = D1.ObjectID
							inner join [Intersect] D2 on 
								R.TargetSubject = D2.[Subject] AND 
								R.TargetObject = D2.[Object] AND 
								R.TargetSubjectID = D2.SubjectID AND 
								R.TargetObjectID = D2.ObjectID
					where	R.Adding = 1 and not exists (select 1 from #points P where P.SourceIntersectID = D1.ID and P.TargetIntersectID = D2.ID)
			end;

		set @currentDepth = 0;

		while( exists(select 1 from #points ps where ps.depth = @currentDepth) and (@currentDepth < @maxDepth) and (@maxItems > 0))
		begin

			set @itemCount = (select count(*) from #points);

			insert into #points
				select	top (@maxItems) 
				    S.ID,
					S.SourceIntersectID,
					S.TargetIntersectID,
					@currentDepth+1
				from	MapItem S
						inner join #points T on T.TargetIntersectID = S.SourceIntersectID and S.ID <> T.ID and T.Depth = @currentDepth
				where	not exists (select 1 from @rows R where R.Deleting = 1 and R.ID = S.ID) and not exists (select ID from #points where ID = S.ID)

			set @maxItems = @maxItems - ((select count(*) from #points) - @itemCount);
			set @itemCount = (select count(*) from #points);

			if (@maxItems > 0)
			begin
				

				insert into #points
					select	top (@maxItems)
						S.ID,
						S.SourceIntersectID,
						S.TargetIntersectID,
						@currentDepth+1
					from	MapItem S
							inner join #points T on T.SourceIntersectID = S.TargetIntersectID and S.ID <> T.ID and T.Depth = @currentDepth
					where	not exists (select 1 from @rows R where R.Deleting = 1 and R.ID = S.ID)
						and not exists (select ID from #points where ID = S.ID)
				set @maxItems = @maxItems - ((select count(*) from #points) - @itemCount);
			end

			set @currentDepth = @currentDepth + 1;
		end
				



		declare @items table (
			ID int,
			SourceIntersectID int, 
			SourceSubjectTypeName nvarchar(500), SourceSubjectName nvarchar(500), SourceSubjectShortName nvarchar(500), SourceSubject varchar(50), SourceSubjectID int, SourceSubjectIconBackColor varchar(7), SourceSubjectIconForeColor varchar(7), 
			SourceObjectTypeName nvarchar(500), SourceObjectName nvarchar(500), SourceObjectShortName nvarchar(500), SourceObject varchar(50), SourceObjectID int, SourceObjectIconBackColor varchar(7), SourceObjectIconForeColor varchar(7),
			
			TargetIntersectID int, 
			TargetSubjectTypeName nvarchar(500), TargetSubjectName nvarchar(500), TargetSubjectShortName nvarchar(500), TargetSubject varchar(50), TargetSubjectID int, TargetSubjectIconBackColor varchar(7), TargetSubjectIconForeColor varchar(7), 
			TargetObjectTypeName nvarchar(500), TargetObjectName nvarchar(500), TargetObjectShortName nvarchar(500), TargetObject varchar(50), TargetObjectID int, TargetObjectIconBackColor varchar(7), TargetObjectIconForeColor varchar(7),

			SourceHasSourceRules bit, TargetHasSourceRules bit
		)

		insert into @items
			select	O.ID,				
					O.SourceIntersectID,
					SS.TypeName as SubjectTypeName,
					SS.DisplayValue as SubjectName,
					SS.DisplayValue as SubjectShortName,
					SI.[Subject],
					SI.SubjectID,
					SS.BackColor as SubjectIconBackColor,
					SS.ForeColor as SubjectIconForeColor,
					SO.TypeName as ObjectTypeName,
					SO.DisplayValue as ObjectName,
					SO.DisplayValue as ObjectShortName,
					SI.[Object],
					SI.ObjectID,
					SO.BackColor as ObjectIconBackColor,
					SO.ForeColor as ObjectIconForeColor,
					O.TargetIntersectID,
					TS.TypeName as SubjectTypeName,
					TS.DisplayValue as SubjectName,
					TS.DisplayValue as SubjectShortName,
					TI.Subject,
					TI.SubjectID,
					TS.BackColor,
					TS.ForeColor,
					TB.TypeName as ObjectTypeName,
					TB.DisplayValue as ObjectName,
					TB.DisplayValue as ObjectShortName,
					TI.Object,
					TI.ObjectID,
					TB.BackColor,
					TB.ForeColor,
					case 
						when SHSR.C > 0 then cast(1 as bit)
						else cast(0 as bit)
					end as SourceHasSourceRules,
										case 
						when THSR.C > 0 then cast(1 as bit)
						else cast(0 as bit)
					end as TargetHasSourceRules
			from	#points O
				inner join PredicateIntersect SI on SI.IntersectID = O.SourceIntersectID
				inner join PredicateIntersect TI on TI.IntersectID = O.TargetIntersectID
				inner join AssetDetail SS on SS.[Object] = SI.[Subject] and SS.ObjectID = SI.SubjectID
				inner join AssetDetail SO on SO.[Object] = SI.[Object] and SO.ObjectID = SI.ObjectID
				inner join AssetDetail TS on TS.[Object] = TI.[Subject] and TS.ObjectID = TI.SubjectID
				inner join AssetDetail TB on TB.[Object] = TI.[Object] and TB.ObjectID = TI.ObjectID
					cross apply (
								select	count(1) as C
								from	MapItem MI 
										inner join MapSequence MS on MS.MapItemID = MI.ID
								where MI.ID = O.ID and (
									(@type = SI.[subject] and @id = SI.subjectid and
										(
											MI.SourceIntersectID in 
											(select id from [intersect] i where 
												(i.[object] = @type and i.objectid = @id) or
												(i.[subject] = @type and i.subjectid = @id)
												)
										)
									)
									or
									(MI.SourceIntersectID in 
										(select id from [intersect] i where
												(i.[object] = @type and i.objectid = @id and i.[subject] = si.[subject] and i.subjectid = si.subjectid) or
												(i.[subject] = @type and i.subjectid = @id and i.[object] = si.[subject] and i.objectid = si.subjectid)
										)
									)

									)
									
								) SHSR
								cross apply (
								select	count(1) as C
								from	MapItem MI 
										inner join MapSequence MS on MS.MapItemID = MI.ID
								where MI.ID = O.ID and (
									(@type = TI.[subject] and @id = TI.subjectid and
										(
											MI.TargetIntersectID in 
											(select id from [intersect] i where 
												(i.[object] = @type and i.objectid = @id) or
												(i.[subject] = @type and i.subjectid = @id)
												)
										)
									)
									or
									(MI.TargetIntersectID in 
										(select id from [intersect] i where
												(i.[object] = @type and i.objectid = @id and i.[subject] = ti.[subject] and i.subjectid = ti.subjectid) or
												(i.[subject] = @type and i.subjectid = @id and i.[object] = ti.[subject] and i.objectid = ti.subjectid)
										)
									)

									)
									
								) THSR


		--if editor data is being passed
		if EXISTS (SELECT 1 FROM @rows)
		begin
			--remove deleting items
			delete I
			from @items I
			inner join @rows R on
				R.SourceSubjectID = I.SourceSubjectID 
				AND R.SourceObjectID = I.SourceObjectID
				AND R.TargetSubjectID = I.TargetSubjectID
				AND R.TargetObjectID = I.TargetObjectID;

			--insert adding items and fill in missing data
			insert into @items
			select
				R.ID,
				R.SourceIntersectID,
				SS.ObjectTypeName as SourceSubjectTypeName,
				coalesce(SS.TextPath, SS.Name) as SourceSubjectName,
				SS.Name as SourceSubjectShortName,
				R.SourceSubject,
				R.SourceSubjectID,
				SS.IconBackColor as SourceSubjectIconBackColor,
				SS.IconForeColor as SourceSubjectIconForeColor,
				SO.ObjectTypeName as SourceObjectTypeName,
				coalesce(SO.TextPath, SO.Name) as SourceObjectName,
				SO.Name as SourceObjectShortName,
				R.SourceObject,
				R.SourceObjectID,
				SO.IconBackColor as SourceObjectIconBackColor,
				SO.IconForeColor as SourceObjectIconForeColor,
				R.TargetIntersectID,
				TS.ObjectTypeName as TargetSubjectTypeName,
				coalesce(TS.TextPath, TS.Name) as TargetSubjectName,
				TS.Name as TargetSubjectShortName,
				R.TargetSubject,
				R.TargetSubjectID,
				TS.IconBackColor as TargetSubjectIconBackColor,
				TS.IconForeColor as TargetSubjectIconForeColor,
				TB.ObjectTypeName as TargetObjectTypeName,
				coalesce(TB.TextPath, TB.Name)  as TargetObjectName,
				TB.Name as TargetObjectShortName,
				R.TargetObject,
				R.TargetObjectID,
				TB.IconBackColor as TargetObjectIconBackColor,
				TB.IconForeColor as TargetObjectIconForeColor,
				0 as SourceHasSourceRules,
				0 as TargetHasSourceRules
			from @rows R 
			inner join cache.ObjectDetails SS on SS.[Object] = R.SourceSubject AND SS.ObjectID = R.SourceSubjectID
			inner join cache.ObjectDetails SO on SO.[Object] = R.SourceObject AND SO.ObjectID = R.SourceObjectID
			inner join cache.ObjectDetails TS on TS.[Object] = R.TargetSubject AND TS.ObjectID = R.TargetSubjectID
			inner join cache.ObjectDetails TB on TB.[Object] = R.TargetObject AND TB.ObjectID = R.TargetObjectID
			where R.Adding = 1
			and not exists (select 1 from @items i where i.SourceIntersectID = r.SourceIntersectID and i.TargetIntersectID = r.TargetIntersectID);
		end
		
		if @view = 0
		begin
			select (
					select distinct
					cast(SI.IntersectTypeID as varchar) + '.' 
					+ cast(I.SourceSubjectID as varchar) + '.'
					+ cast(I.SourceObjectID as varchar) as [sourcekey],
					cast(TI.IntersectTypeID as varchar) + '.' 
					+ cast(I.TargetSubjectID as varchar) + '.'
					+ cast(I.TargetObjectID as varchar) as [targetkey],
					--I.*,
					I.ID
					,I.SourceIntersectID
					,I.SourceSubjectTypeName
					,coalesce(SST.TextPath,I.SourceSubjectName) as SourceSubjectName
					,I.SourceSubjectShortName
					,I.SourceSubject
					,I.SourceSubjectID
					,I.SourceSubjectIconBackColor
					,I.SourceSubjectIconForeColor
					,I.SourceObjectTypeName
					,coalesce(SOT.TextPath,I.SourceObjectName) as SourceObjectName
					,I.SourceObjectShortName
					,I.SourceObject
					,I.SourceObjectID
					,I.SourceObjectIconBackColor
					,I.SourceObjectIconForeColor
					,I.TargetIntersectID
					,I.TargetSubjectTypeName
					,coalesce(TST.TextPath, I.TargetSubjectName) as TargetSubjectName
					,I.TargetSubjectShortName
					,I.TargetSubject
					,I.TargetSubjectID
					,I.TargetSubjectIconBackColor
					,I.TargetSubjectIconForeColor
					,I.TargetObjectTypeName
					,coalesce(OTT.TextPath, I.TargetObjectName) as TargetObjectName
					,I.TargetObjectShortName
					,I.TargetObject
					,I.TargetObjectID
					,I.TargetObjectIconBackColor
					,I.TargetObjectIconForeColor
					,I.SourceHasSourceRules 
					,I.TargetHasSourceRules,
					SI.IntersectTypeID as SourceIntersectTypeID,
					utility.DeriveIntersectTypeName(SIT.ID) as SourceIntersectTypeName,
					TI.IntersectTypeID as TargetIntersectTypeID,
					utility.DeriveIntersectTypeName(TIT.ID) as TargetIntersectTypeName
				from @items I
				inner join [Intersect] SI on SI.ID = I.SourceIntersectID
				left join Asset SS on SS.Object = SI.Subject and SS.ObjectID = SI.SubjectID
				outer apply dbo.GetAssetTextPathById(SS.ID, '/') SST
				left join Asset SO on SO.Object = SI.Object and SO.ObjectID = SI.ObjectID
				outer apply dbo.GetAssetTextPathById(SO.ID, '/') SOT
				inner join IntersectType SIT on SIT.ID = SI.IntersectTypeID
				inner join [Intersect] TI on TI.ID = I.TargetIntersectID
				left join Asset TS on TS.Object = TI.Subject and TS.ObjectID = TI.SubjectID
				outer apply dbo.GetAssetTextPathById(TS.ID, '/') TST
				left join Asset OT on OT.Object = TI.Object and OT.ObjectID = TI.ObjectID
				outer apply dbo.GetAssetTextPathById(OT.ID, '/') OTT
				inner join IntersectType TIT on TIT.ID = TI.IntersectTypeID
				for json path
			) as 'items'
			for json path, WITHOUT_ARRAY_WRAPPER
		end

		if @view = 1
		begin

			insert into @links
					select	distinct
							S.SourceSubject + '.' + cast(S.SourceSubjectID as varchar) as [from],
							S.TargetSubject + '.' + cast(S.TargetSubjectID as varchar) as [to],
							'' as category
					from	@items S
			insert into @nodes
					select	distinct
							A.ID as assetId,
							I.SourceSubject + '.' + cast(I.SourceSubjectID as varchar) as [key],
							I.SourceSubject as [obj],
							I.SourceSubjectID as [objid], 
							I.SourceSubject as [type],
							I.SourceSubjectTypeName as typeName,
							I.SourceSubjectName as name,
							I.SourceSubjectShortName as shortname,
							I.SourceSubjectIconBackColor as back,
							I.SourceSubjectIconForeColor as fore,
							case 
								when I.SourceSubject = @type and I.SourceSubjectID = @id then 'Focal'
								else 'Normal'
							end as template,
							null as other,
							0 as hasSourceRules
					from	@items I
					left join Asset A on A.[Object] = I.SourceSubject and A.ObjectID = I.SourceSubjectID;;

					--perform this update separately to avoid duplicate in the above query
					update n
					set n.HasSourceRules = 1
					from @nodes n
					inner join @items i on n.[key] = (i.SourceSubject + '.' + cast(i.SourceSubjectID as varchar)) and i.SourceHasSourceRules = 1;

			--insert into @nodes
			merge	@nodes as T
			using	(
					select	distinct
							A.ID as assetId,
							I.TargetSubject + '.' + cast(I.TargetSubjectID as varchar) as [key],
							I.TargetSubject as [obj],
							I.TargetSubjectID as [objid], 
							I.TargetSubject as [type],
							I.TargetSubjectTypeName as typeName,
							I.TargetSubjectName as name,
							I.TargetSubjectShortName as shortname,
							I.TargetSubjectIconBackColor as back,
							I.TargetSubjectIconForeColor as fore,
							case 
								when I.TargetSubject = @type and I.TargetSubjectID = @id then 'Focal'
								else 'Normal'
							end as template,
							null as other,
							I.TargetHasSourceRules as HasSourceRules
					from	@items I
					left join Asset A on A.[Object] = I.TargetSubject and A.ObjectID = I.TargetSubjectID
					where	I.TargetSubject + '.' + cast(I.TargetSubjectID as varchar) not in (select [key] from @nodes)
					) S
			on		(T.[key] = S.[key])
			when matched then
			update	set
					T.HasSourceRules = S.HasSourceRules
			when	not matched then
			insert	(assetId, [key], obj, [objid], [type], typeName, name, shortname, back, fore, template, other, HasSourceRules)
			values	(S.assetId, S.[key], S.obj, S.[objid], S.[type], S.typeName, S.name, S.shortname, S.back, S.fore, S.template, S.other, S.HasSourceRules);
					--where	I.TargetSubject + '.' + cast(I.TargetSubjectID as varchar) not in (select [key] from @nodes)

			if @usageOnly = 1 --Remove elements that are not tied to the current object via any Usage predicate type.
			begin
				delete	@nodes
				where	[key] not in 
					(
					--DIRECTLY related to an item via Usage relationship
					select	case 
								when (I.Subject = N.obj and I.SubjectID = N.objid and I.SubjectID <> @id and I.Object = @type and I.ObjectID = @id) then I.Subject + '.' + cast(I.SubjectID as varchar)
								else I.Object + '.' + cast(I.ObjectID as varchar)
							end as [key]
					from	[Intersect] I
							inner join @nodes N on	(
													(I.Subject = N.obj and I.SubjectID = N.objid and I.SubjectID <> @id and I.Object = @type and I.ObjectID = @id) OR
													(I.Object = N.obj and I.ObjectID = N.objid and I.ObjectID <> @id and I.Subject = @type and I.SubjectID = @id)
													)
							inner join IntersectType T on T.ID = I.IntersectTypeID and (I.Deleted = 0 or I.Deleted is null)
							inner join [Predicate] P on P.ID = T.PredicateID and P.[Type] = 10
					union
					--INDIRECTLY related to an item via Usage relationship
					select	N.obj + '.' + cast(N.objid as varchar) as [key]
					from	[Intersect] I1
							inner join [Intersect] I2 on I1.Subject = @type and I1.SubjectID = @id 
														and (
																--(I2.Subject = I1.Subject and I2.SubjectID = I1.SubjectID) --OR
																(I2.Object = I1.Object and I2.ObjectID = I1.ObjectID)
															)
														and I1.ID <> I2.ID
							inner join IntersectType T on T.ID = I2.IntersectTypeID 
														and (I1.Deleted = 0 or I1.Deleted is null)
														and (I2.Deleted = 0 or I2.Deleted is null) 
							inner join @nodes N on (I2.Subject = N.obj and I2.SubjectID = N.objid)
							inner join [Predicate] P on P.ID = T.PredicateID and P.[Type] = 10
					) and [key] <> @type + '.' + cast(@id as varchar)
			end

--select	* from	@links
--select	* from	@nodes

			select	(
					select	*
					from	@links O
					for json path			
					) as 'links',
					(
					select	I.*,
							A.actions
					from	@nodes I
							cross apply (
											select count(1) as actions, max(createdon) as createdon   
											from Issue U
											where U.ObjectID = I.objid AND U.Object = I.obj
										) A
					for json path			
					) as 'nodes'
			for json path, WITHOUT_ARRAY_WRAPPER
		end --view 1

		if @view = 2
		begin
			insert into @links
				select	distinct
						SourceSubject + '.' + cast(SourceSubjectID as varchar) as 'from',
						cast(SourceIntersectID as varchar) + '.S' as 'to',
						'Support' as category
				from	@items
				union
				select	distinct
						cast(SourceIntersectID as varchar) + '.S' as 'from',
						TargetSubject + '.' + cast(TargetSubjectID as varchar) as 'to',
						'' as category
				from	@items O
				where	(SourceObject + cast(SourceObjectID as varchar)) = (TargetObject + cast(TargetObjectID as varchar))
				union
				select	distinct
						cast(SourceIntersectID as varchar) + '.S' as 'from',
						cast(TargetIntersectID as varchar) + '.T' as 'to',
						'' as category
				from	@items O
				where	(SourceObject + cast(SourceObjectID as varchar)) <> (TargetObject + cast(TargetObjectID as varchar))
				--where	TargetIntersectID in (select SourceIntersectID from @items)
				union
				select	distinct
						cast(TargetIntersectID as varchar) + '.T' as 'from',
						TargetSubject + '.' + cast(TargetSubjectID as varchar) as 'to',
						'Support' as category
				from	@items
				where	(SourceObject + cast(SourceObjectID as varchar)) <> (TargetObject + cast(TargetObjectID as varchar))

			insert into @nodes
				select	distinct
						A.ID as assetId,
						SourceSubject + '.' + cast(SourceSubjectID as varchar) as [key],
						SourceSubject as [obj],
						SourceSubjectID as [objid], 
						SourceSubject as [type],
						SourceSubjectTypeName as typeName,
						SourceSubjectName as name,
						SourceSubjectShortName as shortname,
						SourceSubjectIconBackColor as back,
						SourceSubjectIconForeColor as fore,
						case 
							when SourceSubject = @type and SourceSubjectID = @id then 'Focal'
							else 'Normal'
						end as template,
						null as other,
						0 as HasSourceRules
				from	@items 
				left join Asset A on A.[Object] = SourceSubject and A.ObjectID = SourceSubjectID

			insert into @nodes
				select	distinct
						A.ID as assetId,
						cast(SourceIntersectID as varchar) + '.S' as [key],
						SourceObject as [obj],
						SourceObjectID as [objid], 
						SourceObject as [type],
						SourceObjectTypeName as typeName,
						SourceObjectName as name,
						SourceObjectShortName as shortname,
						SourceObjectIconBackColor as back,
						SourceObjectIconForeColor as fore,
						case 
							when SourceObject = @type and SourceObjectID = @id then 'SupportFocal'
							else 'SupportNormal'
						end as template,
						null as other,
						0 as HasSourceRules
				from	@items
				left join Asset A on A.[Object] = SourceObject and A.ObjectID = SourceObjectID

				update n
				set n.HasSourceRules = 1
				from @nodes n
				inner join @items i on n.[key] = (i.SourceSubject + '.' + cast(i.SourceSubjectID as varchar)) and i.SourceHasSourceRules = 1;


			merge	@nodes as T
			using	(
					select	distinct
							A.ID as assetId,
							cast(TargetIntersectID as varchar) + '.T' as [key],
							TargetObject as [obj],
							TargetObjectID as [objid], 
							TargetObject as [type],
							TargetObjectTypeName as typeName,
							TargetObjectName as name,
							TargetObjectShortName as shortname,
							TargetObjectIconBackColor as back,
							TargetObjectIconForeColor as fore,
							case 
								when TargetObject = @type and TargetObjectID = @id then 'SupportFocal'
								else 'SupportNormal'
							end as template,
							null as other,
							TargetHasSourceRules as HasSourceRules
					from	@items
					left join Asset A on A.[Object] = TargetObject and A.ObjectID = TargetObjectID
					where	(SourceObject + cast(SourceObjectID as varchar)) <> (TargetObject + cast(TargetObjectID as varchar))
					) S
			on		(T.[key] = S.[key])
			when	matched then
			update	set
					T.HasSourceRules = S.HasSourceRules
			when	not matched then
			insert	(assetId, [key], obj, [objid], [type], typeName, name, shortname, back, fore, template, other, HasSourceRules)
			values	(S.assetId, S.[key], S.obj, S.[objid], S.[type], S.typeName, S.name, S.shortname, S.back, S.fore, S.template, S.other, S.HasSourceRules);

			merge	@nodes as T
			using	(
					select	distinct
							A.ID as assetId,
							TargetSubject + '.' + cast(TargetSubjectID as varchar) as [key],
							TargetSubject as [obj],
							TargetSubjectID as [objid], 
							TargetSubject as [type],
							TargetSubjectTypeName as typeName,
							TargetSubjectName as name,
							TargetSubjectShortName as shortname,
							TargetSubjectIconBackColor as back,
							TargetSubjectIconForeColor as fore,
							case 
								when TargetSubject = @type and TargetSubjectID = @id then 'Focal'
								else 'Normal'
							end as template,
							null as other,
							TargetHasSourceRules as HasSourceRules
					from	@items
					left join Asset A on A.[Object] = TargetSubject and A.ObjectID = TargetSubjectID
					where	TargetSubject + '.' + cast(TargetSubjectID as varchar) not in (select [key] from @nodes)
					) S
			on		(T.[key] = S.[key])
			when	matched then
			update	set
					T.HasSourceRules = S.HasSourceRules
			when	not matched then
			insert	(assetId, [key], obj, [objid], [type], typeName, name, shortname, back, fore, template, other, HasSourceRules)
			values	(S.assetId, S.[key], S.obj, S.[objid], S.[type], S.typeName, S.name, S.shortname, S.back, S.fore, S.template, S.other, S.HasSourceRules);

--select	* from	@links
--select	* from	@nodes

			if @usageOnly = 1 --Remove elements that are not tied to the current object via any Usage predicate type.
			begin
				declare @usages table ([key] varchar(250))

				insert into @usages
					--DIRECTLY related to an item via Usage relationship
					select	--*,
							case 
								when (I.Subject = N.obj and I.SubjectID = N.objid and I.SubjectID <> @id and I.Object = @type and I.ObjectID = @id) then I.Subject + '.' + cast(I.SubjectID as varchar)
								else I.Object + '.' + cast(I.ObjectID as varchar)
							end as [key]
					from	[Intersect] I
							inner join @nodes N on	(
													(I.Subject = N.obj and I.SubjectID = N.objid and I.SubjectID <> @id and I.Object = @type and I.ObjectID = @id) OR
													(I.Object = N.obj and I.ObjectID = N.objid and I.ObjectID <> @id and I.Subject = @type and I.SubjectID = @id)
													)
							inner join IntersectType T on T.ID = I.IntersectTypeID and (I.Deleted = 0 or I.Deleted is null)
							inner join [Predicate] P on P.ID = T.PredicateID and P.[Type] = 10
					union
					--INDIRECTLY related to an item via Usage relationship
					select	N.obj + '.' + cast(N.objid as varchar) as [key]
					from	[Intersect] I1
							inner join [Intersect] I2 on I1.Subject = @type and I1.SubjectID = @id 
														and (
																--(I2.Subject = I1.Subject and I2.SubjectID = I1.SubjectID) --OR
																(I2.Object = I1.Object and I2.ObjectID = I1.ObjectID)
															)
														and I1.ID <> I2.ID
							inner join IntersectType T on T.ID = I2.IntersectTypeID 
														and (I1.Deleted = 0 or I1.Deleted is null)
														and (I2.Deleted = 0 or I2.Deleted is null) 
							inner join @nodes N on (I2.Subject = N.obj and I2.SubjectID = N.objid)
							inner join [Predicate] P on P.ID = T.PredicateID and P.[Type] = 10

				delete	@nodes
				where	[key] not in 
					(
					select	[key]
					from	@usages
					) 
					and [key] <> @type + '.' + cast(@id as varchar)
					and [template] not like '%Support%'

				delete	@links
				where	[from] not in (select [key] from @nodes)
						or [to] not in (select [key] from @nodes)
				
				delete	@nodes
				where	[template] like '%Support%'
						and [key] not in (
							select	[key]
							from	@nodes 
							where	[template] like '%Support%'
									and [key] in (select [from] from @links)
									and [key] in (select [to] from @links)
						)
			end

--select	* from	@items
--select	* from	@links
--select	* from	@nodes

			select	(
					select	*
					from	@links O
					for json path			
					) as 'links',
					(
					select	I.*,
							A.actions
					from	@nodes I
							cross apply (
											select count(1) as actions, max(createdon) as createdon   
											from Issue U
											where U.ObjectID = I.objid AND U.Object = I.obj
										) A
					for json path			
					) as 'nodes'
			for json path, WITHOUT_ARRAY_WRAPPER
		end --view 2
	end

	if @view in (3,4)
	begin

		IF OBJECT_ID('tempdb..#tFusionPoints') IS NOT NULL
			DROP TABLE #tFusionPoints;

		create table #tFusionPoints (ID int, MapItemID int, SourceFusionAttributeID int, TargetFusionAttributeID int, Depth int, Direction char null);

		CREATE CLUSTERED INDEX PK_temptFusionPoints ON #tFusionPoints ([ID] ASC,[SourceFusionAttributeID] ASC,[TargetFusionAttributeID] ASC, [Depth] ASC, [Direction] ASC);

		declare @tItems table (
			MapItemID int, --MapID int,

			SourceIntersectID int, 
			SourceSubjectTypeName nvarchar(500), SourceSubjectName nvarchar(500), SourceSubjectShortName nvarchar(500), SourceSubject varchar(50), SourceSubjectID int,
			SourceObjectTypeName nvarchar(500), SourceObjectName nvarchar(500), SourceObjectShortName nvarchar(500), SourceObject varchar(50), SourceObjectID int, 
			
			TargetIntersectID int, 
			TargetSubjectTypeName nvarchar(500), TargetSubjectName nvarchar(500), TargetSubjectShortName nvarchar(500), TargetSubject varchar(50), TargetSubjectID int, 
			TargetObjectTypeName nvarchar(500), TargetObjectName nvarchar(500), TargetObjectShortName nvarchar(500), TargetObject varchar(50), TargetObjectID int
		)
	
		if @type = 'FusionAttribute'
			begin
			

				-- iterative approach no cte
				-- insert the starting points
				insert into #tFusionPoints
					select  top (@maxItems) 
							I.ID,
							NULL,
							I.SourceFusionAttributeID,
							I.TargetFusionAttributeID, 
							0,
							'A'
					from	MapRuleItem I
							inner join FusionAttribute SFA on SFA.ID = I.SourceFusionAttributeID and SFA.Deleted = 0
							inner join FusionAttribute TFA on TFA.ID = I.TargetFusionAttributeID and TFA.Deleted = 0
					where	I.SourceFusionAttributeID = @id --or I.TargetFusionAttributeID = @id;

				set @maxItems = @maxItems - (select count(*) from #tFusionPoints);

				if (@maxItems > 0)
					begin
						insert into #tFusionPoints
						select	top (@maxItems)
							    I.ID,
								NULL,
								I.SourceFusionAttributeID,
								I.TargetFusionAttributeID,
								0,
								'A'
						from	MapRuleItem I
								inner join FusionAttribute SFA on SFA.ID = I.SourceFusionAttributeID and SFA.Deleted = 0
								inner join FusionAttribute TFA on TFA.ID = I.TargetFusionAttributeID and TFA.Deleted = 0
						where	I.TargetFusionAttributeID = @id and 
							not exists (select 1 from #tFusionPoints pt where pt.SourceFusionAttributeID = I.TargetFusionAttributeID and pt.TargetFusionAttributeID = I.SourceFusionAttributeID)

						set @maxItems = @maxItems - (select count(*) from #tFusionPoints);
					end


				--insert adding items if editor data is present
				if EXISTS (SELECT 1 FROM @technicalRows)
				begin
					insert into #tFusionPoints
					select
						ID,
						MapItemID,
						SourceFusionAttributeID,
						TargetFusionAttributeID,
						0,
						'A'
					from @technicalRows
					where Adding = 1;
				end;

				--loop through until there are no more new levels
				set @currentDepth = 0;

				while(exists(select 1 from #tFusionPoints ps where ps.depth = @currentDepth) and (@currentDepth < @maxDepth) and (@maxItems > 0))
				begin
					set @itemCount = (select count(*) from #tFusionPoints)

					insert into #tFusionPoints
						select distinct	top (@maxItems)
								S.ID,
								NULL,
								S.SourceFusionAttributeID,
								S.TargetFusionAttributeID,
								@currentDepth+1,
								'F'
						from	MapRuleItem S
								inner join #tFusionPoints FP on FP.SourceFusionAttributeID = S.TargetFusionAttributeID and FP.Depth = @currentDepth and FP.Direction in('A','F')
						where not exists (select ID from #tFusionPoints where ID = S.ID)
							and not exists (select 1 from @technicalRows R where Deleting = 1 and R.ID = S.ID);

						set @maxItems = @maxItems - ((select count(*) from #tFusionPoints) - @itemCount);
						set @itemCount = (select count(*) from #tFusionPoints);

						if @maxItems > 0
						begin
							insert into #tFusionPoints
							select distinct top (@maxItems)	
									S.ID,
									NULL,
									S.SourceFusionAttributeID,
									S.TargetFusionAttributeID,
									@currentDepth+1,
									'B'
							from	MapRuleItem S
									inner join #tFusionPoints FP on FP.TargetFusionAttributeID = S.SourceFusionAttributeID and FP.Depth = @currentDepth and FP.Direction in('A','B')
							where not exists (select ID from #tFusionPoints where ID = S.ID) 
									and not exists (select 1 from @technicalRows R where Deleting = 1 and R.ID = S.ID);

							set @maxItems = @maxItems - ((select count(*) from #tFusionPoints) - @itemCount);
							set @itemCount = (select count(*) from #tFusionPoints);
						end


						set @currentDepth = @currentDepth + 1;
						print @currentDepth;
				end
				

				--remove deleting items if editor data is present
				if EXISTS (SELECT 1 FROM @technicalRows)
				begin
					
					delete I
					from #tFusionPoints I
					inner join @technicalRows R on R.Deleting = 1 AND R.ID = I.ID;
				end

				-- get all items directly tied to the focal object.
				insert into @tItems
					select	MI.ID,
					
							MI.SourceIntersectID,
							SI.SubjectTypeName,
							SI.SubjectName,
							SI.SubjectShortName,
							SI.Subject,
							SI.SubjectID,
							SI.ObjectTypeName,
							SI.ObjectName,
							SI.ObjectShortName,
							SI.Object,
							SI.ObjectID,

							MI.TargetIntersectID,
							TI.SubjectTypeName,
							TI.SubjectName,
							TI.SubjectShortName,
							TI.Subject,
							TI.SubjectID,
							TI.ObjectTypeName,
							TI.ObjectName,
							TI.ObjectShortName,
							TI.Object,
							TI.ObjectID

					from	#tFusionPoints F
							inner join MapRuleItemMapItem J on J.MapRuleItemID = F.ID
							inner join MapItem MI on MI.ID = J.MapItemID
							inner join IntersectDetail SI on SI.ID = MI.SourceIntersectID
							inner join IntersectDetail TI ON TI.ID = MI.TargetIntersectID
 

				-- get all items not directly tied to the focal object, but still tied to maps involved above.
				insert into @tItems
					select	MI.ID,
							--NULL,
					
							MI.SourceIntersectID,
							SI.SubjectTypeName,
							SI.SubjectName,
							SI.SubjectShortName,
							SI.Subject,
							SI.SubjectID,
							SI.ObjectTypeName,
							SI.ObjectName,
							SI.ObjectShortName,
							SI.Object,
							SI.ObjectID,

							MI.TargetIntersectID,
							TI.SubjectTypeName,
							TI.SubjectName,
							TI.SubjectShortName,
							TI.Subject,
							TI.SubjectID,
							TI.ObjectTypeName,
							TI.ObjectName,
							TI.ObjectShortName,
							TI.Object,
							TI.ObjectID

					from	MapItem MI
							inner join	(
										select	ID.MapItemID
										from	MapItemMap DM
												inner join @tItems D on D.MapItemID = DM.MapItemID
												inner join MapItemMap ID on ID.MapID = DM.MapID and ID.MapItemID not in (
																														select MapItemID from @tItems
																														)
										) O on O.MapItemID = MI.ID
							inner join [IntersectDetail] SI on SI.ID = MI.SourceIntersectID
							inner join [IntersectDetail] TI on TI.ID = MI.TargetIntersectID
			end
		else
			begin
				declare @tBusinessPoints table ( ID int, SourceIntersectID int, TargetIntersectID int )

				insert into @objects values (@type, @id)

				if not exists(
					select	MI.ID
					from	MapItem MI
							inner join [Intersect] SI on SI.ID = MI.SourceIntersectID --IntersectDetail
							inner join [Intersect] TI ON TI.ID = MI.TargetIntersectID --IntersectDetail
					where 	( (SI.Subject = @type and SI.SubjectID = @id) OR (SI.Object = @type and SI.ObjectID = @id)  )
							OR ( (TI.Subject = @type and TI.SubjectID = @id) OR (TI.Object = @type and TI.ObjectID = @id)  )
				)
				begin
					insert into @objects
						select	case 
									when I.Subject = @type and I.SubjectID = @id then I.Object
									else I.Subject
								end,
								case 
									when I.Subject = @type and I.SubjectID = @id then I.ObjectID 
									else I.SubjectID 
								end
						from	[Intersect] I
								inner join IntersectType T on T.ID = I.IntersectTypeID 
								inner join [Predicate] P on P.ID = T.PredicateID and P.Type = 6
						where	(I.Subject = @type and I.SubjectID = @id) or (I.Object = @type and I.ObjectID = @id)
				end

				-- get all items directly tied to the focal object.
				insert into @tBusinessPoints
					select	MI.ID, MI.SourceIntersectID, MI.TargetIntersectID
					from	MapItem MI
							inner join [Intersect] SI on SI.ID = MI.SourceIntersectID
							inner join [Intersect] TI ON TI.ID = MI.TargetIntersectID
							inner join @objects O on	( (SI.Subject = O.Type and SI.SubjectID = O.ID) OR (SI.Object = O.Type and SI.ObjectID = O.ID)  ) OR 
														( (TI.Subject = O.Type and TI.SubjectID = O.ID) OR (TI.Object = O.Type and TI.ObjectID = O.ID)  )

				-- get all items not directly tied to the focal object, but still tied to maps involved above.
				insert into @tBusinessPoints
					select	MI.ID, MI.SourceIntersectID, MI.TargetIntersectID
					from	MapItem MI
							inner join	(
										select	ID.MapItemID
										from	MapItemMap DM
												inner join @tBusinessPoints D on D.ID = DM.MapItemID
												inner join MapItemMap ID on ID.MapID = DM.MapID and ID.MapItemID not in (
																														select ID from @tBusinessPoints
																														)
										) O on O.MapItemID = MI.ID;

				with cte as (
					select	ID,
							SourceIntersectID,
							TargetIntersectID,
							1 as [Level]
					from	@tBusinessPoints
					union all
					select	S.ID,
							S.SourceIntersectID,
							S.TargetIntersectID,
							T.[Level] + 1 as [Level]
					from	MapItem S
							inner join cte T on T.SourceIntersectID = S.TargetIntersectID and S.ID <> T.ID
					where	T.[Level] <= 25
				)
				insert into @tBusinessPoints
					select ID, SourceIntersectID, TargetIntersectID from cte where ID not in (select ID from @tBusinessPoints)

					insert into @tItems
					select	O.ID,

							O.SourceIntersectID,
							SIS.TypeName as SourceSubjectTypeName,
							SIS.DisplayValue as SourceSubjectName,
							SIS.DisplayValue as SourceSubjectShortName,
							SIS.Object as SourceSubject,
							SIS.ObjectID as SourceSubjectID,
							SIO.TypeName as SourceObjectTypeName,
							SIO.DisplayValue as SourceObjectName,
							SIO.DisplayValue as SourceObjectShortName,
							SIO.Object as SourceObject,
							SIO.ObjectID as SourceObjectID,

							O.TargetIntersectID,
							TIS.TypeName as TargetSubjectTypeName,
							TIS.DisplayValue as TargetSubjectName,
							TIS.DisplayValue as TargetSubjectShortName,
							TIS.Object as TargetSubject,
							TIS.ObjectID as TargetSubjectID,
							TIO.TypeName as TargetObjectTypeName,
							TIO.DisplayValue as TargetObjectName,
							TIO.DisplayValue as TargetObjectShortName,
							TIO.Object as TargetObject,
							TIO.ObjectID as TargetObjectID
					from	@tBusinessPoints O
					inner join [Intersect] SI on SI.ID = O.SourceIntersectID
					inner join [Intersect] TI on TI.ID = O.TargetIntersectID
					inner join AssetDetail SIS on SIS.Object = SI.Subject and SIS.ObjectID = SI.SubjectID
					inner join AssetDetail SIO on SIO.Object = SI.Object and SIO.ObjectID = SI.ObjectID
					inner join AssetDetail TIS on TIS.Object = TI.Subject and TIS.ObjectID = TI.SubjectID
					inner join AssetDetail TIO on TIO.Object = TI.Object and TIO.ObjectID = TI.ObjectID


				insert into #tFusionPoints
					select	top (@maxItems) 
							J.MapRuleItemID,
							J.MapItemID,
							T.SourceFusionAttributeID,
							T.TargetFusionAttributeID,
							0,
							'A'
					from	@tItems I
							inner join MapRuleItemMapItem J on J.MapItemID = I.MapItemID
							inner join MapRuleItem T on T.ID = J.MapRuleItemID
							inner join FusionAttribute SFA on SFA.ID = T.SourceFusionAttributeID and SFA.Deleted = 0
							inner join FusionAttribute TFA on TFA.ID = T.TargetFusionAttributeID and TFA.Deleted = 0;

				set @maxItems = @maxItems - (select count(*) from #tFusionPoints);
				
				--insert adding items if editor data is passed
				if EXISTS (SELECT 1 FROM @technicalRows)
				begin
					insert into #tFusionPoints
					select
						ID,
						MapItemID,
						SourceFusionAttributeID,
						TargetFusionAttributeID,
						0,
						'A'
					from @technicalRows
					where Adding = 1;
				end;


	

				-- begin iterative version
				--loop through until there are no more new levels
				set @currentDepth = 0;
				
				while( exists(select 1 from #tFusionPoints ps where ps.depth = @currentDepth) and (@currentDepth < @maxDepth) and (@maxItems > 0))
				begin	
					set @itemCount = (select count(*) from #tFusionPoints);

					insert into #tFusionPoints
						select distinct top (@maxItems)	
							    S.ID,
								NULL,
								S.SourceFusionAttributeID,
								S.TargetFusionAttributeID,
								@currentDepth+1,
								'F'
						from	MapRuleItem S
								inner join #tFusionPoints FP on FP.SourceFusionAttributeID = S.TargetFusionAttributeID and FP.Depth = @currentDepth and FP.Direction in('A','F')
						where not exists (select ID from #tFusionPoints where ID = S.ID)
							and not exists (select 1 from @technicalRows R where Deleting = 1 and R.ID = S.ID);

					set @maxItems = @maxItems - ((select count(*) from #tFusionPoints) - @itemCount);
					set @itemCount = (select count(*) from #tFusionPoints);

					if (@maxItems > 0)
					begin
						insert into #tFusionPoints
							select distinct	top (@maxItems) 
							        S.ID,
									NULL,
									S.SourceFusionAttributeID,
									S.TargetFusionAttributeID,
									@currentDepth+1,
									'B'
							from	MapRuleItem S
									inner join #tFusionPoints FP on FP.TargetFusionAttributeID = S.SourceFusionAttributeID and FP.Depth = @currentDepth and FP.Direction in('A','B')
							where not exists (select ID from #tFusionPoints where ID = S.ID) 
									and not exists (select 1 from @technicalRows R where Deleting = 1 and R.ID = S.ID);

					set @maxItems = @maxItems - ((select count(*) from #tFusionPoints) - @itemCount);
					end


						set @currentDepth = @currentDepth + 1;
						print @currentDepth;
				end

				-- end iterative version

				--remove deleting items if editor data is present
				if EXISTS (SELECT 1 FROM @technicalRows)
				begin
					delete I
					from #tFusionPoints I
					inner join @technicalRows R on R.Deleting = 1 AND R.ID = I.ID;
				end;
			end

		if @view = 3
		begin
		--Load tables we will return to caller.
		insert into @links
			select	distinct
					cast(S.SourceFusionAttributeID as varchar),-- + '.' + coalesce(B.SourceSubject, '0') + '.' + coalesce(cast(B.SourceSubjectID as varchar), '0') as [from],
					cast(S.TargetFusionAttributeID as varchar),-- + '.' + coalesce(B.TargetSubject, '0') + '.' + coalesce(cast(B.TargetSubjectID as varchar), '0') as [to],
					'' as category
			from	#tFusionPoints S
					left join MapRuleItemMapItem J on J.MapRuleItemID = S.ID
					left join @tItems B on B.MapItemID = J.MapItemID
		insert into @nodes
			select	distinct
					A.ID as assetId,
					cast(S.SourceFusionAttributeID as varchar),-- + '.' + coalesce(B.SourceSubject, '0') + '.' + coalesce(cast(B.SourceSubjectID as varchar), '0') as [key],
					'FusionAttribute' as [obj],
					SourceFusionAttributeID as [objid], 
					'FusionAttribute' as [type],
					T.Name as typeName,
					FA.TextPath as name,
					FA.Name as shortname,
					COALESCE(ST.IconBackColor, '#000') as back,
					COALESCE(ST.IconForeColor, '#fff') as fore,
					'Fusion' as template,
					B.SourceSubjectTypeName + ' : ' + B.SourceSubjectName as other,
					null
			from	#tFusionPoints S
					inner join FusionAttribute FA on FA.ID = S.SourceFusionAttributeID
					inner join FusionAttributeType T on T.ID = FA.FusionAttributeTypeID
					left join ObjectStyle ST on ST.ObjectType = 'FusionAttributeType' and ST.ObjectID = T.ID
					left join MapRuleItemMapItem J on J.MapRuleItemID = S.ID
					left join @tItems B on B.MapItemID = J.MapItemID
					left join Asset A on A.[Object] = 'FusionAttribute' and A.ObjectID = SourceFusionAttributeID
		insert into @nodes
			select	distinct
					A.ID as assetId,
					cast(S.TargetFusionAttributeID as varchar),-- + '.' + coalesce(B.TargetSubject, '0') + '.' + coalesce(cast(B.TargetSubjectID as varchar), '0') as [key],
					'FusionAttribute' as [obj],
					TargetFusionAttributeID as [objid], 
					'FusionAttribute' as [type],
					T.Name as typeName,
					FA.TextPath as name,
					FA.Name as shortname,
					COALESCE(ST.IconBackColor, '#000') as back,
					COALESCE(ST.IconForeColor, '#fff') as fore,
					'Fusion' as template,
					B.TargetSubjectTypeName + ' : ' + B.TargetSubjectName as other,
					null
			from	#tFusionPoints S
					inner join FusionAttribute FA on FA.ID = S.TargetFusionAttributeID
					inner join FusionAttributeType T on T.ID = FA.FusionAttributeTypeID
					left join ObjectStyle ST on ST.ObjectType = 'FusionAttributeType' and ST.ObjectID = T.ID
					left join MapRuleItemMapItem J on J.MapRuleItemID = S.ID
					left join @tItems B on B.MapItemID = J.MapItemID
					left join Asset A on A.[Object] = 'FusionAttribute' and A.ObjectID = TargetFusionAttributeID
			where	cast(S.TargetFusionAttributeID as varchar) + '.' + coalesce(B.TargetSubject, '0') + '.' + coalesce(cast(B.TargetSubjectID as varchar), '0') not in (select [key] from @nodes)

			--gets rid of dupes
			delete	@nodes 
			where	other is null 
					and (obj + cast([objid] as varchar)) in (
															select	(obj + cast([objid] as varchar))
															from	@nodes 
															where	other is not null
															)
			delete	T
			from	@links T
					left join @nodes S on S.[key] = T.[from] or S.[key] = T.[to]
			where	S.[key] is null
			
			select	(
					select	*
					from	@links O
					for json path			
					) as 'links',
					(
					select	distinct
							*
					from	@nodes
					for json path			
					) as 'nodes'
			for json path, WITHOUT_ARRAY_WRAPPER
		end --view 3

		if @view = 4
		begin
			select (
				select distinct
					F.ID,
					I.MapItemID,
					F.SourceFusionAttributeID,
					FS.TextPath as SourceFusionAttributeName,
					F.TargetFusionAttributeID,
					FT.TextPath as TargetFusionAttributeName 
				from #tFusionPoints F
				left join @tItems I on I.MapItemID = F.MapItemID
				inner join FusionAttribute FS on FS.ID = F.SourceFusionAttributeID
				inner join FusionAttribute FT on FT.ID = F.TargetFusionAttributeID
				for json path
				) as 'items'
			for json path, WITHOUT_ARRAY_WRAPPER
		end --view 4
	end
end
GO

alter procedure [utility].[AddAuditEntry]
	@DependentObject varchar(50),
	@DependentObjectID int,
	@ResourceID int,
	@Date datetime,
	@Action varchar(15),
	@MainObject varchar(50),
	@MainObjectID int
as
begin
	set nocount on;
	declare @DependentObjectName nvarchar(250),
			@MainObjectTypeName nvarchar(250),
			@MainObjectName nvarchar(250),
			@MainDescription nvarchar(max)
	
	declare @tbl table (ID int identity, FieldTypeID int, FieldName nvarchar(250), NewValue nvarchar(max), MostRecentVersion int, Updated bit)

	-- Object Resolution --------------------------------------------------
	if @DependentObject is not null
	begin
		if @DependentObject = 'Fusion'				begin		select @DependentObjectName = Name from Fusion where ID = @DependentObjectID				end		
		if @DependentObject = 'FusionType'			begin		select @DependentObjectName = Name from FusionType where ID = @DependentObjectID			end
		if @DependentObject = 'Group'				begin		select @DependentObjectName = Name from [Group] where ID = @DependentObjectID				end		
		if @DependentObject = 'LoadType'			begin		select @DependentObjectName = Name from LoadType where ID = @DependentObjectID				end
		if @DependentObject = 'LookupType'			begin		select @DependentObjectName = Name from LookupType where ID = @DependentObjectID			end
		if @DependentObject = 'IssueType'			begin		select @DependentObjectName = Name from IssueType where ID = @DependentObjectID				end
		if @DependentObject = 'IntersectType'		begin		select @DependentObjectName = ITyName.Name from IntersectType O cross apply dbo.getIntersectTypeNames(O.ID) ITyName where O.ID = @DependentObjectID			end
		
		if @DependentObject = 'ReferenceItemType'	begin		select @DependentObjectName = Name from ReferenceItemType where ID = @DependentObjectID		end		
		if @DependentObject = 'Report'				begin		select @DependentObjectName = Name from Report where ID = @DependentObjectID				end
		if @DependentObject = 'ResponsibilityType'	begin		select @DependentObjectName = Name from ResponsibilityType where ID = @DependentObjectID	end		
		if @DependentObject = 'StatisticType'		begin		select @DependentObjectName = Name from StatisticType where ID = @DependentObjectID			end
		if @DependentObject = 'SurveyType'			begin		select @DependentObjectName = Name from SurveyType where ID = @DependentObjectID			end				
		else			
			begin		
				select @DependentObjectName = Name from cache.objectdetails where ObjectID = @DependentObjectID	and Object = @DependentObject	
			end
		
	end
	----------------------------------------------------------------------

	-- Action Object Resolution ------------------------------------------


	-- Relevant ONLY to: Artifact, ArtifactType
	if @MainObject = 'Artifact'
	begin
		select	@MainObjectTypeName = A.TypeName,
				@MainObjectName = A.DisplayValue
		from	AssetDetail A				
		where	A.ObjectID = @MainObjectID and A.[Object] = @MainObject

	end

	-- Relevant ONLY to: ArtifactType
	if @MainObject = 'ArtifactType'
	begin
		select	@MainObjectTypeName = 'Artifact Type',
				@MainObjectName = A.Name 
		from	AssetType A
		where	A.ObjectID = @MainObjectID and A.[Object] = @MainObject

		insert into @tbl  select 0, 'Name', Name, 0, 0 from AssetType where ObjectID = @MainObjectID and [Object] = @MainObject		
		insert into @tbl  select 0, 'Description', Description, 0, 0 from AssetType where ObjectID = @MainObjectID and [Object] = @MainObject	
		insert into @tbl  select 0, 'DisplayFormat', DisplayFormat, 0, 0 from AssetType where ObjectID = @MainObjectID and [Object] = @MainObject					
	end
	
	-- Relevant ONLY to: Artifact, Fusion, FusionAttribute, Intersect, Taxonomy
	if @MainObject = 'Attribute'
	begin
		select	@MainObjectTypeName = A.TypeName,
				@MainObjectName = A.DisplayValue
		from	AssetDetail A				
		where	A.ObjectID = @MainObjectID and A.[Object] = @MainObject
	end

	-- Relevant ONLY to: AttributeType
	if @MainObject = 'AttributeType'
	begin
		select	@MainObjectTypeName = 'Attribute Type',
				@MainObjectName = O.Name
		from	AttributeType O
		where	O.ID = @MainObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from AttributeType where ID = @MainObjectID
		insert into @tbl  select 0, 'ParentID', ParentID, 0, 0 from AttributeType where ID = @MainObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from AttributeType where ID = @MainObjectID		
		insert into @tbl  select 0, 'AttributeTypeCategoryID', AttributeTypeCategoryID, 0, 0 from AttributeType where ID = @MainObjectID
	end

	-- Relevant ONLY to: AttributeType
	if @MainObject = 'FieldType'
	begin
		select	@MainObjectTypeName = 'Field Type',
				@MainObjectName = O.FriendlyName
		from	FieldType O
		where	O.ID = @MainObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from FieldType where ID = @MainObjectID
		insert into @tbl  select 0, 'FriendlyName', FriendlyName, 0, 0 from FieldType where ID = @MainObjectID
		insert into @tbl  select 0, 'Description', [Description], 0, 0 from FieldType where ID = @MainObjectID
		insert into @tbl  select 0, 'DisplayDescription', DisplayDescription, 0, 0 from FieldType where ID = @MainObjectID
		insert into @tbl  select 0, 'FormDescription', FormDescription, 0, 0 from FieldType where ID = @MainObjectID
		insert into @tbl  select 0, 'Type', [Type], 0, 0 from FieldType where ID = @MainObjectID
		insert into @tbl  select 0, 'LookupObjectType', LookupObjectType, 0, 0 from FieldType where ID = @MainObjectID
		insert into @tbl  select 0, 'LookupObjectID', LookupObjectID, 0, 0 from FieldType where ID = @MainObjectID
		insert into @tbl  select 0, 'LookupDisplayFormat', LookupDisplayFormat, 0, 0 from FieldType where ID = @MainObjectID
		insert into @tbl  select 0, 'MinimumLength', MinimumLength, 0, 0 from FieldType where ID = @MainObjectID
		insert into @tbl  select 0, 'MaximumLength', MaximumLength, 0, 0 from FieldType where ID = @MainObjectID
		insert into @tbl  select 0, 'Length', [Length], 0, 0 from FieldType where ID = @MainObjectID
		insert into @tbl  select 0, 'SortOrder', [SortOrder], 0, 0 from FieldType where ID = @MainObjectID
		insert into @tbl  select 0, 'IsRequired', [IsRequired], 0, 0 from FieldType where ID = @MainObjectID
		insert into @tbl  select 0, 'IsListable', [IsListable], 0, 0 from FieldType where ID = @MainObjectID
		insert into @tbl  select 0, 'Category', [Category], 0, 0 from FieldType where ID = @MainObjectID
		insert into @tbl  select 0, 'IsDisplayable', [IsDisplayable], 0, 0 from FieldType where ID = @MainObjectID
		insert into @tbl  select 0, 'IsEditable', [IsEditable], 0, 0 from FieldType where ID = @MainObjectID
		insert into @tbl  select 0, 'IsPartOfKey', [IsPartOfKey], 0, 0 from FieldType where ID = @MainObjectID
		insert into @tbl  select 0, 'AllowMultipleValues', [AllowMultipleValues], 0, 0 from FieldType where ID = @MainObjectID
		insert into @tbl  select 0, 'ColumnOrder', [ColumnOrder], 0, 0 from FieldType where ID = @MainObjectID
		insert into @tbl  select 0, 'ColumnWidth', [ColumnWidth], 0, 0 from FieldType where ID = @MainObjectID
		insert into @tbl  select 0, 'IsPrimaryFilter', [IsPrimaryFilter], 0, 0 from FieldType where ID = @MainObjectID
	end

	-- Relevant ONLY to: Fusion
	if @MainObject = 'Fusion'
	begin
		select	@MainObjectTypeName = T.Name,
				@MainObjectName = O.Name 
		from	Fusion O
				inner join FusionType T on T.ID = O.FusionTypeID
		where	O.ID = @MainObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from Fusion where ID = @MainObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from Fusion where ID = @MainObjectID
		insert into @tbl  select 0, 'Enabled', Enabled, 0, 0 from Fusion where ID = @MainObjectID
		insert into @tbl  select 0, 'Manual', Manual, 0, 0 from Fusion where ID = @MainObjectID
		insert into @tbl  select 0, 'LockPromotedItems', LockPromotedItems, 0, 0 from Fusion where ID = @MainObjectID
		insert into @tbl  select 0, 'IntervalType', IntervalType, 0, 0 from Fusion where ID = @MainObjectID
		insert into @tbl  select 0, 'Interval', Interval, 0, 0 from Fusion where ID = @MainObjectID
		insert into @tbl  select 0, 'ForceRefresh', ForceRefresh, 0, 0 from Fusion where ID = @MainObjectID
	end

	-- Relevant ONLY to: FusionAttributeType, FusionType
	if @MainObject = 'FusionAttributeType'
	begin
		select	@MainObjectTypeName = T.Name,
				@MainObjectName = O.Name 
		from	FusionAttributeType O
				inner join FusionType T on T.ID = O.FusionTypeID
		where	O.ID = @MainObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from FusionAttributeType where ID = @MainObjectID
		insert into @tbl  select 0, 'Assignable', Assignable, 0, 0 from FusionAttributeType where ID = @MainObjectID
	end

	-- Relevant ONLY to: FusionType
	if @MainObject = 'FusionType'
	begin
		select	@MainObjectTypeName = 'Fusion Type',
				@MainObjectName = O.Name 
		from	FusionType O
		where	O.ID = @MainObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from FusionType where ID = @MainObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from FusionType where ID = @MainObjectID
	end

	-- Relevant ONLY to: Group
	if @MainObject = 'Group'
	begin
		select	@MainObjectTypeName = 'Group',
				@MainObjectName = O.Name 
		from	[Group] O
		where	O.ID = @MainObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from [Group] where ID = @MainObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from [Group] where ID = @MainObjectID
		insert into @tbl  select 0, 'PrimaryOwnerResourceID', PrimaryOwnerResourceID, 0, 0 from [Group] where ID = @MainObjectID
		insert into @tbl  select 0, 'SecondaryOwnerResourceID', SecondaryOwnerResourceID, 0, 0 from [Group] where ID = @MainObjectID
	end
	
	-- Relevant ONLY to: Artifact, FusionAttribute, Intersect, Taxonomy, Policy, Rule
	if @MainObject = 'Intersect'
	begin
		select	@MainObjectTypeName = ITyName.Name,
				@MainObjectName = Iname.Name 
		from	[Intersect] O
				inner join [IntersectType] T on T.ID = O.IntersectTypeID
				cross apply dbo.getIntersectNames(O.ID) Iname
				cross apply dbo.getIntersectTypeNames(T.ID) ITyName
		where	O.ID = @MainObjectID
	end
	
	-- Relevant ONLY to: IntersectType
	if @MainObject = 'IntersectType'
	begin
		select	@MainObjectTypeName = 'Intersect Type',
				@MainObjectName = ITyName.Name 
		from	IntersectType O
				cross apply dbo.getIntersectTypeNames(O.ID) ITyName
		where	O.ID = @MainObjectID

		insert into @tbl  select 0, 'Name', ITyName.Name, 0, 0 from	IntersectType O cross apply dbo.getIntersectTypeNames(O.ID) ITyName where	O.ID = @MainObjectID
		insert into @tbl  select 0, 'SubjectCardinality', SubjectCardinality, 0, 0 from	IntersectType O where	ID = @MainObjectID
		insert into @tbl  select 0, 'ObjectCardinality', ObjectCardinality, 0, 0 from	IntersectType O where	ID = @MainObjectID
		insert into @tbl  select 0, 'Predicate', Name, 0, 0 from predicate where id = (select predicateid from intersecttype where id = @MainObjectID)
	end

	-- Relevant ONLY to: LookupType
	if @MainObject = 'IssueType'
	begin
		select	@MainObjectTypeName = 'Action Type',
				@MainObjectName = O.Name 
		from	IssueType O
		where	O.ID = @MainObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from IssueType where ID = @MainObjectID
		insert into @tbl  select 0, 'Description', [Description], 0, 0 from IssueType where ID = @MainObjectID
	end

	-- Relevant ONLY to: LoadType
	if @MainObject = 'LoadType'
	begin
		select	@MainObjectTypeName = 'Load Type',
				@MainObjectName = O.Name 
		from	LoadType O
		where	O.ID = @MainObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from LoadType where ID = @MainObjectID
	end

	-- Relevant ONLY to: LoadType
	if @MainObject = 'LoadTypeField'
	begin
		select	@MainObjectTypeName = T.Name,
				@MainObjectName = O.Name 
		from	LoadTypeField O
				inner join LoadType T on T.ID = O.LoadTypeID
		where	O.ID = @MainObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from LoadTypeField where ID = @MainObjectID
		insert into @tbl  select 0, 'SortOrder', SortOrder, 0, 0 from LoadTypeField where ID = @MainObjectID
		insert into @tbl  select 0, 'LookupObjectType', LookupObjectType, 0, 0 from LoadTypeField where ID = @MainObjectID
		insert into @tbl  select 0, 'LookupObjectID', LookupObjectID, 0, 0 from LoadTypeField where ID = @MainObjectID
		insert into @tbl  select 0, 'LookupFieldName', LookupFieldName, 0, 0 from LoadTypeField where ID = @MainObjectID
	end

	-- Relevant ONLY to: LoadType
	if @MainObject = 'LoadTypeRule'
	begin
		select	@MainObjectTypeName = T.Name,
				@MainObjectName = T.Name + ' Rule ' + cast(O.ID as nvarchar(15))
		from	LoadTypeRule O
				inner join LoadType T on T.ID = O.LoadTypeID
		where	O.ID = @MainObjectID

		insert into @tbl  select 0, 'LoadTypeRuleGroup', case LoadTypeRuleGroup when 1 then 'Promotion' when 2 then 'Relation' else 'Unknown' end, 0, 0 from LoadTypeRule where ID = @MainObjectID
		insert into @tbl  select 0, 'SortOrder', SortOrder, 0, 0 from LoadTypeRule where ID = @MainObjectID
		insert into @tbl  select 0, 'ObjectType', ObjectType, 0, 0 from LoadTypeRule where ID = @MainObjectID
		insert into @tbl  select 0, 'ObjectID', ObjectID, 0, 0 from LoadTypeRule where ID = @MainObjectID
		insert into @tbl  select 0, 'UniqueLoadTypeFieldID', UniqueLoadTypeFieldID, 0, 0 from LoadTypeRule where ID = @MainObjectID
	end

	-- Relevant ONLY to: LoadType
	if @MainObject = 'LoadTypeRuleItem'
	begin
		select	@MainObjectTypeName = T.Name,
				@MainObjectName = 'Rule Field ' + cast(O.ID as nvarchar(15))
		from	LoadTypeRuleItem O
				inner join LoadTypeRule R on R.ID = O.LoadTypeRuleID
				inner join LoadType T on T.ID = R.LoadTypeID
		where	O.ID = @MainObjectID

		insert into @tbl  select 0, 'SourceLoadTypeFieldID', SourceLoadTypeFieldID, 0, 0 from LoadTypeRuleItem where ID = @MainObjectID
		insert into @tbl  select 0, 'TargetFieldName', TargetFieldName, 0, 0 from LoadTypeRuleItem where ID = @MainObjectID
		insert into @tbl  select 0, 'IsCustomField', IsCustomField, 0, 0 from LoadTypeRuleItem where ID = @MainObjectID
	end

	-- Relevant ONLY to: LookupType
	if @MainObject = 'Lookup'
	begin
		select	@MainObjectTypeName = T.Name,
				@MainObjectName = T.Name + ' Lookup ' + cast(O.ID as nvarchar(15))
		from	[Lookup] O
				inner join LookupType T on T.ID = O.LookupTypeID
		where	O.ID = @MainObjectID
	end

	-- Relevant ONLY to: LookupType
	if @MainObject = 'LookupType'
	begin
		select	@MainObjectTypeName = 'Lookup Type',
				@MainObjectName = O.Name 
		from	LookupType O
		where	O.ID = @MainObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from LookupType where ID = @MainObjectID
	end
	
	-- Relevant ONLY to: Policy
	if @MainObject = 'Policy'
	begin
		select	@MainObjectTypeName = 'Policy',
				@MainObjectName = A.DisplayValue
		from	AssetDetail A				
		where	A.ObjectID = @MainObjectID and A.[Object] = @MainObject;

	end

	-- Relevant ONLY to: SurveyType
	if @MainObject = 'QuestionType'
	begin
		select	@MainObjectTypeName = T.Name,
				@MainObjectName = O.Name 
		from	QuestionType O
				inner join SurveyType T on T.ID = O.SurveyTypeID
		where	O.ID = @MainObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from QuestionType where ID = @MainObjectID
		insert into @tbl  select 0, 'DisplayStyle', DisplayStyle, 0, 0 from QuestionType where ID = @MainObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from QuestionType where ID = @MainObjectID
	end

	-- Relevant ONLY to: ReferenceItem
	if @MainObject = 'ReferenceItem'
	begin
		select	@MainObjectTypeName = T.Name,
				@MainObjectName = O.Code 
		from	ReferenceItem O
				inner join ReferenceItemType T on T.ID = O.ReferenceItemTypeID
		where	O.ID = @MainObjectID

		insert into @tbl  select 0, 'Code', Code, 0, 0 from ReferenceItem where ID = @MainObjectID
	end

	-- Relevant ONLY to: ReferenceItemType
	if @MainObject = 'ReferenceItemType'
	begin
		select	@MainObjectTypeName = 'Reference Item Type',
				@MainObjectName = O.Name
		from	ReferenceItemType O
		where	O.ID = @MainObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from ReferenceItemType where ID = @MainObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from ReferenceItemType where ID = @MainObjectID
		insert into @tbl  select 0, 'DisplayFormat', DisplayFormat, 0, 0 from ReferenceItemType where ID = @MainObjectID
	end

	-- Relevant ONLY to: Report
	if @MainObject = 'Report'
	begin
		select	@MainObjectTypeName = 'Report',
				@MainObjectName = O.Name
		from	Report O
		where	O.ID = @MainObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from Report where ID = @MainObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from Report where ID = @MainObjectID
		insert into @tbl  select 0, 'ObjectType', ObjectType, 0, 0 from Report where ID = @MainObjectID
		insert into @tbl  select 0, 'ObjectID', ObjectID, 0, 0 from Report where ID = @MainObjectID
		insert into @tbl  select 0, 'ReportLayoutID', ReportLayoutID, 0, 0 from Report where ID = @MainObjectID
	end

	/*
	-- Relevant ONLY to: Artifact, ArtifactType, Intersect, Policy, Rule, Taxonomy, TaxonomyType, Vocabulary
	if @MainObject = 'Responsibility'
	begin
		select	@MainObjectTypeName = 'Responsibility',
				@MainObjectName = T.Name 
		from	Responsibility O
				inner join ResponsibilityType T on T.ID = O.ResponsibilityTypeID

		where	O.ID = @MainObjectID

		insert into @tbl  select 0, 'Context', (
				select	D.Name + ': ' + I.Code + '; '
				from	ResponsibilityContextItem C
						inner join ReferenceItem I on C.ObjectType = 'ReferenceItem' and C.ObjectID = I.ID
						inner join ReferenceItemType D on D.ID = I.ReferenceItemTypeID
				where	ResponsibilityID = @MainObjectID
				for xml path ('')--, root('items')
				), 0, 0 from Responsibility where ID = @MainObjectID
		insert into @tbl  select 0, 'ObjectType', ObjectType, 0, 0 from Responsibility where ID = @MainObjectID
		insert into @tbl  select 0, 'ObjectID', ObjectID, 0, 0 from Responsibility where ID = @MainObjectID
		insert into @tbl  select 0, 'ResponsibleObjectType', ResponsibleObjectType, 0, 0 from Responsibility where ID = @MainObjectID
		insert into @tbl  select 0, 'ResponsibleObjectID', ResponsibleObjectID, 0, 0 from Responsibility where ID = @MainObjectID
	end
	*/
	-- Relevant ONLY to: ResponsibilityType
	if @MainObject = 'ResponsibilityType'
	begin
		select	@MainObjectTypeName = 'Responsibility Type',
				@MainObjectName = O.Name 
		from	ResponsibilityType O
		where	O.ID = @MainObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from ResponsibilityType where ID = @MainObjectID
		insert into @tbl  select 0, 'ResponsibilityTypeGroup', ResponsibilityTypeGroup, 0, 0 from ResponsibilityType where ID = @MainObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from ResponsibilityType where ID = @MainObjectID
	end
	
	-- Relevant ONLY to: Rule
	if @MainObject = 'Rule'
	begin		
		select	@MainObjectTypeName = 'Rule',
				@MainObjectName = A.DisplayValue
		from	AssetDetail A				
		where	A.ObjectID = @MainObjectID and A.[Object] = @MainObject;
	end

	-- Relevant ONLY to: StatisticType
	if @MainObject = 'StatisticType'
	begin
		select	@MainObjectTypeName = 'Statistic Type',
				@MainObjectName = O.Name 
		from	StatisticType O
		where	O.ID = @MainObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from StatisticType where ID = @MainObjectID
		insert into @tbl  select 0, 'CheckType', CheckType, 0, 0 from StatisticType where ID = @MainObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from StatisticType where ID = @MainObjectID
		insert into @tbl  select 0, 'PartOfScore', PartOfScore, 0, 0 from StatisticType where ID = @MainObjectID
		insert into @tbl  select 0, 'Configuration', cast(Configuration as nvarchar(max)), 0, 0 from StatisticType where ID = @MainObjectID
	end

	-- Relevant ONLY to: SurveyType
	if @MainObject = 'SurveyType'
	begin
		select	@MainObjectTypeName = 'Survey Type',
				@MainObjectName = O.Name 
		from	SurveyType O
		where	O.ID = @MainObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from SurveyType where ID = @MainObjectID
		insert into @tbl  select 0, 'Object', Object, 0, 0 from SurveyType where ID = @MainObjectID
		insert into @tbl  select 0, 'ObjectID', ObjectID, 0, 0 from SurveyType where ID = @MainObjectID
	end
	
	-- Relevant ONLY to: Taxonomy, TaxonomyType
	if @MainObject = 'Taxonomy'
	begin
		select	@MainObjectTypeName = A.TypeName + ' model',
				@MainObjectName = A.DisplayValue
		from	AssetDetail A				
		where	A.ObjectID = @MainObjectID and A.[Object] = @MainObject;

	end

	-- Relevant ONLY to: TaxonomyType
	if @MainObject = 'TaxonomyType'
	begin
		select	@MainObjectTypeName = 'Model Type',
				@MainObjectName = A.Name 
		from	AssetType A
		where	A.ObjectID = @MainObjectID and A.[Object] = @MainObject;

		insert into @tbl  select 0, 'Name', Name, 0, 0 from AssetType where ObjectID = @MainObjectID and [Object] = @MainObject		
		insert into @tbl  select 0, 'Description', Description, 0, 0 from AssetType where ObjectID = @MainObjectID and [Object] = @MainObject		
		insert into @tbl  select 0, 'DisplayFormat', DisplayFormat, 0, 0 from AssetType where ObjectID = @MainObjectID and [Object] = @MainObject		
		insert into @tbl  select 0, 'HierarchyMaximumDepth', hierarchymaximumdepth, 0, 0 from AssetType where ObjectID = @MainObjectID and [Object] = @MainObject		
	end

	-- Relevant ONLY to: PolicyType
	if @MainObject = 'PolicyType'
	begin
		select	@MainObjectTypeName = 'Policy Type',
				@MainObjectName = A.Name 
		from	AssetType A
		where	A.ObjectID = @MainObjectID and A.[Object] = @MainObject;

		insert into @tbl  select 0, 'Name', Name, 0, 0 from AssetType where ObjectID = @MainObjectID and [Object] = @MainObject		
		insert into @tbl  select 0, 'Description', Description, 0, 0 from AssetType where ObjectID = @MainObjectID and [Object] = @MainObject		
		insert into @tbl  select 0, 'DisplayFormat', DisplayFormat, 0, 0 from AssetType where ObjectID = @MainObjectID and [Object] = @MainObject		
		insert into @tbl  select 0, 'HierarchyMaximumDepth', hierarchymaximumdepth, 0, 0 from AssetType where ObjectID = @MainObjectID and [Object] = @MainObject		

	end

	-- Get the dynamic fields for the actional object, if available for this type.
	if @MainObject in ('Artifact', 'Attribute', 'Fusion', 'FusionAttribute', 'Lookup', 'Resource', 'Rule', 'Policy', 'Taxonomy') and @DependentObject = @MainObject
	begin
		insert into @tbl  
			select	FieldTypeID, 
					FriendlyName, 
					FormattedValue, 
					0, 
					0 
			from	FieldWithRelation
			where	ObjectType = @MainObject 
					and ObjectID = @MainObjectID
	end
	----------------------------------------------------------------------


	-- Now, determine the description, and whether to create audit row ---
	update	T
	set		T.MostRecentVersion = coalesce(S.[Version], 0),
			T.Updated = case 
							when T.NewValue = S.Value then 0
							when T.NewValue is null and S.Value is null then 0
							else 1
						end
	from	@tbl T
			left join (
						select	V.*,
								F.Value
						from	reporting.Global_Audit A
								inner join reporting.Global_FieldAudit F on F.AuditID = A.ID and A.[Object] = @DependentObject and A.ObjectID = @DependentObjectID and A.ActionObject = @MainObject and A.ActionObjectID = @MainObjectID
								inner join (
											select		F.FieldTypeID,
														F.FieldName,
														max([Version]) as [Version]
											from		reporting.Global_Audit A
														inner join reporting.Global_FieldAudit F on F.AuditID = A.ID and A.[Object] = @DependentObject and A.ObjectID = @DependentObjectID and A.ActionObject = @MainObject and A.ActionObjectID = @MainObjectID
											group by	F.FieldTypeID,
														F.FieldName
											) V on V.FieldTypeID = F.FieldTypeID and V.FieldName = F.FieldNAme and V.[Version] = F.[Version]
						) S on (S.FieldTypeID = 0 and S.FieldTypeID = T.FieldTypeID and S.FieldName = T.FieldName) or (S.FieldTypeID > 0 and S.FieldTypeID = T.FieldTypeID)

	declare	@auditID bigint,
			@current int = 1, 
			@max int,
			@fieldTypeID int,
			@fieldName nvarchar(250),
			@version int,
			@value nvarchar(max),
			@updated bit
	select	@max = max(ID) from @tbl

	if @Action = 'Created'
		begin
			set @MainDescription = @MainObjectTypeName + ' created'
		end
	if @Action = 'Removed'
		begin
			set @MainDescription = @MainObjectTypeName + ' removed'
		end
	if @Action = 'Updated'
		begin
			while @current <= @max
			begin
				select	@fieldTypeID = FieldTypeID,
						@fieldName = FieldName,
						@version = MostRecentVersion,
						@value = NewValue,
						@updated = Updated
				from	@tbl
				where	ID = @current

				if @updated = 1
				begin
					set @MainDescription = coalesce(@MainDescription + ', ', '') + @fieldName + case when @version > 0 then ' updated' else ' added' end
				end

				set @current = @current + 1
			end
		end

	if @MainObjectName is not null and @DependentObjectName is not null
	begin
		set @MainDescription = coalesce(@MainDescription,@MainObject + ' ' + @Action) + '.'

		insert into [reporting].[Global_Audit] values (@DependentObject, @DependentObjectID, @DependentObjectName, coalesce(@ResourceID, 0), @Date, @Action, @MainObject, @MainObjectID, @MainObjectTypeName, @MainObjectName, @MainDescription)
		select @auditID = SCOPE_IDENTITY()

		set @current = 1
		while @current <= @max
		begin
			select	@fieldTypeID = FieldTypeID,
					@fieldName = FieldName,
					@version = MostRecentVersion,
					@value = NewValue,
					@updated = Updated
			from	@tbl
			where	ID = @current

			if @updated = 1
			begin
				insert into [reporting].[Global_FieldAudit] 
				values	(
						@auditID, 
						@fieldTypeID, 
						@fieldName, 
						@version + 1, 
						@value --case FormattedValue when '' then 'EMPTY' else coalesce(FormattedValue, 'NULL') end
						) 		
			end

			set @current = @current + 1
		end
	end
	----------------------------------------------------------------------
end
GO


create VIEW [utility].IntersectAsset
WITH SCHEMABINDING  
AS  
    select
	I.ID,
	I.ID as IntersectID,
	I.IntersectTypeID as IntersectTypeID,
	P.Type as PredicateType,
	a_o.ID as ObjectAssetID,
	I.[Object] as [Object],
	I.ObjectID as [ObjectID],	
	I.[Subject] as [Subject],
	I.SubjectID as [SubjectID]
from 
	dbo.[Intersect] I
	inner join dbo.Asset a_o on I.[Object] = a_o.[Object] and I.[ObjectID] = a_o.ObjectID	
	inner join dbo.[IntersectType] IT on I.IntersectTypeID = IT.ID
	inner join dbo.[Predicate] P on P.ID = IT.PredicateID
GO

CREATE UNIQUE CLUSTERED INDEX [IDEX_IntersectAsset_ObjectAsset_Predicate_IntersectType]
    ON [utility].[IntersectAsset]([ID] ASC, [ObjectAssetID] ASC, [PredicateType] ASC, [IntersectTypeID] ASC);
GO

ALTER FUNCTION [dbo].[GetParentByAssetID]
(	
	@id bigint
)
RETURNS TABLE 
AS
RETURN 
(
	SELECT 
		P.ID as ID, 
		null as IntersectID, 
		null as IntersectTypeID 
	from Asset A
	inner join FusionAttribute FA on FA.ID = A.ObjectID
	inner join AssetType T on T.ID = A.AssetTypeID
	inner join Asset P on P.[Object] = 'FusionAttribute' and P.ObjectID = FA.ParentID
	where A.Object = 'FusionAttribute' and A.ID = @id
	UNION ALL
	SELECT 
		P.ID, 
		I.ID as IntersectID, 
		Y.ID as IntersectTypeID from Asset A
	inner join AssetType T on T.ID = A.AssetTypeID
	inner join [IntersectType] Y on Y.[Object] = T.[Object] and Y.ObjectID = T.ObjectID
	inner join [Predicate] R on R.ID = Y.PredicateID
		and R.[Type] = case when Y.[Subject] = 'PolicyType' or Y.[Subject] = 'TaxonomyType' then 4 else 3 end 
	inner join [Intersect] I on I.IntersectTypeID = Y.ID and I.[Object] = A.[Object] and I.ObjectID = A.ObjectID
	inner join Asset P on P.[Object] = I.[Subject] and P.ObjectID = I.SubjectID
	where A.Object != 'FusionAttribute' and A.ID = @id
)
GO

ALTER FUNCTION [dbo].[GetArtifactParentByAssetID]
(
	@Id bigint
)
RETURNS TABLE 
AS
RETURN 
(
	/*select	A.ID as ID,
			A.ObjectID as ObjectID,
			I.SubjectID as ParentID,
            ID.DisplayValue as ParentDisplayValue,
			PUrl.Url as ParentUrl
				    from	dbo.Asset A
							inner join dbo.[Intersect] I on I.Object = A.Object and I.ObjectID = A.ObjectID
							inner join dbo.[IntersectType] IT on I.IntersectTypeID = IT.ID
							inner join dbo.[Predicate] P on P.ID = IT.PredicateID and P.Type = 3
                            inner join dbo.Asset IA on IA.Object = 'Artifact' and IA.ObjectID = I.SubjectID 
                            inner join dbo.AssetType IAT on IAT.ID = IA.AssetTypeID
                            cross apply dbo.GetAssetDisplayValueById(IA.ID) ID
							cross apply dbo.GetAssetUrl('Artifact', IAT.ObjectID, I.SubjectID) PUrl
					where A.[Object] = 'Artifact' and A.ID = @Id*/

		select	A.ID as ID,
			A.ObjectID as ObjectID,
			IAD.SubjectID as ParentID,
            ID.DisplayValue as ParentDisplayValue,
			PUrl.Url as ParentUrl
				    from	[utility].IntersectAsset IAD
							inner join dbo.Asset A on A.ID = IAD.ObjectAssetID and IAD.PredicateType = 3							
                            inner join dbo.Asset IA on IA.Object = 'Artifact' and IA.ObjectID = IAD.SubjectID 
                            inner join dbo.AssetType IAT on IAT.ID = IA.AssetTypeID
                            cross apply dbo.GetAssetDisplayValueById(IA.ID) ID
							cross apply dbo.GetAssetUrl('Artifact', IAT.ObjectID, IAD.SubjectID) PUrl
					where A.[Object] = 'Artifact' and A.ID = @Id
)
GO
