--table creation
CREATE TABLE [api].[Execution]
(
[ExecutionID] [uniqueidentifier] NOT NULL,
[ResourceID] [int] NOT NULL,
[Total] [int] NOT NULL,
[Processed] [int] NOT NULL,
[Error] [int] NOT NULL,
[StartedOn] [datetime] NOT NULL,
[CompletedOn] [datetime] NULL,
[Fields] [nvarchar] (2500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
)
GO;
-- Constraints and Indexes
ALTER TABLE [api].[Execution] ADD CONSTRAINT [PK_ApiExecution] PRIMARY KEY NONCLUSTERED  ([ExecutionID] DESC)
GO;

create function [utility].[GetHash](
	@value nvarchar(max)
)
RETURNS varchar(32)
AS
BEGIN
    DECLARE @hash varchar(32)

    SELECT @hash = CONVERT(
					varchar(32), 
					SUBSTRING(HASHBYTES('SHA1', @value), 3, 32), 
					2)

    RETURN @hash
END
GO;



--metric logic
CREATE TABLE [metrics].[Rule]
(
[Uid] [uniqueidentifier] NOT NULL CONSTRAINT [DF_MetricsRule_uid] DEFAULT (newid()),
[Name] [nvarchar] (250) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[Description] [nvarchar] (4000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
[SqlStatement] [nvarchar] (max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[CreatedOn] [datetime] NULL,
[CreatedBy] [int] NULL,
[UpdatedOn] [datetime] NULL,
[UpdatedBy] [int] NULL
)
GO;
ALTER TABLE [metrics].[Rule] ADD CONSTRAINT [PK_MetricRule] PRIMARY KEY NONCLUSTERED  ([Uid])
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

CREATE TABLE [metrics].[Asset]
(
[Uid] [uniqueidentifier] NOT NULL CONSTRAINT [DF_MetricsAsset_uid] DEFAULT (newid()),
[ParentUid] [uniqueidentifier] NULL,
[AssetTypeUid] [uniqueidentifier] NOT NULL,
[IsGroup] [bit] NOT NULL CONSTRAINT [DF_MetricAsset_IsGroup] DEFAULT ((1)),
[State] [int] NOT NULL CONSTRAINT [DF_MetricAsset_State] DEFAULT ((1)),
[Name] [nvarchar] (250) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[Description] [nvarchar] (4000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
[CreatedOn] [datetime] NULL,
[CreatedBy] [int] NULL,
[UpdatedOn] [datetime] NULL,
[UpdatedBy] [int] NULL,
[OldMapID] [int] NULL
)
GO;
ALTER TABLE [metrics].[Asset] ADD CONSTRAINT [PK_MetricAsset] PRIMARY KEY NONCLUSTERED  ([Uid])
GO;

CREATE TABLE [metrics].[AssetVersion]
(
[Uid] [uniqueidentifier] NOT NULL,
[EffectiveDate] [date] NOT NULL,
[Weight] [decimal] (5, 3) NOT NULL,
[ConditionAndOr] [varchar] (1) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
[Internal] [bit] NOT NULL CONSTRAINT [DF_MetricAssetVersion_Internal] DEFAULT ((0)),
[MetricRuleUid] [uniqueidentifier] NULL,
[CreatedOn] [datetime] NULL,
[CreatedBy] [int] NULL
)
GO;
ALTER TABLE [metrics].[AssetVersion] ADD CONSTRAINT [PK_MetricAssetVersion] PRIMARY KEY NONCLUSTERED  ([Uid], [EffectiveDate] DESC)
GO;
ALTER TABLE [metrics].[AssetVersion] ADD CONSTRAINT [FK_MetricAssetVersion_MetricAsset] FOREIGN KEY ([Uid]) REFERENCES [metrics].[Asset] ([Uid]) ON DELETE CASCADE
GO;

CREATE TABLE [metrics].[AssetVersionCondition]
(
[Uid] [uniqueidentifier] NOT NULL,
[EffectiveDate] [date] NOT NULL,
[FieldTypeID] [int] NOT NULL,
[Operator] [varchar] (10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[ValueJson] [nvarchar] (max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL
)
GO;
ALTER TABLE [metrics].[AssetVersionCondition] ADD CONSTRAINT [PK_MetricAssetVersionCondition] PRIMARY KEY NONCLUSTERED  ([Uid], [EffectiveDate] DESC, [FieldTypeID])
GO;
ALTER TABLE [metrics].[AssetVersionCondition] ADD CONSTRAINT [FK_MetricAssetVersionCondition_FieldType] FOREIGN KEY ([FieldTypeID]) REFERENCES [dbo].[FieldType] ([ID]) ON DELETE CASCADE
GO;
ALTER TABLE [metrics].[AssetVersionCondition] ADD CONSTRAINT [FK_MetricAssetVersionCondition_MetricAssetVersion] FOREIGN KEY ([Uid], [EffectiveDate]) REFERENCES [metrics].[AssetVersion] ([Uid], [EffectiveDate]) ON DELETE CASCADE
GO;

CREATE TABLE [metrics].[AssetVersionParameter]
(
[Uid] [uniqueidentifier] NOT NULL,
[EffectiveDate] [date] NOT NULL,
[RuleParameterUid] [uniqueidentifier] NOT NULL,
[RuleParameterValue] [nvarchar] (250) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL
)
GO;
ALTER TABLE [metrics].[AssetVersionParameter] ADD CONSTRAINT [PK_AssetVersionParameter] PRIMARY KEY NONCLUSTERED  ([Uid], [EffectiveDate] DESC, [RuleParameterUid])
GO;
ALTER TABLE [metrics].[AssetVersionParameter] ADD CONSTRAINT [FK_MetricAssetVersionParameter_MetricAssetVersion] FOREIGN KEY ([Uid], [EffectiveDate]) REFERENCES [metrics].[AssetVersion] ([Uid], [EffectiveDate]) ON DELETE CASCADE
GO;

CREATE FUNCTION [metrics].[AssetMeetsConditions]
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

--metric data migration
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
GO;

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
		inner join h S on S.Uid = T.Uid;


ALTER TABLE [metrics].[MapResult] DROP CONSTRAINT [FK_MetricMapResult_MetricMap]
GO;
ALTER TABLE [metrics].[ScoreItem] DROP CONSTRAINT [FK_MetricScoreItem_MetricMap]
GO;
ALTER TABLE [metrics].[StagingResult] DROP CONSTRAINT [FK_StagingResult_Map]
GO;
/*
drop table [metrics].[ConditionValue]
GO;
drop table [metrics].[Condition]
GO;
DROP TABLE [metrics].[Map]
GO;
DROP TABLE [metrics].[Item]
GO;
DROP TABLE [metrics].[Group]
GO;
*/

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

CREATE TABLE [metrics].[ScoreItem]
(
[AssetUid] [uniqueidentifier] NOT NULL,
[MetricAssetUid] [uniqueidentifier] NOT NULL,
[EffectiveDate] [date] NOT NULL,
[UpdatedOn] [datetime] NOT NULL,
[Value] [bit] NOT NULL,
[AdjustedWeight] [decimal] (5, 3) NULL
)
GO;
ALTER TABLE [metrics].[ScoreItem] ADD CONSTRAINT [PK_MetricScoreItem] PRIMARY KEY CLUSTERED  ([AssetUid], [MetricAssetUid], [EffectiveDate] DESC)
GO;

CREATE TABLE [metrics].[StagingScoreItem]
(
	[AssetUid] [uniqueidentifier] NOT NULL,
	[MetricAssetUid] [uniqueidentifier] NOT NULL,
	[EffectiveDate] [date] NOT NULL,
	[Result] [bit] NOT NULL,
	[Processing] [bit] NOT NULL CONSTRAINT [DF_MetricsStagingScoreItem_Processing] DEFAULT ((0)),
	[Archived] [bit] NOT NULL CONSTRAINT [DF_MetricsStagingScoreItem_Archived] DEFAULT ((0))
)
GO;
ALTER TABLE [metrics].[StagingScoreItem] ADD CONSTRAINT [PK_MetricStagingScoreItem] PRIMARY KEY CLUSTERED  ([Archived] DESC, [AssetUid], [MetricAssetUid], [EffectiveDate] DESC)
GO;
ALTER TABLE [metrics].[StagingScoreItem] ADD  CONSTRAINT [DF_MetricsStagingScoreItem_Processing]  DEFAULT ((0)) FOR [Processing]
GO;
ALTER TABLE [metrics].[StagingScoreItem] ADD  CONSTRAINT [DF_MetricsStagingScoreItem_Archived]  DEFAULT ((0)) FOR [Archived]
GO;

CREATE PROCEDURE [dbo].[GetAverageScoreByAsset]
--declare
	@assetID bigint-- = 42
AS
begin
	declare @date date = getutcdate(),
			@name nvarchar(250),
			@assetTypeID int,
			@typeName nvarchar(250),
			@averageScore int,
			@score int

	select	@name = utility.GetAssetDisplayValue(A.ID),
			@typeName = T.Name,
			@assetTypeID = T.ID
	from	Asset A
			inner join AssetType T on T.ID = A.AssetTypeID and A.ID = @assetID;

	select	top 1
			@score = cast(Value * 100 as int)
	from	metrics.Score
	where	AssetID = @assetID
			and EffectiveDate in (
				select	min(EffectiveDate) as EffectiveDate
				from	metrics.Score
				where	AssetID = @assetID
						and EffectiveDate <= @date
			);

	select	@averageScore = avg(cast(SC.Value * 100 as int))
	from	metrics.Score SC
			inner join (
			select		S.AssetID,
						min(S.EffectiveDate) as EffectiveDate
			from		metrics.Score S
						inner join Asset A on A.AssetTypeID = @assetTypeID and S.EffectiveDate <= @date
			group by	S.AssetID
			) S on S.AssetID = SC.AssetID and S.EffectiveDate = SC.EffectiveDate;

	select	@assetID as AssetID, 
			@name as AssetName, 
			@assetTypeID as AssetTypeID,
			@typeName as AssetTypeName, 
			@score as Score, 
			@averageScore as AverageScore 
end
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
end;

--display value stuff
drop table [dbo].[AssetDisplayValue]
GO;

CREATE TABLE [dbo].[AssetDisplayValue](
	[AssetID] [bigint] NOT NULL,
	[DisplayValue] [nvarchar](max) NOT NULL,
	[DisplayValueHash] [nvarchar](50) NULL,	
	[DisplayValuePrefix] [nvarchar](250) NOT NULL,
	[UpdatedOn] [datetime] constraint DF_AssetDisplayValue_UpdatedOn DEFAULT(getutcdate())NOT NULL
 CONSTRAINT [PK_AssetDisplayValue] PRIMARY KEY NONCLUSTERED 
(
	[AssetID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)
GO;

create clustered index IX_AssetDisplayValue_DisplayValuePrefix_AssetID on dbo.AssetDisplayValue(DisplayValuePrefix, AssetID)
GO;

drop procedure UpdateDependentObjectTypeDisplayValues
GO;

create PROCEDURE UpdateDependentObjectTypeDisplayValues		
	@ChangedObject varchar(20),
	@ChangedObjectTypeID int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	Declare @ObjectType varchar(20);
	Declare @ObjectID int;
	Declare @AssetTypeID int;

	SELECT @ChangedObject = REPLACE(@ChangedObject, 'Type', '');


	-- if there are any lookups on this type update this asset types display values 

	if exists (select 1 from FieldType where LookupObjectType = @ChangedObject and LookupObjectID = @ChangedObjectTypeID)
	begin
		Print 'Found dependent lookup fields updating them'
		-- loop through the affected types update there display values and call this function with there info
		Declare curP cursor LOCAL For

		 select distinct [Object] as ObjectType, ObjectID, AssetTypeID from FieldType where LookupObjectType = @ChangedObject and LookupObjectID = @ChangedObjectTypeID and AssetTypeID is not null

		OPEN curP 
		Fetch Next From curP Into @ObjectType, @ObjectID,@AssetTypeID

		While @@Fetch_Status = 0 Begin

			print 'Updating dependent AssetTypeID'
			print @AssetTypeID

			exec GenerateAssetTypeDisplayValues	@AssetTypeID

			--exec UpdateDependentObjectTypeDisplayValues  @ObjectType, @ObjectID

		Fetch Next From curP Into @ObjectType, @ObjectID,@AssetTypeID

		End -- End of Fetch

		Close curP
		Deallocate curP
	end

END
GO;

drop procedure GenerateAssetDisplayValue
GO;

create PROCEDURE GenerateAssetDisplayValue	
	@AssetID bigint,
	@Object varchar(20),
	@ObjectID int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @DisplayValue NVARCHAR(MAX);
	DECLARE @DisplayValueHash NVARCHAR(50);

	if @AssetID is null or @AssetID <= 0
	begin
		select @AssetID = id from asset where [object] = @Object and [objectid] = @ObjectID		
	end

	Select @displayValue = DisplayValue from GetAssetDisplayValueById(@AssetID);
	SELECT @DisplayValueHash = CONVERT(NVARCHAR(32),HashBytes('SHA1', @displayValue),2)
		
	Print 'DisplayValue: ' + @DisplayValue
	Print 'DisplayValueHash: ' + @DisplayValueHash
	-- if exists update
	

	if exists(select 1 from AssetDisplayValue where AssetID = @AssetID)
	begin		
			UPDATE AssetDisplayValue
				SET DisplayValue = A.DisplayValue,
					DisplayValueHash = @DisplayValueHash,
					DisplayValuePrefix = SUBSTRING(A.DisplayValue, 1, 250),
					UpdatedOn = getutcdate()
				FROM GetAssetDisplayValueById(@AssetID) A		
				where AssetID = @AssetID	
	end
	else
	begin
			insert into AssetDisplayValue (AssetID,DisplayValue,DisplayValueHash,DisplayValuePrefix, UpdatedOn) values(@AssetID,@displayValue,@DisplayValueHash,SUBSTRING(@displayValue, 1, 250),getutcdate())
	end	

	Declare @assetObjectType varchar(20);
	Declare @assetObjectID int;
	
	select @assetObjectType = ATT.[Object], @assetObjectID = ATT.ObjectID from Asset A inner join AssetType ATT on A.AssetTypeID = ATT.ID where A.id = @AssetID

	exec UpdateDependentObjectTypeDisplayValues @assetObjectType,@assetObjectID	
END
GO;

drop procedure GenerateAssetTypeDisplayValues
GO;

CREATE PROCEDURE GenerateAssetTypeDisplayValues	
	@AssetTypeID int
AS
BEGIN
		SET NOCOUNT ON;
  DECLARE @trancount int;
  SET @trancount = @@trancount;
  BEGIN TRY
    IF @trancount = 0
      BEGIN TRANSACTION
      ELSE
        SAVE TRANSACTION usp_my_procedure_name;

			--delete by the asset type
			delete from AssetDisplayValue where assetid in (select id from asset where assettypeid = @AssetTypeID);

			insert into AssetDisplayValue (AssetID, DisplayValue, DisplayValueHash, DisplayValuePrefix, UpdatedOn)
				select
					A.ID,
					ADV.DisplayValue,
					CONVERT(NVARCHAR(32),HashBytes('SHA1', ADV.DisplayValue),2) as DisplayValueHash,
					SUBSTRING(ADV.DisplayValue, 1, 250) as DisplayValuePrefix,
					getutcdate()
				from
					Asset A
					cross apply GetAssetDisplayValueByID(A.ID) ADV
				where 
					A.AssetTypeID = @AssetTypeID and ADV.DisplayValue is not null		

				lbexit:
      IF @trancount = 0
      COMMIT;
  END TRY
  BEGIN CATCH
    DECLARE @error int,
            @message varchar(4000),
            @xstate int;

    SELECT
      @error = ERROR_NUMBER(),
      @message = ERROR_MESSAGE(),
      @xstate = XACT_STATE();

    IF @xstate = -1
      ROLLBACK;
    IF @xstate = 1 AND @trancount = 0
      ROLLBACK
    IF @xstate = 1 AND @trancount > 0
      ROLLBACK TRANSACTION usp_my_procedure_name;

    RAISERROR ('GenerateAllAssetTypeDisplayValues: %d: %s', 16, 1, @error, @message);
  END CATCH
END
GO;

drop procedure GenerateAllAssetTypeDisplayValues
GO;

CREATE PROCEDURE GenerateAllAssetTypeDisplayValues	
AS
BEGIN
	SET NOCOUNT ON;
  DECLARE @trancount int;
  SET @trancount = @@trancount;
  BEGIN TRY
    IF @trancount = 0
      BEGIN TRANSACTION
      ELSE
        SAVE TRANSACTION usp_my_procedure_name;

			--delete by the asset type
			delete from AssetDisplayValue;

			insert into AssetDisplayValue (AssetID, DisplayValue, DisplayValueHash, DisplayValuePrefix, UpdatedOn)
				select
					A.ID,
					ADV.DisplayValue,
					CONVERT(NVARCHAR(32),HashBytes('SHA1', ADV.DisplayValue),2) as DisplayValueHash,
					SUBSTRING(ADV.DisplayValue, 1, 250) as DisplayValuePrefix,
					getutcdate()
				from
					Asset A
					cross apply GetAssetDisplayValueByID(A.ID) ADV		
				where ADV.DisplayValue is not null and A.[Object] not in( 'FusionAttribute','FusionQueryAttribute')	

		lbexit:
      IF @trancount = 0
      COMMIT;
  END TRY
  BEGIN CATCH
    DECLARE @error int,
            @message varchar(4000),
            @xstate int;

    SELECT
      @error = ERROR_NUMBER(),
      @message = ERROR_MESSAGE(),
      @xstate = XACT_STATE();

    IF @xstate = -1
      ROLLBACK;
    IF @xstate = 1 AND @trancount = 0
      ROLLBACK
    IF @xstate = 1 AND @trancount > 0
      ROLLBACK TRANSACTION usp_my_procedure_name;

    RAISERROR ('GenerateAllAssetTypeDisplayValues: %d: %s', 16, 1, @error, @message);
  END CATCH
END
GO;

drop procedure CheckDisplayValues
GO;

CREATE PROCEDURE CheckDisplayValues	
	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	-- CHECK FOR ASSETS MISSING DISPLAY VALUES AND INSERT THEM
	insert into AssetDisplayValue (AssetID, DisplayValue, DisplayValueHash, DisplayValuePrefix, UpdatedOn)
				select
					A.ID,
					ADV.DisplayValue,
					CONVERT(NVARCHAR(32),HashBytes('SHA1', ADV.DisplayValue),2) as DisplayValueHash,
					SUBSTRING(ADV.DisplayValue, 1, 250) as DisplayValuePrefix,
					getutcdate()
				from
					Asset A
					cross apply GetAssetDisplayValueByID(A.ID) ADV		
				where ADV.DisplayValue is not null and not exists ( select 1 from assetdisplayvalue ad where ad.assetid = A.id)	
					and A.[Object] not in( 'FusionAttribute','FusionQueryAttribute')
	
END
GO;

exec GenerateAllAssetTypeDisplayValues
GO;



-- other proc/object updates

alter procedure [asset].[BulkUpsert]
--declare 
	@isInsert bit,
	@uid uniqueidentifier,
	@r int
as
begin
	set nocount on;
/*
	-- test to set parameters
	declare @isInsert bit = 1, @uid uniqueidentifier = 'A9B94F4B-14F6-474F-9572-80F954C8FC59', @r int = 1

	--TESTING LOGIC

	drop table if exists #AssetTable;
	create table #AssetTable (
		ItemNumber int not null,

		Uid uniqueidentifier null,
		AssetID bigint null,
		Object varchar(50) null,
		ObjectID int null,
		KeyHash varchar(50) null,

		ParentUid uniqueidentifier null,
		ParentObject varchar(50) null,
		ParentObjectID int null,

		[Message] nvarchar(2500) null,

		Success bit null,
		IsNew bit null
	);
	drop table if exists #AssetFieldTable;
	create table #AssetFieldTable (
		ItemNumber int not null,
		FieldName nvarchar(250) not null,
		FieldValue nvarchar(max) null,
		FieldTypeID int null,
		LookupValue nvarchar(250) null

	);

	insert into #AssetTable (ItemNumber, [Uid]) values (1, null);--'AC8AE7C0-8CD0-482D-AC44-DB05502150B3');
	insert into #AssetFieldTable (ItemNumber, FieldName, FieldValue) values (1, 'Name', 'Pappas loads with asset.BulkUpsert');
	insert into #AssetFieldTable (ItemNumber, FieldName, FieldValue) values (1, 'PersonalDataFlag', 'true');
	insert into #AssetFieldTable (ItemNumber, FieldName, FieldValue) values (1, 'GDPRCompliant', 'false');
	insert into #AssetFieldTable (ItemNumber, FieldName, FieldValue) values (1, 'CDE', 'false');
	insert into #AssetFieldTable (ItemNumber, FieldName, FieldValue) values (1, 'SpecialData', 'true');
	insert into #AssetFieldTable (ItemNumber, FieldName, FieldValue) values (1, 'Status', 'In progress');
	insert into #AssetFieldTable (ItemNumber, FieldName, FieldValue) values (1, 'SubjectArea', 'Investments');
*/

	declare @ot varchar(50),
			@otid int,
			@at int,
			@class int,
			@parentIntersectTypeUid uniqueidentifier,
			@parentIntersectTypeID int,
			@parentOt varchar(50),
			@parentOtId int
	select	@ot = Object,
			@otid = ObjectID,
			@at = ID,
			@class = [Class] 
	from	AssetType
	where	[uid] = @uid

	--Determine if there should be a parent present.
	select	@parentIntersectTypeUid = I.[Uid],
			@parentIntersectTypeID = I.ID,
			@parentOt = I.Subject,
			@parentOtId = I.SubjectID
	from	IntersectType I
			inner join [Predicate] P on P.ID = I.PredicateID 
									and I.Object = @ot
									and I.ObjectID = @otid
									and P.[Type] = case @ot
														when 'PolicyType' then 4
														when 'TaxonomyType' then 4
														else 3 --InterTypeHierarchy
													end

	-- Resolve the FieldTypeIDs for the fields you have added.
	update	T
	set		T.FieldTypeID = S.ID
	from	#AssetFieldTable T
			inner join FieldType S on S.AssetTypeID = @at and S.Name = T.FieldName
	----------------------------------------------------------

	BEGIN 
		-- Validation checks ----------

		-- 0. Did user pass any UIDs when this is an INSERT-only action?
		if @isInsert = 1
		begin
			update	#AssetTable
			set		Success = 0,
					[Message] = coalesce([Message] + '; ', '') + 'You may not provide a Uid for this asset when you are attempting to add it'
			where	[Uid] is not null 
		end;

		-- 0. Did user pass proper Uids when this is an UPDATE-only action?
		if @isInsert = 0
		begin
			update	#AssetTable
			set		Success = 0,
					[Message] = coalesce([Message] + '; ', '') + 'You must provide a valid Uid for this asset when you are attempting to update it'
			where	[Uid] is null or [Uid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER) -- (empty guid)
		end;

		-- 0. Did user pass any Parent Uids when this is an UPDATE-only action?
		if @isInsert = 0
		begin
			update	#AssetTable
			set		Success = 0,
					[Message] = coalesce([Message] + '; ', '') + 'You may not provide a Parent Uid for this asset when you are attempting to update it'
			where	[ParentUid] is not null 
		end;

		-- 0. If parents required and this is an INSERT command, make sure there is a parentUid present and it is valid.
		IF @parentIntersectTypeID is not null and @isInsert = 1
		BEGIN
			update	#AssetTable
			set		Success = 0,
					[Message] = coalesce([Message] + '; ', '') + 'Asset is missing a required ParentUid value'
			where	ParentUid is null;

			update	T
			set		T.ParentObject = S.Object,
					T.ParentObjectID = S.ObjectID
			from	#AssetTable T
					inner join Asset S on S.[Uid] = T.ParentUid and T.ParentUid is not null
					inner join AssetType ST on ST.ID = S.AssetTypeID and ST.Object = @parentOt and ST.ObjectID = @parentOtId;

			update	#AssetTable
			set		Success = 0,
					[Message] = coalesce([Message] + '; ', '') + 'Asset does not contain a valid ParentUid value'
			where	ParentObjectID is null
					and ParentUid is not null;
		END;

		-- 1. Does asset have all the key fields defined?
		--if @isInsert = 1
		--begin
			update	T
			set		T.Success = 0,
					T.[Message] = coalesce(T.[Message] + '; ', '') + 'Asset is missing key field(s): [' + S.Names + ']'
			from	#AssetTable T
					inner join	(
								select	A.ItemNumber,
										STRING_AGG(FT.Name, ', ') as Names
								from	#AssetTable A
										inner join FieldType FT on FT.AssetTypeID = @at 
																	and FT.IsPartOfKey = 1
										left join #AssetFieldTable F on F.ItemNumber = A.ItemNumber and F.FieldTypeID = FT.ID
								where	F.ItemNumber is null
								group by A.ItemNumber
								) S on S.ItemNumber = T.ItemNumber;
		--end;

		-- 2. Does asset have all required fields defined?
		if @isInsert = 1
		begin
			update	T
			set		T.Success = 0,
					T.[Message] = coalesce(T.[Message] + '; ', '') + 'Asset is missing required field(s): [' + S.Names + ']'
			from	#AssetTable T
					inner join	(
								select	A.ItemNumber,
										STRING_AGG(FT.Name, ', ') as Names
								from	#AssetTable A
										inner join FieldType FT on FT.AssetTypeID = @at 
																	and FT.IsRequired = 1
										left join #AssetFieldTable F on F.ItemNumber = A.ItemNumber and F.FieldTypeID = FT.ID 
								where	F.ItemNumber is null
								group by A.ItemNumber
								) S on S.ItemNumber = T.ItemNumber
		end;

		-- 3. Are all lookup fields valid, based on field's LookupEditFormat, or LookupDisplayFormat?

		--- A. Get the valid lookup values.
		--the query below is SUPER slow, using the one below that just looks for reference list lookups for now.
		--update	T
		--set		T.LookupValue = S.[Value]
		--from	#AssetFieldTable T
		--		inner join FieldType F on F.ID = T.FieldTypeID and F.[Type] = 'Lookup'
		--		inner join FieldLookupValue S on S.FieldTypeID = F.ID and S.[Text] = T.FieldValue
		update	T
		set		T.LookupValue = RI.ID
		from	#AssetFieldTable T
				inner join FieldType F on F.ID = T.FieldTypeID and F.[Type] = 'Lookup'
				inner join ReferenceItem RI ON F.LookupObjectType = 'ReferenceItem' and F.LookupObjectID = RI.ReferenceItemTypeID 

					and T.FieldValue = utility.GetFormattedFieldLookupValue(F.Type, coalesce(F.LookupEditFormat, F.LookupDisplayFormat), F.LookupObjectType, F.LookupObjectID, RI.ID);

		update	T
		set		T.LookupValue = RI.ID
		from	#AssetFieldTable T
				inner join FieldType F on F.ID = T.FieldTypeID and F.[Type] = 'Lookup'
				inner join ReferenceItemType RI ON F.LookupObjectType = 'ReferenceItemType'
					and T.FieldValue = utility.GetFormattedFieldLookupValue(F.Type, coalesce(F.LookupEditFormat, F.LookupDisplayFormat), F.LookupObjectType, F.LookupObjectID, RI.ID);

		--- B. Check which fields do not have a valid lookup value from query above.
		update	T
		set		T.Success = 0,
				T.[Message] = coalesce(T.[Message] + '; ', '') + 'Asset contains one or more fields with invalid lookup values: [' + S.Names + ']'
		from	#AssetTable T
				inner join	(
							select		A.ItemNumber,
										STRING_AGG(FT.Name+'='+F.FieldValue, ', ') as Names
							from		#AssetTable A
										inner join FieldType FT on FT.AssetTypeID = @at 
																	and FT.[Type] = 'Lookup'
										inner join #AssetFieldTable F on F.ItemNumber = A.ItemNumber and F.FieldTypeID = FT.ID and F.LookupValue is null
							group by	A.ItemNumber
							) S on S.ItemNumber = T.ItemNumber;

		-- 4. Are all values valid based on field's data type?
		update	T
		set		T.Success = 0,
				T.[Message] = coalesce(T.[Message] + '; ', '') + 'Asset contains one or more field that are invalid based on their data types: [' + S.Names + ']'
		from	#AssetTable T
				inner join	(
							select	A.ItemNumber,
									STRING_AGG(FT.Name + ' is ' + FT.[Type] + ' but has a value of ' + F.FieldValue, ', ') as Names
							from	#AssetTable A
									inner join FieldType FT on FT.AssetTypeID = @at 
									inner join #AssetFieldTable F on F.ItemNumber = A.ItemNumber 
																	and F.FieldTypeID = FT.ID 
																	and (
																		(FT.[Type] = 'Boolean' and LOWER(F.FieldValue)  not in ('false', 'true')) or 
																		(FT.[Type] = 'Date' and ISDATE(F.FieldValue) = 0) or 
																		(FT.[Type] = 'DateTime' and ISDATE(F.FieldValue) = 0) or 
																		(FT.[Type] = 'Number' and ISNUMERIC(F.FieldValue + '.e0') = 0) or 
																		(FT.[Type] = 'Decimal' and ISNUMERIC(F.FieldValue) = 0) or 
																		(FT.[Type] = 'Link' and (CHARINDEX('|', F.FieldValue, 0) = 0 OR CHARINDEX('|', F.FieldValue, 0) is null) ) or 
																		(FT.[Type] = 'Percentage' and ISDATE(F.FieldValue) = 0)
																	)
							group by A.ItemNumber
							) S on S.ItemNumber = T.ItemNumber;

		-- 5. Check if length populated, if so is the field's length valid?
		update	T
		set		T.Success = 0,
				T.[Message] = coalesce(T.[Message] + '; ', '') + 'Asset contains one or more field that have an invalid length: [' + S.Names + ']'
		from	#AssetTable T
				inner join	(
							select	A.ItemNumber,
									STRING_AGG(FT.Name + ' must have an exact length of ' + cast(FT.[Length] as nvarchar), ', ') as Names
							from	#AssetTable A
									inner join FieldType FT on FT.AssetTypeID = @at 
									inner join #AssetFieldTable F on F.ItemNumber = A.ItemNumber 
																	and F.FieldTypeID = FT.ID 
																	and FT.[Length] is not null
																	and FT.[Length] <> LEN(F.FieldValue)
							group by A.ItemNumber
							) S on S.ItemNumber = T.ItemNumber;

		-- 6. Check if minimum length populated, if so is the field's minimum length valid?
		update	T
		set		T.Success = 0,
				T.[Message] = coalesce(T.[Message] + '; ', '') + 'Asset contains one or more field that have an invalid minimum length: [' + S.Names + ']'
		from	#AssetTable T
				inner join	(
							select	A.ItemNumber,
									STRING_AGG(FT.Name + ' must have a minimum length of ' + cast(FT.[MinimumLength] as nvarchar), ', ') as Names
							from	#AssetTable A
									inner join FieldType FT on FT.AssetTypeID = @at 
									inner join #AssetFieldTable F on F.ItemNumber = A.ItemNumber 
																	and F.FieldTypeID = FT.ID 
																	and FT.[MinimumLength] is not null
																	and FT.[MinimumLength] > LEN(F.FieldValue)
							group by A.ItemNumber
							) S on S.ItemNumber = T.ItemNumber;

		-- 7. Check if maximum length populated, if so is the field's maximum length valid?
		update	T
		set		T.Success = 0,
				T.[Message] = coalesce(T.[Message] + '; ', '') + 'Asset contains one or more field that have an invalid maximum length: [' + S.Names + ']'
		from	#AssetTable T
				inner join	(
							select	A.ItemNumber,
									STRING_AGG(FT.Name + ' must have a maximum length of ' + cast(FT.[MaximumLength] as nvarchar), ', ') as Names
							from	#AssetTable A
									inner join FieldType FT on FT.AssetTypeID = @at 
									inner join #AssetFieldTable F on F.ItemNumber = A.ItemNumber 
																	and F.FieldTypeID = FT.ID 
																	and FT.[MaximumLength] is not null
																	and FT.[MaximumLength] < LEN(F.FieldValue)
							group by A.ItemNumber
							) S on S.ItemNumber = T.ItemNumber;

		-- 8. If regex defined, validate against the Pattern field as defined on FieldType.
		-- TODO: perhaps implement a CLR function here.
		-- https://stackoverflow.com/questions/194652/sql-server-regular-expressions-in-t-sql

		-- 9. If KeyHash matches an asset with a different UID than the one provided (IF provided), throw an error.

		--- A. First, figure out what the hash should be, if this is an insert
		--if @isInsert = 1
		--begin
			update	T
			set		T.KeyHash = S.KeyHash
			from	#AssetTable T
					inner join	(
								select	O.ItemNumber,
										utility.GetHash(STRING_AGG(O.Value, '|')) as KeyHash
								from	(
										select	top 100 percent
												A.ItemNumber,
												coalesce(F.LookupValue, F.FieldValue) as [Value]
										from	#AssetTable A
												inner join FieldType FT on FT.AssetTypeID = @at 
												inner join #AssetFieldTable F on F.ItemNumber = A.ItemNumber 
																				and F.FieldTypeID = FT.ID 
																				and FT.IsPartOfKey = 1
																				and A.Success is null -- We have not failed yet.
										order by FT.ColumnOrder						
										) O
								group by O.ItemNumber
								) S on S.ItemNumber = T.ItemNumber;
		--end

		--- B. Next, validate the hash against the object we are trying to update.
		if @isInsert = 1
		begin
			update	T
			set		T.Success = 0,
					T.[Message] = coalesce(T.[Message] + '; ', '') + 'Asset contains an error: [' + S.Error + ']'
			from	#AssetTable T
					inner join	(
								select	T.ItemNumber,
										'Key values match another asset under a different set of key fields.' as Error
								from	#AssetTable T
										inner join Asset S on S.AssetTypeID = @at 
										cross apply dbo.GetAssetKeyHashById(S.ID) K
								where	K.KeyHash = T.KeyHash
								) S on S.ItemNumber = T.ItemNumber;
		end
		else
		begin 
			update	T
			set		T.Success = 0,
					T.[Message] = coalesce(T.[Message] + '; ', '') + 'Asset contains an error: [' + S.Error + ']'
			from	#AssetTable T
					inner join	(
								select	T.ItemNumber,
										'Key values match another asset under a different public uid.' as Error
								from	#AssetTable T
										inner join Asset S on S.AssetTypeID = @at 
										cross apply dbo.GetAssetKeyHashById(S.ID) K
								where	K.KeyHash = T.KeyHash and T.[Uid] <> S.[Uid]
								) S on S.ItemNumber = T.ItemNumber;
		end

	END	-------------------------------

	-- Now upsert the valid assets.
	drop table if exists #ObjectMergeTableResult;
	create table #ObjectMergeTableResult (ID int, ItemNumber int, [Action] nvarchar(10));
	CREATE NONCLUSTERED INDEX IX_TempObjectMergeTableResult ON #ObjectMergeTableResult ( ItemNumber ASC );

	if @isInsert = 0
	begin
		update	T
		set		T.Object = S.Object,
				T.ObjectID = S.ObjectID,
				T.AssetID = S.ID
		from	#AssetTable T
				inner join Asset S on S.[Uid] = T.[Uid]
	end;

	declare @current int = 1,	-- to track which ItemNumber row you are on.
			@max int = 0,
			@objectId int

	select @max = max(ItemNumber) from #AssetTable

	IF @class = 1 --GLOSSARY
	BEGIN
		if @isInsert = 1
		begin
			while @current <= @max
			begin
				if exists(select ItemNumber from #AssetTable where ItemNumber = @current and Success is null and ObjectID is null)
				begin
					insert Artifact(ArtifactTypeID, CreatedOn, UpdatedBy, UpdatedOn, Visible)
					values (@otid, getutcdate(), @r, getutcdate(), 1);

					set	@objectId = SCOPE_IDENTITY()

					update	T
					set		T.Object ='Artifact',
							T.ObjectID = @objectId,
							T.AssetID = S.ID,
							T.[Uid] = S.[Uid],
							T.IsNew = 1
					from	#AssetTable T
							inner join Asset S on S.Object = 'Artifact' and S.ObjectID = @objectId and T.ItemNumber = @current; 
				end
				set @current = @current + 1
			end
		end
		else
		begin
			update	T
			set		T.UpdatedBy = @r,
					T.UpdatedOn = getutcdate()
			from	Artifact T
					inner join #AssetTable S on S.ObjectID = T.ID;

			update	#AssetTable
			set		IsNew = 0
			where	Success is null;
		end
	END;

	IF @class = 2 --MODEL
	BEGIN
		if @isInsert = 1
		begin
			while @current <= @max
			begin
				if exists(select ItemNumber from #AssetTable where ItemNumber = @current and Success is null and ObjectID is null)
				begin
					insert Taxonomy(TaxonomyTypeID, UpdatedBy, UpdatedOn, Visible)
					values (@otid, @r, getutcdate(), 1);

					set	@objectId = SCOPE_IDENTITY()

					update	T
					set		T.Object ='Taxonomy',
							T.ObjectID = @objectId,
							T.AssetID = S.ID,
							T.[Uid] = S.[Uid],
							T.IsNew = 1
					from	#AssetTable T
							inner join Asset S on S.Object = 'Taxonomy' and S.ObjectID = @objectId and T.ItemNumber = @current; 
				end
				set @current = @current + 1
			end
		end
		else
		begin
			update	T
			set		T.UpdatedBy = @r,
					T.UpdatedOn = getutcdate()
			from	Taxonomy T
					inner join #AssetTable S on S.ObjectID = T.ID;

			update	#AssetTable
			set		IsNew = 0
			where	Success is null;
		end
	END;

	IF @class = 3 --FUSION ATTRIBUTE
	BEGIN
		if @isInsert = 1

		begin
			while @current <= @max
			begin
				declare @fusionId int,
						@fusionName nvarchar(250)

				select	@fusionId = cast(F.FieldValue as int),
						@fusionName = N.FieldValue
				from	#AssetTable A
						inner join #AssetFieldTable N on N.ItemNumber = A.ItemNumber and N.FieldName = 'Name'
						inner join #AssetFieldTable F on F.ItemNumber = A.ItemNumber and F.FieldName = 'FusionID'
				where	A.Success is null -- no errors from validation
						and A.ObjectID is null
						and A.ItemNumber = @current;

				if @fusionId is not null and @fusionName is not null
				begin
					insert FusionAttribute(FusionAttributeTypeID, Name, FusionID)
					values (@otid, @fusionName, @fusionId);

					set	@objectId = SCOPE_IDENTITY()

					update	T
					set		T.Object ='FusionAttribute',
							T.ObjectID = @objectId,
							T.AssetID = S.ID,
							T.[Uid] = S.[Uid],
							T.IsNew = 1
					from	#AssetTable T
							inner join Asset S on S.Object = 'FusionAttribute' and S.ObjectID = @objectId and T.ItemNumber = @current; 
				end

				set @current = @current + 1
			end
		end
		else
		begin
			update	#AssetTable
			set		IsNew = 0
			where	Success is null;
		end
	END;

	IF @class = 6 --POLICY
	BEGIN
		if @isInsert = 1
		begin
			while @current <= @max
			begin
				if exists(select ItemNumber from #AssetTable where ItemNumber = @current and Success is null and ObjectID is null)
				begin
					insert [Policy](PolicyTypeID, UpdatedBy, UpdatedOn, Visible)
					values (@otid, @r, getutcdate(), 1);

					set	@objectId = SCOPE_IDENTITY()

					update	T
					set		T.Object ='Policy',
							T.ObjectID = @objectId,
							T.AssetID = S.ID,
							T.[Uid] = S.[Uid],
							T.IsNew = 1
					from	#AssetTable T
							inner join Asset S on S.Object = 'Policy' and S.ObjectID = @objectId and T.ItemNumber = @current;
				end
				set @current = @current + 1
			end
		end
		else
		begin
			update	T
			set		T.UpdatedBy = @r,
					T.UpdatedOn = getutcdate()
			from	[Policy] T
					inner join #AssetTable S on S.ObjectID = T.ID;

			update	#AssetTable
			set		IsNew = 0
			where	Success is null;
		end
	END;

	IF @class = 7 --RULE
	BEGIN
		if @isInsert = 1
		begin
			while @current <= @max
			begin
				if exists(select ItemNumber from #AssetTable where ItemNumber = @current and Success is null and ObjectID is null)
				begin

					insert [Rule](RuleTypeID, UpdatedBy, UpdatedOn, Visible)
					values (@otid, @r, getutcdate(), 1);

					set	@objectId = SCOPE_IDENTITY()

					update	T
					set		T.Object ='Rule',
							T.ObjectID = @objectId,
							T.AssetID = S.ID,
							T.[Uid] = S.[Uid],
							T.IsNew = 1
					from	#AssetTable T
							inner join Asset S on S.Object = 'Rule' and S.ObjectID = @objectId and T.ItemNumber = @current;

				end
				set @current = @current + 1 
			end
		end
		else
		begin
			update	T
			set		T.UpdatedBy = @r,
					T.UpdatedOn = getutcdate()
			from	[Rule] T
					inner join #AssetTable S on S.ObjectID = T.ID;

			update	#AssetTable
			set		IsNew = 0
			where	Success is null;
		end
	END;

	IF @class = 9 --REFERENCE
	BEGIN
		if @isInsert = 1
		begin
			--while @current > 0 and @current is not null
			while @current <= @max
			begin
				--set @current = 0

				declare @code nvarchar(250)

				select		top 1
							--@current = A.ItemNumber,
							@code = F.FieldValue-- + ' ' + cast(A.ItemNumber as nvarchar)
				from		#AssetTable A
							inner join #AssetFieldTable F on F.ItemNumber = A.ItemNumber and F.FieldName = 'Code'
				where		A.Success is null -- no errors from validation
							and A.ObjectID is null
							and A.ItemNumber = @current
				--order by	A.ItemNumber;

				if @code is not null
				begin
					insert ReferenceItem(ReferenceItemTypeID, Code, UpdatedBy, UpdatedOn, Visible)
					values (@otid, @code, @r, getutcdate(), 1);

					set	@objectId = SCOPE_IDENTITY()

					update	T
					set		T.Object ='ReferenceItem',
							T.ObjectID = @objectId,
							T.AssetID = S.ID,
							T.[Uid] = S.[Uid],
							T.IsNew = 1
					from	#AssetTable T
							inner join Asset S on S.Object = 'ReferenceItem' and S.ObjectID = @objectId and T.ItemNumber = @current
				end

				set @current = @current + 1
			end
		end
		else
		begin
			update	T
			set		T.UpdatedBy = @r,
					T.UpdatedOn = getutcdate()
			from	ReferenceItem T
					inner join #AssetTable S on S.ObjectID = T.ID;

			update	#AssetTable
			set		IsNew = 0
			where	Success is null;
		end
	END;

	/*
	-- testing
	declare @isInsert bit = 1, @uid uniqueidentifier = 'A9B94F4B-14F6-474F-9572-80F954C8FC59', @r int = 1
	declare @ot varchar(50),
			@otid int,
			@at int,
			@class int,
			@parentIntersectTypeUid uniqueidentifier,
			@parentIntersectTypeID int,
			@parentOt varchar(50),
			@parentOtId int
	select	@ot = Object,
			@otid = ObjectID,
			@at = ID,
			@class = [Class] 
	from	AssetType
	where	[uid] = @uid
	*/

	-- Merge the parent/child relationships if required.
	IF @parentIntersectTypeID is not null and @isInsert = 1
	BEGIN
		-- Remove parent/child records that are no longer valid for the assets we are loading.
		delete	T
		from	[Intersect] T
				inner join #AssetTable S on T.IntersectTypeID = @parentIntersectTypeID 
											and S.Object = T.Object 
											and S.ObjectID = T.ObjectID 
											and (S.ParentObject <> T.Subject OR S.ParentObjectID <> T.SubjectID)
											and S.Object is not null 
											and S.ObjectID is not null 
											and S.ParentObject is not null 
											and S.ParentObjectID is not null;

		-- Merge parent/child relationships.
		merge into  [Intersect] T
		using		(
					select      *
					from        #AssetTable
					where		Object is not null 
								and ObjectID is not null 
								and ParentObject is not null 
								and ParentObjectID is not null
								and Success is null	-- We have not failed in validation.
                ) S
		on      ( T.IntersectTypeID = @parentIntersectTypeID and S.Object = T.Object and S.ObjectID = T.ObjectID )
		when not matched by target then
			insert  (IntersectTypeID, Subject, SubjectID, Object, ObjectID, CreatedBy, UpdatedBy)
			values  (@parentIntersectTypeID, S.ParentObject, S.ParentObjectID, S.Object, S.ObjectID, @r, @r);
	END;




	-- Merge field data ---------------------------
	merge into  Field T
    using       (
                select  distinct 
                        A.AssetID,
						A.Object, 
                        A.ObjectID, 
                        F.FieldTypeID,
                        coalesce(F.LookupValue, F.FieldValue) as Value
                from    #AssetFieldTable F
                        inner join #AssetTable A on A.ItemNumber = F.ItemNumber 
                            and A.ObjectID is not null 
                            and F.FieldTypeID is not null
							and A.Success is null	-- We have not failed in validation.
                ) S
    on          (
                    T.FieldTypeID = S.FieldTypeID and 
                    T.AssetID = S.AssetID
                )
    when		matched then
	update		set
					T.Value = S.Value
    when		not matched by target then
	insert		(FieldTypeID, ObjectType, ObjectID, AssetID, Value)
    values		(S.FieldTypeID, S.Object, S.ObjectID, S.AssetID, S.Value);
	-----------------------------------------------

	update	#AssetTable
	set		Success = 1
	where	Success is null
			and Object is not null
			and ObjectID is not null;

	select * from #AssetTable
	--select * from #AssetFieldTable
	--update #AssetTable set Success = null
	--update #AssetFieldTable set LookupValue = null
end
GO;

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

	--Testing
	--	insert into [dbo].[Testing_AddAuditEntry]
	--(DependentObject,DependentObjectID,ResourceID,[Date],[Action],MainObject,MainObjectID)
	--Select @DependentObject,@DependentObjectID,@ResourceID,@Date,@Action,@MainObject,@MainObjectID

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
				select @DependentObjectName = D.[Name]
				from
				(
					select DisplayValue as [Name], [Object], ObjectID from AssetDetail
					union all
					select [Name], [Object], ObjectID from AssetType
				) D where D.ObjectID = @DependentObjectID	and D.[Object] = @DependentObject
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

		insert into @tbl  select 0, 'IconBackColor', IconBackColor, 0, 0 from objectstyle where ObjectID = @MainObjectID and [ObjectType] = @MainObject		
		insert into @tbl  select 0, 'IconForeColor', IconForeColor, 0, 0 from objectstyle where ObjectID = @MainObjectID and [ObjectType] = @MainObject		

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
GO;

alter procedure [utility].[ClearDatabase]
as
begin
	truncate table [analytics].[Statistic];
	truncate table [analytics].[Action];
	truncate table [analytics].[BrowserLanguage];
	truncate table [analytics].[Host];
	truncate table [analytics].[Ip];
	truncate table [analytics].[Object];
	truncate table [analytics].[UserAgent];

	delete [Artifact];
	DBCC CHECKIDENT ('dbo.Artifact', RESEED, 1);













	delete Asset;
	DBCC CHECKIDENT ('dbo.Asset', RESEED, 1);



	delete AssetType;
	DBCC CHECKIDENT ('dbo.AssetType', RESEED, 1);

	delete [ArtifactType];
	DBCC CHECKIDENT ('dbo.ArtifactType', RESEED, 1);
	SET IDENTITY_INSERT dbo.ArtifactType ON;
	INSERT INTO ArtifactType (ID, Name, CanOwnFusion, UpdatedOn, UpdatedBy, AllowHierarchy) VALUES (1, 'Business Term', 0, getutcdate(), 0, 0);
	INSERT INTO ArtifactType (ID, Name, CanOwnFusion, UpdatedOn, UpdatedBy, AllowHierarchy) VALUES (2, 'Application', 1, getutcdate(), 0, 0);
	SET IDENTITY_INSERT dbo.ArtifactType OFF;

	truncate table [Attribute];
	truncate table [AttributeTypeRelation];
	delete [AttributeType];
	DBCC CHECKIDENT ('dbo.AttributeType', RESEED, 1);
	delete AttributeTypeCategory;
	DBCC CHECKIDENT ('dbo.AttributeTypeCategory', RESEED, 1);
	SET IDENTITY_INSERT dbo.AttributeTypeCategory ON;
	INSERT INTO AttributeTypeCategory (ID, Name) VALUES (1, 'Characteristics');
	SET IDENTITY_INSERT dbo.AttributeTypeCategory OFF;

	truncate table CommentRelation;
	truncate table CommentVote;
	delete Comment;
	DBCC CHECKIDENT ('dbo.Comment', RESEED, 1);

	delete [Contract];
	DBCC CHECKIDENT ('dbo.Contract', RESEED, 1);

	truncate table [Favorite];

	ALTER TABLE [dbo].[Field] SET (SYSTEM_VERSIONING = OFF);  
	truncate table [Field];
	truncate table [Field_History];
	ALTER TABLE [dbo].[Field] SET ( SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Field_History]) );   

	delete [FieldTypeFilteredLookupDefinition];
	DBCC CHECKIDENT ('dbo.FieldTypeFilteredLookupDefinition', RESEED, 1);
	truncate table [FieldTypeFilteredLookupDisplayField];
	DBCC CHECKIDENT ('dbo.FieldTypeFilteredLookupDisplayField', RESEED, 1);

	delete [FieldTypeFusionLookupDefinition];
	DBCC CHECKIDENT ('dbo.FieldTypeFusionLookupDefinition', RESEED, 1);
	truncate table [FieldTypeFusionLookupDisplayField];
	DBCC CHECKIDENT ('dbo.FieldTypeFusionLookupDisplayField', RESEED, 1);

	truncate table [FieldTypeLookup];

	ALTER TABLE [dbo].[FieldType] SET (SYSTEM_VERSIONING = OFF);  
	truncate table  [FieldType_History];
	delete [FieldType];
	DBCC CHECKIDENT ('dbo.FieldType', RESEED, 1);
	ALTER TABLE [dbo].[FieldType] SET ( SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[FieldType_History]) );   


	truncate table [Follow];

	truncate table [FusionFilter];
	truncate table [FusionOwner];

	truncate table [FusionAttribute];
	delete [FusionAttributeType];
	DBCC CHECKIDENT ('dbo.FusionAttributeType', RESEED, 1);

	truncate table [FusionQueryAttribute];
	delete [FusionQueryAttributeType];
	DBCC CHECKIDENT ('dbo.FusionQueryAttributeType', RESEED, 1);

	truncate table [FusionSchedule];
	truncate table [FusionStatusLog];

	delete [Fusion];
	DBCC CHECKIDENT ('dbo.Fusion', RESEED, 1);

	delete [FusionType];
	DBCC CHECKIDENT ('dbo.FusionType', RESEED, 1);

	truncate table [ResourceGroup];
	delete [Group];
	DBCC CHECKIDENT ('dbo.Group', RESEED, 1);

	delete [Intersect];
	DBCC CHECKIDENT ('dbo.Intersect', RESEED, 1);

	truncate table [IntersectGroupItem];

	delete [IntersectGroup];
	DBCC CHECKIDENT ('dbo.IntersectGroup', RESEED, 1);

	delete [IntersectType];
	DBCC CHECKIDENT ('dbo.IntersectType', RESEED, 1);

	delete [Predicate] where IsSystem = 0;
	DBCC CHECKIDENT ('dbo.Predicate', RESEED, 9);

	INSERT INTO [IntersectType] ( [UpdatedOn],[UpdatedBy],[Subject],[SubjectID],[Object],[ObjectID],[IsSystem],[CreatedBy],[CreatedOn],[PredicateID])
	VALUES ( getutcdate(),0,'ArtifactType',1,'ArtifactType',1,0,0,getutcdate(),2);

	INSERT INTO [IntersectType] ( [UpdatedOn],[UpdatedBy],[Subject],[SubjectID],[Object],[ObjectID],[IsSystem],[CreatedBy],[CreatedOn],[PredicateID])
	VALUES ( getutcdate(),0,'ArtifactType',2,'ArtifactType',1,0,0,getutcdate(),1);

	truncate table [Issue];
	delete [IssueType] where IsSystem = 0;
	DBCC CHECKIDENT ('dbo.IssueType', RESEED, 3);

	truncate table [LoadItemColumn];
	delete [LoadItem];
	delete [LoadColumn];
	delete [Load];
	DBCC CHECKIDENT ('dbo.Load', RESEED, 1);

	truncate table [Lookup];
	DBCC CHECKIDENT ('dbo.Lookup', RESEED, 1);
	delete [LookupType];
	DBCC CHECKIDENT ('dbo.LookupType', RESEED, 1);


	truncate table [MapItemMap];
	truncate table [MapRuleItemMapItem];
	truncate table [MapRuleItemMapRule];
	truncate table [MapRuleMap];
	truncate table [MapSequenceContext];
	delete MapSequence;
	DBCC CHECKIDENT ('dbo.MapSequence', RESEED, 1);

	delete Map;
	DBCC CHECKIDENT ('dbo.Map', RESEED, 1);

	truncate table NymRelation;
	truncate table Nym;

	truncate table [ObjectStyle];
	truncate table [OrganizationDomain];
	truncate table [OrganizationInvitation];
	truncate table [OrganizationRegistration];
	truncate table [OrganizationResource];

	delete Organization;
	DBCC CHECKIDENT ('dbo.Organization', RESEED, 1);

	truncate table [Policy];
	delete PolicyTypeLevel;
	delete PolicyType;
	DBCC CHECKIDENT ('dbo.PolicyType', RESEED, 1);
	delete PolicyTypeClass;
	DBCC CHECKIDENT ('dbo.PolicyTypeClass', RESEED, 1);
	insert into PolicyTypeClass ([Name], [UpdatedOn], [UpdatedBy]) values ('Regulatory', getutcdate(), 0);

	truncate table [QuestionOption];
	delete [Question];
	DBCC CHECKIDENT ('dbo.Question', RESEED, 1);
	delete QuestionTypeOption;
	DBCC CHECKIDENT ('dbo.QuestionTypeOption', RESEED, 1);
	delete QuestionType;
	DBCC CHECKIDENT ('dbo.QuestionType', RESEED, 1);

	truncate table ReferenceItem;
	DBCC CHECKIDENT ('dbo.ReferenceItem', RESEED, 1);
	delete ReferenceItemType;
	DBCC CHECKIDENT ('dbo.ReferenceItemType', RESEED, 1);

	truncate table [ReportResponsibility];
	DBCC CHECKIDENT ('dbo.ReportResponsibility', RESEED, 1);
	truncate table [ReportTile];
	DBCC CHECKIDENT ('dbo.ReportTile', RESEED, 1);
	delete [Report];
	DBCC CHECKIDENT ('dbo.Report', RESEED, 1);

	truncate table ResourcePasswordReset;

	truncate table [ResponsibilityTypeRelation];
	delete ResponsibilityType;
	DBCC CHECKIDENT ('dbo.ResponsibilityType', RESEED, 1);

	SET IDENTITY_INSERT dbo.ResponsibilityType ON;
	INSERT INTO ResponsibilityType	([ID], [Name],[ResponsibilityTypeGroup],[UpdatedOn],[UpdatedBy]) 
	VALUES							(1, 'Data Steward', 1, getutcdate(), 0);
	INSERT INTO ResponsibilityType	([ID], [Name],[ResponsibilityTypeGroup],[UpdatedOn],[UpdatedBy]) 
	VALUES							(2, 'Business Owner', 1, getutcdate(), 0);
	INSERT INTO ResponsibilityType	([ID], [Name],[ResponsibilityTypeGroup],[UpdatedOn],[UpdatedBy]) 
	VALUES							(3, 'Technical Custodian', 1, getutcdate(), 0);
	SET IDENTITY_INSERT dbo.ResponsibilityType OFF;

	INSERT INTO ResponsibilityTypeRelation (ResponsibilityTypeID, ObjectType, ObjectID) VALUES (1,'ArtifactType',1);
	INSERT INTO ResponsibilityTypeRelation (ResponsibilityTypeID, ObjectType, ObjectID) VALUES (2,'ArtifactType',1);
	INSERT INTO ResponsibilityTypeRelation (ResponsibilityTypeID, ObjectType, ObjectID) VALUES (3,'ArtifactType',2);



	delete [ResponsibilityTypeRelationRuleResult];



	delete [ResponsibilityTypeRelationOverrideItem];
	DBCC CHECKIDENT ('dbo.ResponsibilityTypeRelationOverrideItem', RESEED, 1);

	truncate table RuleResultFusionAttribute;
	DBCC CHECKIDENT ('dbo.RuleResultFusionAttribute', RESEED, 1);

	truncate table RuleResultQualifier;

	delete RuleResultQualifierType;
	DBCC CHECKIDENT ('dbo.RuleResultQualifierType', RESEED, 1);

	delete RuleResult;
	DBCC CHECKIDENT ('dbo.RuleResult', RESEED, 1);

	delete RuleImplementation;
	DBCC CHECKIDENT ('dbo.RuleImplementation', RESEED, 1);

	delete [Rule];
	DBCC CHECKIDENT ('dbo.Rule', RESEED, 1);

	delete [RuleType] where ID > 4;
	delete [RuleDimension] where IsSystemDefined = 0;

	truncate table metrics.MapResult;
	delete metrics.Score;
	DBCC CHECKIDENT ('metrics.Score', RESEED, 1);
	truncate table metrics.ConditionValue;
	delete metrics.Condition;
	delete metrics.Map;
	DBCC CHECKIDENT ('metrics.Map', RESEED, 1);
	delete metrics.Item;
	DBCC CHECKIDENT ('metrics.Item', RESEED, 1);
	delete metrics.[Group];
	DBCC CHECKIDENT ('metrics.Group', RESEED, 1);

	truncate table ShoppingCartItem;
	delete ShoppingCart;
	DBCC CHECKIDENT ('dbo.ShoppingCart', RESEED, 1);

	delete SiteNavPermission where SiteNavID > 9;
	delete SiteNav where ID > 9;

	delete Survey;
	DBCC CHECKIDENT ('dbo.Survey', RESEED, 1);
	delete SurveyType;
	DBCC CHECKIDENT ('dbo.SurveyType', RESEED, 1);

	truncate table Taxonomy;
	DBCC CHECKIDENT ('dbo.Taxonomy', RESEED, 1);

	delete TaxonomyTypeLevel;

	delete TaxonomyType;
	DBCC CHECKIDENT ('dbo.TaxonomyType', RESEED, 1);

	SET IDENTITY_INSERT dbo.TaxonomyType ON;
	INSERT INTO TaxonomyType (ID, Name, MaximumDepth, TaxonomyTypeClassID, UpdatedOn, UpdatedBy) VALUES (1, 'Investments', 3, 1, getutcdate(), 0);
	SET IDENTITY_INSERT dbo.TaxonomyType OFF;

	insert into TaxonomyTypeLevel values (1, 1, 'Level 1', null);
	insert into TaxonomyTypeLevel values (1, 2, 'Level 2', null);
	insert into TaxonomyTypeLevel values (1, 3, 'Level 3', null);

	delete TaxonomyTypeClass where ID > 3;

	truncate table [fusion].[AgentErrorItem];
	DBCC CHECKIDENT ('fusion.AgentErrorItem', RESEED, 1);
	delete [fusion].[AgentError];
	DBCC CHECKIDENT ('fusion.AgentError', RESEED, 1);

	truncate table [fusion].[Error];
	truncate table [fusion].[Result];

	truncate table [fusion].[RuleFilter];
	DBCC CHECKIDENT ('fusion.RuleFilter', RESEED, 1);
	truncate table [fusion].[RuleItem];
	DBCC CHECKIDENT ('fusion.RuleItem', RESEED, 1);
	truncate table [fusion].[RuleLog];
	DBCC CHECKIDENT ('fusion.RuleLog', RESEED, 1);
	truncate table [fusion].[RulePromotion];
	DBCC CHECKIDENT ('fusion.RulePromotion', RESEED, 1);

	truncate table [fusion].[RuleStepMapping];
	DBCC CHECKIDENT ('fusion.RuleStepMapping', RESEED, 1);
	truncate table [fusion].[RuleStepSetting];

	delete fusion.[RuleStep];
	DBCC CHECKIDENT ('fusion.RuleStep', RESEED, 1);

	delete fusion.[Rule];
	DBCC CHECKIDENT ('fusion.Rule', RESEED, 1);


	truncate table fusion.StagingFileItem;
	DBCC CHECKIDENT ('fusion.StagingFileItem', RESEED, 1);
	delete fusion.StagingFile;
	DBCC CHECKIDENT ('fusion.StagingFile', RESEED, 1);

	truncate table fusion.StagingRelation;
	DBCC CHECKIDENT ('fusion.StagingRelation', RESEED, 1);
	truncate table  fusion.StagingRelationUnresolved;
	DBCC CHECKIDENT ('fusion.StagingRelationUnresolved', RESEED, 1);

	delete fusion.Execution
	DBCC CHECKIDENT ('fusion.Execution', RESEED, 1);

	truncate table [queue].Task;

	truncate table [reporting].[Global_FieldAudit];
	delete [reporting].[Global_Audit];
	DBCC CHECKIDENT ('reporting.Global_Audit', RESEED, 1);

	truncate table [workflow].[EventRegistration];
	DBCC CHECKIDENT ('workflow.EventRegistration', RESEED, 1);

	truncate table [workflow].[ItemAssignment];
	DBCC CHECKIDENT ('workflow.ItemAssignment', RESEED, 1);
	truncate table [workflow].[ItemStepTransition];
	delete [workflow].[ItemStep];
	DBCC CHECKIDENT ('workflow.ItemStep', RESEED, 1);

	delete [workflow].[VersionStepTransition];
	DBCC CHECKIDENT ('workflow.VersionStepTransition', RESEED, 1);
	delete [workflow].[VersionStep];
	DBCC CHECKIDENT ('workflow.VersionStep', RESEED, 1);
	update [workflow].[Type] set PublishedVersionID = null;
	delete [workflow].[Version];
	DBCC CHECKIDENT ('workflow.Version', RESEED, 1);
	delete [workflow].[Type];
	DBCC CHECKIDENT ('workflow.Type', RESEED, 1);
end
GO;

ALTER PROCEDURE [dbo].[GenerateAssetDisplayValue]	
	@AssetID bigint,
	@Object varchar(20),
	@ObjectID int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @DisplayValue NVARCHAR(MAX);
	DECLARE @DisplayValueHash NVARCHAR(50);

	if @AssetID is null or @AssetID <= 0
	begin
		select @AssetID = id from asset where [object] = @Object and [objectid] = @ObjectID		
	end

	-- if there is no such asset bail
	if @AssetID is null or @AssetID <= 0
	begin
		return;
	end

	Select @displayValue = DisplayValue from GetAssetDisplayValueById(@AssetID);
	SELECT @DisplayValueHash = CONVERT(NVARCHAR(32),HashBytes('SHA1', @displayValue),2)

	Print 'DisplayValue: ' + @DisplayValue
	Print 'DisplayValueHash: ' + @DisplayValueHash
	-- if exists update


	if exists(select 1 from AssetDisplayValue where AssetID = @AssetID)
	begin		
			UPDATE AssetDisplayValue
				SET DisplayValue = A.DisplayValue,
					DisplayValueHash = @DisplayValueHash,
					DisplayValuePrefix = SUBSTRING(A.DisplayValue, 1, 250),
					UpdatedOn = getutcdate()
				FROM GetAssetDisplayValueById(@AssetID) A		
				where AssetID = @AssetID	
	end
	else
	begin
			insert into AssetDisplayValue (AssetID,DisplayValue,DisplayValueHash,DisplayValuePrefix, UpdatedOn) values(@AssetID,@displayValue,@DisplayValueHash,SUBSTRING(@displayValue, 1, 250),getutcdate())
	end	

	Declare @assetObjectType varchar(20);
	Declare @assetObjectID int;

	select @assetObjectType = ATT.[Object], @assetObjectID = ATT.ObjectID from Asset A inner join AssetType ATT on A.AssetTypeID = ATT.ID where A.id = @AssetID

	exec UpdateDependentObjectTypeDisplayValues @assetObjectType,@assetObjectID	
END
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

alter procedure [lineage].[GetByObject]
--declare
	@Object varchar(50) = 'Artifact',
	@ObjectID int = 4680,
	@MaxLevel int = 10
as
begin
	set nocount on;
	declare @level int = 0

	DROP TABLE IF EXISTS #usedIntersectIDs;

	create table #usedIntersectIDs (ID int)

	CREATE CLUSTERED INDEX IX_temp_usedIntersectIDs ON #usedIntersectIDs ([ID] ASC);

	DROP TABLE IF EXISTS #levelResults;

	create table #levelResults (IntersectTypeID int, IntersectID int, Subject varchar(50), SubjectID int, Object varchar(50), ObjectID int, [State] int, PredicateName nvarchar(250))


	DROP TABLE IF EXISTS #lineage;

	create table #lineage (
							IntersectTypeID int, IntersectID int, Direction char(1), 
							[SubjectLevel] int, Subject varchar(50), SubjectID int, 
							[ObjectLevel] int, Object varchar(50), ObjectID int, 
							[State] int, PredicateName nvarchar(250)
						  );
	CREATE INDEX IX_temp_lineage_subject ON #lineage (Direction ASC, [SubjectLevel] ASC, [Subject] ASC, [SubjectID] ASC);
	CREATE INDEX IX_temp_lineage_object ON #lineage (Direction ASC, [ObjectLevel] ASC, [Object] ASC, [ObjectID] ASC);
	CREATE INDEX IX_temp_lineage_subjectLevel ON #lineage ([SubjectLevel] ASC);
	CREATE INDEX IX_temp_lineage_objectLevel ON #lineage ([ObjectLevel] ASC);

	if @MaxLevel > 0
	begin
		-- Load level 1 forward AND backward items.
		insert into #lineage
			select	I.IntersectTypeID,
					I.IntersectID,
					'P' as Direction,
					1 as SubjectLevel,	I.Subject,	I.SubjectID,
					2 as ObjectLevel,	I.Object,	I.ObjectID,
					I.State,
					I.PredicateName
			from	PredicateIntersect I
			where	I.Subject = @Object and I.SubjectID = @ObjectID and I.PredicateType = 1

			union
			select	I.IntersectTypeID,

					I.IntersectID,
					'N' as Direction,
					2 as SubjectLevel,	I.Subject,	I.SubjectID,
					1 as ObjectLevel,	I.Object,	I.ObjectID,
					I.State,
					I.PredicateName
			from	PredicateIntersect I
			where	I.Object = @Object and I.ObjectID = @ObjectID and I.PredicateType = 1
	end

	set @level = 2

	while exists(select 1 from #lineage where SubjectLevel = @level or ObjectLevel = @level) and @level <= @MaxLevel
	begin

		insert into #lineage
			select	distinct
					O.IntersectTypeID,


					O.IntersectID,
					S.Direction,
					IIF(S.Direction = 'P', @level, @level+1),
					O.Subject,
					O.SubjectID,
					IIF(S.Direction = 'P', @level+1, @level),
					O.Object,
					O.ObjectID,
					O.State,
					O.PredicateName
			from	#lineage S
					inner join PredicateIntersect O on (
												(S.[ObjectLevel] = @level and S.Direction = 'P' and O.Subject = S.Object and O.SubjectID = S.ObjectID)

												or 
												(S.[SubjectLevel] = @level and S.Direction = 'N' and O.Object = S.Subject and O.ObjectID = S.SubjectID)
											 ) and O.PredicateType = 1
			where	O.IntersectID not in (select IntersectID from #lineage)

		set @level = @level + 1
	end

	--select * from #lineage

	DROP TABLE IF EXISTS #nodes;
	create table #nodes (
		[key] varchar(50), assetId bigint, [object] varchar(50), objectId int, 
		[name] nvarchar(250), backColor varchar(25), foreColor varchar(25), 
		objectTypeName nvarchar(250), objectType varchar(50), objectTypeId int, assetTypeId int
	);
	DROP TABLE IF EXISTS #edges;
	create table #edges (
		[from] varchar(50), [to] varchar(50), intersectId int, [state] int, [predicate] nvarchar(250), intersectTypeId int
	);

	insert into #nodes
		select	distinct

				case 
					when I.[SubjectLevel] = 1 and I.Subject = @Object and I.SubjectID = @ObjectID then 'C' + cast(I.[SubjectLevel] as varchar) 

					else Direction + cast(I.[SubjectLevel] as varchar)
				end + '.' + cast(A.ID as varchar) as [key],
				A.ID as assetId,
				I.Subject as [object],
				I.SubjectID as objectId,
				A.DisplayValue as [name],
				A.BackColor as backColor,
				A.ForeColor as foreColor,
				A.TypeName as objectTypeName,
				A.Type as objectType,
				A.TypeID as objectTypeId,
				A.AssetTypeID as assetTypeID
		from	#lineage I
				inner join AssetDetail A on A.Object = I.Subject and A.ObjectID = I.SubjectID
		union
		select	distinct
				case 
					when I.[ObjectLevel] = 1 and I.Object = @Object and I.ObjectID = @ObjectID then 'C' + cast(I.[ObjectLevel] as varchar) 
					else Direction + cast(I.[ObjectLevel] as varchar)
				end + '.' + cast(A.ID as varchar) as [key],
				A.ID as assetId,
				I.Object as [object],
				I.ObjectID as objectId,
				A.DisplayValue as [name],
				A.BackColor as backColor,
				A.ForeColor as foreColor,
				A.TypeName as objectTypeName,
				A.Type as objectType,
				A.TypeID as objectTypeId,
				A.AssetTypeID as assetTypeID
		from	#lineage I
				inner join AssetDetail A on A.Object = I.Object and A.ObjectID = I.ObjectID
		;

	--select * from #nodes

	insert into #edges
		select	case 
					when I.[SubjectLevel] = 1 and I.Subject = @Object and I.SubjectID = @ObjectID then 'C' + cast(I.[SubjectLevel] as varchar) 
					else Direction + cast(I.[SubjectLevel] as varchar)
				end + '.' + cast(SA.ID as varchar) as [from],
				case 
					when I.[ObjectLevel] = 1 and I.Object = @Object and I.ObjectID = @ObjectID then 'C' + cast(I.[ObjectLevel] as varchar) 
					else Direction + cast(I.[ObjectLevel] as varchar)
				end + '.' + cast(OA.ID as varchar) as [to],
				I.IntersectID as intersectId,
				I.[state],
				I.PredicateName as [predicate],
				I.IntersectTypeID as intersectTypeId
		from	#lineage I
				inner join Asset SA on SA.Object = I.Subject and SA.ObjectID = I.SubjectID
				inner join Asset OA on OA.Object = I.Object and OA.ObjectID = I.ObjectID

	if not exists(select 1 from #nodes)
	begin
		insert into #nodes

			select	'C1.' + cast(ID as varchar) as [key],
					ID as assetId,
					[object],
					objectId,
					DisplayValue as [name],
					BackColor as backColor,
					ForeColor as foreColor,

					TypeName as objectTypeName,

					Type as objectType,
					TypeID as objectTypeId,
					AssetTypeID as assetTypeID
			from	AssetDetail
			where	Object = @Object 
					and ObjectID = @ObjectID


	end










	-- Return the full results to the caller.
	select	(
			select	* 
			from	#nodes
					for json path
			) as nodes,
			(
			select	* 
			from	#edges
					for json path
			) as links
	for json path, WITHOUT_ARRAY_WRAPPER



end
GO;

ALTER PROCEDURE [dbo].[GetCommentDetailByID]
	@id int
AS
BEGIN
	with i (ResourceID) 
	as
	(
		select	r.ResourceID
		from	ResponsibilityDetail r
				inner join Comment c on c.OwnerObjectType = r.Object and c.OwnerObjectID = r.ObjectID and c.ID = @id
	),
	P (ID, ParentID)
	AS
	(
		SELECT		C.ID,
					C.ParentID
		FROM		Comment C
		WHERE		ID = @id
	)

	SELECT		C.*,
				C.CreatingResourceID,
				O.DisplayValue as ObjectName,				
				AUrl.Url as ObjectUrl,
				case
					WHEN C.ParentID IS NULL THEN C.OwnerObjectType
					ELSE 'Resource'
				end as ObjectType,
				O.DisplayValue as ResourceName,
				case 
					WHEN C.ParentID IS NULL THEN C.OwnerObjectID
					ELSE C.CreatingResourceID
				end as ObjectID,
				(
				select	CRD.Object,
						CRD.ObjectID,
						CRD.TextPath,
						CRD.ObjectTypeName,
						CRD.Url,
						CRD.ForeColor as IconForeColor,
						CRD.BackColor as IconBackColor,
						CRD.NgUrl
				from	CommentRelation CR
				inner join (
					select Object, ObjectID, ForeColor, BackColor, TypeName as ObjectTypeName, AUrl.Url as Url, AUrl.Url as NgUrl, DisplayValue as TextPath from AssetDetail A
					cross apply [dbo].[GetAssetUrl](A.[Object], A.TypeID, A.ObjectID) AUrl
					union all
					select T.Object, T.ObjectID, OS.IconForeColor as ForeColor, OS.IconBackColor as BackColor, null as ObjectTypeName, TUrl.Url as Url, TUrl.Url as NgUrl, Name as TextPath from AssetType T
					cross apply [dbo].[GetAssetUrl](T.[Object], T.ObjectID, T.ObjectID) TUrl
					left join ObjectStyle OS on OS.ObjectType = T.Object and OS.ObjectID = T.ObjectID
				) CRD on CR.CommentID = C.ID 
					and CR.ObjectType = CRD.[Object] 
					and CR.ObjectID = CRD.ObjectID
				where Object != 'Resource'
					and TextPath != 'FirstNameLastName'
				for xml path('tag'), root('tags'), type
				) as TagsXml,
				(
				select CommentID,
						ResourceID,
						vote as VoteValue
				from commentvote
				where commentid = p.ID
					for xml path('vote'), root('votes'), type
			) as VotesXML,
			CASE WHEN (select count(*) from i where ResourceID = C.CreatingResourceID) > 0  THEN
				cast(1 as bit)
			WHEN (select count(*) from i where ResourceID = C.CreatingResourceID) > 0  THEN
				cast(1 as bit)
			ELSE
				cast(0 as bit)
			END as CreatorIsOwner
	FROM		Comment C
				left join AssetDetail O on O.[Object] = C.OwnerObjectType and O.ObjectID = C.OwnerObjectID
				outer apply [dbo].[GetAssetUrl](O.[Object], O.TypeID, O.ObjectID) AUrl
				INNER JOIN P ON C.ID = P.ID
	ORDER BY	C.ParentID, C.DateCreated DESC
END
GO;

ALTER PROCEDURE [dbo].[GetCommentDetailsByFollower]
--declare
	@resourceID int,
	@skip int,
	@take int,
	@dateStart datetime = null,
	@dateEnd datetime = null,
	@commentTypeID int = 0,
	@searchPhrase varchar(100) = ''
--set @resourceID = 1
--set @skip = 0
--set @take = 200
AS
BEGIN
	set nocount on;

	with p as
	(
	select	c.*,
			case 
				when c.CreatingResourceID = @resourceID then 1
				when c.VisibilityID = 2 then 1
				when c.VisibilityID = 3 then 1
				when coalesce(c.VisibilityID, 4) = 4  then 1
				else 0
			end as IsVisible
	from	Comment c
	where	c.ID in	(
					select	CommentID as ID
					from	FollowDetail f
							inner join CommentRelation cr on cr.ObjectID = f.ObjectID and cr.ObjectType = f.ObjectType
					where	f.ResourceID = @resourceId
					union all
					select	ID 
					from	Comment 
					where	CreatingResourceID = @resourceid
					union all
					select	ID 
					from	comment c2
							inner join	ResponsibilityDetail o on o.ResourceID = @resourceID and o.Object = c2.OwnerObjectType and o.ObjectID = c2.OwnerObjectID
					)
			AND C.isdeleted = 0
			AND (
					coalesce(@commentTypeID,0) = 0 OR (C.CommentTypeID = @commentTypeID)
				) 
			AND (
					(C.DateCreated between @dateStart and @dateEnd and @dateStart is not null and @dateEnd is not null) or
					(@dateStart is null and @dateEnd is null)
				)
			AND C.ParentID is null
			AND (
				coalesce(ltrim(rtrim(@searchPhrase)),'')='' or 
				lower(Body) like lower('%'+@searchPhrase+'%')
				)
	order by c.datecreated DESC
	OFFSET		@skip ROWS 
	FETCH NEXT	@take ROWS ONLY
	)

	select	a.*,
			a.OwnerObjectType as ObjectType,
			a.OwnerObjectId as ObjectId,
			R.FirstName + ' ' + R.LastName as ResourceName,
			R.Email as ResourceEmail,
			D.DisplayValue as ObjectName,
			AUrl.Url as ObjectUrl,
			(
			select	CRD.Object,
					CRD.ObjectID,
					CRD.TextPath,
					CRD.ObjectTypeName,
					CRD.Url,
					CRD.BackColor as IconBackColor,
					CRD.ForeColor as IconForeColor,
					CRD.NgUrl
			from	CommentRelation CR
				inner join (
					select Object, ObjectID, ForeColor, BackColor, TypeName as ObjectTypeName, AUrl.Url as Url, AUrl.Url as NgUrl, DisplayValue as TextPath from AssetDetail A
					cross apply [dbo].[GetAssetUrl](A.[Object], A.TypeID, A.ObjectID) AUrl
					union all
					select T.Object, T.ObjectID, OS.IconForeColor as ForeColor, OS.IconBackColor as BackColor, null as ObjectTypeName, TUrl.Url as Url, TUrl.Url as NgUrl, Name as TextPath from AssetType T
					cross apply [dbo].[GetAssetUrl](T.[Object], T.ObjectID, T.ObjectID) TUrl
					left join ObjectStyle OS on OS.ObjectType = T.Object and OS.ObjectID = T.ObjectID
				) CRD on CR.CommentID = a.ID and a.ParentID is null and CR.ObjectType = CRD.[Object] and CR.ObjectID = CRD.ObjectID
			for xml path('tag'), root('tags'), type
			) as TagsXml,
			(
			select	CommentID,
					ResourceID,
					vote as VoteValue
			from	commentvote
			where	commentid = a.ID
			for		xml path('vote'), root('votes'), type
			) as VotesXML,
			0 as CreatorIsOwner
	from	(
			select	* 
			from	p
			union all
			select	r.*,
					1 as IsVisible 
			from	Comment r
					inner join p on r.ParentID = p.ID
			) a
			left join reporting.Global_Resource R on R.ResourceID = a.CreatingResourceID
			left join AssetDetail D on D.[Object] = a.OwnerObjectType and D.ObjectID = a.OwnerObjectID
			outer apply [dbo].[GetAssetUrl](D.[Object], D.TypeID, D.ObjectID) AUrl
	where	IsVisible = 1;
END
GO;

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
	declare @maxDepth int = 15;
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

		IF @view in (0,2)
		BEGIN

			IF OBJECT_ID('tempdb..#items') IS NOT NULL DROP TABLE #items;
			create table #items (
				ID int,
				SourceIntersectID int, 
				SourceSubjectTypeName nvarchar(500), SourceSubjectName nvarchar(500), SourceSubjectShortName nvarchar(500), SourceSubject varchar(50), SourceSubjectID int, SourceSubjectIconBackColor varchar(7), SourceSubjectIconForeColor varchar(7), 
				SourceObjectTypeName nvarchar(500), SourceObjectName nvarchar(500), SourceObjectShortName nvarchar(500), SourceObject varchar(50), SourceObjectID int, SourceObjectIconBackColor varchar(7), SourceObjectIconForeColor varchar(7),

				TargetIntersectID int, 
				TargetSubjectTypeName nvarchar(500), TargetSubjectName nvarchar(500), TargetSubjectShortName nvarchar(500), TargetSubject varchar(50), TargetSubjectID int, TargetSubjectIconBackColor varchar(7), TargetSubjectIconForeColor varchar(7), 
				TargetObjectTypeName nvarchar(500), TargetObjectName nvarchar(500), TargetObjectShortName nvarchar(500), TargetObject varchar(50), TargetObjectID int, TargetObjectIconBackColor varchar(7), TargetObjectIconForeColor varchar(7),

				SourceHasSourceRules bit, TargetHasSourceRules bit
			);

			CREATE CLUSTERED INDEX IX_TempItems ON #items (id, sourceintersectid, targetintersectid); --vastly improves performance

			insert into #items
				select	O.ID,				
						O.SourceIntersectID,
						SS.TypeName as SubjectTypeName,
						SSD.DisplayValue as SubjectName,
						SSD.DisplayValue as SubjectShortName,
						SI.[Subject],
						SI.SubjectID,
						SS.BackColor as SubjectIconBackColor,
						SS.ForeColor as SubjectIconForeColor,
						SO.TypeName as ObjectTypeName,
						SOD.DisplayValue as ObjectName,
						SOD.DisplayValue as ObjectShortName,
						SI.[Object],
						SI.ObjectID,
						SO.BackColor as ObjectIconBackColor,
						SO.ForeColor as ObjectIconForeColor,
						O.TargetIntersectID,
						TS.TypeName as SubjectTypeName,
						TSD.DisplayValue as SubjectName,
						TSD.DisplayValue as SubjectShortName,
						TI.Subject,
						TI.SubjectID,
						TS.BackColor,
						TS.ForeColor,
						TB.TypeName as ObjectTypeName,
						TBD.DisplayValue as ObjectName,
						TBD.DisplayValue as ObjectShortName,
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
					inner join [Intersect] SI on SI.ID = O.SourceIntersectID
					inner join [Intersect] TI on TI.ID = O.TargetIntersectID
					inner join AssetWithType SS on SS.[Object] = SI.[Subject] and SS.ObjectID = SI.SubjectID 
					inner join AssetWithType SO on SO.[Object] = SI.[Object] and SO.ObjectID = SI.ObjectID
					inner join AssetWithType TS on TS.[Object] = TI.[Subject] and TS.ObjectID = TI.SubjectID
					inner join AssetWithType TB on TB.[Object] = TI.[Object] and TB.ObjectID = TI.ObjectID
					cross apply dbo.GetAssetDisplayValueById(SS.ID) SSD
					cross apply dbo.GetAssetDisplayValueById(SO.ID) SOD
					cross apply dbo.GetAssetDisplayValueById(TS.ID) TSD
					cross apply dbo.GetAssetDisplayValueById(TB.ID) TBD
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
				from #items I
				inner join @rows R on
					R.SourceSubjectID = I.SourceSubjectID 
					AND R.SourceObjectID = I.SourceObjectID
					AND R.TargetSubjectID = I.TargetSubjectID
					AND R.TargetObjectID = I.TargetObjectID;

				--insert adding items and fill in missing data
				insert into #items
				select
					R.ID,
					R.SourceIntersectID,
					SS.TypeName as SourceSubjectTypeName,
					SSD.TextPath as SourceSubjectName,
					SS.DisplayValue as SourceSubjectShortName,
					R.SourceSubject,
					R.SourceSubjectID,
					SS.BackColor as SourceSubjectIconBackColor,
					SS.ForeColor as SourceSubjectIconForeColor,
					SO.TypeName as SourceObjectTypeName,
					SOD.TextPath as SourceObjectName,
					SO.DisplayValue as SourceObjectShortName,
					R.SourceObject,
					R.SourceObjectID,
					SO.BackColor as SourceObjectIconBackColor,
					SO.ForeColor as SourceObjectIconForeColor,
					R.TargetIntersectID,
					TS.TypeName as TargetSubjectTypeName,
					TSD.TextPath as TargetSubjectName,
					TS.DisplayValue as TargetSubjectShortName,
					R.TargetSubject,
					R.TargetSubjectID,
					TS.BackColor as TargetSubjectIconBackColor,
					TS.ForeColor as TargetSubjectIconForeColor,
					TB.TypeName as TargetObjectTypeName,
					TBD.TextPath  as TargetObjectName,
					TB.DisplayValue as TargetObjectShortName,
					R.TargetObject,
					R.TargetObjectID,
					TB.BackColor as TargetObjectIconBackColor,
					TB.ForeColor as TargetObjectIconForeColor,
					0 as SourceHasSourceRules,
					0 as TargetHasSourceRules
				from @rows R 
				inner join AssetDetail SS on SS.[Object] = R.SourceSubject AND SS.ObjectID = R.SourceSubjectID
				inner join AssetDetail SO on SO.[Object] = R.SourceObject AND SO.ObjectID = R.SourceObjectID
				inner join AssetDetail TS on TS.[Object] = R.TargetSubject AND TS.ObjectID = R.TargetSubjectID
				inner join AssetDetail TB on TB.[Object] = R.TargetObject AND TB.ObjectID = R.TargetObjectID
				cross apply dbo.GetAssetTextPathById(SS.ID, '.') SSD
				cross apply dbo.GetAssetTextPathById(SO.ID, '.') SOD
				cross apply dbo.GetAssetTextPathById(TS.ID, '.') TSD
				cross apply dbo.GetAssetTextPathById(TB.ID, '.') TBD

				where R.Adding = 1
				and not exists (select 1 from #items i where i.SourceIntersectID = r.SourceIntersectID and i.TargetIntersectID = r.TargetIntersectID);
			end

		end -- end view 0,2

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
				from #items I
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

		IF OBJECT_ID('tempdb..#systemItems') IS NOT NULL DROP TABLE #systemItems;
		create table #systemItems (
			ID int,
			SourceSubjectTypeName nvarchar(500), SourceSubjectName nvarchar(500), SourceSubjectShortName nvarchar(500), SourceSubject varchar(50), SourceSubjectID int, SourceSubjectIconBackColor varchar(7), SourceSubjectIconForeColor varchar(7), 
			TargetSubjectTypeName nvarchar(500), TargetSubjectName nvarchar(500), TargetSubjectShortName nvarchar(500), TargetSubject varchar(50), TargetSubjectID int, TargetSubjectIconBackColor varchar(7), TargetSubjectIconForeColor varchar(7), 
			SourceHasSourceRules bit, TargetHasSourceRules bit
		);

		CREATE CLUSTERED INDEX IX_TempSystemItems ON #systemItems (id, sourcesubject, sourcesubjectid, targetsubject, targetsubjectid); --vastly improves performance

		insert into #systemItems (ID, SourceSubjectTypeName, SourceSubjectName, SourceSubjectShortName, SourceSubject, SourceSubjectID, SourceSubjectIconBackColor,SourceSubjectIconForeColor,
		TargetSubjectTypeName, TargetSubjectName, TargetSubjectShortName,  TargetSubject, TargetSubjectID, TargetSubjectIconBackColor, TargetSubjectIconForeColor, 
		SourceHasSourceRules, TargetHasSourceRules)
			select	
					O.ID as ID,				
					SS.TypeName as SourceSubjectTypeName,
					SSD.DisplayValue as SourceSubjectName,
					SSD.DisplayValue as SourceSubjectShortName,
					SI.[Subject] as SourceSubject,
					SI.SubjectID as SourceSubjectID,
					SS.BackColor as SourceSubjectIconBackColor,
					SS.ForeColor as SourceSubjectIconForeColor,
					TS.TypeName as TargetSubjectTypeName,
					TSD.DisplayValue as TargetSubjectName,
					TSD.DisplayValue as TargetSubjectShortName,
					TI.[Subject] as TargetSubject,
					TI.SubjectID as TargetSubjectID,
					TS.BackColor as TargetSubjectIconBackColor,
					TS.ForeColor as TargetSubjectIconForeColor,
					case 
						when SHSR.C > 0 then cast(1 as bit)
						else cast(0 as bit)
					end as SourceHasSourceRules,
										case 
						when THSR.C > 0 then cast(1 as bit)
						else cast(0 as bit)
					end as TargetHasSourceRules
			from	#points O
				inner join [Intersect] SI on SI.ID = O.SourceIntersectID
				inner join [Intersect] TI on TI.ID = O.TargetIntersectID
				inner join AssetWithType SS on SS.[Object] = SI.[Subject] and SS.ObjectID = SI.SubjectID 
				inner join AssetWithType TS on TS.[Object] = TI.[Subject] and TS.ObjectID = TI.SubjectID
				cross apply dbo.GetAssetDisplayValueById(SS.ID) SSD
				cross apply dbo.GetAssetDisplayValueById(TS.ID) TSD
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

			insert into @links
					select	distinct
							S.SourceSubject + '.' + cast(S.SourceSubjectID as varchar) as [from],
							S.TargetSubject + '.' + cast(S.TargetSubjectID as varchar) as [to],
							'' as category
					from	#systemItems S
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
					from	#systemItems I
					left join Asset A on A.[Object] = I.SourceSubject and A.ObjectID = I.SourceSubjectID;;

					--perform this update separately to avoid duplicate in the above query
					update n
					set n.HasSourceRules = 1
					from @nodes n
					inner join #systemItems i on n.[key] = (i.SourceSubject + '.' + cast(i.SourceSubjectID as varchar)) and i.SourceHasSourceRules = 1;

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
					from	#systemItems I
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
				from	#items
				union
				select	distinct
						cast(SourceIntersectID as varchar) + '.S' as 'from',
						TargetSubject + '.' + cast(TargetSubjectID as varchar) as 'to',
						'' as category
				from	#items O
				where	(SourceObject + cast(SourceObjectID as varchar)) = (TargetObject + cast(TargetObjectID as varchar))
				union
				select	distinct
						cast(SourceIntersectID as varchar) + '.S' as 'from',
						cast(TargetIntersectID as varchar) + '.T' as 'to',
						'' as category
				from	#items O
				where	(SourceObject + cast(SourceObjectID as varchar)) <> (TargetObject + cast(TargetObjectID as varchar))
				--where	TargetIntersectID in (select SourceIntersectID from #items)
				union
				select	distinct
						cast(TargetIntersectID as varchar) + '.T' as 'from',
						TargetSubject + '.' + cast(TargetSubjectID as varchar) as 'to',
						'Support' as category
				from	#items
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
				from	#items 
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
				from	#items
				left join Asset A on A.[Object] = SourceObject and A.ObjectID = SourceObjectID

				update n
				set n.HasSourceRules = 1
				from @nodes n
				inner join #items i on n.[key] = (i.SourceSubject + '.' + cast(i.SourceSubjectID as varchar)) and i.SourceHasSourceRules = 1;


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
					from	#items
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
					from	#items
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

--select	* from	#items
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
				select
							MI.ID,

							MI.SourceIntersectID,
							SIS.TypeName as SourceSubjectTypeName,
							FSIS.TextPath as SourceSubjectName,
							SIS.DisplayValue as SourceSubjectShortName,
							SIS.Object as SourceSubject,
							SIS.ObjectID as SourceSubjectID,
							SIO.TypeName as SourceObjectTypeName,
							FSIO.TextPath as SourceObjectName,
							SIO.DisplayValue as SourceObjectShortName,
							SIO.Object as SourceObject,
							SIO.ObjectID as SourceObjectID,

							MI.TargetIntersectID,
							TIS.TypeName as TargetSubjectTypeName,
							FTIS.TextPath as TargetSubjectName,
							TIS.DisplayValue as TargetSubjectShortName,
							TIS.Object as TargetSubject,
							TIS.ObjectID as TargetSubjectID,
							TIO.TypeName as TargetObjectTypeName,
							FTIO.TextPath as TargetObjectName,
							TIO.DisplayValue as TargetObjectShortName,
							TIO.Object as TargetObject,
							TIO.ObjectID as TargetObjectID
					from	#tFusionPoints F
					inner join MapRuleItemMapItem J on J.MapRuleItemID = F.ID
					inner join MapItem MI on MI.ID = J.MapItemID
					inner join [Intersect] SI on SI.ID = MI.SourceIntersectID
					inner join [Intersect] TI on TI.ID = MI.TargetIntersectID
					inner join AssetDetail SIS on SIS.Object = SI.Subject and SIS.ObjectID = SI.SubjectID
					inner join AssetDetail SIO on SIO.Object = SI.Object and SIO.ObjectID = SI.ObjectID
					inner join AssetDetail TIS on TIS.Object = TI.Subject and TIS.ObjectID = TI.SubjectID
					inner join AssetDetail TIO on TIO.Object = TI.Object and TIO.ObjectID = TI.ObjectID
					inner join FusionAttribute FSIS on SIS.Object = 'FusionAttribute' and FSIS.ID = SIS.ObjectID
					inner join FusionAttribute FSIO on SIO.Object = 'FusionAttribute' and FSIO.ID = SIO.ObjectID
					inner join FusionAttribute FTIS on TIS.Object = 'FusionAttribute' and FTIS.ID = TIS.ObjectID
					inner join FusionAttribute FTIO on TIO.Object = 'FusionAttribute' and FTIO.ID = TIO.ObjectID;

				-- get all items not directly tied to the focal object, but still tied to maps involved above.
				insert into @tItems
					select	
							MI.ID,

							MI.SourceIntersectID,
							SIS.TypeName as SourceSubjectTypeName,
							FSIS.TextPath as SourceSubjectName,
							SIS.DisplayValue as SourceSubjectShortName,
							SIS.Object as SourceSubject,
							SIS.ObjectID as SourceSubjectID,
							SIO.TypeName as SourceObjectTypeName,
							FSIO.TextPath as SourceObjectName,
							SIO.DisplayValue as SourceObjectShortName,
							SIO.Object as SourceObject,
							SIO.ObjectID as SourceObjectID,

							MI.TargetIntersectID,
							TIS.TypeName as TargetSubjectTypeName,
							FTIS.TextPath as TargetSubjectName,
							TIS.DisplayValue as TargetSubjectShortName,
							TIS.Object as TargetSubject,
							TIS.ObjectID as TargetSubjectID,
							TIO.TypeName as TargetObjectTypeName,
							FTIO.TextPath as TargetObjectName,
							TIO.DisplayValue as TargetObjectShortName,
							TIO.Object as TargetObject,
							TIO.ObjectID as TargetObjectID
					from	MapItem MI
							inner join	(
										select	ID.MapItemID
										from	MapItemMap DM
												inner join @tItems D on D.MapItemID = DM.MapItemID
												inner join MapItemMap ID on ID.MapID = DM.MapID and ID.MapItemID not in (
																														select MapItemID from @tItems
																														)
										) O on O.MapItemID = MI.ID
					inner join [Intersect] SI on SI.ID = MI.SourceIntersectID
					inner join [Intersect] TI on TI.ID = MI.TargetIntersectID
					inner join AssetDetail SIS on SIS.Object = SI.Subject and SIS.ObjectID = SI.SubjectID
					inner join AssetDetail SIO on SIO.Object = SI.Object and SIO.ObjectID = SI.ObjectID
					inner join AssetDetail TIS on TIS.Object = TI.Subject and TIS.ObjectID = TI.SubjectID
					inner join AssetDetail TIO on TIO.Object = TI.Object and TIO.ObjectID = TI.ObjectID
					inner join FusionAttribute FSIS on SIS.Object = 'FusionAttribute' and FSIS.ID = SIS.ObjectID
					inner join FusionAttribute FSIO on SIO.Object = 'FusionAttribute' and FSIO.ID = SIO.ObjectID
					inner join FusionAttribute FTIS on TIS.Object = 'FusionAttribute' and FTIS.ID = TIS.ObjectID
					inner join FusionAttribute FTIO on TIO.Object = 'FusionAttribute' and FTIO.ID = TIO.ObjectID;

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
GO;

alter procedure [tile].[GetObjectStatistics]
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

alter procedure [utility].[GetOwnersForWorkflow]
	@workflowID int,
	@workflowStepID int = 0,
	@workflowItemID int = 0
as
begin
	declare @objectId int,			
			@objectType varchar(50),
			@assetId bigint,
			@assetTypeId bigint,
			@responsibilityTypeID int,
			@issueId int;
	declare @xmlSettings xml;
	declare @responsibleSide varchar(50);

	declare @tbl table (ResourceID int, FirstName nvarchar(250), LastName nvarchar(250), Email nvarchar(500), Username nvarchar(500), DateLastLoggedIn datetime null, ResourceTypeID int, Status nvarchar(25))
	declare @responsibilityIDTbl table (RowID int not null identity(1,1) primary key, ResponsibilityTypeID int not null);
	--get the responsibility for this step from the settings of the step

	select @xmlSettings = settings from [workflow].[VersionStep] where id = @workflowStepID

	insert into @responsibilityIDTbl select T.C.value('.','int') as responsibility from @xmlSettings.nodes('(/settings/ResponsibilityTypeID)') as T(C) ;

	select @responsibleSide = upper(T.C.value('.','varchar(50)')) from @xmlSettings.nodes('(/settings/ResponsibilitySide)') as T(C);

	declare @i int
	select @i = min(RowID) from @responsibilityIDTbl
	declare @max int
	select @max = max(RowID) from @responsibilityIDTbl

	while @i <= @max and not exists (select 1 from @tbl) begin
		select @responsibilityTypeID = ResponsibilityTypeID from @responsibilityIDTbl where RowID = @i
		set @i = @i + 1

		-- check object	
		begin
			select 
				@objectType = i.object, 
				@objectId = i.objectid,
				@assetId = a.id,
				@assetTypeId = a.assetTypeId 
			from [workflow].[item] i
			left join Asset a on a.object = i.object and A.objectid = i.objectid 
			where i.id = @workflowItemID;

			if @objectType = 'Issue'
			begin				
				select 
					@issueId = i.id, 
					@objectType = i.[object], 
					@objectId = i.[objectid],
					@assetId = a.id,
					@assetTypeId = a.assetTypeID
				from Issue i
				left join Asset a on a.Object = i.Object and a.ObjectID = i.ObjectID
				where i.id = @objectId
			end

			--if the object is an intersect we need to look at the settings to see what side of the intersect to look at
			-- then we need to load the object from the corresponding side.

			if @objectType = 'Intersect'
			begin				
				if @responsibleSide = 'SUBJECT'
				begin
					select @objectType = [subject], @objectId = [subjectId] from [intersect] where id = @objectId;
				end
				else if @responsibleSide = 'OBJECT'
				begin
					select @objectType = [object], @objectId = [objectId] from [intersect] where id = @objectId;
				end
			end

			insert into @tbl
				select	R.ResourceID, 
						R.FirstName, 
						R.LastName, 
						R.Email, 
						R.Email, 
						R.DateLastLoggedIn, 
						1 as ResourceTypeID, 
						R.Status 
				from	ResponsibilityDetail RD
						inner join reporting.Global_Resource R on 
								((RD.Object = @objectType and RD.ObjectID = @objectId) 
									or (@assetTypeId != 0 and RD.AssetID = 0 and RD.AssetTypeID = @assetTypeId))
								and RD.ResponsibilityTypeID = @responsibilityTypeID
								and RD.ResourceID = R.ResourceID
								and R.Email not like '%?subject=%' and R.Status = 'Active'
		end		
	end;

	select * from @tbl;
end
GO;

alter proc [dbo].[GetPageInformation]
--declare 
	@o varchar(50),-- = 'Artifact',
	@oid int,-- = 23450,
	@rid int --= 1
as
begin
	declare @breadcrumbsRaw table ([Level] int, [TypeName] nvarchar(500), [Name] nvarchar(max), [TypeUrl] nvarchar(2500), [Url] nvarchar(2500));
	declare @breadcrumbs table ([Name] nvarchar(max), [Url] nvarchar(2500), Active bit, IsType bit);

	with h as
		(
		select	A.ID,
				A.[ObjectID], 
				A.AssetTypeID,
				I.SubjectID as [ParentID], 
				0 as [Level]
		from	Asset A
				left join PredicateIntersect I on I.Object = A.Object and I.ObjectID = A.ObjectID and I.PredicateType = 3
		where	A.[Object] = @o and A.ObjectID = @oid
		union all
		select	P.ID,
				P.[ObjectID] as ID, 
				P.AssetTypeID,
				I.SubjectID as ParentID, 
				h.[Level]-1 as [Level]
		from	Asset P
				inner join h on P.[Object] = @o and P.ObjectID = h.ParentID
				outer apply (
							select	SubjectID
							from	PredicateIntersect 
							where	Object = P.Object 
									and ObjectID = P.ObjectID 
									and PredicateType = 3
							) I
		)

	insert into @breadcrumbsRaw
		select		distinct	
					[Level],
					ltrim(rtrim(T.Name)),
					ltrim(rtrim(D.DisplayValue)),
					UT.Url,
					U.Url
		from		h 
					inner join AssetType T on T.ID = h.AssetTypeID
					left join dbo.GetAssetDisplayValue() D on D.ID = h.ID
					cross apply dbo.GetAssetUrl(@o, T.ObjectID, h.ObjectID) U
					cross apply dbo.GetAssetUrl(T.Object, T.ObjectID, T.ObjectID) UT
		where		ltrim(rtrim(T.Name)) is not null
					and ltrim(rtrim(D.DisplayValue)) is not null
		order by	[Level]

	declare @max int = 0,
			@min int
	select	@min = min([Level]) from @breadcrumbsRaw

	insert into @breadcrumbs values ('Glossary', null, 0, 0)

	while @min <= @max
	begin
		insert into @breadcrumbs
			select	TypeName, TypeUrl, 0, 1 from @breadcrumbsRaw where [Level] = @min

		insert into @breadcrumbs
			select	Name, 
					Url, 
					case @min when 0 then 1 else 0 end, 
					0 
			from	@breadcrumbsRaw 
			where	[Level] = @min

		set @min = @min + 1
	end

	select	distinct
			O.[Uid],
			A.ID,
			O.ID as AssetID,
			O.AssetTypeID,
			OD.DisplayValue,
			T.Name as [TypeName],
			case 
				when Dash.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as HasDashboards,
			case 
				when Work.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as HasWorkflow,
			case 
				when Child.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as HasChildArtifacts,
			case 
				when Attr.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as AllowAttributes,
			case 
				when Hier.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as AllowPredicateHierarchies,
			(
			select	*
			from	(
					select	P.ID as [ID],
							P.Name as [Name]
					from	[Predicate] P
					where	exists(SELECT * FROM IntersectType IT WHERE P.[type] = 6 and P.ID = IT.PredicateID and ((IT.Subject = T.Object and IT.SubjectID = T.ObjectID) OR (IT.Object = T.Object and IT.ObjectID =T.ObjectID)))
					union	
					select	P.ID as [ID], 
							P.Name as [Name] 
					from	[NymRelation] R 
							inner join [dbo].[predicate] P on P.ID = R.PredicateID where R.[Object] = T.Object and R.ObjectID = T.ObjectID
					) NMT
			for		json path
			)
			as NymTypes,
			(
			select	* 
			from	@breadcrumbs
			for		json path
			) as Breadcrumbs
	from	Artifact A 
			inner join Asset O on O.Object = @o and O.ObjectID = A.ID 
			inner join AssetType T on T.ID = O.AssetTypeID
			left join dbo.GetAssetDisplayValue() OD on OD.ID = O.ID
			--cross apply [dbo].GetAssetDisplayValueById(O.ID) as OD
			cross apply (
						select	count(1) as [Count]
						from	Report
						where	ObjectType = O.Object
								and ObjectID = T.ObjectID
						) Dash
			cross apply (
						select	count(1) as [Count]
						from	workflow.EventRegistration WER
								inner join workflow.Type WT on WER.TypeID = WT.ID and WT.PublishedVersionID is not null and WT.[State] = 1 and WER.ChangeType = 8 --ACTIVE
						where	WER.Object = T.Object
								and WER.ObjectID = T.ObjectID
						) Work
			cross apply (
						select	count(1) as [Count]
						from	[PredicateIntersect]
						where	Subject = O.Object
								and SubjectID = O.ObjectID
								and PredicateType = 3
						) Child
			cross apply (
						select	count(1) as [Count]
						from	AttributeTypeRelation
						where	ObjectType = T.Object and ObjectID = T.ObjectID
						) Attr
			cross apply (
						select	count(1) as [Count]
						from	IntersectType IT
								inner join [Predicate] P on P.ID = IT.PredicateID and P.[Type] = 3 -- TypeOf
						where	((IT.Subject = T.Object and IT.SubjectID = T.ObjectID) OR (IT.Object = T.Object and IT.ObjectID = T.ObjectID))
						) Hier
	where   A.ID = @oid 
			and A.[Visible] = 1 
			and not exists (select 1 from ResponsibilityDetail where PermissionsBitMask & 1 = 0 and ResourceID = @rid and ( (AssetID = O.ID) OR (AssetTypeID = O.AssetTypeID and AssetID = 0)))
	for json path, WITHOUT_ARRAY_WRAPPER
end
GO;

ALTER PROCEDURE [dbo].[GetRenderedTemplateBodyNg]
--declare
	@TemplateType varchar(25),
	@Type varchar(50),
	@ID int,
	@Action varchar(50),
	@SubjectName VARCHAR (200) = 'Governing Domain',
	@resourceId int = -1
--set @TemplateType = 'Lookup'
--set @Type = 'Artifact'
--set @ID = 7004--16435
--set @Action = 'Preview'--'Certificate'
AS
BEGIN
	SET NOCOUNT ON;

	declare @html nvarchar(max),
			@link nvarchar(2500),
			@icon nvarchar(250),
			@hasDynamicFields bit = 0,
			@hasStats bit = 0,
			@typeID int,

			@showIcon bit = 1,

			@current int,
			@max int,
			@name nvarchar(250),
			@value nvarchar(max);

	declare @tbl table (ID int identity, Name nvarchar(250), Value nvarchar(max));

	if @TemplateType = 'Tooltip'
	begin
		select	@html = TemplateBody
		from	TooltipTemplate
		where	Name = @Type
				and [Action] = @Action
	end

	-- Get the static tokens, depending on the type.
	declare @n nvarchar(250), @t nvarchar(250), @s nvarchar(25), @v int, @dc datetime, @du datetime, @d nvarchar(4000);

	-- Get common fields
	select	@typeID = C_D.TypeID,
			@icon = '<div title=''' + C_D.DisplayValue + ''' class=''tooltip-icon'' style=''background-color: ' + C_D.BackColor + '; color: ' + C_D.ForeColor + '''><i class=''fa fa-' + C_D.Icon + '''></i></div>',
			@n = C_D.DisplayValue,
			@t = C_D.TypeName,
			@d = f.formattedvalue,
			@link = AUrl.Url
	from	AssetDetail C_D	
			cross apply [dbo].[GetAssetUrl](C_D.[Object], C_D.TypeID, C_D.ObjectID) AUrl
			left join fieldtype ft on (ft.[object] = C_D.[type] and ft.objectid = C_D.typeid and ft.name = 'Description')
			left join field f on (f.fieldtypeid = ft.id and f.[objecttype] = C_D.[object] and f.objectid = C_D.objectid)
	where	C_D.[Object] = @Type
			and C_D.ObjectID = @ID;

	--fusion attributes arent in cache
	if @Type = 'FusionAttribute'
	begin		
		select 
			@typeID = fa.fusionattributetypeid,
			@n = fa.name,
			@t = fat.Name,
			@link = dbo.GenerateNgObjectUrl('FusionAttribute', fat.id, fa.id) 
		from fusionattribute fa 
			inner join fusionattributetype fat on (fa.fusionattributetypeid = fat.id) 
		where fa.id = @ID
	end

	if @n is not null
	begin
		if @link is null
		begin
			insert into @tbl values ('Name', @n)
		end
		else
		begin
			insert into @tbl values ('Name', '<a routerLink="/' + @link + '">' + @n + '</a>')
		end
		insert into @tbl values ('Description', @d)
	end
	insert into @tbl values ('Type', @t)

	if @Action = 'AssigningItemPreview'
	begin
		set @html = '<h3>{Name}</h3>'
	end


	if @Action = 'LookupPreview'
	begin
		set @html = '{Items}'

		if @Type = 'FusionAttribute'
		begin
			-- BUILD LIST HTML -----------------------------------------
			declare @fusionAttributeItemsHtml nvarchar(max)

			set @fusionAttributeItemsHtml = '<div style="height: 200px; overflow-y: scroll"><table class="hoverable bordered striped" style="width:100%"><thead>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '<th style="margin-right: 15px">Name</th>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</thead><tbody>'

			select		--top 10 
						@fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '<tr>' 
											+ '<td>' + Name + '</td>'
											+ '</tr>'
			from		FusionAttribute
			where		ParentID = @ID
			order by	Name asc

			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</tbody>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</table></div>'

			insert into @tbl values ('Items', @fusionAttributeItemsHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'LookupType' OR @Type = 'Lookup'
		begin
			-- BUILD LOOKUP LIST HTML -----------------------------------------
			declare @lookups table (RowID int identity, ID int)

			declare @MyLookupTypeID int
			if @Type = 'Lookup'
				begin
					select @MyLookupTypeID = LookupTypeID from [Lookup] where ID = @ID 
				end
			else
				begin
					set @MyLookupTypeID = @ID
				end

			insert into @lookups 
				select top 10 ID from [Lookup] where LookupTypeID = @MyLookupTypeID order by ID desc

			declare @lookupFieldTypes table (ID int identity, Name nvarchar(250))
			insert into @lookupFieldTypes
				select FriendlyName from FieldType where [Object] = 'LookupType' and ObjectID = @MyLookupTypeID order by ColumnOrder asc

			declare @lookupHtml nvarchar(max)

			set @lookupHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @lookupHtml = @lookupHtml + '<thead>'
			set		@current = 1
			select	@max = max(ID) from @lookupFieldTypes
			while @current <= @max
			begin
				select	@name = Name
				from	@lookupFieldTypes
				where	ID = @current

				set @lookupHtml = @lookupHtml + '<th style="margin-right: 15px">' + @name  + '</th>'

				set @current = @current + 1
			end
			set @lookupHtml = @lookupHtml + '</thead>'
			-----------------------------------------

			set @lookupHtml = @lookupHtml + '<tbody>'

			-- Loop through event list --------------
			select	@current = min(RowID) from @lookups
			select	@max = max(RowID) from @lookups

			while @current <= @max
			begin
				set @lookupHtml = @lookupHtml + '<tr>'	-- Open row for selected event.

				declare @lookupFields table (Name nvarchar(250), Value nvarchar(4000))

				declare @lookupID int

				select	@lookupID = ID from @lookups where RowID = @current

				insert into @lookupFields
					select		FriendlyName,
								FormattedValue
					from		FieldWithRelation
					where		ObjectType = 'Lookup' 
								and ObjectID = @lookupID

					-- Loop through each field for this selected event --
					declare @lfCurrent int,
							@lfMax int
					set		@lfCurrent = 1
					select	@lfMax = max(ID) from @lookupFieldTypes
					while @lfCurrent <= @lfMax
					begin
						select	@name = Name from @lookupFieldTypes where ID = @lfCurrent

						select @lookupHtml = @lookupHtml + '<td>' + coalesce(Value, '') + '</td>' from @lookupFields where Name = @name

						set @lfCurrent = @lfCurrent + 1
					end
					-----------------------------------------------------

				delete @lookupFields

				set @lookupHtml = @lookupHtml + '</tr>'	-- Close off row for selected lookup.

				set @current = @current + 1
			end
			-----------------------------------------

			set @lookupHtml = @lookupHtml + '</tbody>'

			set @lookupHtml = @lookupHtml + '</table>'

			insert into @tbl values ('Items', @lookupHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'ReferenceItemType' OR @Type = 'ReferenceItem'
		begin
			-- BUILD LOOKUP LIST HTML -----------------------------------------
			declare @refs table (RowID int identity, ID int)
			declare @isHierarchy bit = 0;

			declare @MyRefTypeID int
			if @Type = 'ReferenceItem'
				begin
					select @MyRefTypeID = ReferenceItemTypeID from ReferenceItem where ID = @ID 

					-- check if this item is in a hierarchy if so set the flag as true
					select  @isHierarchy = count(1) from intersecttypedetail where [object] = 'ReferenceItemType' and [objectid] = @MyRefTypeID and predicatetype = 3
				end
			else
				begin
					set @MyRefTypeID = @ID
				end

			if @isHierarchy = 1 
				begin
				insert into @refs 					
					select	top 500 
							ri.ID 
					from	[ReferenceItem] ri
							inner join Asset ast on (ri.id = ast.objectid and ast.[object] = 'ReferenceItem')
							inner join [intersect] id on (id.objectid = ri.id and id.[object] = 'ReferenceItem')
							inner join [intersect] id_2 on (id_2.[object] = 'ReferenceItem' and id_2.[objectid] = @id and id_2.subjectid = id.subjectid)
							inner join [intersecttypedetail] it on (it.id = id.intersecttypeid and it.id = id_2.intersecttypeid and it.[object]='ReferenceItemType' and it.predicatetype = 3)
					where	ri.ReferenceItemTypeID = @MyRefTypeID 
							and ast.[State] = 1 
							and ast.ID not in (select AssetID from ResponsibilityDetail where ((PermissionsBitMask & 1) = 0) and ResourceID = @resourceId)
					order by DisplayValue asc
				end
			else
				begin
				insert into @refs 
					select	top 500 
							ri.ID 
					from	[ReferenceItem] ri
							inner join Asset ast on (ri.id = ast.objectid and ast.[object] = 'ReferenceItem')
					where	ri.ReferenceItemTypeID = @MyRefTypeID 
							and ast.[State] = 1 
							and ast.ID not in (select AssetID from ResponsibilityDetail where ((PermissionsBitMask & 1) = 0) and ResourceID = @resourceId)
					order by DisplayValue asc
				end

			declare @refFieldTypes table (ID int identity, Name nvarchar(250))
			insert into @refFieldTypes values ('Code')
			insert into @refFieldTypes
				select FriendlyName from FieldType where [Object] = 'ReferenceItemType' and ObjectID = @MyRefTypeID order by ColumnOrder asc

			declare @refHtml nvarchar(max)

			set @refHtml = '<table class="hoverable bordered striped" style="width:100%; min-width: 400px">'

			-- Loop through field name list ---------
			set @refHtml = @refHtml + '<thead>'
			set		@current = 1
			select	@max = max(ID) from @refFieldTypes
			while @current <= @max
			begin
				select	@name = Name
				from	@refFieldTypes
				where	ID = @current

				set @refHtml = @refHtml + '<th style="margin-right: 15px">' + @name  + '</th>'

				set @current = @current + 1
			end
			set @refHtml = @refHtml + '</thead>'
			-----------------------------------------

			set @refHtml = @refHtml + '<tbody>'

			-- Loop through event list --------------
			select	@current = min(RowID) from @refs
			select	@max = max(RowID) from @refs

			while @current <= @max
			begin
				set @refHtml = @refHtml + '<tr>'	-- Open row for selected event.

				declare @refFields table (Name nvarchar(250), Value nvarchar(4000))

				declare @refID int

				select	@refID = ID from @refs where RowID = @current

				insert into @refFields
					select	'Code', Code from ReferenceItem where ID = @refID

				insert into @refFields
					select		FriendlyName,
								FormattedValue
					from		FieldWithRelation
					where		ObjectType = 'ReferenceItem' 
								and ObjectID = @refID

					-- Loop through each field for this selected event --
					declare @rfCurrent int,
							@rfMax int,
							@rfCurrentVal nvarchar(max);

					set		@rfCurrent = 1
					select	@rfMax = max(ID) from @refFieldTypes
					while @rfCurrent <= @rfMax
					begin
						select	@name = Name from @refFieldTypes where ID = @rfCurrent

						if exists (select 1 from @refFields where Name = @name)
						begin
							select @refHtml = @refHtml + '<td>' + coalesce(Value, '1') + '</td>' from @refFields where Name = @name;
						end
						else
						begin
							set @refHtml = @refHtml + '<td>&nbsp;</td>';
						end

						set @rfCurrent = @rfCurrent + 1
					end
					-----------------------------------------------------

				delete @refFields

				set @refHtml = @refHtml + '</tr>'	-- Close off row for selected lookup.

				set @current = @current + 1
			end

			-----------------------------------------

			set @refHtml = @refHtml + '</tbody>'

			set @refHtml = @refHtml + '</table>'

			if @max >= 500
			begin
				set @refHtml = @refHtml + '<div style="font-weight:bold;padding-top:10px">Showing top 500 items</div>'	
			end

			insert into @tbl values ('Items', @refHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'Resource' OR @Type = 'ResourceType'
		begin
			-- BUILD Resource LIST HTML -----------------------------------------
			declare @resourceItemsHtml nvarchar(max)

			set @resourceItemsHtml = '<table class="hoverable bordered striped" style="width:100%"><thead>'
			set @resourceItemsHtml = @resourceItemsHtml + '<th style="margin-right: 15px">First Name</th><th style="margin-right: 15px">Last Name</th><th>Email</th>'
			set @resourceItemsHtml = @resourceItemsHtml + '</thead><tbody>'

			select		top 10 
						@resourceItemsHtml = @resourceItemsHtml + '<tr>' + 
											'<td>' + FirstName + '</td>' + 
											'<td>' + LastName + '</td>' + 
											'<td>' + Email + '</td>'
											+ '</tr>'
			from		reporting.Global_Resource
			order by	LastName, FirstName asc

			set @resourceItemsHtml = @resourceItemsHtml + '</tbody>'
			set @resourceItemsHtml = @resourceItemsHtml + '</table>'

			insert into @tbl values ('Items', @resourceItemsHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'ReferenceItem'
		begin

			declare @myReferenceListID int

			select	@myReferenceListID = ReferenceItemTypeID from ReferenceItem where ID = @ID
			-- BUILD LIST HTML -----------------------------------------
			declare @referenceItemHtml nvarchar(max)

			set @referenceItemHtml = '<table class="hoverable bordered striped" style="width:100%">'
			set @referenceItemHtml = @referenceItemHtml + '<thead><th style="margin-right: 15px">Name</th></thead>'
			set @referenceItemHtml = @referenceItemHtml + '<tbody>'



			select		top 10 
						@referenceItemHtml = @referenceItemHtml + '<tr>' + '<td>' + DisplayValue + '</td>' + '</tr>'             
			from		ReferenceItem
			where		ReferenceItemTypeID = @myReferenceListID
			order by	DisplayValue desc

			set @referenceItemHtml = @referenceItemHtml + '</tbody>'
			set @referenceItemHtml = @referenceItemHtml + '</table>'

			insert into @tbl values ('Items', @referenceItemHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'ReferenceItemType'
		begin

		--	declare @myReferenceListID int

			--select	@myReferenceListID = ReferenceItemTypeID from ReferenceItem where ID = @ID
			-- BUILD LIST HTML -----------------------------------------
			declare @referenceItemTypeHtml nvarchar(max)

			set @referenceItemTypeHtml = '<table class="hoverable bordered striped" style="width:100%">'
			set @referenceItemTypeHtml = @referenceItemTypeHtml + '<thead><th style="margin-right: 15px">Display Value</th></thead>'
			set @referenceItemTypeHtml = @referenceItemTypeHtml + '<tbody>'



			select		top 10 
						@referenceItemTypeHtml = @referenceItemTypeHtml + '<tr>' + '<td>' + DisplayValue + '</td>' + '</tr>'             
			from		ReferenceItem
			where		ReferenceItemTypeID = @ID
			order by	DisplayValue desc

			set @referenceItemTypeHtml = @referenceItemTypeHtml + '</tbody>'
			set @referenceItemTypeHtml = @referenceItemTypeHtml + '</table>'

			insert into @tbl values ('Items', @referenceItemTypeHtml)
			------------------------------------------------------------------
		end;

	end

	if @Action = 'None'
	begin
		set @html = '<h3>{Name}</h3><div>'
	end

	if @Action = 'Preview'
	begin
		set @html = '<h3 style="positon: relative">{Name} <small style="background-color: #fff; float:right;font-size:65%;">{Type}</small></h3><div>{Description}</div>'
		set @showIcon = 0

		if @Type = 'Artifact'
		begin
			declare @artifactPathHtml nvarchar(2500) = '<table>';
			declare @artLevelResult table(ID int identity, LevelName nvarchar(250), DisplayValue nvarchar(250), Url varchar(1000));

			with ap as (
				select	O.ID,
						O.ParentID,
						'INVALID' as DisplayValue,
						L.Name as LevelName,
						dbo.GenerateNgObjectUrl('Artifact', L.ID, O.ID) as Url,
						1 as [Level]
				from	Artifact O
						inner join ArtifactType L on L.ID = O.ArtifactTypeID
				where	O.ID = @ID
				union all
				select	O.ID,
						O.ParentID,
						'INVALID' as DisplayValue,
						L.Name as LevelName,
						dbo.GenerateNgObjectUrl('Artifact', L.ID, O.ID) as Url,
						C.[Level] + 1 as [Level]
				from	Artifact O
						inner join ArtifactType L on L.ID = O.ArtifactTypeID
						inner join ap as C on C.ParentID = O.ID
			)

			insert into @artLevelResult
				select LevelName, DisplayValue, Url from ap order by [Level] desc

			select		@artifactPathHtml = coalesce(@artifactPathHtml + '', '') + '<tr><td style="width: 15px">' +  cast([ID] as varchar) + '</td><td>' +  LevelName + '</td><td><b><a href="' + Url + '">' + DisplayValue + '</a></b>' + '</td></tr>'
			from		@artLevelResult

			set @artifactPathHtml =  @artifactPathHtml + '</table>'

			set @html = @html + '<div><b>Path:</b></div><div>' + coalesce(@artifactPathHtml,'') + '</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'FusionAttribute'
		begin
			declare @faPathHtml nvarchar(2500) = '<table>';
			declare @faLevelResult table(ID int identity, LevelName nvarchar(250), Name nvarchar(250));

			with fap as (
				select	O.ID,
						O.ParentID,
						O.Name,
						L.Name as LevelName,
						1 as [Level]
				from	FusionAttribute O
						inner join FusionAttributeType L on L.ID = O.FusionAttributeTypeID
				where	O.ID = @ID
				union all
				select	O.ID,
						O.ParentID,
						O.Name,
						L.Name as LevelName,
						C.[Level] + 1 as [Level]
				from	FusionAttribute O
						inner join FusionAttributeType L on L.ID = O.FusionAttributeTypeID
						inner join fap as C on C.ParentID = O.ID
			)

			insert into @faLevelResult
				select LevelName, Name from fap order by [Level] desc

			select		@faPathHtml = @faPathHtml + 
						'<tr><td colspan="2">Configuration</td><td><b><a href="/fusion/' + cast(F.ID as nvarchar) + '">' + coalesce(F.Name,'') + '</a></b></td></tr>' 
			from		Fusion F 
						inner join FusionAttribute A on A.FusionID = F.ID and A.ID = @ID

			select		@faPathHtml = coalesce(@faPathHtml + '', '') + '<tr><td style="width: 15px">' +  cast(ID as varchar) + '</td><td>' +  LevelName + '</td><td><b>' + Name + '</b>' + '</td></tr>'
			from		@faLevelResult

			set @faPathHtml =  @faPathHtml + '</table>'

			set @html = @html + '<div><b>Path:</b></div><div>' + coalesce(@faPathHtml,'') + '</div>'

			set @hasDynamicFields = 1
		end

		if @Type = 'Intersect'
		begin
			set @hasDynamicFields = 1
		end;


		if @Type = 'Issue'
		begin
			insert into @tbl values('Name', '')
			insert into @tbl values('Description', '')

			if exists (select id from issue where id = @ID)
			begin			
				set @html = @html + '<div><b>Issue Type:</b> {IssueType}</div>'
				set @html = @html + '<div><b>Criticality:</b> {Criticality}</div>'

				insert into @tbl 
					select 'IssueType', it.name 
					from issuetype it inner join issue i on(i.issuetypeid = it.id) 
					where i.id = @ID

				insert into @tbl 
					select 'Criticality', case when i.Criticality = 0 then 'Negligible' when i.Criticality = 1 then 'Low' when i.Criticality = 2 then 'Medium' when i.Criticality = 3 then 'High'  when i.Criticality = 4 then 'Critical' else 'N/A' end
					from issuetype it inner join issue i on(i.issuetypeid = it.id) 
					where i.id = @ID

				set @hasDynamicFields = 1
			end			
		end;

		if @Type = 'Resource'
		begin
			--declare @e nvarchar(500)--, @fn nvarchar(250), @ln nvarchar(250)
			--select	@e = Email--, @fn = FirstName, @ln = LastName
			--from	reporting.Global_Resource
			--where	ResourceID = @ID

			--insert into @tbl values ('Email', @e)
			--insert into @tbl values ('FirstName', @fn)
			--insert into @tbl values ('LastName', @ln)
			--insert into @tbl values ('Role', '')

			--set @html = @html + '<div><b>Email:</b> {Email}</div>'
			--set @html = @html + '<div><b>First Name:</b> {FirstName}</div>'
			--set @html = @html + '<div><b>Last Name:</b> {LastName}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'Rule'
		begin
			insert into @tbl
				select	'Name', DisplayValue
				from	[Rule] O
				where	ID = @ID

			set @hasDynamicFields = 1
		end;

		if @Type = 'RuleDimension'
		begin
			insert into @tbl
				select	'Description', [Description]
				from	RuleDimension
				where	ID = @ID
			insert into @tbl
				select	'Name', [Name]
				from	RuleDimension
				where	ID = @ID

			--set @html = @html + '<div><b>Path:</b> {Description}</div>'

		end;

		if @Type = 'Taxonomy'
		begin
			declare @taxonomyPathHtml nvarchar(2500) = '<table>';

			with tp as (
				select	O.ID,
						O.ParentID,
						O.DisplayValue,
						coalesce(L.Name, 'Level ' + cast(O.[Level] as varchar)) as LevelName,
						O.[Level]
				from	[Taxonomy] O
						left join TaxonomyTypeLevel L on L.TaxonomyTypeID = O.TaxonomyTypeID and L.[Level] = O.[Level]
				where	O.ID = @ID
				union all
				select	O.ID,
						O.ParentID,
						O.DisplayValue,
						coalesce(L.Name, 'Level ' + cast(O.[Level] as varchar)) as LevelName,
						O.[Level]
				from	[Taxonomy] O
						outer apply (
									select Name from TaxonomyTypeLevel where TaxonomyTypeID = O.TaxonomyTypeID and [Level] = O.[Level]
									) L
						--left join TaxonomyTypeLevel L on L.TaxonomyTypeID = O.TaxonomyTypeID and L.[Level] = O.[Level]
						inner join tp as C on C.ParentID = O.ID
			)

			select		@taxonomyPathHtml = coalesce(@taxonomyPathHtml + '', '') + '<tr><td style="width: 15px">' +  cast([Level] as varchar) + '</td><td>' +  LevelName + '</td><td><b>' + DisplayValue + '</b>' + '</td></tr>'
			from		tp
			order by	[Level]

			set @taxonomyPathHtml =  @taxonomyPathHtml + '</table>'

			set @html = @html + '<div><b>Path:</b></div><div>' + coalesce(@taxonomyPathHtml,'') + '</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'TaxonomyType'
		begin
			insert into @tbl
				select	'Name', Name
				from	TaxonomyType O
				where	ID = @ID

			set @hasDynamicFields = 1
		end;

		-- If required, get dynamic fields to add to list.
		if @hasDynamicFields = 1
		begin
			select	@html = @html + '<div><b>' + FriendlyName + '</b>: ' + '{' + Name + '}' + '</div>' 
			from	FieldWithRelation
			where	ObjectType = @Type
					and ObjectID = @ID
					and Name not in (select Name from @tbl)

			insert into @tbl
				select	Name,
						FormattedValue
				from	FieldWithRelation
				where	ObjectType = @Type
						and ObjectID = @ID
						and Name not in (select Name from @tbl)
		end;
	end

	if @Action = 'Statistics'
	begin
		set @html = '<h3>{Name}</h3><div>{Statistics}</div>'

		set @hasStats = case @Type
							when 'Artifact' then 1
							when 'Taxonomy' then 1
							else 0
						end

		-- If required, build statistics table
		if @hasStats = 1
		begin
			-- BUILD STATS LIST HTML -----------------------------------------
			declare @statsHtml nvarchar(max)

			declare @stats table (ID int identity, Name nvarchar(250), Score bit)

			--insert into @stats 
			--	select		G.Name + ': ' + I.Name,
			--				MR.Value
			--	from		metrics.ScoreItem S
			--				inner join metrics.MapResult MR on MR.ScoreID = S.ID and S.EffectiveEndDate = '12/31/9999' --and S.Object = @Type and S.ObjectID = @ID
			--				inner join metrics.Map M on M.ID = MR.MapID
			--				inner join metrics.[Group] G on G.ID = M.GroupID
			--				inner join metrics.Item I on I.ID = M.ItemID
			--	order by	G.Name + ': ' + I.Name

			set @statsHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @statsHtml = @statsHtml + '<tbody>'
			set		@current = 1
			select	@max = max(ID) from @stats
			while @current <= @max
			begin
				select	@statsHtml = @statsHtml + '<tr><td>' + Name  + '</td>' + '<td>' + case when Score = 1 then 'Pass' else 'Fail' end  + ' </td></tr>'
				from	@stats
				where	ID = @current

				set @current = @current + 1
			end
			set @statsHtml = @statsHtml + '</tbody>'
			-----------------------------------------

			insert into @tbl values ('Statistics', @statsHtml)

			------------------------------------------------------------------
		end;
	end

	if exists (select 1 from ResponsibilityDetail where ((PermissionsBitMask & 1) = 0) and resourceid = @resourceId and [object] = @Type and objectid = @ID)
	begin
		set @html = 'This item either does not exist or you do not have access to its details.';

		-- Return the properly formatted values.
		select	'' as Title,
				@html as Body;
	end
	else
	begin
		-- Replace the fields in the template with the appropriate text value.
		set		@current = 1
		select	@max = max(ID) from @tbl

		while @current <= @max
		begin
			select	@name = '{' + Name + '}',
					@value = COALESCE(Value, '')
			from	@tbl 
			where	ID = @current

			if @showIcon = 1
			begin
				if @name = '{Name}' and @icon is not null
				begin
					update	@tbl 
					set		Value = '<div class="pull-left" style="width: 30px">' + @icon + '</div>' + '<div class="pull-right">' + @value + '</div>'
					where	ID = @current
					--set @usedIconAlready = 1
				end
			end

			set @html = REPLACE(@html, @name, @value)

			set @current = @current + 1
		end

		--if @showIcon = 1 and @icon is not null
		--begin
		--	set @html = @icon + '<br/>' + @html
		--end

		set @html = '<div style="max-height: 500px; min-width: 400px; overflow-y: auto">' + @html + '</div>'

		-- Return the properly formatted values.
		select	'' as Title,
				@html as Body;
	end
END
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

alter procedure [metrics].[LoadFromStaging]
as
begin
	-- 1. Remove all except the most recent staging values, grouped by date (not time).

	set nocount on;

	DECLARE @TranName VARCHAR(20);  
	SELECT @TranName = 'UpdateScores';  
	begin transaction @TranName;

	begin try

		update	metrics.StagingScoreItem

		set		Processing = 1

		where	Archived = 0;

		-- STEP: Get the list of uniue asset / date combinations in order to calculate the score for these assets, for these 
		--		 dates - using the metrics in the transactional table.
		drop table if exists #assetEffectiveDates;
		create table #assetEffectiveDates (AssetTypeUid uniqueidentifier not null, AssetUid uniqueidentifier not null, EffectiveDate date not null);
		insert into #assetEffectiveDates
			select		distinct
						T.[Uid] as AssetTypeUid,
						A.[Uid] as AssetUid,
						R.EffectiveDate
			from		metrics.StagingScoreItem R
						inner join dbo.Asset A on A.[uid] = R.AssetUid
						inner join dbo.AssetType T on T.ID = A.AssetTypeID
			where		R.Archived = 0
						and R.Processing = 1;

		--select * from #assetEffectiveDates

		-- STEP: Get the all the relevant metrics.
		drop table if exists #step1;
		create table #step1 (
			AssetUid uniqueidentifier not null, EffectiveDate date not null,
			MetricAssetUid uniqueidentifier not null, ParentUid uniqueidentifier null, IsGroup bit not null,
			Weight decimal(5,3) not null,
			[Value] bit not null
			);

		insert into #step1
			select	E.AssetUid,
					E.EffectiveDate,
					A.[Uid],
					A.ParentUid,
					A.IsGroup,
					V.Weight,
					coalesce(S.Result, P.Value, 0) as Value
			from	metrics.AssetVersion V
					inner join metrics.Asset A on A.[Uid] = V.[Uid]
					inner join #assetEffectiveDates E on E.AssetTypeUid = A.AssetTypeUid and A.State = 1
					cross apply (
						select	max(EffectiveDate) as EffectiveDate
						from	metrics.AssetVersion
						where	[Uid] = A.[Uid]
								and EffectiveDate <= E.EffectiveDate
					) MV
					left join metrics.StagingScoreItem S on S.AssetUid = E.AssetUid and S.MetricAssetUid = V.Uid and S.EffectiveDate = E.EffectiveDate --and V.EffectiveDate <= S.EffectiveDate
					outer apply (
						select		top 1
									AssetUid,
									MetricAssetUid,
									Value,
									max(EffectiveDate) OVER(PARTITION BY AssetUid, MetricAssetUid, EffectiveDate) as EffectiveDate
						from		metrics.ScoreItem
						where		AssetUid = E.AssetUid and MetricAssetUid = V.Uid and EffectiveDate <= S.EffectiveDate
					) P
			where	V.EffectiveDate = MV.EffectiveDate
					and metrics.AssetMeetsConditions(V.[Uid], V.EffectiveDate, E.AssetUid) = 1;

		--select * from #step1


		-- STEP: Calculate the level and adjust the weight at each level.
		drop table if exists #step2;
		create table #step2 (
			AssetUid uniqueidentifier not null, EffectiveDate date not null,
			MetricAssetUid uniqueidentifier not null, ParentUid uniqueidentifier null, IsGroup bit not null,
			Weight decimal(5,3) not null,
			[Value] bit not null,
			[Level] int null,
			AdjustedWeight decimal(5,3) null,
			Score decimal(5,3) null
		);

		with h as (
			select	AssetUid,
					EffectiveDate,
					MetricAssetUid,
					ParentUid,
					IsGroup,
					Weight,
					Value,
					1 as [Level]
			from	#step1
			where	ParentUid is null
			union all
			select	c.AssetUid,
					c.EffectiveDate,
					c.MetricAssetUid,
					c.ParentUid,
					c.IsGroup,
					c.Weight,
					c.Value,
					h.[Level] + 1 as [Level]
			from	#step1 c
					inner join h on h.AssetUid = c.AssetUid and h.MetricAssetUid = c.[ParentUid] and c.EffectiveDate = h.EffectiveDate
		)

		insert into #step2 (AssetUid, EffectiveDate, MetricAssetUid, ParentUid, IsGroup, Weight, [Value], [Level])
			select	*

			from	h

			order by EffectiveDate, [Level];

		--select * from #step2

		-- Fix the weights that users inevitably screwed up. Adjust them based on sibling ratios.













		update	T
		set		T.AdjustedWeight = IIF(T.Weight = 0, 1, T.Weight) / IIF(S.Weight = 0, 1, S.Weight) --select *
		from	#step2 T
				outer apply (
					select	sum(Weight) Weight
					from	#step2
					where	AssetUid = T.AssetUid
							and EffectiveDate = T.EffectiveDate
							and ( (ParentUid = T.ParentUid) or (ParentUid is null and T.ParentUid is null) )
				) S;

		update	#step2
		set		Score = case [Value]
							when 1 then AdjustedWeight
							else 0
						end
		where	IsGroup = 0;

		update	#step2
		set		Score = null
		where	IsGroup = 1;

		--select * from #step2

		declare @stopLevel int = 1,
				@currentLevel int
		select	@currentLevel = max([Level])
		from	#step2;

		while @currentLevel >= @stopLevel
		begin
			update	T
			set		T.Score = S.Score*AdjustedWeight
			from	#step2 T
					cross apply (
						select	sum(coalesce(Score, AdjustedWeight)) as Score
						from	#step2
						where	ParentUid = T.MetricAssetUid
								and EffectiveDate = T.EffectiveDate
								and AssetUid = T.AssetUid
								--and [Value] is not null
								and [Level] = @currentLevel+1			
					) S
			where	T.[Level] = @currentLevel
					and T.IsGroup = 1

			set @currentLevel = @currentLevel - 1
		end;

		-- Adjust for any groups that have no child meatrics, and set the Score value appropriately.
		update	#step2
		set		Score = case when Value = 1 then AdjustedWeight else 0 end
		where	Score is null;

		--select * from #step2

		-- Merge SCORES

		merge		metrics.Score as T

		using		(
					select		AssetUid,
								EffectiveDate,
								round(sum(Score), 2) as Value
					from		#step2
					where		[Level] = 1
								and Score is not null
					group by	AssetUid,
								EffectiveDate
					) S
		on			(T.AssetUid = S.AssetUid and T.EffectiveDate = S.EffectiveDate)
		when		matched then

			update	set
					T.Value = S.Value
		when		not matched by target then
			insert	(AssetUid, EffectiveDate, Value)
			values	(S.AssetUid, S.EffectiveDate, S.Value);

		-- Merge SCOREITEMS
		merge		metrics.ScoreItem as T
		using		(
					select		AssetUid,
								MetricAssetUid,

								EffectiveDate,
								Value,
								AdjustedWeight
					from		#step2
					where		IsGroup = 0

					) S
		on			(T.AssetUid = S.AssetUid and T.MetricAssetUid = S.MetricAssetUid and T.EffectiveDate = S.EffectiveDate)
		when		matched then
			update	set
					T.AdjustedWeight = S.AdjustedWeight,
					T.Value = S.Value,
					T.UpdatedOn = getutcdate()
		when		not matched by target then
			insert	(AssetUid, MetricAssetUid, EffectiveDate, UpdatedOn, Value, AdjustedWeight)
			values	(S.AssetUid, S.MetricAssetUid, S.EffectiveDate, getutcdate(), S.Value, S.AdjustedWeight);

		update	metrics.StagingScoreItem
		set		Processing = 0,
				Archived = 1
		where	Processing = 1;







		commit transaction @TranName;
	end try
	begin catch
		rollback transaction @TranName;
		throw
	end catch
end
GO;

alter procedure [bulkload].[Promotions]
--declare
	@id int
--set @id = 84
as
begin
	set nocount on;

	declare @levels table (rowIndex int, [level] int, processed bit);

	declare @Object varchar(50),
			@ObjectID int,
			@Action varchar(1),
			@UpdatedOn datetime = getutcdate(),
			@UpdatedBy int = 0

	select	@Object = [Object], 
			@ObjectID = ObjectID,
			@Action = [Action],
			@UpdatedBy = UpdatedBy
	from	[Load]
	where	ID = @id;

	update	LoadItem
	set		Object = null, 
			ObjectID = null, 
			Status = null,
			StatusMessage = null
	where	LoadID = @id;


	print '(starting resolve lookup) ' 
	print getdate() 
	-- resolve lookups first as we need the id to generate the hash correctly

	-- Resolve Single-value LOOKUP fields
	exec [bulkload].[UpdateDynamicLookupFieldColumns] @id

	print '(completed resolve lookup) ' 
	print getdate() 


	if exists (select 1 from LoadItem LI
						inner join LoadColumn C on C.LoadID = LI.LoadID
						inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
				where
					FT.AllowMultipleValues = 1 and LI.LoadID = @id )
	begin
		-- Resolve Multi-value LOOKUP fields
		update	IC
		set		IC.LookupObject = MV.LookupObject,
				IC.LookupValue = MV.LookupValue
		from	LoadItemColumn IC
				inner join	(
							select		IC.LoadID,
										IC.RowIndex,
										IC.ColumnIndex,
										'ReferenceItem' as LookupObject,
										string_agg(AD.ID, ',') as LookupValue
							from		LoadItem LI
										inner join LoadItemColumn IC on LI.LoadID = @id and LI.LoadID = IC.LoadID and IC.RowIndex = LI.RowIndex
										inner join LoadColumn C on C.LoadID = IC.LoadID and C.ColumnIndex = IC.ColumnIndex
										inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.AllowMultipleValues = 1
										cross apply string_split(IC.Value, ',') VS									
										left join ReferenceItem AD on AD.ReferenceITemTypeID = FT.LookupObjectID
										CROSS APPLY [dbo].[GetReferenceItemDisplayValue](AD.ID, FT.ID) GRIDV
							where GRIDV.DisplayValue = ltrim(rtrim(VS.Value))
							group by	IC.LoadID,
										IC.RowIndex,
										IC.ColumnIndex			
							) MV on MV.LoadID = IC.LoadID and MV.RowIndex = IC.RowIndex and MV.ColumnIndex = IC.ColumnIndex
	end



	-- Log error messages for reference list resolution.
	update	LI
	set		LI.StatusMessage = coalesce(LI.StatusMessage,'') + FT.Name + ' could not be resolved to an existing reference item.' 
	from	LoadItem LI
			inner join LoadItemColumn IC on LI.LoadID = @id and IC.LoadID = LI.LoadID and IC.RowIndex = LI.RowIndex
			inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID
			inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
										and FT.Name = C.Name 
										and FT.Type = 'Lookup' 
										and FT.LookupObjectType = 'ReferenceItem' 
										and (FT.IsRequired = 1 or FT.IsPartOfKey = 1) 
										and ( 
												(FT.AllowMultipleValues = 0 AND IC.LookupObjectID is null) OR 
												(FT.AllowMultipleValues = 1 AND IC.LookupValue is null)
											);

	-- Resolve Allow All LOOKUP field values
	update	IC
	set		IC.LookupObject = REPLACE(FT.LookupObjectType, 'Type', ''),
			IC.LookupObjectID = 0,
			IC.LookupValue = 0
	from	LoadItemColumn IC
			inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID and C.LoadID = @id
			inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.Type = 'Lookup' and FT.AllowAllValue = 1 and IC.Value = FT.AllowAllLabel;

	-- Process hashes for Load Items needs to be after lookup, lookup
	if @Object = 'ReferenceItemType'
	begin		
		update	T
		set		T.KeyHash = CONVERT(
									varchar(32), 
									SUBSTRING(HASHBYTES('SHA1', substring(ltrim(rtrim(IC.Value)), 1, 250)), 3, 32), 
									2),
				T.FieldHash = V.FieldHash
		from	LoadItem T
				inner join LoadColumn C on C.LoadID = T.LoadID and C.Name = 'Code'
				inner join LoadItemColumn IC on IC.LoadID = C.LoadID and IC.RowIndex = T.RowIndex and IC.ColumnIndex = C.ColumnIndex
				inner join	(
							select		RowIndex,
										CONVERT(
											varchar(32), 
											SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
											2) as FieldHash
							from		(
										select		top 100 percent
													I.RowIndex,
													FT.ID as FieldTypeID,
													coalesce(cast(IC.LookupObjectID as varchar(100)), IC.Value, '') as Value
										from		LoadItem I
													inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
													inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex													
													left join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and C.Name !='Code'
													left join dbo.ReferenceItem RI on C.Name = 'Code' and RI.ID = @ObjectID
										order by	I.RowIndex,
													FT.ID
										) A
							group by	A.RowIndex	
							) V on V.RowIndex = T.RowIndex
		where	T.LoadID = @id;
	end
	else if @Object = 'TaxonomyType'
	begin
		declare @currRow int, @maxRow int, @currLevel int;
		set @currRow = 1;
		set @currLevel = 0;
		set @maxRow = (select max(RowIndex) from LoadItem where LoadID = @id);	

		while @currRow < @maxRow
		begin
			set @currRow = @currRow + 1;

			--get level for current row
			select		@currLevel = coalesce(max(L.[Level]), 1) 
			from		TaxonomyTypeLevel L
						inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
						inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = @currRow and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
			where		L.TaxonomyTypeID = @ObjectID

			insert into @levels (rowIndex, level, processed) values (@currRow, @currLevel, 0);

			--update the key hash based on the current level
			update	T
			set		T.KeyHash = K.KeyHash,
					T.FieldHash = V.FieldHash
			from	LoadItem T
					left join	(
								select		RowIndex,
											CONVERT(
												varchar(32), 
												SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
												2) as KeyHash
								from		(
												select top 100 percent
													IC.RowIndex, 
													FT.ID as FieldTypeID, 
													coalesce(cast(IC.LookupObjectID as varchar(100)), IC.[Value],'') as [Value] 
												from LoadColumn LC
												inner join LoadItemColumn IC on IC.LoadID = @id and IC.RowIndex = @currRow and IC.ColumnIndex = LC.ColumnIndex
												inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 and FT.Name = reverse(substring(reverse(LC.[Name]), 0, charindex(' ',reverse(LC.[Name]))))			
												where LC.LoadID = @id and LC.ColumnIndex in (
			 										select		LC.ColumnIndex 
													from		TaxonomyTypeLevel L
																inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
																inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = @currRow and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
													where		L.TaxonomyTypeID = @ObjectID and L.[Level] = @currLevel
													)
											) A
								group by	A.RowIndex
								) K on K.RowIndex = T.RowIndex
					inner join	(
								select		RowIndex,
											CONVERT(
												varchar(32), 
												SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
												2) as FieldHash
								from		(
											select		top 100 percent
														I.RowIndex,
														FT.ID as FieldTypeID,
														coalesce(IC.Value, '') as Value
											from		LoadItem I
														inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
														inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
														inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
											order by	I.RowIndex,
														FT.ID
											) A
								group by	A.RowIndex	
								) V on V.RowIndex = T.RowIndex
			where	T.LoadID = @id and T.RowIndex = @currRow;
		end
	end
	else
	begin
		update	T
		set		T.KeyHash = K.KeyHash,
				T.FieldHash = V.FieldHash
		from	LoadItem T
				inner join	(
							select		RowIndex,
										CONVERT(
											varchar(32), 
											SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' +Value, char(59))), 3, 32), 
											2) as KeyHash
							from		(
										select		top 1000000000 --percent
													I.RowIndex,
													FT.ID as FieldTypeID,
													coalesce(cast(IC.LookupObjectID as varchar(100)), IC.Value, '') as Value
										from		LoadItem I
													inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id and IC.Value is not null
													inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
													inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 and FT.Name = C.Name
										order by	I.RowIndex,
													FT.ID
										) A
							group by	A.RowIndex
							) K on K.RowIndex = T.RowIndex
				inner join	(
							select		RowIndex,
										CONVERT(
											varchar(32), 
											SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
											2) as FieldHash
							from		(
										select		top 100 percent
													I.RowIndex,
													FT.ID as FieldTypeID,
													coalesce(IC.Value, '') as Value
										from		LoadItem I
													inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
													inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
													inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
										order by	I.RowIndex,
													FT.ID
										) A
							group by	A.RowIndex	
							) V on V.RowIndex = T.RowIndex
		where	T.LoadID = @id;
	end
	-- -----------------------------


	-- Resolve RELATIONSHIP fields
	if exists (select 1 from LoadColumn C
					inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.Type = 'Relationship' where C.LoadID = @id )
	begin
		declare @relFieldLookups table (LoadID int, RowIndex int, ColumnIndex int, Object varchar(50), ObjectID int )

		insert into @relFieldLookups
			select	IC.LoadID,
					Ic.RowIndex,
					IC.ColumnIndex,
					D.Object,
					D.ObjectID
			from	LoadItemColumn IC
					inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID and C.LoadID = @id
					inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.Type = 'Relationship'
					inner join IntersectType IT on FT.LookupObjectType = 'IntersectType' and FT.LookupObjectID = IT.ID
					inner join AssetType DT on DT.Object = case when IT.Subject = @Object and IT.SubjectID = @ObjectID then IT.Object else IT.Subject end
												and DT.ObjectID = case when IT.Subject = @Object and IT.SubjectID = @ObjectID then IT.ObjectID else IT.SubjectID end
					inner join dbo.GetAssetDisplayValue() D on D.AssetTypeID = DT.ID and D.DisplayValue = ltrim(rtrim(IC.Value));

		update	T
		set		T.LookupObject = S.Object,
				T.LookupObjectID = S.ObjectID
		from	LoadItemColumn T
				inner join @relFieldLookups S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex;
	end


	-- Capture changes for logging purposes.
	--declare @tbl table (ObjectID int, RowIndex int, [Action] varchar(1), [FieldsLoaded] bit null, [RelationshipsLoaded] bit null);

	IF OBJECT_ID('tempdb..#tbl') IS NOT NULL
			DROP TABLE #tbl;

	create table #tbl (ObjectID int, RowIndex int, [Action] varchar(1), [FieldsLoaded] bit null, [RelationshipsLoaded] bit null);

	CREATE CLUSTERED INDEX PK_tempTbl ON #tbl ([RowIndex] ASC,[Action] ASC);

	--declare @insertToPerform table (RowID int identity, KeyHash varchar(250));
	IF OBJECT_ID('tempdb..#insertToPerform') IS NOT NULL
			DROP TABLE #insertToPerform;

	create table #insertToPerform (RowID int identity, KeyHash varchar(250));

	CREATE CLUSTERED INDEX PK_tempinsertToPerform ON #insertToPerform ([KeyHash] ASC);

	--declare @insertOutputID table (RowID int identity, ObjectID int);
	IF OBJECT_ID('tempdb..#insertOutputID') IS NOT NULL
			DROP TABLE #insertOutputID;

	create table #insertOutputID (RowID int identity, ObjectID int);

	-- COMMON ------------------
	-- Identify which load items already exist based on key hash.
	-- oddly wonky
	/*update	T
	set		T.Object = A.Object,
			T.ObjectID = A.ObjectID
	from	LoadItem T
			inner join AssetType ST on ST.Object = @Object and ST.ObjectID = @ObjectID
			inner join GetAssetKeyHash() S on S.AssetTypeID = ST.ID and S.KeyHash = T.KeyHash and T.LoadID = @id
			inner join Asset A on A.ID = S.ID;*/

	/*update	T
	set		T.Object = A.Object,
			T.ObjectID = A.ObjectID
	from	LoadItem T
			inner join AssetType ST on ST.Object = @Object and ST.ObjectID = @ObjectID
			cross apply GetAssetKeyHashById(ST.ID) S 
			inner join Asset A on A.ID = S.ID
	where S.KeyHash = T.KeyHash and T.LoadID = @id*/

	update	T
	set		T.Object = A.Object,
			T.ObjectID = A.ObjectID
	from	LoadItem T
			inner join AssetType ST on ST.Object = @Object and ST.ObjectID = @ObjectID			
			inner join Asset A on A.AssetTypeID = ST.ID
			cross apply GetAssetKeyHashById(A.ID) S 
	where S.KeyHash = T.KeyHash and T.LoadID = @id


	--BEGIN TRANSACTION;
    --SAVE TRANSACTION PromotionCreationTrans;

	--BEGIN Try 
			-- ARTIFACTS ---------------
			if @Object = 'ArtifactType'
			begin
				-- Mark the existing artifacts as being updated.
				update	T
				set		T.UpdatedBy = @UpdatedBy,
						T.UpdatedOn = @UpdatedOn
				from	Artifact T
						inner join LoadItem S on S.LoadID = @id and S.Object = 'Artifact' and S.ObjectID = T.ID and T.ArtifactTypeID = @ObjectID;

				-- Insert the updated records into temp table for logging.
				insert into #tbl 
					select	ObjectID,
							RowIndex,
							'U', null, null
					from	LoadItem
					where	LoadID = @id 
							and ObjectID is not null;

				-- Insert new items into the Artifact table.
				insert into #insertToPerform
					select	distinct
							KeyHash
					from	LoadItem
					where	LoadID = @id
							and ObjectID is null
							and KeyHash is not null;

				--declare @insertOutputID table (RowID int identity, ObjectID int);
				insert Artifact (ArtifactTypeID, UpdatedOn, UpdatedBy, CreatedOn, CreatedBy)
				output inserted.ID into #insertOutputID
					select	@ObjectID, 
							@UpdatedOn, 
							@UpdatedBy, 
							@UpdatedOn, 
							@UpdatedBy
					from	#insertToPerform;

				-- Insert the added records into temp table for logging.
				insert into #tbl 
					select	N.ObjectID,
							I.RowIndex,
							'A', null, null
					from	LoadItem I
							inner join #insertToPerform P on P.KeyHash = I.KeyHash and I.LoadID = @id 
							inner join #insertOutputID N on N.RowID = P.RowID;

				-- Update the LoadItem table with the Object and ObjectID generated from the insert above.
				update	T
				set		T.Object = 'Artifact',
						T.ObjectID = S.ObjectID
				from	LoadItem T
						inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and S.[Action] = 'A';
			end
			-------------------------

			-- MODEL ----------------
		   if @Object = 'TaxonomyType'
		   begin
				declare 
					@row int, 
					@level int, 
					@rows int, 
					@rowObject varchar(50), 
					@rowObjectId int, 
					@parentKeyHash varchar(50),
					@intersectTypeid int,
					@parentObjectId int;

				declare @ids table (id int);

				set @row = 0;
				set @level = 0;

				-- Insert the updated records into temp table for logging.
				insert into #tbl 
					select	ObjectID,
							RowIndex,
							'U', null, null
					from	LoadItem
					where	LoadID = @id 
							and ObjectID is not null;

				while (select count(*) from @levels where processed = 0) > 0
				begin
					set @parentKeyHash = null;
					set @parentObjectId = null;
					delete from @ids;

					--need to process rows in order of level (low to high) to make sure parent items are added or exist
					select		top 1
								@row = L.RowIndex, 
								@level = L.[Level], 
								@rowObject = LC.[Object], 
								@rowObjectId = LC.ObjectID 
					from		@levels L
								inner join LoadItem LC on LC.RowIndex = L.RowIndex and LC.LoadID = @id
					where		L.processed = 0
					order by	L.[Level] asc;

					if @rowObjectId is not null
					begin
						update	Taxonomy
						set		UpdatedOn = @UpdatedOn,
								UpdatedBy = @UpdatedBy
						where	ID = @rowObjectId;
					end
					else
					begin
						if @level > 1
						begin
							--hash key fields at (level - 1) and check against asset or LoadItem
							select @parentKeyHash = CONVERT(
											varchar(32), 
											SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
											2)
							from		(
											select		top 100 percent
														FT.ID as FieldTypeID, 
														coalesce(IC.[Value],'') as [Value] 
											from		LoadColumn LC
														inner join LoadItemColumn IC on IC.LoadID = @id and IC.RowIndex = @row and IC.ColumnIndex = LC.ColumnIndex
														inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 
															and FT.Name = reverse(substring(reverse(LC.[Name]), 0, charindex(' ',reverse(LC.[Name]))))			
											where		LC.LoadID = @id and LC.ColumnIndex in (
			 												select	LC.ColumnIndex 
															from	TaxonomyTypeLevel L
																	inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
																	inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = @row and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
															where	L.TaxonomyTypeID = @ObjectID and L.[Level] = (@level-1)
															)
										) A;

							select @parentObjectId = coalesce(
									(
									select		top 1 
												a.ObjectID 
									from		Asset A
												inner join AssetType T on T.Object = @Object and T.ObjectID = @ObjectID and A.AssetTypeID = T.ID
												inner join GetAssetKeyHash() H on H.ID = A.ID
									where		H.KeyHash = @parentKeyHash
									),
									(
									select		top 1 
												a.ObjectID 
									from		LoadItem L
												inner join Asset A on A.[Object] = L.[Object] and A.ObjectID = L.ObjectID
									where		LoadID = @id and L.KeyHash = @parentKeyHash
									)
								);

							if @parentObjectId is not null
							begin
								insert Taxonomy (TaxonomyTypeID, UpdatedOn, UpdatedBy)
								output inserted.ID into @ids
									select	@ObjectID, 
											@UpdatedOn, 
											@UpdatedBy;

								insert into #tbl
								select	id,
										@row,
										'A', null, null
								from	@ids

								select  @intersectTypeId = id 
								from	intersecttypedetail 
								where	[subject] = @Object and subjectid = @ObjectID 
										and [object] = @Object and objectid = @objectID
										and predicatetype = 4;

								if @intersectTypeId is not null 
									and not exists (
										select		1 
										from		[Intersect] 
										where		IntersectTypeID = @intersectTypeId 
													and ObjectID = (select id from @ids) 
													and SubjectID = @parentObjectId)
								begin						
									insert into [Intersect] (IntersectTypeId, [Subject], [Object], SubjectID, ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
									select	@intersectTypeId as IntersectTypeId,
											'Taxonomy' as [Subject],
											'Taxonomy' as [Object],
											@parentObjectId as SubjectID,
											(select id from @ids) as ObjectID,
											@UpdatedBy as CreatedBy,
											@UpdatedOn as CreatedOn,
											@UpdatedBy as UpdatedBy,
											@UpdatedOn as UpdatedOn,
											'BulkLoad' as [Owner];
								end
							end
						end
						else --root item
						begin			
							insert Taxonomy (TaxonomyTypeID, UpdatedOn, UpdatedBy)
							output inserted.ID into @ids
								select	@ObjectID, 
										@UpdatedOn, 
										@UpdatedBy;

							insert into #tbl
							select	id,
									@row,
									'A', null, null
							from	@ids;									
						end
					end

					update	@levels 
					set		processed = 1 
					where	rowIndex = @row 
							and [level] = @level;

					update	T
					set		T.Object = 'Taxonomy',
							T.ObjectID = S.ObjectID
					from	LoadItem T
							inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and S.[Action] = 'A';
				end

			end
			--------------------------

			-- REFERENCE ------------
			if @Object = 'ReferenceItemType'
			begin
				declare @ri_insertToPerform table (RowID int identity, Code nvarchar(250), KeyHash varchar(250));
				declare @ri_insertOutputID table (RowID int identity, ObjectID int);

				-- Mark the existing items as being updated.
				update	T
				set		T.UpdatedBy = @UpdatedBy,
						T.UpdatedOn = @UpdatedOn
				from	ReferenceItem T
						inner join LoadItem S on S.LoadID = @id and S.Object = 'ReferenceItem' and S.ObjectID = T.ID and T.ReferenceItemTypeID = @ObjectID;

				-- Insert the updated records into temp table for logging.
				insert into #tbl 
					select	ObjectID,
							RowIndex,
							'U', null, null
					from	LoadItem
					where	LoadID = @id 
							and ObjectID is not null;

				-- Insert new items into the ReferenceItem table.
				insert into @ri_insertToPerform
					select	distinct
							substring(ltrim(rtrim(IC.Value)), 1, 250),
							I.KeyHash
					from	LoadItem I
							inner join LoadColumn C on C.LoadID = I.LoadID and C.Name = 'Code'
							inner join LoadItemColumn IC on C.ColumnIndex = IC.ColumnIndex and IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex 
					where	I.LoadID = @id
							and I.ObjectID is null
							and I.KeyHash is not null;

				insert ReferenceItem (ReferenceItemTypeID, Code, UpdatedOn, UpdatedBy, CreatedOn, CreatedBy)
				output inserted.ID into @ri_insertOutputID
					select	@ObjectID, 
							Code,
							@UpdatedOn, 
							@UpdatedBy, 
							@UpdatedOn, 
							@UpdatedBy
					from	@ri_insertToPerform;

				-- Insert the added records into temp table for logging.
				insert into #tbl 
					select	N.ObjectID,
							I.RowIndex,
							'A', null, null
					from	LoadItem I
							inner join @ri_insertToPerform P on P.KeyHash = I.KeyHash and I.LoadID = @id 
							inner join @ri_insertOutputID N on N.RowID = P.RowID;

				-- Update the LoadItem table with the Object and ObjectID generated from the insert above.
				update	T
				set		T.Object = 'ReferenceItem',
						T.ObjectID = S.ObjectID
				from	LoadItem T
						inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and S.[Action] = 'A';
			end
			-------------------------


			-- Capture field logs	
			IF OBJECT_ID('tempdb..#fields') IS NOT NULL
					DROP TABLE #fields;

			create table #fields (RowIndex int, ColumnIndex int, [Action] varchar(25));


			--CREATE CLUSTERED INDEX PK_tempFields ON #fields ([RowIndex] ASC,[ColumnIndex] ASC);

			-- Non-relationship fields
			print '(starting merge fields) ' 
			print getdate() 

				merge	Field as T
				using	(
						select	I.FieldTypeID,
								I.Type,
								I.AllowMultipleValues,
								I.Object,
								I.ObjectID,
								case 
									when I.Type = 'Lookup' and I.AllowMultipleValues = 0 then cast(C.LookupObjectID as nvarchar)
									when I.Type = 'Lookup' and I.AllowMultipleValues = 1 then C.LookupValue
									else C.Value
								end as [Value],
								C.RowIndex,
								C.ColumnIndex
						from	(
								select		I.LoadID,
											FT.ID as FieldTypeID,
											FT.Type,
											FT.AllowMultipleValues,
											I.Object,
											I.ObjectID,
											min(I.RowIndex) as RowIndex,
											C.ColumnIndex
								from		LoadItem I
											inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id
											inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
											inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
																	and  (
																		FT.Name = LC.Name or
																			(
																				@Object = 'TaxonomyType'
																				 and LC.ColumnIndex in (
																					select LC2.ColumnIndex from TaxonomyTypeLevel L2
																					inner join LoadColumn LC2 on LC2.LoadID = @id and L2.[Name] = substring(LC2.[Name], 1, len(LC2.[Name]) - charindex(' ', reverse(LC2.[Name])))
																					inner join LoadItemColumn LI2 on LI2.LoadID = @id and LI2.RowIndex = C.RowIndex and LI2.ColumnIndex = LC2.ColumnIndex and LI2.[Value] is not null
																					where L2.TaxonomyTypeID = @ObjectID and L2.[Level] = (select [level] from @levels where rowIndex = C.RowIndex)
																				 )
																				 and FT.Name = reverse(substring(reverse(LC.[Name]), 0, charindex(' ',reverse(LC.[Name])))) 
																			)
																		)
																	and FT.Type <> 'Relationship' 
																	and ( 
																			(FT.Type <> 'Lookup' and C.Value is not null) OR 
																			(FT.Type = 'Lookup' and FT.AllowMultipleValues = 0 and C.LookupObjectID is not null) OR
																			(FT.Type = 'Lookup' and FT.AllowMultipleValues = 1 and C.LookupValue is not null)
																		)
								where		I.ObjectID is not null
								group by	I.LoadID,
											FT.ID,
											FT.Type,
											FT.AllowMultipleValues,
											I.Object,
											I.ObjectID,
											C.ColumnIndex
								) I
								inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and C.ColumnIndex = I.ColumnIndex
						) S on (T.FieldTypeID = S.FieldTypeID and S.Object = T.ObjectType and S.ObjectID = T.ObjectID)
				when matched then
					update	set
							Value = S.Value
				when not matched then
					insert (FieldTypeID, ObjectType, ObjectID, Value)
					values (S.FieldTypeID, S.Object, S.ObjectID, S.Value)
				output S.RowIndex, S.ColumnIndex, $action into #fields;

	print '(end merge fields) ' 
	print getdate() 

	--END TRY
    /*BEGIN CATCH
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION PromotionCreationTrans; -- rollback to PromotionCreationTrans
        END
    END CATCH
    COMMIT TRANSACTION */

	delete	T
	from	FieldValue T
			left join (
				select		FT.ID as FieldTypeID,
							I.Object,
							I.ObjectID,
							VS.Value
				from		LoadItem I
							inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id and I.ObjectID is not null and C.LookupValue is not null
							cross apply string_split(C.LookupValue, ',') VS
							inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
							inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
													and FT.Name = LC.Name 
													and FT.Type = 'Lookup' 
													and FT.AllowMultipleValues = 1
			) S on S.FieldTypeID = T.FieldTypeID and S.Object = T.ObjectType and S.ObjectID = T.ObjectID and S.value = T.Value
			inner join (	--LIMITS THE IMPACT OF THIS STATEMENT
				select		FT.ID as FieldTypeID,
							I.Object,
							I.ObjectID
				from		LoadItem I
							inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id and I.ObjectID is not null and C.LookupValue is not null
							inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
							inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
													and FT.Name = LC.Name 
													and FT.Type = 'Lookup' 
													and FT.AllowMultipleValues = 1			
			) L on L.FieldTypeID = T.FieldTypeID and L.Object = T.ObjectType and L.ObjectID = T.ObjectID
	where	S.FieldTypeID is null;

	insert into FieldValue (FieldTypeID, ObjectType, ObjectID, Value)
		select		FT.ID,
					I.Object,
					I.ObjectID,
					VS.Value
		from		LoadItem I
					inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id and I.ObjectID is not null and C.LookupValue is not null
					cross apply string_split(C.LookupValue, ',') VS
					inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
					inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
											and FT.Name = LC.Name 
											and FT.Type = 'Lookup' 
											and FT.AllowMultipleValues = 1
					left join FieldValue FV on FV.FieldTypeID = FT.ID and FV.ObjectType = I.Object and FV.ObjectID = I.ObjectID and FV.Value = VS.Value
		where		FV.ID is null;



	update	T
	set		T.FieldsLoaded = 1
	from	#tbl T
			inner join	(
						select		RowIndex,
									[Action]
						from		#fields
						group by	RowIndex, 
									[Action]
						) S on S.RowIndex = T.RowIndex;

	truncate table #fields;

	-- Parent fields
	declare @parentTypeID int = null,
			@parentTypeName nvarchar(250) = null;
	declare @parentIntersectTypeId int = null;

	select 
		@parentTypeID = I.SubjectID,
		@parentTypeName = I.SubjectName,
		@parentIntersectTypeId = I.ID
	from 
		intersecttypedetail I                
	where I.[PredicateType] = 3 and [Object] = @Object and ObjectID = @ObjectId;

	if @parentTypeID is not null
	begin

		-- look for column with the parent type name this contains the parent 
		merge	[Intersect] as T
		using	(
				select	distinct
						AD.ObjectID as ParentObjectID,
						AD.[Object] as ParentObject,
						AD.[TypeID] as ParentTypeID,
						AD.[Type] as ParentType,
						@parentIntersectTypeId as IntersectTypeID,
						LI.[Object] as ItemObject,
						LI.ObjectID as ItemObjectID
				from	LoadItem I
						inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id
						inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex and LC.Name = @parentTypeName
						inner join AssetDetail AD on AD.TypeID = @parentTypeID and AD.DisplayValue = C.Value and AD.[Type] = @Object	
						inner join LoadItem LI on LI.RowIndex = C.RowIndex and LI.LoadID = C.LoadID					
				where	I.ObjectID is not null
				) S on (T.IntersectTypeID = S.IntersectTypeID and T.Subject = S.ParentObject and T.SubjectID = S.ParentObjectID and T.Object = S.ItemObject and T.ObjectID = S.ItemObjectID)

		when matched then
			update	set
					T.Subject	= S.ParentObject,
					T.SubjectID = S.ParentObjectID,
					T.Object	= S.ItemObject,
					T.ObjectID	= S.ItemObjectID
		when not matched then
			insert (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner], Visible)
			values (
					S.IntersectTypeID,
					S.ParentObject, 
					S.ParentObjectID,
					S.ItemObject, 
					S.ItemObjectID, 
					0, @UpdatedBy, @UpdatedOn, @UpdatedBy, @UpdatedOn, 'BulkLoad', 1
					);

	end

	print '(starting merge relationship fields) ' 
	print getdate() 
	-- Relationship fields
	merge	[Intersect] as T
	using	(
			select	distinct
					FT.LookupObjectID as IntersectTypeID,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then cast(1 as bit)
						else cast(0 as bit)
					end as IsSubject,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then I.Object
						else C.LookupObject
					end as Subject,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then I.ObjectID
						else C.LookupObjectID
					end as SubjectID,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then C.LookupObject
						else I.Object
					end as Object,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then C.LookupObjectID
						else I.ObjectID
					end as ObjectID
			from	LoadItem I
					inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id
					inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
					inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
											and FT.Name = LC.Name and FT.Type = 'Relationship' 
											and C.LookupObject is not null and C.LookupObjectID is not null
					inner join IntersectType IT on FT.LookupObjectType = 'IntersectType' and FT.LookupObjectID = IT.ID
			where	I.ObjectID is not null
			) S on (
					T.IntersectTypeID = S.IntersectTypeID 
					and (
							(S.IsSubject = 1 and S.Subject = T.Subject and S.SubjectID = T.SubjectID) OR
							(S.IsSubject = 0 and S.Object = T.Object and S.ObjectID = T.ObjectID)
						)
					)
	when matched then
		update	set
				T.Subject	= S.Subject,
				T.SubjectID = S.SubjectID,
				T.Object	= S.Object,
				T.ObjectID	= S.ObjectID
	when not matched then
		insert (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner], Visible)
		values (
				S.IntersectTypeID,
				S.Subject, 
				S.SubjectID,
				S.Object, 
				S.ObjectID, 
				0, @UpdatedBy, @UpdatedOn, @UpdatedBy, @UpdatedOn, 'BulkLoad', 1
				);

	print '(done merge relationship fields) ' 
	print getdate()

	-- Capture logs and update load status. -----
	update	T
	set		T.Status = 1,
			T.StatusMessage = 'Item successfully ' + case S.[Action] when 'A' then 'added' else 'updated' end + '.'
	from	LoadItem T
			inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and T.[Object] is not null and T.ObjectID is not null;

	update	LoadItem
	set		Status = 0,
			StatusMessage = 'Item load failed. ' + coalesce(StatusMessage, '')
	where	([Object] is null or ObjectID is null)
			and LoadID = @id;



	----Finally, close out the Load.
	update	[Load] 
	set		DateCompleted = getutcdate()
	where	ID = @id
	---------------------------------------------
end
GO;

ALTER FUNCTION [dbo].[GetAssetKeyHashById](
	@Id bigint
)
RETURNS TABLE 
AS
RETURN 
(


	select		A.AssetTypeID,
				A.ID,
				utility.GetHash(STRING_AGG(F.Value, '|')) as KeyHash
	from		Asset A
				inner join Field F on F.ObjectType = A.Object and F.ObjectID = A.ObjectID and A.Object != 'ReferenceItem'
				inner join FieldType FT on FT.ID = F.FieldTypeID 
										and FT.AssetTypeID = A.AssetTypeID
										and FT.IsPartOfKey = 1
	where a.id = @id
	group by	A.AssetTypeID, A.ID
	union
	select		A.AssetTypeID,
				A.ID,
				utility.GetHash(STRING_AGG(R.Code, '|')) as KeyHash
	from		Asset A
				inner join referenceitem r on (a.object = 'ReferenceItem' and r.id = a.objectid)
	where a.id = @id
	group by	A.AssetTypeID, A.ID

)
GO;

ALTER FUNCTION [dbo].[GetOwnersListForWorkflow]
(
	@workflowID int,
	@workflowStepID int = 0	
)
RETURNS varchar(max)
AS
BEGIN
	declare @objectId int,			
			@objectType varchar(50),
			@responsibilityTypeID int;

	declare @tbl table (ResourceID int, FirstName nvarchar(250), LastName nvarchar(250), Email nvarchar(500), Username nvarchar(500), DateLastLoggedIn datetime null, ResourceTypeID int, Status nvarchar(25))

	select @objectType = object, @objectId = objectid from [workflow].[eventregistration] where typeid = @workflowID;

	--get the responsibility for this step from the settings of the step
	select @responsibilityTypeID = settings.value('(/settings/ResponsibilityTypeID)[1]', 'int') from [workflow].[VersionStep] where id = @workflowStepID

		--1. Check for owners
	insert into @tbl
		select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
		from	ResponsibilityDetail RD 					
				inner join reporting.Global_Resource R  on 
						RD.Type = @objectType and RD.TypeID = @objectId
						and RD.ResponsibilityTypeID = @responsibilityTypeID
						and	RD.ResourceID = R.ResourceID
						and R.Email not like '%?subject=%' 
						and R.Status = 'Active'

	-- if noone found email the group responsible or admins
	if not exists (select 1 from @tbl)
		begin			
			begin			
				insert into @tbl
					select 
						R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
					from 
						reporting.Global_Resource R where isadministrator = 1 and status = 'Active'
			end
		end


	return (select string_agg(FirstName + ' ' + LastName,', ') as Resources from @tbl)

END
GO;

ALTER VIEW [dbo].[AttributeTypeRelationDetail]
AS
	SELECT	R.AttributeTypeID,
			R.ObjectID,
			coalesce(D.Name, R.ObjectType) AS ObjectName, 
			R.ObjectType,
			cast(0 as bit) as Required,
			R.AllowMultipleEntries
	FROM	AttributeTypeRelation R
			left join AssetType D on D.[Object] = R.ObjectType and D.ObjectID = R.ObjectID
GO;

ALTER VIEW [dbo].[FieldTypeWithRelation]
AS
	SELECT	T.ID,
			T.Name,
			T.FriendlyName,
			T.Category,
			T.Description,
			T.DisplayDescription,
			T.FormDescription,
			T.ValidationDescription,
			T.Type,
			T.LookupObjectType,
			T.LookupObjectID ,
			T.LookupDisplayFormat,
			T.Length,
			T.MinimumLength,
			T.MaximumLength,
			T.Pattern,
			T.[Object],
			T.ObjectID,
			D.Name as ObjectName,
			T.IsDisplayable,
			T.IsEditable,
			T.IsListable,
			T.IsRequired,
			T.SortOrder,
			T.DefaultValue
	FROM	FieldType T
			inner join (
				select Name, Object, ObjectID from AssetType
				union all
				select ITypeName.Name as Name, 'IntersectType' as Object, ID as ObjectID from IntersectType IT
				cross apply dbo.GetIntersectTypeNames(IT.ID) ITypeName
			) D on D.[Object] = T.[Object] and D.ObjectID = T.ObjectID
GO;

alter view [dbo].[IntersectDetail]
as
	select	I.IntersectID as ID,
			I.IntersectTypeID,
			I.State,
			I.Subject,
			I.SubjectID,
			S.Name as SubjectName,
			S.Name as SubjectShortName,
			dbo.GenerateNgObjectUrl(S.[Type], S.TypeID, S.ObjectID) as SubjectUrl,
			S.Type as SubjectType,
			S.TypeID as SubjectTypeID,
			S.TypeName as SubjectTypeName,
			S.BackColor as SubjectIconBackColor,
			S.ForeColor as SubjectIconForeColor,
			S.Icon as SubjectIconText,

			I.Object,
			I.ObjectID,
			O.Name as ObjectName,
			O.Name as ObjectShortName,
			dbo.GenerateNgObjectUrl(O.[Type], O.TypeID, O.ObjectID) as ObjectUrl,
			O.Type as ObjectType,
			O.TypeID as ObjectTypeID,
			O.TypeName as ObjectTypeName,
			O.BackColor as ObjectIconBackColor,
			O.ForeColor as ObjectIconForeColor,
			O.Icon as ObjectIconText,

			I.PredicateID,
			I.PredicateType,
			case I.PredicateType
				when 1 then 'DataLineage'
				when 2 then 'ReferenceLineage'
				when 3 then 'InterTypeHierarchy'
				when 4 then 'IntraTypeHierarchy'
				when 5 then 'UserOwnership'
				when 6 then 'Grammar'
				when 7 then 'Simple'
				when 8 then 'FusionMapping'
				when 9 then 'SeeAlso'
				when 10 then 'Usage'
				when 11 then 'ObjectOwnerhip'
			end as PredicateTypeName,
			I.PredicateName,
			I.PredicateInverse
	from	PredicateIntersect I with(nolock)
			inner join (
				select coalesce(FA.TextPath,DisplayValue) as Name, Object, ObjectID, ForeColor, BackColor, Icon, Type, TypeID, TypeName from AssetDetail A
				left join FusionAttribute FA on FA.ID = A.ObjectID and A.Object = 'FusionAttribute'
				union all
				select NI.Name as Name, 'Intersect' as Object, I.ID as ObjectID, null as ForeColor, null as BackColor, null as Icon, 'IntersectType' as Type, IntersectTypeID as TypeID, NIT.Name as TypeName from [Intersect] I
				inner join IntersectType T on T.ID = I.IntersectTypeID
				cross apply dbo.GetIntersectNames(I.ID) NI	
				cross apply dbo.GetIntersectTypeNames(T.ID) NIT
				union all
				select TA.Name, TA.Object, TA.ObjectID, null as ForeColor, null as BackColor, null as Icon, 'ReferenceItemType' as Type, 0 as TypeID, TA.Name as TypeName from AssetType TA
				where TA.Object = 'ReferenceItemType'
			) S on S.Object = I.Subject and S.ObjectID = I.SubjectID
			inner join (
				select coalesce(FA.TextPath,DisplayValue) as Name, Object, ObjectID, ForeColor, BackColor, Icon, Type, TypeID, TypeName from AssetDetail A
				left join FusionAttribute FA on FA.ID = A.ObjectID and A.Object = 'FusionAttribute'
				union all
				select NI.Name as Name, 'Intersect' as Object, I.ID as ObjectID, null as ForeColor, null as BackColor, null as Icon, 'IntersectType' as Type, IntersectTypeID as TypeID, NIT.Name as TypeName from [Intersect] I
				inner join IntersectType T on T.ID = I.IntersectTypeID
				cross apply dbo.GetIntersectNames(I.ID) NI	
				cross apply dbo.GetIntersectTypeNames(T.ID) NIT
				union all
				select TA.Name, TA.Object, TA.ObjectID, null as ForeColor, null as BackColor, null as Icon, 'ReferenceItemType' as Type, 0 as TypeID, TA.Name as TypeName from AssetType TA
				where TA.Object = 'ReferenceItemType'
			) O on O.Object = I.Object and O.ObjectID = I.ObjectID
GO;

ALTER VIEW [dbo].[MapRuleItemDetail]
AS
	select 	'MapRule' as Type,
			0 as ID,
			'MapRule|0' TextID,
			NULL as ParentTextID,
			NULL as Transformation,
			NULL as SourceFusion,
			NULL as SourceFusionAttributeID,		
			NULL as SourceFusionAttributeTextPath,
			NULL as SourceObjectName,
			NULL as SourceObjectID,
			NULL as SourceObject,
			NULL as TargetFusion,
			NULL as TargetFusionAttributeID,		
			NULL as TargetFusionAttributeTextPath,
			NULL as TargetObjectName,
			NULL as TargetObjectID,
			NULL as TargetObject
	union
	select 	'MapRule' as Type,
			mr.ID,
			'MapRule|' + cast(mr.ID as varchar) TextID,
			NULL as ParentTextID,
			mr.Transformation,
			NULL as SourceFusion,
			NULL as SourceFusionAttributeID,		
			NULL as SourceFusionAttributeTextPath,
			NULL as SourceObjectName,
			NULL as SourceObjectID,
			NULL as SourceObject,
			NULL as TargetFusion,
			NULL as TargetFusionAttributeID,		
			NULL as TargetFusionAttributeTextPath,
			NULL as TargetObjectName,
			NULL as TargetObjectID,
			NULL as TargetObject
	from	MapRule mr
	union
	select 	'MapRuleItem' as Type,
			mri.ID,
			'MapRuleItem|' + cast(mri.ID as varchar) TextID,
			'MapRule|' + cast(coalesce(mr.ID, 0) as varchar) as ParentTextID,
			NULL as Transformation,
			fS.Name as SourceFusion,
			mri.SourceFusionAttributeID as SourceFusionAttributeID,		
			faS.TextPath as SourceFusionAttributeTextPath,
			odS.DisplayValue as SourceObjectName,
			mri.SourceOwnerID as SourceObjectID,
			mri.SourceOwner as SourceObject,
			fT.Name as TargetFusion,
			mri.TargetFusionAttributeID as TargetFusionAttributeID,		
			faT.TextPath as TargetFusionAttributeTextPath,
			odT.DisplayValue as TargetObjectName,
			mri.TargetOwnerID as TargetObjectID,
			mri.TargetOwner as TargetObject
	from	MapRuleItem mri
			left join MapRuleItemMapRule mrim on mrim.MapRuleItemID = mri.ID
			left join MapRule mr on mr.ID = mrim.MapRuleID

			inner join FusionAttribute faS on mri.SourceFusionAttributeID = faS.ID
			inner join Fusion fS on fS.ID = faS.FusionID
			left join AssetDetail odS on mri.[SourceOwner] = odS.[Object] and mri.SourceOwnerID = odS.ObjectID

			inner join FusionAttribute faT on mri.TargetFusionAttributeID = faT.ID	
			inner join Fusion fT on fT.ID = faT.FusionID
			left join AssetDetail odT on mri.[TargetOwner] = odT.[Object] and mri.TargetOwnerID = odT.ObjectID
GO;


--columns additions
alter table [integration].[ExecutionAssetTypeMetric] add [MisalignedResponsibilities] int CONSTRAINT [DF_IntegrationExecutionAssetTypeMetric_MisalignedResponsibilities]  DEFAULT (0) not null
GO;
alter table [integration].[SynchedAssetTypeRelationItemTarget] add UseForWriteBack bit constraint DF_IntegrationSynchedAssetTypeRelationItemTarget_UseForWriteBack default(0) not null;
GO;
alter table [workflow].[ItemAssignment] add StepID int null
GO;
alter table [Predicate] add [Uid] uniqueidentifier NOT NULL CONSTRAINT [DF_Predicate_uid] DEFAULT (newid())
GO;

--constraint additions
ALTER TABLE [dbo].[RuleResultQualifierType] ADD CONSTRAINT [DF_RuleResultQualifierType_RuleImplementationID_Name] UNIQUE NONCLUSTERED  ([RuleImplementationID], [Name])
GO;


--drops
DROP TABLE [dbo].[AssetDataQualityProperty]
GO;

DROP TABLE [dbo].[AssetDataQualityImplementationResultQualifier]
GO;

DROP TABLE [dbo].[AssetDataQualityImplementationResultFusion]
GO;

DROP TABLE [dbo].[AssetDataQualityImplementationResult]
GO;

DROP TABLE [dbo].[AssetDataQualityDimension]
GO;

DROP TABLE [dbo].[AssetDataQualityImplementationQualifierType]
GO;

DROP TABLE [dbo].[AssetDataQualityImplementation]
GO;

DROP TABLE [dbo].[AssetDisplayFieldTypes]
GO;

DROP TABLE [dbo].[AssetDisplayFormatFieldTypes]
GO;

DROP TABLE [dbo].[AssetScheduleItem]
GO;

DROP TABLE [dbo].[AssetSchedule]
GO;

DROP TABLE [dbo].[AssetTypeQuery]
GO;

-- data updates
if not exists (select 1 from [predicate] where name = 'Asset Owned For' and [Type] = 7 and IsSystem = 1)
begin
	insert into [predicate] (Name, Inverse,[Type],IsSystem,Code, [Uid]) values('Asset Owned For','Asset Owned By',7,1,0, '2A7FA12D-63AA-4595-83D0-CFA98AAC2AA4')
end
GO;

if not exists (select 1 from [predicate] where name = 'Validates' and [Type] = 7 and IsSystem = 1)
begin
    insert into [predicate] values('Validates','Is Validated By',7,1,0, 'c88ebecd-eed5-4c27-99be-a1eed29c13dd')
end
GO;

--system types
update [predicate] set [Uid] = 'D7FF74B8-5606-4FB9-A7EF-F42BE4299DC9' where id = 1;
update [predicate] set [Uid] = 'B8A4C392-6431-4CD7-A4EE-ABF260D538FD' where id = 2;
update [predicate] set [Uid] = '0F718E3D-13B1-4EFB-A407-258DEC05B844' where id = 3;
update [predicate] set [Uid] = '267D2361-CBE0-4C38-935E-226C222EE51D' where id = 4;
update [predicate] set [Uid] = 'DF813D88-7D53-482A-AF7A-DC35B13001ED' where id = 5;
update [predicate] set [Uid] = 'C88EBECD-EED5-4C27-99BE-A1EED29C13DD' where id = 44;
update [predicate] set [Uid] = '2A7FA12D-63AA-4595-83D0-CFA98AAC2AA4' where id = 45;
GO;

update ruleresultqualifiertype set name = name +cast(id as varchar(20)) where id in (
SELECT max(id)
FROM ruleresultqualifiertype
GROUP BY ruleimplementationid, name
HAVING COUNT(1) > 1)


-- BEGIN GOV-5718 DUPLICATED COMMENTS DUE TO DUPLICATED USERS IN ASSET TABLE
-- delete duplicated resources in asset table
with R as (
select *, row_number() over(partition by [object], objectid order by (select null)) as rn
from asset where [object] = 'Resource'
)
delete R
where rn > 1;
GO;

delete Asset where ID in (
select	max(ID) as ID
		--,count(1) as [Count],
		--Object,
		--ObjectID
from	Asset 
group by Object,
		ObjectID
		having count(1) > 1
)
GO;

-- add constraint
ALTER TABLE Asset ADD CONSTRAINT UC_Asset_Object_ObjectID UNIQUE ([Object],[ObjectID]);
GO;
ALTER TABLE ruleresultqualifiertype ADD CONSTRAINT DF_RuleResultQualifierType_RuleImplementationID_Name UNIQUE(RuleImplementationID, Name)
GO;
-- END GOV-5718 DUPLICATED COMMENTS DUE TO DUPLICATED USERS IN ASSET TABLE

--index additions
CREATE NONCLUSTERED INDEX [IX_Field_FieldTypeID_Include_date] ON [dbo].[Field] ([FieldTypeID]) INCLUDE ([UpdatedOn])
GO;



--GOV-5885
ALTER Function [dbo].[GetEmailStepRecipients]
(
	@workflowItemStepID int	
)
RETURNS varchar(max)
BEGIN
	declare @tbl table (ResourceID int, FirstName nvarchar(250), LastName nvarchar(250), Email nvarchar(500), Username nvarchar(500), DateLastLoggedIn datetime null, ResourceTypeID int, Status nvarchar(25))
	
	insert into @tbl
		select 
			R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.LastLoggedInOn as DateLastLoggedIn, 1 as ResourceTypeID, case R.State when 1 then 'Active' else 'Inactive' end as [Status]
		from workflow.itemstep s 
			outer apply s.settings.nodes('settings/emails/email') as m(c) 
			inner join reporting.Global_Resource R  on trim(m.c.value('@address', 'varchar(max)')) = R.email
		where id = @workflowItemStepID

	return (select string_agg(FirstName + ' ' + LastName,', ') as Resources from @tbl)
end
GO;

ALTER procedure [utility].[GetOwnersForWorkflowV2]
	@workflowID int,
	@workflowStepID int = 0,
	@workflowItemID int = 0
as
begin
	declare @objectId int,			
			@objectType varchar(50),
			@responsibilityTypeID int;

	declare @tbl table (ResourceID int, FirstName nvarchar(250), LastName nvarchar(250), Email nvarchar(500), Username nvarchar(500), DateLastLoggedIn datetime null, ResourceTypeID int, Status nvarchar(25))
	
	--get the responsibility for this step from the settings of the step
	select @responsibilityTypeID = settings.value('(/settings/ResponsibilityTypeID)[1]', 'int') from [workflow].[VersionStep] where id = @workflowStepID
	
	-- check object
	begin
			select @objectType = object, @objectId = objectid from [workflow].[item] where id = @workflowItemID;

			insert into @tbl
			select	R.ResourceID, 
					R.FirstName, 
					R.LastName, 
					R.Email, 
					R.Email, 
					R.LastLoggedInOn as DateLastLoggedIn, 
					1 as ResourceTypeID, 
					case R.State when 1 then 'Active' else 'Inactive' end as [Status]
			from	ResponsibilityDetails RD
					inner join reporting.Global_Resource R on RD.Object = @objectType
							and RD.ObjectID = @objectId
							and RD.ResponsibilityTypeID = @responsibilityTypeID
							and RD.ResourceID = R.ResourceID
							and R.Email not like '%?subject=%' and R.State = 1
		end
	
	-- if noone found email admins
	if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select 
					R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.LastLoggedInOn as DateLastLoggedIn, 1 as ResourceTypeID, case R.State when 1 then 'Active' else 'Inactive' end as [Status]
				from 
					reporting.Global_Resource R where isadministrator = 1 and State = 1
		end
	

	select * from @tbl
end
GO;

ALTER procedure [utility].[GetOwnersForWorkflow]
	@workflowID int,
	@workflowStepID int = 0,
	@workflowItemID int = 0
as
begin
	declare @objectId int,			
			@objectType varchar(50),
			@assetId bigint,
			@assetTypeId bigint,
			@responsibilityTypeID int,
			@issueId int;
	declare @xmlSettings xml;
	declare @responsibleSide varchar(50);

	declare @tbl table (ResourceID int, FirstName nvarchar(250), LastName nvarchar(250), Email nvarchar(500), Username nvarchar(500), DateLastLoggedIn datetime null, ResourceTypeID int, Status nvarchar(25))
	declare @responsibilityIDTbl table (RowID int not null identity(1,1) primary key, ResponsibilityTypeID int not null);
	--get the responsibility for this step from the settings of the step

	select @xmlSettings = settings from [workflow].[VersionStep] where id = @workflowStepID

	insert into @responsibilityIDTbl select T.C.value('.','int') as responsibility from @xmlSettings.nodes('(/settings/ResponsibilityTypeID)') as T(C) ;

	select @responsibleSide = upper(T.C.value('.','varchar(50)')) from @xmlSettings.nodes('(/settings/ResponsibilitySide)') as T(C);

	declare @i int
	select @i = min(RowID) from @responsibilityIDTbl
	declare @max int
	select @max = max(RowID) from @responsibilityIDTbl

	while @i <= @max and not exists (select 1 from @tbl) begin
		select @responsibilityTypeID = ResponsibilityTypeID from @responsibilityIDTbl where RowID = @i
		set @i = @i + 1

		-- check object	
		begin
			select 
				@objectType = i.object, 
				@objectId = i.objectid,
				@assetId = a.id,
				@assetTypeId = a.assetTypeId 
			from [workflow].[item] i
			left join Asset a on a.object = i.object and A.objectid = i.objectid 
			where i.id = @workflowItemID;

			if @objectType = 'Issue'
			begin				
				select 
					@issueId = i.id, 
					@objectType = i.[object], 
					@objectId = i.[objectid],
					@assetId = a.id,
					@assetTypeId = a.assetTypeID
				from Issue i
				left join Asset a on a.Object = i.Object and a.ObjectID = i.ObjectID
				where i.id = @objectId
			end

			--if the object is an intersect we need to look at the settings to see what side of the intersect to look at
			-- then we need to load the object from the corresponding side.

			if @objectType = 'Intersect'
			begin				
				if @responsibleSide = 'SUBJECT'
				begin
					select @objectType = [subject], @objectId = [subjectId] from [intersect] where id = @objectId;
				end
				else if @responsibleSide = 'OBJECT'
				begin
					select @objectType = [object], @objectId = [objectId] from [intersect] where id = @objectId;
				end
			end

			insert into @tbl
				select	R.ResourceID, 
						R.FirstName, 
						R.LastName, 
						R.Email, 
						R.Email, 
						R.LastLoggedInOn as DateLastLoggedIn, 
						1 as ResourceTypeID, 
						case R.State when 1 then 'Active' else 'Inactive' end as [Status]
				from	ResponsibilityDetail RD
						inner join reporting.Global_Resource R on 
								((RD.Object = @objectType and RD.ObjectID = @objectId) 
									or (@assetTypeId != 0 and RD.AssetID = 0 and RD.AssetTypeID = @assetTypeId))
								and RD.ResponsibilityTypeID = @responsibilityTypeID
								and RD.ResourceID = R.ResourceID
								and R.Email not like '%?subject=%' and R.State = 1
		end		
	end;

	select * from @tbl;
end
GO;