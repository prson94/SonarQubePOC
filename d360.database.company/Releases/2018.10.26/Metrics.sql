CREATE TABLE [metrics].[Rule] (
	[Uid] uniqueidentifier CONSTRAINT [DF_MetricsRule_uid]  DEFAULT (newid()) NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[Description] [nvarchar](4000) NULL,
	[SqlStatement] nvarchar(max) NOT NULL,
	[CreatedOn] [datetime] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedOn] [datetime] NULL,
	[UpdatedBy] [int] NULL,
	CONSTRAINT [PK_MetricRule] PRIMARY KEY NONCLUSTERED ( [Uid] ASC )
)
GO;

CREATE TABLE [metrics].[RuleParameter] (
	[Uid] uniqueidentifier NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[Description] [nvarchar](4000) NULL,
	[LookupClass] int NOT NULL, -- ReferenceItemType, ResponsibilityType, IntersectType
	CONSTRAINT [PK_MetricRuleParameter] PRIMARY KEY NONCLUSTERED ( [Uid] ASC, [Name] asc )
)
GO;

ALTER TABLE [metrics].[RuleParameter]  WITH CHECK ADD  CONSTRAINT [FK_MetricRuleParameter_MetricRule] FOREIGN KEY([Uid]) REFERENCES [metrics].[Rule] ([Uid]) ON DELETE CASCADE
ALTER TABLE [metrics].[RuleParameter] CHECK CONSTRAINT [FK_MetricRuleParameter_MetricRule]
GO;


CREATE TABLE [metrics].[Asset] (
	[Uid] uniqueidentifier CONSTRAINT [DF_MetricsAsset_uid]  DEFAULT (newid()) NOT NULL,
	[ParentUid] uniqueidentifier NULL,
	AssetTypeUid uniqueidentifier NOT NULL,
	IsGroup bit constraint DF_MetricAsset_IsGroup default(1) NOT NULL,
	[State] int CONSTRAINT [DF_MetricAsset_State]  DEFAULT (1) NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[Description] [nvarchar](4000) NULL,
	[CreatedOn] [datetime] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedOn] [datetime] NULL,
	[UpdatedBy] [int] NULL,
	OldMapID int null,
	CONSTRAINT [PK_MetricAsset] PRIMARY KEY NONCLUSTERED ( [Uid] ASC )
)
GO;

CREATE TABLE [metrics].[AssetVersion] (
	[Uid] uniqueidentifier NOT NULL,
	EffectiveDate date NOT NULL,
	[Weight] decimal(5,3) NOT NULL,
	ConditionAndOr varchar(1) NULL,
	Internal bit constraint DF_MetricAssetVersion_Internal default(0) not null,
	MetricRuleUid uniqueidentifier NULL,
	CreatedOn datetime NULL,
	CreatedBy int NULL,
	CONSTRAINT PK_MetricAssetVersion PRIMARY KEY NONCLUSTERED ( [Uid] ASC, EffectiveDate DESC )
)
GO;

ALTER TABLE [metrics].[AssetVersion]  WITH CHECK ADD  CONSTRAINT [FK_MetricAssetVersion_MetricAsset] FOREIGN KEY([Uid]) REFERENCES [metrics].[Asset] ([Uid]) ON DELETE CASCADE
ALTER TABLE [metrics].[AssetVersion] CHECK CONSTRAINT [FK_MetricAssetVersion_MetricAsset]
GO;

CREATE TABLE [metrics].[AssetVersionCondition] (
	[Uid] uniqueidentifier NOT NULL,
	EffectiveDate date NOT NULL,
	FieldTypeID int NOT NULL,
	Operator varchar(10) NOT NULL,
	ValueJson nvarchar(max) NOT NULL,
	CONSTRAINT PK_MetricAssetVersionCondition PRIMARY KEY NONCLUSTERED ( [Uid] ASC, EffectiveDate DESC, FieldTypeID ASC )
)
GO;

ALTER TABLE [metrics].[AssetVersionCondition]  WITH CHECK ADD  CONSTRAINT [FK_MetricAssetVersionCondition_MetricAssetVersion] FOREIGN KEY([Uid], EffectiveDate) REFERENCES [metrics].[AssetVersion] ([Uid], EffectiveDate) ON DELETE CASCADE
ALTER TABLE [metrics].[AssetVersionCondition] CHECK CONSTRAINT [FK_MetricAssetVersionCondition_MetricAssetVersion]
GO;

ALTER TABLE [metrics].[AssetVersionCondition]  WITH CHECK ADD  CONSTRAINT [FK_MetricAssetVersionCondition_FieldType] FOREIGN KEY(FieldTypeID) REFERENCES dbo.FieldType (ID) ON DELETE CASCADE
ALTER TABLE [metrics].[AssetVersionCondition] CHECK CONSTRAINT [FK_MetricAssetVersionCondition_FieldType]
GO;

CREATE TABLE [metrics].[AssetVersionParameter](
	[Uid] [uniqueidentifier] NOT NULL,
	[EffectiveDate] [date] NOT NULL,
	[RuleParameterUid] uniqueidentifier NOT NULL,
	[RuleParameterValue] [nvarchar](250) NOT NULL,
	CONSTRAINT [PK_AssetVersionParameter] PRIMARY KEY NONCLUSTERED ( [Uid] ASC, [EffectiveDate] DESC, [RuleParameterUid] ASC )
)
GO;

ALTER TABLE [metrics].[AssetVersionParameter]  WITH CHECK ADD  CONSTRAINT [FK_MetricAssetVersionParameter_MetricAssetVersion] FOREIGN KEY([Uid], [EffectiveDate]) REFERENCES [metrics].[AssetVersion] ([Uid], [EffectiveDate]) ON DELETE CASCADE
ALTER TABLE [metrics].[AssetVersionParameter] CHECK CONSTRAINT [FK_MetricAssetVersionParameter_MetricAssetVersion]
GO;


insert into [metrics].[Asset]
	select	distinct
			C.[uid] as [Uid],
			P.[uid] as [ParentUid],
			'00000000-0000-0000-0000-000000000000' as AssetTypeUid,
			1 as IsGroup,
			C.[State],
			C.Name,
			C.Description,
			C.[CreatedOn],
			C.[CreatedBy],
			C.[UpdatedOn],
			C.[UpdatedBy],
			null
	from	metrics.[Group] C
			left join metrics.[Group] P on P.ID = C.ParentID
	where	C.[uid] <> '00000000-0000-0000-0000-000000000000'
	union
	select	distinct
			newid() as [Uid],
			P.[uid] as [ParentUid],
			A.[uid] as AssetTypeUid,
			0 as IsGroup,
			1 as [State],
			C.Name,
			C.Description,
			C.[CreatedOn],
			C.[CreatedBy],
			C.[UpdatedOn],
			C.[UpdatedBy],
			M.ID
	from	metrics.[Item] C
			inner join metrics.Map M on M.ItemID = C.ID
			inner join AssetType A on A.ID = M.AssetTypeID
			inner join metrics.[Group] P on P.ID = M.GroupID

ALTER TABLE [metrics].[Asset]  WITH CHECK ADD  CONSTRAINT [FK_MetricAsset_Parent] FOREIGN KEY([ParentUid]) REFERENCES [metrics].[Asset] ([Uid]) ON DELETE NO ACTION
ALTER TABLE [metrics].[Asset] CHECK CONSTRAINT [FK_MetricAsset_Parent]
GO;

insert into [metrics].[AssetVersion]
	select	distinct
			[Uid],
			[EffectiveStartDate] as EffectiveDate, --CreatedOn as EffectiveDate,
			[Weight],
			'a' as ConditionAndOr,
			0, null,
			[UpdatedOn],
			coalesce([UpdatedBy], 0) as [UpdatedBy]
	from	metrics.[Group]
	where	[uid] <> '00000000-0000-0000-0000-000000000000'
	        and [Uid] in (select [Uid] from metrics.Asset)

insert into [metrics].[AssetVersion]
	select	distinct
			MA.[Uid],
			M.EffectiveStartDate,
			M.[Weight],
			'a' as ConditionAndOr,
			0, null,
			M.[UpdatedOn],
			M.[UpdatedBy]
	from	metrics.Map M
			inner join metrics.Asset MA on MA.OldMapID = M.ID

INSERT INTO [metrics].[AssetVersionCondition]
	select	distinct
			MA.[Uid],
			M.EffectiveStartDate,
			C.FieldTypeID,
			C.Operator,
			'[''' + C.Value + ''']'
	from	metrics.Map M
			inner join metrics.Asset MA on MA.OldMapID = M.ID
			inner join metrics.Condition C on C.MapId = M.ID
GO;

with h as (
	select	Uid,
			ParentUid,
			AssetTypeUid
	from	metrics.Asset
	where	IsGroup = 0
	union all
	select	P.Uid,
			P.ParentUid,
			C.AssetTypeUid
	from	metrics.Asset P
			inner join h as C on C.ParentUid = P.Uid
)

update	T
set		T.AssetTypeUid = S.AssetTypeUid
from	metrics.Asset T
		inner join h S on S.Uid = T.Uid

drop table [metrics].[ConditionValue]
GO;
drop table [metrics].[Condition]
GO;
ALTER TABLE [metrics].[MapResult] DROP CONSTRAINT [FK_MetricMapResult_MetricMap]
GO;
ALTER TABLE [metrics].[ScoreItem] DROP CONSTRAINT [FK_MetricScoreItem_MetricMap]
GO;
ALTER TABLE [metrics].[StagingResult] DROP CONSTRAINT [FK_StagingResult_Map]
GO;
DROP TABLE [metrics].[Map]
GO;
DROP TABLE [metrics].[Item]
GO;
DROP TABLE [metrics].[Group]
GO;

select	A.Uid,
		A.ParentUid,
		A.AssetTypeUid,
		A.IsGroup,
		A.Name,
		A.Description,
		V.EffectiveDate,
		V.Weight,
		V.ConditionAndOr,
		(
			select	FieldTypeID,
					Operator,
					[ValueJson] as [Values]
			from	metrics.AssetVersionCondition
			where	Uid = V.Uid and EffectiveDate = V.EffectiveDate
			for		json path

		) as Conditions
from	metrics.Asset A
		cross apply (
			select	max(EffectiveDate) as EffectiveDate
			from	metrics.AssetVersion
			where	Uid = A.Uid
		) MV
		inner join metrics.AssetVersion V on V.Uid = A.Uid and V.EffectiveDate = MV.EffectiveDate
where	A.AssetTypeUid = '8AA15152-0BA5-4A17-B023-1A4BD1CDDFD2'
for		json path
	
select	A.Uid,
		A.ParentUid,
		A.AssetTypeUid,
		A.IsGroup,
		A.Name,
		A.Description,
		V.EffectiveDate,
		V.Weight,
		V.ConditionAndOr,
		(
			select	FieldTypeID,
					Operator,
					[ValueJson] as [Values]
			from	metrics.AssetVersionCondition
			where	Uid = V.Uid and EffectiveDate = V.EffectiveDate
			for		json path

		) as Conditions
from	metrics.Asset A
		cross apply (
			select	max(EffectiveDate) as EffectiveDate
			from	metrics.AssetVersion
			where	Uid = A.Uid
		) MV
		inner join metrics.AssetVersion V on V.Uid = A.Uid and V.EffectiveDate = MV.EffectiveDate
where	A.AssetTypeUid = 'a9b94f4b-14f6-474f-9572-80f954c8fc59'
for		json path


select	F.ID,
		F.FriendlyName as Name,
		F.Type,
		(
			select	Value,
					Text
			from	FieldLookupValue
			where	FieldTypeID = F.ID
			for		json path

		) as Conditions
from	AssetType A
		inner join FieldType F on F.AssetTypeID = A.ID and A.[uid] = 'a9b94f4b-14f6-474f-9572-80f954c8fc59' and F.Type in ('Boolean', 'Date', 'Lookup', 'Number', 'Text')
for		json path
GO;


select * from metrics.AssetVersion where Uid = '281056bc-e4ee-4a5b-b7c6-8333e5785cf2' and EffectiveDate = '08/14/2018'
GO;

DROP TABLE [metrics].[StagingResultArchive]
GO;
DROP TABLE [metrics].[StagingResult]
GO;
ALTER TABLE [metrics].[StagingItem] DROP CONSTRAINT [DF_MetricsStagingItem_Archived]
GO;
ALTER TABLE [metrics].[StagingItem] DROP CONSTRAINT [DF_MetricsStagingItem_Processing]
GO;
DROP TABLE [metrics].[StagingItem]
GO;

EXEC sp_rename 'metrics.ScoreItem', 'ScoreItemBackup';
sp_rename 'metrics.PK_MetricScoreItem', 'PK_MetricScoreItemBackup'; 
EXEC sp_rename 'metrics.Score', 'ScoreBackup';
sp_rename 'metrics.PK_MetricScore', 'PK_MetricScoreBackup'; 

CREATE TABLE [metrics].[Score] (
	AssetUid uniqueidentifier NOT NULL,
	EffectiveDate date NOT NULL,
	Value [decimal](5, 3) NOT NULL,
	CONSTRAINT PK_MetricScore PRIMARY KEY CLUSTERED ( AssetUid asc, EffectiveDate desc ) 
)
GO;

ALTER TABLE [metrics].[Score] ADD  CONSTRAINT [DF_MetricsNewScore_EffectiveDate]  DEFAULT (getutcdate()) FOR [EffectiveDate]
GO;

CREATE TABLE [metrics].[ScoreItem](
	AssetUid uniqueidentifier NOT NULL,
	MetricAssetUid uniqueidentifier NOT NULL,
	EffectiveDate date NOT NULL,
	UpdatedOn datetime NOT NULL,
	[Value] bit NOT NULL,
	CONSTRAINT [PK_MetricScoreItem] PRIMARY KEY CLUSTERED ( AssetUid ASC, MetricAssetUid ASC, EffectiveDate DESC )
)
GO;

CREATE TABLE [metrics].[StagingScoreItem] (
	AssetUid uniqueidentifier NOT NULL,
	MetricAssetUid uniqueidentifier NOT NULL,
	EffectiveDate date NOT NULL,
	Result bit NOT NULL,
	Processing bit NOT NULL,
	Archived bit NOT NULL,
	CONSTRAINT PK_MetricStagingScoreItem PRIMARY KEY CLUSTERED ( Archived DESC, AssetUid ASC, MetricAssetUid ASC, EffectiveDate DESC )
)
GO;

ALTER TABLE [metrics].[StagingScoreItem] ADD  CONSTRAINT [DF_MetricsStagingScoreItem_Processing]  DEFAULT ((0)) FOR [Processing]
GO;

ALTER TABLE [metrics].[StagingScoreItem] ADD  CONSTRAINT [DF_MetricsStagingScoreItem_Archived]  DEFAULT ((0)) FOR [Archived]
GO;

ALTER PROCEDURE [dbo].[GetScoreHistoryByObject]
--declare
	@assetUid uniqueidentifier --= '5DFA86D6-9DFE-4BB6-B417-F75E3BC9E095'
AS
begin
	declare @date date = getutcdate()

	select	EffectiveDate as [Date],
			cast(Value * 100 as int) as Score
	from	metrics.Score
	where	AssetUid = @assetUid
			and EffectiveDate <= @date
	union
	select	cast(@date as date) as [Date],
			cast(Value * 100 as int) as Score
	from	metrics.Score S
			inner join (
				select	max(EffectiveDate) as EffectiveDate
				from	metrics.Score
				where	AssetUid = @assetUid
						and EffectiveDate <= @date
			) M on M.EffectiveDate = S.EffectiveDate and S.AssetUid = @assetUid
end
GO;

ALTER PROCEDURE [dbo].[GetAverageScoreByObjectType]
--declare
	@assetUid uniqueidentifier --= '5DFA86D6-9DFE-4BB6-B417-F75E3BC9E095'
AS
begin
	declare @assetTypeID int,
			@oName nvarchar(250),
			@oTypeName nvarchar(250),
			@AverageScore int,
			@ObjectScore int

	select	@oName = utility.GetAssetDisplayValue(A.ID),
			@oTypeName = T.Name,
			@assetTypeID = T.[ID]
	from	Asset A
			inner join AssetType T on T.ID = A.AssetTypeID  and A.[Uid] = @assetUid

	select	@ObjectScore = cast(Value * 100 as int)
	from	metrics.Score S
			inner join (
				select	max(EffectiveDate) as EffectiveDate
				from	metrics.Score
				where	AssetUid = @assetUid
						and EffectiveDate <= getutcdate()
			) M on M.EffectiveDate = S.EffectiveDate and S.AssetUid = @assetUid

	select	@AverageScore = avg(cast(S.Value * 100 as int))
	from	metrics.Score S
			inner join (
				select	max(I_S.EffectiveDate) as EffectiveDate,
						I_S.AssetUid
				from	metrics.Score I_S
						inner join dbo.Asset A on A.[Uid] = I_S.AssetUid 
											and A.AssetTypeID = @assetTypeID 
											and I_S.EffectiveDate <= getutcdate()
				group by I_S.AssetUid
			) M on M.AssetUid = S.AssetUid and M.EffectiveDate = S.EffectiveDate

	select	@oName as ObjectName, 
			@ObjectScore as ObjectScore, 
			@oTypeName as ObjectTypeName, 
			@AverageScore as AverageScore 
end
GO;

ALTER procedure [tile].[GetObjectStatistics]
	@type varchar(50),
	@id int
AS
BEGIN
	declare @table table (Name nvarchar(250), Value varchar(250), [Group] varchar(25), Url varchar(250), MostRecent datetime, TypeID int)
	
	declare @ObjectScore varchar(250),
			@assetUid uniqueidentifier;

	select @assetUid = [Uid] from dbo.Asset where Object = @type and ObjectID = @id;

	insert into @table
		select NULL, count(1), 'Followers', '', max(datecreated),null
		from	Follow F
		inner join reporting.Global_Resource R on R.ResourceID = F.ResourceID
		where	F.ObjectType = @type and F.ObjectID = @id
	
	insert into @table
		select	NULL, count(1), 'Comments', '', max(datecreated),null
		from	Comment C
				inner join CommentRelation R	on R.CommentID = C.ID and C.ParentID is null
												and R.ObjectType = @type and R.ObjectID = @id
                                                and C.ParentID is null
												and C.IsDeleted = 0

	select	@ObjectScore = cast(Value * 100 as int)
	from	metrics.Score S
			inner join (
				select	max(EffectiveDate) as EffectiveDate
				from	metrics.Score
				where	AssetUid = @assetUid
						and EffectiveDate <= getutcdate()
			) M on M.EffectiveDate = S.EffectiveDate and S.AssetUid = @assetUid

	insert into @table values (null, @ObjectScore, 'Score', null, null, null)

	if @type = 'Artifact'
	begin
		insert into @table 
			select 
				lower(c.childartifacttypename), 
				count(1),
				'Children',
				'',
				getutcdate(),
				c.ChildArtifactTypeID
			from 
				asset a
			cross apply [dbo].[GetArtifactChildByAssetID](a.id) c
			where a.objectid = @id and a.[object] = 'Artifact' group by c.childartifacttypename, c.ChildArtifactTypeID
			
		insert into @table
			select 
				'Issue',
				count(1),
				'Issues',	
				'',
				max(i.CreatedOn),
				null
			from workflow.item wi
				inner join issue i on (wi.objectid = i.id and wi.[object] = 'Issue')
			where 
				i.object = 'Artifact' and i.objectid = @id and completedon is null

				
	end


	select * from @table

END
GO;

-- Migrate the data from old tables to new tables.

GO;

--Before dropping tables below, you need to migrate this data.
DROP TABLE [metrics].[MapResult]
GO;
DROP TABLE [metrics].ScoreItemBackup
GO;
DROP TABLE [metrics].ScoreBackup
GO;
--Before dropping tables above...

--ALTER procedure [metrics].[LoadFromStaging]
--GO;

CREATE FUNCTION metrics.AssetMeetsConditions
(
	@metricUid uniqueidentifier,
	@effectiveDate date,
	@assetUid uniqueidentifier
)
RETURNS bit
AS
BEGIN
	--declare @metricUid uniqueidentifier = '281056bc-e4ee-4a5b-b7c6-8333e5785cf2',
	--		@effectiveDate date = '2018-09-25',
	--		@assetUid uniqueidentifier = 'c82f1f66-ffbf-41b6-b4d5-55958f0b0548'

	declare @valid bit = 0

	declare @conditions table (FieldTypeID int, Operator varchar(10), ValueJson nvarchar(max))
	insert into @conditions
		select	FieldTypeID,
				Operator,
				ValueJson
		from	[metrics].[AssetVersionCondition]
		where	[Uid] = @metricUid
				and EffectiveDate = @effectiveDate

	if exists (select 1 from @conditions)
	begin
		declare @stats table (Val bit)
		insert into @stats
			select	case 
						when C.Operator = 'eq' then
							case 
								when C.ValueJson = F.Value then cast(1 as bit)
								else cast(0 as bit)
							end
						when C.Operator = 'neq' then
							case 
								when C.ValueJson <> F.Value then cast(1 as bit)
								else cast(0 as bit)
							end
						when C.Operator = 'lt' then
							case 
								when C.ValueJson < F.Value then cast(1 as bit)
								else cast(0 as bit)
							end
						when C.Operator = 'lte' then
							case 
								when C.ValueJson <= F.Value then cast(1 as bit)
								else cast(0 as bit)
							end
						when C.Operator = 'gt' then
							case 
								when C.ValueJson > F.Value then cast(1 as bit)
								else cast(0 as bit)
							end
						when C.Operator = 'gte' then
							case 
								when C.ValueJson >= F.Value then cast(1 as bit)
								else cast(0 as bit)
							end	
						else cast(1 as bit)
					end as Valid
			from	Field F
					inner join @conditions C on C.FieldTypeID = F.FieldTypeID
					inner join dbo.Asset A on A.ID = F.AssetID and A.[Uid] = @assetUid
		if exists(select 1 from @stats where Val = 0)
		begin
			set @valid = 0 -- there is at least one condition that is not a match.
		end
		else
		begin
			set @valid = 1 --default
		end
	end
	else
	begin
		set @valid = 1
	end

	return @valid
END
GO;

alter table [metrics].[ScoreItem] add AdjustedWeight decimal(5,3) null
GO;