-- GOV-5361 --------------------------------
DROP TABLE [dbo].[ScoreMetric]
GO
DROP TABLE [dbo].[Score]
GO
DROP TABLE [dbo].[ScoreTypeMetricVersion]
GO
DROP TABLE [dbo].[ScoreTypeMetric]
GO
DROP TABLE [dbo].[ScoreType]
GO

create Function [dbo].[GetEmailStepRecipients]
(
	@workflowItemStepID int	
)
RETURNS varchar(max)
BEGIN
	declare @tbl table (ResourceID int, FirstName nvarchar(250), LastName nvarchar(250), Email nvarchar(500), Username nvarchar(500), DateLastLoggedIn datetime null, ResourceTypeID int, Status nvarchar(25))
	
	insert into @tbl
		select 
			R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
		from workflow.itemstep s 
			outer apply s.settings.nodes('settings/emails/email') as m(c) 
			inner join reporting.Global_Resource R  on trim(m.c.value('@address', 'varchar(max)')) = R.email
		where id = @workflowItemStepID

	return (select string_agg(FirstName + ' ' + LastName,', ') as Resources from @tbl)

end
GO

--Need to remove these before processing temporal deletions below.
DROP VIEW [utility].[IntersectAsset]
GO
DROP VIEW [utility].[ArtifactAssetParent]
GO
DROP VIEW [utility].[ArtifactAssetParentIntermediate]
GO

-- GOV-5387
ALTER TABLE Asset SET ( SYSTEM_VERSIONING = OFF  )
GO
drop table Asset_History
GO

ALTER TABLE Asset DROP PERIOD FOR SYSTEM_TIME; 
alter table Asset drop column [EffectiveStartDate]
alter table Asset drop column [EffectiveEndDate]
GO

create VIEW [utility].[IntersectAsset]
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

CREATE VIEW [utility].[ArtifactAssetParentIntermediate]
WITH SCHEMABINDING  
AS  
    select	a_o.ID as AssetID,		
			I.SubjectID as ParentArtifactID
	from
		dbo.[Intersect] I
		inner join dbo.Asset a_o on I.[Object] = a_o.[Object] and I.[ObjectID] = a_o.ObjectID	
		inner join dbo.[IntersectType] IT on I.IntersectTypeID = IT.ID
		inner join dbo.[Predicate] P on P.ID = IT.PredicateID and P.[Type] = 3		
	where I.[Object] = 'Artifact'
GO

create VIEW [utility].[ArtifactAssetParent]
WITH SCHEMABINDING  
AS  
    select	
		aim.AssetID,
		aim.ParentArtifactID,
		IA.ID as ParentAssetID
	from [utility].[ArtifactAssetParentIntermediate] aim
		inner join dbo.Asset IA on IA.Object = 'Artifact' and aim.ParentArtifactID = IA.ObjectID 	
GO

--Need to remove this before processing temporal deletions below.
DROP VIEW [dbo].[ResponsibilityAllAsset]
GO

-- GOV-5387
ALTER TABLE [AssetType] SET ( SYSTEM_VERSIONING = OFF  )
GO
drop table AssetType_History
GO

ALTER TABLE AssetType DROP PERIOD FOR SYSTEM_TIME; 
alter table AssetType drop column [EffectiveStartDate]
alter table AssetType drop column [EffectiveEndDate]
GO

-- GOV-5388
ALTER TABLE [Field] SET ( SYSTEM_VERSIONING = OFF  )
GO
drop table Field_History
GO

ALTER TABLE Field DROP PERIOD FOR SYSTEM_TIME; 

alter table Field add UpdatedOn datetime constraint DF_Field_UpdatedOn default(getutcdate()) not null
GO
disable trigger [Field_AfterUpsert] on Field
GO
update Field set UpdatedOn = EffectiveStartDate
GO
enable trigger [Field_AfterUpsert] on Field
GO
alter table Field drop column [EffectiveStartDate]
alter table Field drop column [EffectiveEndDate]
GO

ALTER TABLE [FieldType] SET ( SYSTEM_VERSIONING = OFF  )
GO
drop table FieldType_History
GO

ALTER TABLE FieldType DROP PERIOD FOR SYSTEM_TIME; 
alter table FieldType drop column [EffectiveStartDate]
alter table FieldType drop column [EffectiveEndDate]
GO

-- GOV-5176
ALTER TABLE ResponsibilityTypeRelationOverrideItem SET ( SYSTEM_VERSIONING = OFF  )
GO
drop table ResponsibilityTypeRelationOverrideItem_History
GO

ALTER TABLE ResponsibilityTypeRelationOverrideItem DROP PERIOD FOR SYSTEM_TIME; 
alter table ResponsibilityTypeRelationOverrideItem drop column [EffectiveStartDate]
alter table ResponsibilityTypeRelationOverrideItem drop column [EffectiveEndDate]
GO

ALTER TABLE ResponsibilityTypeRelationRuleResult SET ( SYSTEM_VERSIONING = OFF  )
GO
drop table ResponsibilityTypeRelationRuleResult_History
GO

ALTER TABLE ResponsibilityTypeRelationRuleResult DROP PERIOD FOR SYSTEM_TIME; 
alter table ResponsibilityTypeRelationRuleResult drop column [EffectiveStartDate]
alter table ResponsibilityTypeRelationRuleResult drop column [EffectiveEndDate]
GO

CREATE VIEW [dbo].[ResponsibilityAllAsset] with SCHEMABINDING as 
	-- users
	(select	O.RuleID,
			O.ResponsibilityTypeID,
			RT.Name as ResponsibilityTypeName,
			O.AssetID,
			O.AssetTypeID,
			O.SecurityAssetID as ResourceID,
			R.FirstName + ' ' + R.LastName as ResourceName,
			O.SecurityAsset,
			O.SecurityAssetID,
			(R.FirstName + ' ' + R.LastName) as SecurityAssetName,  
			O.Context,
			O.ApplyToType,
			O.PermissionsBitMask,
			O.IsVisible,
			O.OverrideID,			
			T.Object as [Type],
			T.ObjectID as TypeID
	from	dbo.ResponsibilityTypeRelationRuleResult O
			inner join dbo.ResponsibilityType RT on RT.ID = O.ResponsibilityTypeID
			inner join dbo.AssetType T on T.ID = O.AssetTypeID			
			inner join reporting.Global_Resource R on R.ResourceID = O.SecurityAssetID
	where	O.Overridden = 0 and O.SecurityAsset != 'G' and O.SecurityAsset !='O')
	union
	(select	O.RuleID,
			O.ResponsibilityTypeID,
			RT.Name as ResponsibilityTypeName,
			O.AssetID,
			O.AssetTypeID,
			RG.ResourceID as ResourceID,
			R.FirstName + ' ' + R.LastName as ResourceName,
			O.SecurityAsset,
			O.SecurityAssetID,
			G.Name as SecurityAssetName,  
			O.Context,
			O.ApplyToType,
			O.PermissionsBitMask,
			O.IsVisible,
			O.OverrideID,			
			T.Object as [Type],
			T.ObjectID as TypeID
	from	dbo.ResponsibilityTypeRelationRuleResult O
			inner join dbo.ResponsibilityType RT on RT.ID = O.ResponsibilityTypeID
			inner join dbo.AssetType T on T.ID = O.AssetTypeID			
			inner join dbo.[Group] G on G.ID = O.SecurityAssetID
			inner join dbo.ResourceGroup RG on RG.GroupID = G.ID			
			inner join reporting.Global_Resource R on R.ResourceID = RG.ResourceID
	where	O.Overridden = 0 and O.SecurityAsset = 'G')
	union
	(select	O.RuleID,
			O.ResponsibilityTypeID,
			RT.Name as ResponsibilityTypeName,
			O.AssetID,
			O.AssetTypeID,
			RD.ResourceID as ResourceID,
			R.FirstName + ' ' + R.LastName as ResourceName,
			O.SecurityAsset,
			O.SecurityAssetID,
			D.Name as SecurityAssetName,  
			O.Context,
			O.ApplyToType,
			O.PermissionsBitMask,
			O.IsVisible,
			O.OverrideID,			
			T.Object as [Type],
			T.ObjectID as TypeID
	from	dbo.ResponsibilityTypeRelationRuleResult O
			inner join dbo.ResponsibilityType RT on RT.ID = O.ResponsibilityTypeID
			inner join dbo.AssetType T on T.ID = O.AssetTypeID			
			inner join dbo.[Organization] D on O.SecurityAsset = 'O' and D.ID = O.SecurityAssetID
			inner join dbo.OrganizationResource RD on RD.OrganizationID = D.ID
			inner join reporting.Global_Resource R on R.ResourceID = RD.ResourceID
	where	O.Overridden = 0 and O.SecurityAsset = 'O')
GO

-- GOV-5385
update IntersectType set [uid] = newid() where [uid] = '00000000-0000-0000-0000-000000000000'
GO

-- Convert uid on AssetCrossReference
DROP INDEX [IX_AssetCrossReference_uid] ON [dbo].[AssetCrossReference]
GO

DROP INDEX [IX_AssetCrossReference_uid_DataSource] ON [dbo].[AssetCrossReference]
GO

delete assetCrossReference where TRY_CONVERT(uniqueidentifier, [uid]) is null
GO

ALTER TABLE [dbo].[AssetCrossReference] DROP CONSTRAINT [PK_AssetCrossReference] WITH ( ONLINE = OFF )
GO

alter table AssetCrossReference alter column [uid] uniqueidentifier not null
GO

ALTER TABLE [dbo].[AssetCrossReference] ADD  CONSTRAINT [PK_AssetCrossReference] PRIMARY KEY CLUSTERED 
(
	[DataSource] ASC,
	[Type] ASC,
	[ExternalID] ASC,
	[uid] ASC
)
GO

CREATE NONCLUSTERED INDEX [IX_AssetCrossReference_uid] ON [dbo].[AssetCrossReference]
(
	[uid] ASC
)
GO

CREATE NONCLUSTERED INDEX [IX_AssetCrossReference_uid_DataSource] ON [dbo].[AssetCrossReference]
(
	[uid] ASC,
	[DataSource] ASC
)
GO

-- GOV-5416
ALTER procedure [integration].[ProcessExecutionAssetType]
--declare	
	@ExecutionID bigint,
	@SynchedAssetTypeID int,
	@AssetTypeID int,
	@ResourceID int,
	@Section int --0 = Asset, 1 = Field, 2 = Relationships, 3 = Responsibilities
--set @ExecutionID = 2
--set @SynchedAssetTypeID = 12
--set @AssetTypeID = 29
--set @ResourceID = 0
--set @Section = 0
as
begin
	set nocount on;

	--line below used for testing.
	--declare	 @ExecutionID bigint = 6002, @SynchedAssetTypeID int = 1, @AssetTypeID int = 3, @ResourceID int = 0, @Section int = 2

	declare @archived bit = 0

	select	@archived = Archived from integration.Execution where ID = @ExecutionID

	if @archived = 1 
	begin
		RAISERROR (N'This exection is marked as Archived and can no longer be processed.', 10, 1);
	end

	-- BEGIN CORE ASSET
	if @Section = 0
	begin
		declare	@Object varchar(50),
				@ObjectID int,
				@OptionalID int,
				@TriggerTopicMessage bit,
				@Level int,
				@ParentIntersectTypeID int,
				@SourceSystemCount int,
				@PulledCount int;

		select	@Object = [Object],
				@ObjectID = [ObjectID],
				@OptionalID = [OptionalID],
				@TriggerTopicMessage = [TriggerTopicMessage],
				@Level = [Level]
		from	integration.SynchedAssetType
		where	ID = @SynchedAssetTypeID;

		select	@ParentIntersectTypeID = IT.ID
		from	IntersectType IT
				inner join [Predicate] P on P.ID = IT.PredicateID and IT.Object = @Object and IT.ObjectID = @ObjectID and P.[Type] = case @Object when 'PolicyType' then 4 when 'TaxonomyType' then 4 else 3 end

		drop table if exists #Assets;
		create table #Assets (AssetTypeID int, AssetID bigint, [Object] varchar(50), ObjectID int, [Type] varchar(50), TypeID int, SourceID nvarchar(250), ParentSourceID nvarchar(250), [Action] char(1), Error nvarchar(max));
		CREATE CLUSTERED INDEX CIX_TempAssets ON #Assets (SourceID)

		--Get counts ------
		select	@SourceSystemCount = CurrentSourceAssetCount
		from	[integration].[ExecutionAssetType]
		where	ExecutionID = @ExecutionID 
				and SynchedAssetTypeID = @SynchedAssetTypeID;

		select	@PulledCount = count(1)
		from	[integration].[ExecutionAsset]
		where	ExecutionID = @ExecutionID 
				and SynchedAssetTypeID = @SynchedAssetTypeID;
		-------------------

		--Get distinct list of assets
		insert into #Assets (AssetTypeID, AssetID, [Object], ObjectID, [Type], TypeID, SourceID)
			select		A.AssetTypeID, 
						A.ID,
						A.Object,
						A.ObjectID,
						@Object,
						@ObjectID,
						R.SourceID 
			from		integration.ExecutionAsset R --cross apply OPENJSON(R.RawObject) U
						left join Asset A on A.AssetTypeID = @AssetTypeID and A.SourceID = R.SourceID
			where		R.ExecutionID = @ExecutionID 
						and R.SynchedAssetTypeID = @SynchedAssetTypeID
						--and U.[key] = 'modified_on'
			group by	A.AssetTypeID, 
						A.ID,
						A.Object,
						A.ObjectID,
						R.SourceID;

		drop table if exists #Context;
		create table #Context (SourceID nvarchar(250), RawValue nvarchar(max), [ParentContextPosition] int);

		insert into #Context
			select	A.SourceID,
					RF.[value],
					F.[ParentContextPosition]
			from	integration.ExecutionAsset A
					cross apply OPENJSON(A.RawObject) RF
					inner join [integration].[SynchedAssetTypeFieldItem] F on F.SynchedAssetTypeID = A.SynchedAssetTypeID and F.SourceField = RF.[key] COLLATE DATABASE_DEFAULT
					inner join integration.SynchedAssetType SAT on SAT.ID = A.SynchedAssetTypeID
					left join FieldType FT on FT.AssetTypeID = SAT.AssetTypeID and FT.Name = F.TargetField
			where	A.ExecutionID = @ExecutionID--145 
					and A.SynchedAssetTypeID = @SynchedAssetTypeID
					and RF.[key] = '_context' and F.ArrayValueDelimiter is null;

		BEGIN	-- Process ParentSourceID
			declare @ParentContextPosition int;
			select	top 1
					@ParentContextPosition = [ParentContextPosition]
			from	#Context

			if @ParentContextPosition = 99
			begin
				update	T
				set		T.ParentSourceID = S.ParentSourceID
				from	#Assets T
						inner join	(
									select		J.SourceID,
												max(C.[key]) as [key]
									from		#Context J
												cross apply OPENJSON(J.RawValue) C
												cross apply OPENJSON(c.value) with (ParentSourceID nvarchar(500) '$._id') P
									group by	J.SourceID
									) MContext on MContext.SourceID = T.SourceID
						inner join (
									select		J.SourceID,
												P.ParentSourceID,
												C.[key]
									from		#Context J
												cross apply OPENJSON(J.RawValue) C
												cross apply OPENJSON(c.value) with (ParentSourceID nvarchar(500) '$._id') P
									) S on S.SourceID = T.SourceID and S.[key] = MContext.[key];
			end
			else
			begin
				update	T
				set		T.ParentSourceID = S.ParentSourceID
				from	#Assets T
						inner join (
									select		J.SourceID,
												P.ParentSourceID,
												C.[key]
									from		#Context J
												cross apply OPENJSON(J.RawValue) C
												cross apply OPENJSON(c.value) with (ParentSourceID nvarchar(500) '$._id') P
									where		C.[key] = J.[ParentContextPosition]
									) S on S.SourceID = T.SourceID;
			end
		END

		--See which assets do not yet exist, that need to be added.
		update	#Assets
		set		[Action] = IIF(AssetID is null, 'A', 'U');

		--BEGIN Deletion query logic. See which ones need to be deleted, IF FULL REFRESH ONLY.
		declare @DeleteAssetTypeID int
		select	@DeleteAssetTypeID = AssetTypeID from integration.ExecutionAssetType E inner join integration.SynchedAssetType S on S.ID = E.SynchedAssetTypeID and E.ExecutionID = @ExecutionID and E.SynchedAssetTypeID = @SynchedAssetTypeID and E.IsFullRefresh = 1

		declare	@HasFieldToConsiderWhenDeleting bit

		select	@HasFieldToConsiderWhenDeleting = case 
													when count(1) > 0 then cast(1 as bit)
													else cast(0 as bit)
												  end
		from	integration.SynchedAssetTypeFieldItem 
		where	SynchedAssetTypeID = @SynchedAssetTypeID 
				and ConsiderWhenDeleting = 1

		--We get the asset type ID here again so we can verify if this is a full refresh. if not a full refresh, then we skip the query process below.
		if @DeleteAssetTypeID is not null
		begin
			-- First, get ones where there is no level to deal with, AND have no default value field to worry about.
			insert into #Assets
				select	D.AssetTypeID,
						D.ID,
						D.Object,
						D.ObjectID,
						@Object,
						@ObjectID,
						D.SourceID,
						NULL,
						'D' as [Action],
						NULL
				from	Asset D
						left join #Assets S on S.AssetTypeID = D.AssetTypeID and S.SourceID = D.SourceID
				where	S.SourceID is null
						and D.AssetTypeID = @DeleteAssetTypeID
						and @Level is null
						and @HasFieldToConsiderWhenDeleting = 0
				group by D.AssetTypeID, D.ID, D.Object, D.ObjectID, D.SourceID;

			-- Next, get ones where there is no level to deal with, and HAVE a default value field to worry about.
			insert into #Assets
				select	D.AssetTypeID,
						D.ID,
						D.Object,
						D.ObjectID,
						@Object,
						@ObjectID,
						D.SourceID,
						NULL,
						'D' as [Action],
						NULL
				from	Asset D
						inner join Field CF on CF.AssetID = D.ID
						inner join FieldType CFT on CFT.AssetTypeID = D.AssetTypeID and CFT.ID = CF.FieldTypeID
						inner join integration.SynchedAssetTypeFieldItem SF on SF.SynchedAssetTypeID = @SynchedAssetTypeID and SF.ConsiderWhenDeleting = 1 and SF.TargetField = CFT.Name and CF.Value = SF.DefaultValue
						left join #Assets S on S.AssetTypeID = D.AssetTypeID and S.SourceID = D.SourceID
				where	S.SourceID is null
						and D.AssetTypeID = @DeleteAssetTypeID
						and @Level is null
						and @HasFieldToConsiderWhenDeleting = 1
				group by D.AssetTypeID, D.ID, D.Object, D.ObjectID, D.SourceID;

			-- Next, get ones where there is a level to deal with, and no default value to consider.
			insert into #Assets
				select	D.AssetTypeID,
						D.ID,
						D.Object,
						D.ObjectID,
						@Object,
						@ObjectID,
						D.SourceID,
						NULL,
						'D' as [Action],
						NULL
				from	Asset D
						cross apply dbo.GetAssetLevelById(D.ID) L
						left join #Assets S on S.AssetTypeID = D.AssetTypeID and S.SourceID = D.SourceID
				where	S.SourceID is null
						and D.AssetTypeID = @DeleteAssetTypeID
						and L.[Level] = @Level
						and @HasFieldToConsiderWhenDeleting = 0
				group by D.AssetTypeID, D.ID, D.Object, D.ObjectID, D.SourceID;

			-- Last, get ones where there is a level to deal with, and HAS default value to consider.
			insert into #Assets
				select	D.AssetTypeID,
						D.ID,
						D.Object,
						D.ObjectID,
						@Object,
						@ObjectID,
						D.SourceID,
						NULL,
						'D' as [Action],
						NULL
				from	Asset D
						cross apply dbo.GetAssetLevelById(D.ID) L
						inner join Field CF on CF.AssetID = D.ID
						inner join FieldType CFT on CFT.AssetTypeID = D.AssetTypeID and CFT.ID = CF.FieldTypeID
						inner join integration.SynchedAssetTypeFieldItem SF on SF.SynchedAssetTypeID = @SynchedAssetTypeID and SF.ConsiderWhenDeleting = 1 and SF.TargetField = CFT.Name and CF.Value = SF.DefaultValue
						left join #Assets S on S.AssetTypeID = D.AssetTypeID and S.SourceID = D.SourceID
				where	S.SourceID is null
						and D.AssetTypeID = @DeleteAssetTypeID
						and L.[Level] = @Level
						and @HasFieldToConsiderWhenDeleting = 1
				group by D.AssetTypeID, D.ID, D.Object, D.ObjectID, D.SourceID;
		end
		--END Deletion query logic.

		BEGIN --Do actual deletes
			IF @SourceSystemCount = @PulledCount
				BEGIN	--proceed with delete
					DROP TABLE IF EXISTS #deletes
					create table #deletes (ID int identity, AssetID bigint, Object varchar(50), ObjectID int)
					CREATE CLUSTERED INDEX [CIX_TempDeletes] ON #deletes ( ID ASC )

					insert into #deletes
						select AssetID, Object, ObjectID from #Assets where [Action] = 'D';

					declare @current int = 1,
							@max int,
							@o varchar(50),
							@oID int
					select	@max = coalesce(max(ID),0) from #deletes
					while	@current <= @max
					begin
						select	@o = Object, @oID = ObjectID from #deletes where ID  = @current
						exec DeleteObject @o, @oID, 0
						set		@current = @current + 1
					end;
					begin try
						merge	integration.ExecutionAssetTypeMetric as T
						using	(
								select		count(1) as GovernDeletedAssetCount
								from		#deletes O
								) S
						on		(T.ExecutionID = @ExecutionID and T.SynchedAssetTypeID = @SynchedAssetTypeID)
						when matched then
							update set
							T.GovernDeletedAssetCount = S.GovernDeletedAssetCount
						when not matched by target then
							insert  (
									ExecutionID,
									SynchedAssetTypeID,
									GovernDeletedAssetCount
									)
							values  (
									@ExecutionID,
									@SynchedAssetTypeID,
									GovernDeletedAssetCount
									);
					end try
					begin catch
						update	integration.ExecutionAssetType
						set		ErrorMessage = coalesce(ErrorMessage, '') + '; ' + ERROR_MESSAGE()
						where	ExecutionID = @ExecutionID and
								SynchedAssetTypeID = @SynchedAssetTypeID
					end catch
				END
			ELSE
				BEGIN
					update	integration.ExecutionAssetType
					set		ErrorMessage = coalesce(ErrorMessage + '; ', '') + 'The reported asset count from the source system did not match what we were able to pull from the API. Will not proceed with deletion.'
					where	ExecutionID = @ExecutionID and
							SynchedAssetTypeID = @SynchedAssetTypeID				
				END
		END

		-- Perform INSERTS and UPDATES

		if @Object = 'ArtifactType'
		begin
			insert into Artifact (ArtifactTypeID, SourceID, CreatedBy, UpdatedBy, Visible)
				select	@ObjectID,
						SourceID,
						@ResourceID,
						@ResourceID,
						1
				from	#Assets
				where	[Action] = 'A'

			update	T
			set		T.Object = 'Artifact',
					T.ObjectID = S.ID
			from	#Assets T
					inner join Artifact S on S.ArtifactTypeID = @ObjectID and S.SourceID = T.SourceID

			if @ParentIntersectTypeID is not null
			begin
				insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, CreatedBy, UpdatedBy)
					select	@ParentIntersectTypeID as IntersectTypeID,
							P.Object as Subject, P.ObjectID as SubjectID,
							C.Object, C.ObjectID,
							@ResourceID, @ResourceID
					from	#Assets C
							inner join Asset P on P.SourceID = C.ParentSourceID
							left join [Intersect] I on I.IntersectTypeID = @ParentIntersectTypeID and I.Subject = P.Object and I.SubjectID = P.ObjectID and I.Object = C.Object and I.ObjectID = C.ObjectID
					where	C.[Action] in ('A', 'U')
							and I.ID is null
			end
		end
		if @Object = 'FusionAttributeType'
		begin
			insert into FusionAttribute (FusionAttributeTypeID, FusionID, ParentID, SourceID, Name)
				select	@ObjectID,
						@OptionalID,
						P.ObjectID,
						C.SourceID,
						RF.[value] as Name
				from	#Assets C
						inner join integration.ExecutionAsset EA on EA.ExecutionID = @ExecutionID and EA.SynchedAssetTypeID = @SynchedAssetTypeID and EA.SourceID = C.SourceID
						cross apply OPENJSON(EA.RawObject) RF
						left join Asset P on P.SourceID = C.ParentSourceID
				where	[Action] = 'A'
						and RF.[key] = '_name'

			update	T
			set		T.Object = 'FusionAttribute',
					T.ObjectID = S.ID
			from	#Assets T
					inner join FusionAttribute S on S.FusionAttributeTypeID = @ObjectID and S.SourceID = T.SourceID

			update	T
			set		T.ParentID = S.ParentID
			from	FusionAttribute T
					inner join (
						select	P.ObjectID as ParentID,
								C.ObjectID as ID
						from	#Assets C
								inner join Asset P on P.SourceID = C.ParentSourceID
						where	[Action] = 'U'
					) S on S.ID = T.ID

			if @ParentIntersectTypeID is not null
			begin
				insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, CreatedBy, UpdatedBy)
					select	@ParentIntersectTypeID as IntersectTypeID,
							P.Object as Subject, P.ObjectID as SubjectID,
							C.Object, C.ObjectID,
							@ResourceID, @ResourceID
					from	#Assets C
							inner join Asset P on P.SourceID = C.ParentSourceID
							left join [Intersect] I on I.IntersectTypeID = @ParentIntersectTypeID and I.Subject = P.Object and I.SubjectID = P.ObjectID and I.Object = C.Object and I.ObjectID = C.ObjectID
					where	C.[Action] in ('A', 'U')
							and I.ID is null
			end
		end
		if @Object = 'PolicyType'
		begin
			insert into [Policy] (PolicyTypeID, SourceID, UpdatedBy, Visible)
				select	@ObjectID,
						SourceID,
						@ResourceID,
						1
				from	#Assets
				where	[Action] = 'A'

			update	T
			set		T.Object = 'Policy',
					T.ObjectID = S.ID
			from	#Assets T
					inner join [Policy] S on S.PolicyTypeID = @ObjectID and S.SourceID = T.SourceID

			if @ParentIntersectTypeID is not null
			begin
				insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, CreatedBy, UpdatedBy)
					select	@ParentIntersectTypeID as IntersectTypeID,
							P.Object as Subject, P.ObjectID as SubjectID,
							C.Object, C.ObjectID,
							@ResourceID, @ResourceID
					from	#Assets C
							inner join Asset P on P.SourceID = C.ParentSourceID
							left join [Intersect] I on I.IntersectTypeID = @ParentIntersectTypeID and I.Subject = P.Object and I.SubjectID = P.ObjectID and I.Object = C.Object and I.ObjectID = C.ObjectID
					where	C.[Action] in ('A', 'U')
							and I.ID is null
			end
		end 
		if @Object = 'ReferenceItemType'
		begin
			insert into ReferenceItem (ReferenceItemTypeID, SourceID, CreatedBy, UpdatedBy, Code, Visible)
				select	@ObjectID,
						C.SourceID,
						@ResourceID,
						@ResourceID,
						RF.[value],
						1
				from	#Assets C
						inner join integration.ExecutionAsset EA on EA.ExecutionID = @ExecutionID and EA.SynchedAssetTypeID = @SynchedAssetTypeID and EA.SourceID = C.SourceID
						cross apply OPENJSON(EA.RawObject) RF
						left join Asset P on P.SourceID = C.ParentSourceID
				where	[Action] = 'A'
						and RF.[key] = '_name'

			update	T
			set		T.Object = 'ReferenceItem',
					T.ObjectID = S.ID
			from	#Assets T
					inner join ReferenceItem S on S.ReferenceItemTypeID = @ObjectID and S.SourceID = T.SourceID

			--if @ParentIntersectTypeID is not null
			--begin
			--	insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, CreatedBy, UpdatedBy)
			--		select	@ParentIntersectTypeID as IntersectTypeID,
			--				P.Object as Subject, P.ObjectID as SubjectID,
			--				C.Object, C.ObjectID,
			--				@ResourceID, @ResourceID
			--		from	#Assets C
			--				inner join Asset P on P.SourceID = C.ParentSourceID
			--		where	C.[Action] = 'A'
			--end
		end
		if @Object = 'RuleType'
		begin
			insert into dbo.[Rule] (RuleTypeID, SourceID, CreatedBy, UpdatedBy, Visible)
				select	@ObjectID,
						SourceID,
						@ResourceID,
						@ResourceID,
						1
				from	#Assets
				where	[Action] = 'A'

			update	T
			set		T.Object = 'Rule',
					T.ObjectID = S.ID
			from	#Assets T
					inner join [Rule] S on S.RuleTypeID = @ObjectID and S.SourceID = T.SourceID

			if @ParentIntersectTypeID is not null
			begin
				insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, CreatedBy, UpdatedBy)
					select	@ParentIntersectTypeID as IntersectTypeID,
							P.Object as Subject, P.ObjectID as SubjectID,
							C.Object, C.ObjectID,
							@ResourceID, @ResourceID
					from	#Assets C
							inner join Asset P on P.SourceID = C.ParentSourceID
							left join [Intersect] I on I.IntersectTypeID = @ParentIntersectTypeID and I.Subject = P.Object and I.SubjectID = P.ObjectID and I.Object = C.Object and I.ObjectID = C.ObjectID
					where	C.[Action] in ('A', 'U')
							and I.ID is null
			end
		end
		if @Object = 'TaxonomyType'
		begin
			insert into Taxonomy (TaxonomyTypeID, SourceID, UpdatedBy, Visible)
				select	@ObjectID,
						SourceID,
						@ResourceID,
						1
				from	#Assets
				where	[Action] = 'A'

			update	T
			set		T.Object = 'Taxonomy',
					T.ObjectID = S.ID
			from	#Assets T
					inner join Taxonomy S on S.TaxonomyTypeID = @ObjectID and S.SourceID = T.SourceID

			if @ParentIntersectTypeID is not null
			begin
				insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, CreatedBy, UpdatedBy)
					select	@ParentIntersectTypeID as IntersectTypeID,
							P.Object as Subject, P.ObjectID as SubjectID,
							C.Object, C.ObjectID,
							@ResourceID, @ResourceID
					from	#Assets C
							inner join Asset P on P.SourceID = C.ParentSourceID
							left join [Intersect] I on I.IntersectTypeID = @ParentIntersectTypeID and I.Subject = P.Object and I.SubjectID = P.ObjectID and I.Object = C.Object and I.ObjectID = C.ObjectID
					where	C.[Action] in ('A', 'U')
							and I.ID is null
			end
		end

		-- Update Asset instance info
		update	T
		set		T.AssetID = S.ID,
				T.Object = S.Object,
				T.ObjectID = S.ObjectID
		from	#Assets T
				inner join Asset S on S.AssetTypeID = T.AssetTypeID and S.SourceID = T.SourceID and T.[Action] = 'A' and T.AssetID is null;

		-- Insert parent/child relationships we were not able to resolve.
		if @ParentIntersectTypeID is not null
		begin
			insert into [integration].[ExecutionUnresolvedRelationItem] (
				ExecutionID, IntersectTypeID, SourceID, 
				ObjectAssetTypeID, ObjectSourceID, ObjectAssetID, Object, ObjectID,
				[Action]
			)
					select	@ExecutionID,
							@ParentIntersectTypeID,
							C.SourceID,
							@AssetTypeID, C.SourceID, C.AssetID, C.Object, C.ObjectID,
							C.[Action]
					from	#Assets C
					where	C.ParentSourceID not in (select SourceID from Asset);
		end

		select	SourceID,
				AssetID,
				Object,
				ObjectID,
				Type,
				TypeID,
				[Action]
		from	#Assets
		where	[Action] = 'A' and [Action] is not null
	end
	-- END CORE ASSET

	-- BEGIN FIELDS
	if @Section = 1
	begin

		drop table if exists #Field_Step1;
		create table #Field_Step1 (
			AssetTypeID int, SourceID nvarchar(250), 
			AssetID bigint, Object varchar(50), ObjectID int,
			FieldTypeID int, SourceFieldName nvarchar(250), RawValue nvarchar(max), 
			[ParentContextPosition] int, [IsArray] bit, DefaultValue nvarchar(250), [ArrayValueDelimiter] varchar(10), [ArrayValueFieldName] varchar(50),
			NewValue nvarchar(max), [Action] char(1)
		);
		--CREATE NONCLUSTERED INDEX IX_TempField_Step1 ON #Field_Step1 ([SynchedAssetTypeRelationItemID],[Type]) INCLUDE ([IsSubject],[SourceID],[ID]);

		insert into #Field_Step1
			select	SAT.AssetTypeID,
					EA.SourceID,
					A.ID, A.Object, A.ObjectID,
					FT.ID,
					RF.[key] as FieldName,
					RF.[value] as FieldValue,
					F.[ParentContextPosition], 
					F.[IsArray], 
					F.DefaultValue, 
					F.[ArrayValueDelimiter], 
					F.[ArrayValueFieldName],
					NULL, NULL
			from	integration.ExecutionAsset EA
					cross apply OPENJSON(EA.RawObject) RF
					inner join [integration].[SynchedAssetTypeFieldItem] F on F.SynchedAssetTypeID = EA.SynchedAssetTypeID and F.SourceField = RF.[key] COLLATE DATABASE_DEFAULT
					inner join integration.SynchedAssetType SAT on SAT.ID = EA.SynchedAssetTypeID
					inner join Asset A on A.AssetTypeID = SAT.AssetTypeID and A.SourceID = EA.SourceID
					left join FieldType FT on FT.AssetTypeID = SAT.AssetTypeID and FT.Name = F.TargetField
			where	EA.ExecutionID = @ExecutionID--145 
					and EA.SynchedAssetTypeID = @SynchedAssetTypeID
					and EA.RawObject is not null;

		BEGIN	-- Process array value-delimited fields
			update	T
			set		NewValue = S.NewValue
			from	#Field_Step1 T
					inner join	(
								select		J.SourceID,
											J.FieldTypeID,
											STRING_AGG(P.Val, ' / ')  as NewValue
											--C.[key]
								from		#Field_Step1 J
											cross apply OPENJSON(J.RawValue) C
											cross apply OPENJSON(c.value) with (Val nvarchar(500) '$._name') P
								where		SourceFieldName = '_context'
											and ArrayValueDelimiter is not null
											and ArrayValueFieldName is not null
								group by	J.SourceID,
											J.FieldTypeID
								) S on S.SourceID = T.SourceID and S.FieldTypeID = T.FieldTypeID;
		END

		-- Do this BEFORE enum-step.
		BEGIN	-- Process non-array fields
			update	#Field_Step1
			set		NewValue =	case
									when RawValue is null and DefaultValue is not null then DefaultValue
									when RawValue is null and DefaultValue is null then null
									when RawValue = '' and DefaultValue is not null then DefaultValue
									when RawValue = '' and DefaultValue is null then null
									else RawValue
								end
			where	FieldTypeID is not null
					and IsArray = 0;
		END

		BEGIN	-- Process enum-based fields
			declare @Enums nvarchar(max)
			select	@Enums = EnumFieldValues from integration.ExecutionAssetType where ExecutionID = @ExecutionID and SynchedAssetTypeID = @SynchedAssetTypeID
			drop table if exists #EnumValueTable;
			create table #EnumValueTable (PropertyName nvarchar(250), Code nvarchar(100), DisplayValue nvarchar(500))
			CREATE CLUSTERED INDEX CIX_TempEnumValueTable ON #EnumValueTable (PropertyName,Code);
			insert into #EnumValueTable
				select * from OPENJSON(@Enums) with (PropertyName nvarchar(250) '$.PropertyName', Code nvarchar(100) '$.Code', DisplayValue nvarchar(500) '$.DisplayValue')

			-- Parse non-array enum fields.
			update	T
			set		T.NewValue = S.NewValue
			from	#Field_Step1 T
					inner join	(
								select		SourceID,
											SourceFieldName,
											E.DisplayValue as NewValue
								from		#Field_Step1 J
											inner join #EnumValueTable E on E.PropertyName = J.SourceFieldName and E.Code = J.RawValue
								where		FieldTypeID is not null
											and IsArray = 0
								) S on S.SourceID = T.SourceID and S.SourceFieldName = T.SourceFieldName;


			-- Parse array-based enum fields.
			update	T
			set		T.NewValue = S.NewValue
			from	#Field_Step1 T
					inner join	(
								select		SourceID,
											SourceFieldName,
											STRING_AGG(E.DisplayValue, ', ') WITHIN GROUP ( ORDER BY E.DisplayValue ASC ) as NewValue
								from		#Field_Step1 J
											cross apply OPENJSON(J.RawValue) RV
											--cross apply OPENJSON(@Enums) with (PropertyName nvarchar(250) '$.PropertyName', Code nvarchar(50) '$.Code', DisplayValue nvarchar(500) '$.DisplayValue') E 
											inner join #EnumValueTable E on E.PropertyName = J.SourceFieldName and E.Code = RV.value
								where		FieldTypeID is not null
											and IsArray = 1
											and ArrayValueDelimiter is null
											--and E.PropertyName = J.SourceFieldName
											--and E.Code = RV.value
								group by	AssetTypeID,
											SourceID,
											SourceFieldName
								) S on S.SourceID = T.SourceID and S.SourceFieldName = T.SourceFieldName;
		END

		BEGIN	-- Update modification properties on impacted objects
			declare @ObjToUpdate varchar(50)
			drop table if exists #ObjectsToUpdateDateOn
			create table #ObjectsToUpdateDateOn (Object varchar(50), ObjectID int);
			CREATE CLUSTERED INDEX CIX_TempObjectsToUpdateDateOn ON #ObjectsToUpdateDateOn (ObjectID);
			insert into #ObjectsToUpdateDateOn
				select	N.Object,
						N.ObjectID
				from	#Field_Step1 N
						left join Field E on E.ObjectType = N.Object and E.ObjectID = N.ObjectID and E.FieldTypeID = N.FieldTypeID
				where	N.FieldTypeID is not null
						and (
							(E.ID is not null and N.NewValue <> E.Value)
							or
							E.ID is null --Field does not yet exist
							);

			select	top 1
					@ObjToUpdate = Object
			from	#ObjectsToUpdateDateOn

			if @ObjToUpdate = 'Artifact'
			begin
				update	T
				set		T.[UpdatedBy] = @ResourceID,
						T.[UpdatedOn] = getutcdate()
				from	Artifact T
						inner join #ObjectsToUpdateDateOn S on S.ObjectID = T.ID;
			end

			if @ObjToUpdate = 'Policy'
			begin
				update	T
				set		T.[UpdatedBy] = @ResourceID,
						T.[UpdatedOn] = getutcdate()
				from	[Policy] T
						inner join #ObjectsToUpdateDateOn S on S.ObjectID = T.ID;
			end

			if @ObjToUpdate = 'ReferenceItem'
			begin
				update	T
				set		T.[UpdatedBy] = @ResourceID,
						T.[UpdatedOn] = getutcdate()
				from	ReferenceItem T
						inner join #ObjectsToUpdateDateOn S on S.ObjectID = T.ID;
			end

			if @ObjToUpdate = 'Rule'
			begin
				update	T
				set		T.[UpdatedBy] = @ResourceID,
						T.[UpdatedOn] = getutcdate()
				from	dbo.[Rule] T
						inner join #ObjectsToUpdateDateOn S on S.ObjectID = T.ID;
			end

			if @ObjToUpdate = 'Taxonomy'
			begin
				update	T
				set		T.[UpdatedBy] = @ResourceID,
						T.[UpdatedOn] = getutcdate()
				from	Taxonomy T
						inner join #ObjectsToUpdateDateOn S on S.ObjectID = T.ID;
			end
		END

		merge into  Field T
		using       (
					select	*
					from	#Field_Step1
					where	FieldTypeID is not null
					) S
		on          (
						T.FieldTypeID = S.FieldTypeID and 
						T.ObjectType = S.Object and
						T.ObjectID = S.ObjectID
					)
		when matched and ( (T.Value <> S.NewValue) OR (T.Value is null) OR (S.NewValue is null and T.Value is not null) ) then
			update set
					T.Value = S.NewValue,
					T.FormattedValue = S.NewValue --utility.GetFormattedFieldLookupValueWithMultiple(S.Type, S.LookupDisplayFormat, S.LookupObjectType, S.LookupObjectID, S.FieldValue, S.AllowMultipleValues)
		when not matched by target then
			insert  (FieldTypeID, ObjectType, ObjectID, Value, FormattedValue)
			values  (S.FieldTypeID, S.Object, S.ObjectID, S.NewValue, S.NewValue);--utility.GetFormattedFieldLookupValueWithMultiple(S.Type, S.LookupDisplayFormat, S.LookupObjectType, S.LookupObjectID, S.FieldValue, S.AllowMultipleValues));

		--select * from #Field_Step1
	end
	--END FIELDS

	--BEGIN RELATIONSHIPS
	if @section = 2
	begin
		drop table if exists #Rel_Step1;
		create table #Rel_Step1 (SynchedAssetTypeRelationItemID int, IsSubject bit, SourceID nvarchar(250), [Type] nvarchar(250), ID nvarchar(250));
		CREATE NONCLUSTERED INDEX IX_TempRel_Step1 ON #Rel_Step1 ([SynchedAssetTypeRelationItemID],[Type]) INCLUDE ([IsSubject],[SourceID],[ID]);
		insert into #Rel_Step1
			select	R.ID
					,R.IsSubject
					,A.SourceID--,IIF(R.IsSubject=1,A.SourceID,RIIF._id) as SubjectSourceID
					,RIIF._type
					,RIIF._id--,IIF(R.IsSubject=0,A.SourceID,RIIF._id) as ObjectSourceID
			from	integration.ExecutionAsset A
					cross apply OPENJSON(A.RawRelationships) RF
					inner join [integration].[SynchedAssetTypeRelationItem] R on R.SynchedAssetTypeID = A.SynchedAssetTypeID and R.[SourceField] = RF.[key] COLLATE DATABASE_DEFAULT and RF.[key] is not null
					outer apply OPENJSON(RF.[value]) with (items nvarchar(max) '$.items' as json) RIF
					outer apply OPENJSON(RIF.items) with (_type nvarchar(max) '$._type', _id nvarchar(max) '$._id') RIIF
			where	A.ExecutionID = @ExecutionID
					and A.SynchedAssetTypeID = @SynchedAssetTypeID
--and A.SourceID = '5b818a0c.187019e4.p22m92joq.ku4e27e.42r9v7.oiegds2mjhclbmefe5pe9'
					and A.RawRelationships is not null
					and RIIF._type is not null;

		drop table if exists #Rel_Step2;
		create table #Rel_Step2 (
			SourceID nvarchar(250),
			IntersectTypeID int,
			SubjectAssetTypeID int, SubjectSourceID nvarchar(250), SubjectAssetID bigint, Subject varchar(50), SubjectID int,
			ObjectAssetTypeID int, ObjectSourceID nvarchar(250), ObjectAssetID bigint, Object varchar(50), ObjectID int,
			IntersectID int, [Action] char(1)
		);

		insert into #Rel_Step2
			select	S.SourceID
					,R.IntersectTypeID
					,ST.ID as SubjectAssetTypeID
					,IIF(S.IsSubject=1,S.SourceID,S.ID) as SubjectSourceID
					,SA.ID as SubjectAssetID
					,SA.Object as Subject
					,SA.ObjectID as SubjectID
					,OT.ID as ObjectAssetTypeID
					,IIF(S.IsSubject=0,S.SourceID,S.ID) as ObjectSourceID
					,OA.ID as ObjectAssetID
					,OA.Object as Object
					,OA.ObjectID as ObjectID
					,I.ID
					,IIF(I.ID is null, null, 'N')
			from	#Rel_Step1 S
					inner join [integration].[SynchedAssetTypeRelationItemTarget] R on R.[SynchedAssetTypeRelationItemID] = S.SynchedAssetTypeRelationItemID and S.[Type] like R.[SourceAssetType] + '%'
					inner join IntersectType IT on IT.ID = R.IntersectTypeID
					inner join AssetType ST on ST.Object = IT.Subject and ST.ObjectID = IT.SubjectID
					inner join AssetType OT on OT.Object = IT.Object and OT.ObjectID = IT.ObjectID
					left join Asset SA on SA.AssetTypeID = ST.ID and SA.SourceID = IIF(S.IsSubject=1,S.SourceID,S.ID)
					left join Asset OA on OA.AssetTypeID = OT.ID and OA.SourceID = IIF(S.IsSubject=0,S.SourceID,S.ID)
					left join [Intersect] I on I.IntersectTypeID = IT.ID and I.Subject = SA.Object and I.SubjectID = SA.ObjectID and I.Object = OA.Object and I.ObjectID = OA.ObjectID;

		update	#Rel_Step2
		set		[ACtion] = 'A'
		where	IntersectID is null;

--select * from #Rel_Step2

		drop table if exists #IntersectTypes;
		create table #IntersectTypes (ID int);
		insert into #IntersectTypes
			select		RT.IntersectTypeID
			from		[integration].[SynchedAssetTypeRelationItemTarget] RT
						inner join [integration].[SynchedAssetTypeRelationItem] R on R.ID = RT.[SynchedAssetTypeRelationItemID] and R.SynchedAssetTypeID = @SynchedAssetTypeID
			group by	RT.IntersectTypeID;

		--BEGIN Query for records we need to delete.
		insert into #Rel_Step2
			select	null as SourceID,
					I.IntersectTypeID,
					S.AssetTypeID, S.SourceID, S.ID, S.Object, S.ObjectID,
					O.AssetTypeID, O.SourceID, O.ID, O.Object, O.ObjectID,
					I.ID, 'D' as [Action]
			from	[Intersect] I
					inner join #IntersectTypes SIT on SIT.ID = I.IntersectTypeID
					inner join integration.SynchedAssetTypeRelationItemTarget SRIT on SRIT.IntersectTypeID = SIT.ID
					inner join integration.SynchedAssetTypeRelationItem SRI on SRI.SynchedAssetTypeID = @SynchedAssetTypeID and SRI.ID = SRIT.SynchedAssetTypeRelationItemID
					inner join Asset S on S.Object = I.Subject and S.ObjectID = I.SubjectID  
					inner join Asset O on O.Object = I.Object and O.ObjectID = I.ObjectID
					inner join integration.ExecutionAsset EA on EA.ExecutionID = @ExecutionID 
																and EA.SynchedAssetTypeID = @SynchedAssetTypeID 
																and EA.RawRelationships is not null
																and EA.SourceID = case when SRI.IsSubject = 1 then S.SourceID else O.SourceID end
					--inner join integration.ExecutionAsset OE on OE.ExecutionID = @ExecutionID and OE.SynchedAssetTypeID = @SynchedAssetTypeID and OE.SourceID = O.SourceID
					left join #Rel_Step2 SI on SI.IntersectID = I.ID
			where	SI.IntersectID is null
					--and S.SourceID not in (select SourceID from integration.ExecutionAsset where ExecutionID = @ExecutionID and SynchedAssetTypeID = @SynchedAssetTypeID and RawRelationships is null)
					--and O.SourceID not in (select SourceID from integration.ExecutionAsset where ExecutionID = @ExecutionID and SynchedAssetTypeID = @SynchedAssetTypeID and RawRelationships is null)
					;

		--END Query for records we need to delete.

		BEGIN	-- Try to process previously unresolved relationships.

			-- Resolve the missing subject information from these as-yet unresolved relationships.
			update	U
			set		U.SubjectAssetID = A.ID,
					U.Subject = A.Object,
					U.SubjectID = A.ObjectID
			from	[integration].[ExecutionUnresolvedRelationItem] U
					inner join IntersectType IT on U.ExecutionID = @ExecutionID and IT.ID = U.IntersectTypeID and U.SubjectAssetID is null 
					inner join Asset A on A.AssetTypeID = U.SubjectAssetTypeID and A.SourceID = U.SubjectSourceID;

			-- Resolve the missing object information form these as-yet unresolved relationships.
			update	U
			set		U.ObjectAssetID = A.ID,
					U.Object = A.Object,
					U.ObjectID = A.ObjectID
			from	[integration].[ExecutionUnresolvedRelationItem] U
					inner join IntersectType IT on U.ExecutionID = @ExecutionID and IT.ID = U.IntersectTypeID and U.ObjectAssetID is null 
					inner join Asset A on A.AssetTypeID = U.ObjectAssetTypeID and A.SourceID = U.ObjectSourceID;

			-- Add to the normal relationship temp table for further processing.
			insert into #Rel_Step2
				select	SourceID
						,IntersectTypeID
						,SubjectAssetTypeID
						,SubjectSourceID
						,SubjectAssetID
						,Subject
						,SubjectID
						,ObjectAssetTypeID
						,ObjectSourceID
						,ObjectAssetID
						,Object
						,ObjectID
						,null
						,[Action]
				from	[integration].[ExecutionUnresolvedRelationItem]
				where	ExecutionID = @ExecutionID
						and (SubjectAssetID is not null OR ObjectAssetID is not null);
		END

		--begin Add
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, CreatedBy, UpdatedBy)
			select	distinct
					IntersectTypeID,
					Subject, SubjectID, Object, ObjectID,
					@ResourceID, @ResourceID
			from	#Rel_Step2
			where	[Action] = 'A'
					and Subject is not null and SubjectID is not null and Object is not null and ObjectID is not null;

		BEGIN	-- Get the new Intersect IDs for added relationships.
			update	T
			set		T.IntersectID = S.ID
			from	#Rel_Step2 T
					inner join [Intersect] S on T.[Action] = 'A' and S.IntersectTypeID = T.IntersectTypeID and S.Subject = T.Subject and S.SubjectID = T.SubjectID and S.Object = T.Object and S.ObjectID = T.ObjectID;

			update	T
			set		T.IntersectID = S.ID
			from	[integration].[ExecutionUnresolvedRelationItem] T
					inner join [Intersect] S on T.ExecutionID = @ExecutionID and T.[Action] = 'A' and S.IntersectTypeID = T.IntersectTypeID and S.Subject = T.Subject and S.SubjectID = T.SubjectID and S.Object = T.Object and S.ObjectID = T.ObjectID;

			delete	[integration].[ExecutionUnresolvedRelationItem]
			where	ExecutionID = @ExecutionID and IntersectID is not null;
		END

		BEGIN	-- Save the relationships I was not able to resolve. For later processing.
			insert into [integration].[ExecutionUnresolvedRelationItem] (
				ExecutionID, IntersectTypeID, 
				SourceID, SubjectAssetTypeID, SubjectSourceID, SubjectAssetID, Subject, SubjectID,
				ObjectAssetTypeID, ObjectSourceID, ObjectAssetID, Object, ObjectID,
				IntersectID, [Action]
			)
				select	@ExecutionID, R.IntersectTypeID, 
						R.SourceID, R.SubjectAssetTypeID, R.SubjectSourceID, R.SubjectAssetID, R.Subject, R.SubjectID,
						R.ObjectAssetTypeID, R.ObjectSourceID, R.ObjectAssetID, R.Object, R.ObjectID,
						R.IntersectID, R.[Action]
				from	#Rel_Step2 R
						left join [integration].[ExecutionUnresolvedRelationItem] EU on EU.ExecutionID = @ExecutionID and EU.IntersectTypeID = R.IntersectTypeID and EU.SourceID = R.SourceID and (EU.SubjectAssetID = R.SubjectAssetID or EU.ObjectAssetID = R.ObjectAssetID) 
				where	R.[Action] = 'A'
						and EU.ID is null
						and R.IntersectID is null;
		END;
		--end Add

		BEGIN	-- Delete
			begin try
				merge	integration.ExecutionAssetTypeMetric as T
				using	(
						select	count(1) as GovernDeletedRelationshipCount
						from	[Intersect] T
								inner join #Rel_Step2 S on S.[Action] = 'D' and T.ID = S.IntersectID
						) S
				on		(T.ExecutionID = @ExecutionID and T.SynchedAssetTypeID = @SynchedAssetTypeID)
				when matched then
					update set
					T.GovernDeletedRelationshipCount = S.GovernDeletedRelationshipCount
				when not matched by target then
					insert  (
							ExecutionID,
							SynchedAssetTypeID,
							GovernDeletedRelationshipCount
							)
					values  (
							@ExecutionID,
							@SynchedAssetTypeID,
							GovernDeletedRelationshipCount
							);
			end try
			begin catch
				update	integration.ExecutionAssetType
				set		ErrorMessage = coalesce(ErrorMessage, '') + '; ' + ERROR_MESSAGE()
				where	ExecutionID = @ExecutionID and
						SynchedAssetTypeID = @SynchedAssetTypeID
			end catch

			delete	T
			from	[Intersect] T
					inner join #Rel_Step2 S on S.[Action] = 'D' and T.ID = S.IntersectID;
		END

		--Return results to caller.
		select		*
		from		(
					select		distinct
								IntersectTypeID, 
								IntersectID, 
								[Action] 
					from		#Rel_Step2 
					where		[Action] <> 'N' 
								and [Action] is not null 
								and IntersectID is not null
					--order by	IntersectID
					union
					select	top 250
							I.IntersectTypeID, 
							I.ID as IntersectID, 
							'A' as [Action]
					from	[workflow].[EventRegistration] ER
							inner join [Intersect] I on ER.Object = 'IntersectType' and I.IntersectTypeID = ER.ObjectID and ER.State = 1
							inner join #IntersectTypes IT on IT.ID = I.IntersectTypeID
							inner join workflow.[Type] T on T.ID = ER.TypeID 
															and T.State = 1 
															and T.CreatedOn < I.CreatedOn 
															and I.CreatedOn <= DATEADD(mi, -60, getutcdate())
							left join workflow.Item WI on WI.Object = 'Intersect' and WI.ObjectID = I.ID
					where	WI.ID is null
					) O 
		order by	O.IntersectID;
	end
	--END RELATIONSHIPS

	--BEGIN RESPONSIBILITIES
	if @section = 3
	begin
		drop table if exists #Resp_Step1;
		create table #Resp_Step1 (AssetID bigint, SourceID nvarchar(250), ResponsibilityTypeID int, ResourceIdentifier nvarchar(250), ResourceID int, [Action] varchar(1), Error varchar(max), OverrideItemID bigint);
		CREATE NONCLUSTERED INDEX IX_TempResp_Step1_ResourceIdentifier ON #Resp_Step1 (ResourceIdentifier) INCLUDE (AssetID, ResponsibilityTypeID, [Action]);
		CREATE NONCLUSTERED INDEX IX_TempResp_Step1_ResourceID ON #Resp_Step1 (ResourceID) INCLUDE (AssetID, ResponsibilityTypeID, [Action]);
		CREATE NONCLUSTERED INDEX IX_TempResp_Step1_ResourceIDAction ON #Resp_Step1 (ResourceID, [Action]) INCLUDE (AssetID, ResponsibilityTypeID);
		CREATE NONCLUSTERED INDEX IX_TempResp_Step1_DeleteAndUpdateStepIndex ON #Resp_Step1 (ResponsibilityTypeID, AssetID, [Action])
		CREATE NONCLUSTERED INDEX IX_TempResp_Step1_AddStepIndex ON #Resp_Step1 ([Action])

		insert into #Resp_Step1 (AssetID, SourceID, ResponsibilityTypeID, ResourceIdentifier)
			select	A.ID as AssetID
					,substring(ltrim(rtrim(E.SourceID)), 1, 250)
					,RT.ID as ResponsibilityTypeID
					,substring(rtrim(ltrim(J.value)), 1, 250)
			from	integration.ExecutionAsset E
					cross apply OPENJSON(E.RawResponsibilitites) J--with (_type nvarchar(max) '$._type', _id nvarchar(max) '$._id') J
					inner join Asset A on A.AssetTypeID = @AssetTypeID and A.SourceID = E.SourceID
					inner join [integration].[SynchedAssetTypeRoleItem] R on R.SynchedAssetTypeID = E.SynchedAssetTypeID and R.SourceIdField = J.[key] COLLATE DATABASE_DEFAULT
					inner join ResponsibilityType RT on RT.Name = R.RoleName
			where	E.ExecutionID = @ExecutionID
					and E.SynchedAssetTypeID = @SynchedAssetTypeID
					and E.RawResponsibilitites is not null;

		update	#Resp_Step1
		set		[Action] = 'D'   -- Delete action
		where	ResourceIdentifier is null or ResourceIdentifier = '';

		update	T
		set		T.ResourceID = RE.ResourceID
		from	#Resp_Step1 T
				inner join Field F on F.ObjectType = 'Resource' and F.Value = T.ResourceIdentifier and F.FieldTypeID in (select ID from FieldType where Object = 'ResourceType' and ObjectID = 1 and Name = 'UserId')
				inner join reporting.Global_Resource RE on RE.ResourceID = F.ObjectID;

--select * from #Resp_Step1

		update	#Resp_Step1
		set		[Action] = 'D',
				[Error] = 'User could not be found based on identifier [' + coalesce(ResourceIdentifier,'') + '].'
		where	ResourceIdentifier is not null and ResourceIdentifier <> '' and ResourceID is null;

		update	T
		set		T.[Action] = 'N' -- No action
		from	#Resp_Step1 T
				inner join ResponsibilityTypeRelationOverrideItem S on S.AssetID = T.AssetID and S.ResponsibilityTypeID = T.ResponsibilityTypeID and S.SecurityAsset = 'R' and S.SecurityAssetID = T.ResourceID and T.ResourceID is not null;

		update	T
		set		T.[Action] = 'U' -- Update action
		from	#Resp_Step1 T
				inner join ResponsibilityTypeRelationOverrideItem S on S.AssetID = T.AssetID and S.ResponsibilityTypeID = T.ResponsibilityTypeID and S.SecurityAsset = 'R' and S.SecurityAssetID <> T.ResourceID
		where	T.ResourceID is not null
				and T.[Action] is null;

		update	T
		set		T.[Action] = 'A' -- Add action
		from	#Resp_Step1 T
				left join ResponsibilityTypeRelationOverrideItem S on S.AssetID = T.AssetID and S.ResponsibilityTypeID = T.ResponsibilityTypeID
		where	T.ResourceID is not null
				and T.[Action] is null
				and T.AssetID is not null
				and S.[ID] is null;

		-- Log the error messages.
		update	T
		set		T.ErrorMessages = coalesce(T.ErrorMessages+'; ', '') + S.[Error]
		from	integration.ExecutionAsset T
				inner join #Resp_Step1 S on T.ExecutionID = @ExecutionID 
											and T.SynchedAssetTypeID = @SynchedAssetTypeID 
											and T.SourceID = S.SourceID 
											and S.[Error] is not null;

		--DELETE
		delete	T
		from	ResponsibilityTypeRelationOverrideItem T
				inner join #Resp_Step1 S on S.ResponsibilityTypeID = T.ResponsibilityTypeID and S.AssetID = T.AssetID and S.[Action] = 'D' and T.SecurityAsset = 'R';

		--UPDATE
		update	T
		set		T.SecurityAssetID = S.ResourceID
		from	ResponsibilityTypeRelationOverrideItem T
				inner join #Resp_Step1 S on S.ResponsibilityTypeID = T.ResponsibilityTypeID and S.AssetID = T.AssetID and S.[Action] = 'U' and T.SecurityAsset = 'R';

		--ADD
		insert into ResponsibilityTypeRelationOverrideItem (ResponsibilityTypeID, AssetID, SecurityAsset, SecurityAssetID)
			select	ResponsibilityTypeID, 
					AssetID,
					'R' as SecurityAsset,
					ResourceID
			from	#Resp_Step1
			where	[Action] = 'A';

		-- DELETE any general dupes found.
		delete	ResponsibilityTypeRelationOverrideItem
		where	ID in (
					select		max(ID) as ID
					from		ResponsibilityTypeRelationOverrideItem
					group by 	ResponsibilityTypeID,
								AssetID,
								SecurityAsset,
								SecurityAssetID		
					having		count(1) > 1
				)

		-- Get the OverrideItemID.
		update	T
		set		T.OverrideItemID = S.ID
		from	#Resp_Step1 T
				inner join ResponsibilityTypeRelationOverrideItem S on S.AssetID = T.AssetID and S.ResponsibilityTypeID = T.ResponsibilityTypeID and S.SecurityAsset = 'R' and S.SecurityAssetID = T.ResourceID;
	end
	--END RESPONSIBILITIES

	--BEGIN METRICS CAPTURE
	if @section = 4
	begin
		merge	integration.ExecutionAssetTypeMetric as T
		using	(
				select	E.ExecutionID,
						E.SynchedAssetTypeID,
						E.AssetsWithMissingDefinition,
						E.AssetsWithMissingRelationships,
						E.AssetsWithMissingResponsibilities,
						E.AssetsWithErrors,
						E.RetrievedAssetCount, 
						IGC_R.RetrievedAssetRelationshipCount, 
						IGC_O.RetrievedAssetResponsibilityCount,
						GOV_C.GovernAssetCount, 
						GOV_R.GovernAssetRelationshipCount, 
						GOV_O.GovernAssetResponsibilityCount,
						(
							select		RIT.IntersectTypeID as 'intersectTypeId',
										RIT.SourceAssetType as 'type',
										count(1) as 'count'
							from		integration.ExecutionAsset A
										cross apply OPENJSON(A.[RawRelationships]) C
										cross apply OPENJSON(c.value, '$.items') with (id nvarchar(500) '$._id', [type] nvarchar(500) '$._type') R
										inner join integration.SynchedAssetTypeRelationItem RI on RI.SynchedAssetTypeID = A.SynchedAssetTypeID and RI.[SourceField] = C.[Key] COLLATE DATABASE_DEFAULT
										inner join [integration].[SynchedAssetTypeRelationItemTarget] RIT on RIT.SynchedAssetTypeRelationItemID = RI.ID and R.[type] COLLATE DATABASE_DEFAULT like RIT.SourceAssetType+'%'
							where		A.ExecutionID = E.ExecutionID  
										and A.SynchedAssetTypeID = E.SynchedAssetTypeID 
							group by	RIT.IntersectTypeID,
										RIT.SourceAssetType
							for json path
						) as IGCAssetRelationshipBreakdown,
						(
							select		O.IntersectTypeID as 'intersectTypeId',
										O.SourceAssetType as 'type',
										count(1) as 'count'
							from		(
										select		RIT.IntersectTypeID,
													RIT.SourceAssetType
										from		integration.SynchedAssetTypeRelationItem RI
													inner join [integration].[SynchedAssetTypeRelationItemTarget] RIT on RIT.SynchedAssetTypeRelationItemID = RI.ID
										where		RI.SynchedAssetTypeID = E.SynchedAssetTypeID  
										group by	RIT.IntersectTypeID,
													RIT.SourceAssetType
										) O
										inner join [Intersect] I on I.IntersectTypeID = O.IntersectTypeID and (I.Deleted = 0 or I.Deleted is null) and I.State not in (2,3)
										inner join IntersectType IT on IT.ID = I.IntersectTypeID
										inner join Asset A on (
																(
																	IT.Subject = IT.Object and IT.SubjectID = IT.ObjectID and 
																	A.Object = I.Subject and A.ObjectID = I.SubjectID
																)
																or 
																(
																	(IT.Subject <> IT.Object or IT.SubjectID <> IT.ObjectID) and 
																	(
																		(A.Object = I.Subject and A.ObjectID = I.SubjectID) or 
																		(A.Object = I.Object and A.ObjectID = I.ObjectID)
																	)
																)
															) and A.AssetTypeID = E.AssetTypeID
							group by	O.IntersectTypeID,
										O.SourceAssetType
							for json path
						) as GovernAssetRelationshipBreakdown, 
						(
							select		RI.RoleName as 'role',
										count(1) as 'count'
							from		integration.ExecutionAsset A
										cross apply OPENJSON(A.[RawResponsibilitites]) C
										inner join integration.SynchedAssetTypeRoleItem RI on RI.SynchedAssetTypeID = A.SynchedAssetTypeID and C.[key] COLLATE DATABASE_DEFAULT = RI.[SourceIdField] and C.value is not null and C.value <> ''
							where		A.ExecutionID = E.ExecutionID  
										and A.SynchedAssetTypeID = E.SynchedAssetTypeID 
							group by	RI.RoleName
							for json path
						) as IGCAssetResponsibilityBreakdown,
						(
							select		I_RT.Name as 'role',
										count(1) as 'count'
							from		integration.ExecutionAsset I_EA
										inner join Asset I_A on I_A.AssetTypeID = E.AssetTypeID and I_A.SourceID = I_EA.SourceID
										inner join ResponsibilityTypeRelationOverrideItem I_R on I_R.AssetID = I_A.ID
										inner join ResponsibilityType I_RT on I_RT.ID = I_R.ResponsibilityTypeID
										inner join integration.SynchedAssetTypeRoleItem I_RI on I_RI.SynchedAssetTypeID = E.SynchedAssetTypeID and I_RI.RoleName = I_RT.Name
							where		I_EA.ExecutionID = E.ExecutionID  
										and I_EA.SynchedAssetTypeID = E.SynchedAssetTypeID 
							group by	I_RT.Name
							for json path
						) as GovernAssetResponsibilityBreakdown
				from	(
						select		A.AssetTypeID,
									ET.ExecutionID,
									ET.SynchedAssetTypeID,
									ET.CurrentSourceAssetCount as SourceTotal,
									count(1) as RetrievedAssetCount,
									ET.ErrorMessage,
									sum(case when EA.ErrorMessages is not null and EA.ErrorMessages <> '' then 1 else 0 end) as AssetsWithErrors,
									sum(case when EA.RawObject is null then 1 else 0 end) as AssetsWithMissingDefinition,
									sum(case when EA.RawRelationships is null then 1 else 0 end) as AssetsWithMissingRelationships,
									sum(case when EA.RawResponsibilitites is null then 1 else 0 end) as AssetsWithMissingResponsibilities
						from		integration.ExecutionAssetType ET
									inner join integration.ExecutionAsset EA on EA.ExecutionID = ET.ExecutionID and EA.SynchedAssetTypeID = ET.SynchedAssetTypeID
									inner join integration.SynchedAssetType A on A.ID = ET.SynchedAssetTypeID
						where		ET.IsFullRefresh = 1 
									and ET.ExecutionID = @ExecutionID
									and ET.SynchedAssetTypeID = @SynchedAssetTypeID
						group by	A.AssetTypeID,
									ET.ExecutionID,
									ET.SynchedAssetTypeID,
									ET.CurrentSourceAssetCount,
									ET.ErrorMessage
						) E 
						cross apply (
							select		count(1) as GovernAssetCount
							from		Asset
							where		AssetTypeID = E.AssetTypeID
						) GOV_C
						cross apply (
							select	count(1) as GovernAssetRelationshipCount
							from	(
									select		RIT.IntersectTypeID
									from		integration.SynchedAssetTypeRelationItem RI
												inner join [integration].[SynchedAssetTypeRelationItemTarget] RIT on RI.SynchedAssetTypeID = E.SynchedAssetTypeID and RIT.SynchedAssetTypeRelationItemID = RI.ID
									group by	RIT.IntersectTypeID
									) O
									inner join [Intersect] I on I.IntersectTypeID = O.IntersectTypeID and I.State not in (2,3)
						) GOV_R
						cross apply (
							select	count(1) as GovernAssetResponsibilityCount
							from	integration.SynchedAssetTypeRoleItem RO
									inner join ResponsibilityType RT on RT.Name = RO.RoleName and RO.SynchedAssetTypeID = E.SynchedAssetTypeID and RO.[Active] = 1
									inner join ResponsibilityTypeRelationOverrideItem O on O.ResponsibilityTypeID = RT.ID and O.SecurityAsset = 'R' and O.AssetID > 0
						) GOV_O
						cross apply (
							select		count(1) as RetrievedAssetRelationshipCount
							from		integration.ExecutionAsset A
										cross apply OPENJSON(A.[RawRelationships]) C
										cross apply OPENJSON(c.value, '$.items') with (id nvarchar(500) '$._id', [type] nvarchar(500) '$._type') R
										cross apply (
											select	distinct 1 as C--RIT.SourceAssetType
											from	integration.SynchedAssetTypeRelationItem RI
													inner join [integration].[SynchedAssetTypeRelationItemTarget] RIT on RI.SynchedAssetTypeID = A.SynchedAssetTypeID and RIT.SynchedAssetTypeRelationItemID = RI.ID
													and R.[type] COLLATE DATABASE_DEFAULT like RIT.SourceAssetType+'%'
										) T
							where		A.ExecutionID = E.ExecutionID  
										and A.SynchedAssetTypeID = E.SynchedAssetTypeID 
										and C.[Key] COLLATE DATABASE_DEFAULT in (select SourceField from integration.SynchedAssetTypeRelationItem where SynchedAssetTypeID = A.SynchedAssetTypeID)
						) IGC_R
						cross apply (
							select		count(1) as RetrievedAssetResponsibilityCount
							from		integration.ExecutionAsset A
										cross apply OPENJSON(A.RawResponsibilitites) C
							where		A.ExecutionID = E.ExecutionID  
										and A.SynchedAssetTypeID = E.SynchedAssetTypeID 
										and C.[Key] COLLATE DATABASE_DEFAULT in (select [SourceIdField] from integration.SynchedAssetTypeRoleItem where SynchedAssetTypeID = A.SynchedAssetTypeID)
										and C.value <> ''
						) IGC_O
				) S
		on		(T.ExecutionID = S.ExecutionID and T.SynchedAssetTypeID = S.SynchedAssetTypeID)
		when matched then
			update set
			T.AssetsWithMissingDefinition = S.AssetsWithMissingDefinition,
			T.AssetsWithMissingRelationships = S.AssetsWithMissingRelationships,
			T.AssetsWithMissingResponsibilities = S.AssetsWithMissingResponsibilities,
			T.AssetsWithErrors = S.AssetsWithErrors,
			T.RetrievedAssetCount = S.RetrievedAssetCount,
			T.RetrievedAssetRelationshipCount = S.RetrievedAssetRelationshipCount,
			T.RetrievedAssetResponsibilityCount = S.RetrievedAssetResponsibilityCount,
			T.GovernAssetCount = S.GovernAssetCount,
			T.GovernAssetRelationshipCount = S.GovernAssetRelationshipCount,
			T.GovernAssetResponsibilityCount = S.GovernAssetResponsibilityCount,
			T.IGCAssetRelationshipBreakdown = S.IGCAssetRelationshipBreakdown,
			T.GovernAssetRelationshipBreakdown = S.GovernAssetRelationshipBreakdown,
			T.IGCAssetResponsibilityBreakdown = S.IGCAssetResponsibilityBreakdown,
			T.GovernAssetResponsibilityBreakdown = S.GovernAssetResponsibilityBreakdown
		when not matched by target then
			insert  (
					ExecutionID,
					SynchedAssetTypeID,
					AssetsWithMissingDefinition,
					AssetsWithMissingRelationships,
					AssetsWithMissingResponsibilities,
					AssetsWithErrors,
					RetrievedAssetCount,
					RetrievedAssetRelationshipCount,
					RetrievedAssetResponsibilityCount,
					GovernAssetCount,
					GovernAssetRelationshipCount,
					GovernAssetResponsibilityCount,
					IGCAssetRelationshipBreakdown,
					GovernAssetRelationshipBreakdown,
					IGCAssetResponsibilityBreakdown,
					GovernAssetResponsibilityBreakdown
					)
			values  (
					S.ExecutionID,
					S.SynchedAssetTypeID,
					S.AssetsWithMissingDefinition, 
					S.AssetsWithMissingRelationships,
					S.AssetsWithMissingResponsibilities,
					S.AssetsWithErrors,
					S.RetrievedAssetCount,
					S.RetrievedAssetRelationshipCount,
					S.RetrievedAssetResponsibilityCount,
					S.GovernAssetCount,
					S.GovernAssetRelationshipCount,
					S.GovernAssetResponsibilityCount,
					S.IGCAssetRelationshipBreakdown,
					S.GovernAssetRelationshipBreakdown,
					S.IGCAssetResponsibilityBreakdown,
					S.GovernAssetResponsibilityBreakdown
					);

	end
	--END METRICS CAPTURE

end
GO



alter table metrics.Map add AssetTypeID int null
GO

-- Convert to INTs instead of LONGs.
ALTER TABLE [metrics].[Condition] DROP CONSTRAINT [FK_MetricCondition_MetricMap]
GO
ALTER TABLE [metrics].[MapResult] DROP CONSTRAINT [FK_MetricMapResult_MetricMap]
GO
ALTER TABLE [metrics].[StagingResult] DROP CONSTRAINT [FK_StagingResult_Map]
GO
ALTER TABLE [metrics].[Map] DROP CONSTRAINT [PK_MetricMap]
GO

alter table metrics.Map alter column ID int not null
GO
ALTER TABLE [metrics].[ConditionValue] DROP CONSTRAINT [FK_MetricConditionValue_MetricCondition]
GO
ALTER TABLE [metrics].[Condition] DROP CONSTRAINT [PK_MetricCondition]
GO
alter table metrics.[Condition] alter column MapID int not null
GO
ALTER TABLE [metrics].[Condition] ADD  CONSTRAINT [PK_MetricCondition] PRIMARY KEY NONCLUSTERED ( [MapID] ASC, [FieldTypeID] ASC )
GO
ALTER TABLE [metrics].[ConditionValue] DROP CONSTRAINT [PK_MetricConditionValue]
GO
alter table metrics.[ConditionValue] alter column MapID int not null
GO
ALTER TABLE [metrics].[ConditionValue] ADD  CONSTRAINT [PK_MetricConditionValue] PRIMARY KEY NONCLUSTERED ( [MapID] ASC, [FieldTypeID] ASC, [Value] ASC )
GO
ALTER TABLE [metrics].[ConditionValue]  WITH CHECK ADD  CONSTRAINT [FK_MetricConditionValue_MetricCondition] FOREIGN KEY([MapID], [FieldTypeID]) REFERENCES [metrics].[Condition] ([MapID], [FieldTypeID]) ON DELETE CASCADE
GO
ALTER TABLE [metrics].[ConditionValue] CHECK CONSTRAINT [FK_MetricConditionValue_MetricCondition]
GO
ALTER TABLE [metrics].[StagingResult] DROP CONSTRAINT [PK_MetricStagingResult]
GO
alter table metrics.[StagingResult] alter column MapID int not null
GO
ALTER TABLE [metrics].[StagingResult] ADD  CONSTRAINT [PK_MetricStagingResult] PRIMARY KEY NONCLUSTERED ( [MapID] ASC, [EffectiveDate] DESC, [AssetID] ASC )
GO
ALTER TABLE [metrics].[MapResult] DROP CONSTRAINT [PK_MetricMapResult]
GO
alter table metrics.[MapResult] alter column MapID int not null
GO
ALTER TABLE [metrics].[MapResult] ADD  CONSTRAINT [PK_MetricMapResult] PRIMARY KEY NONCLUSTERED ( [MapID] ASC, [ScoreID] ASC )
GO
ALTER TABLE [metrics].[Map] ADD  CONSTRAINT [PK_MetricMap] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
GO

ALTER TABLE [metrics].[Condition]  WITH CHECK ADD  CONSTRAINT [FK_MetricCondition_MetricMap] FOREIGN KEY([MapID]) REFERENCES [metrics].[Map] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [metrics].[Condition] CHECK CONSTRAINT [FK_MetricCondition_MetricMap]
GO
ALTER TABLE [metrics].[MapResult]  WITH CHECK ADD  CONSTRAINT [FK_MetricMapResult_MetricMap] FOREIGN KEY([MapID]) REFERENCES [metrics].[Map] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [metrics].[MapResult] CHECK CONSTRAINT [FK_MetricMapResult_MetricMap]
GO

ALTER TABLE [metrics].[StagingResult]  WITH CHECK ADD  CONSTRAINT [FK_StagingResult_Map] FOREIGN KEY([MapID]) REFERENCES [metrics].[Map] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [metrics].[StagingResult] CHECK CONSTRAINT [FK_StagingResult_Map]
GO
-----------------------------------
ALTER TABLE [metrics].[Group] DROP CONSTRAINT [DF_MetricGroup_EffectiveStartDate]
GO
--alter table metrics.[Group] drop column [EffectiveStartDate]
--GO
--alter table metrics.[Group] drop column [EffectiveEndDate]
--GO
alter table metrics.[Group] add [uid] uniqueidentifier constraint DF_MetricsGroup_uid default(newid()) not null
GO
alter table metrics.[Item] add [uid] uniqueidentifier constraint DF_MetricsItem_uid default(newid()) not null
GO

alter table metrics.[Map] add EffectiveDate datetime null
GO

--update metrics.[Map] set EffectiveDate = EffectiveStartDate
--alter table metrics.[Map] alter column EffectiveDate datetime not null
--GO

CREATE TABLE [metrics].[StagingItem](
	AssetUid uniqueidentifier not null,
	MetricGroupUid uniqueidentifier not null,
	MetricItemUid uniqueidentifier not null,
	[EffectiveDate] date NOT NULL,
	[Result] [bit] NOT NULL,
	[Processing] [bit] NOT NULL,
	[Archived] [bit] NOT NULL,
	CONSTRAINT [PK_MetricStagingItem] PRIMARY KEY NONCLUSTERED ( AssetUid ASC, MetricGroupUid ASC, MetricItemUid ASC, [EffectiveDate] DESC )
)
GO

ALTER TABLE [metrics].[StagingItem] ADD  CONSTRAINT [DF_MetricsStagingItem_Archived]  DEFAULT ((0)) FOR [Archived]
GO

ALTER TABLE [metrics].[StagingItem] ADD  CONSTRAINT [DF_MetricsStagingItem_Processing]  DEFAULT ((0)) FOR [Processing]
GO

CREATE CLUSTERED INDEX CIX_MetricStagingItem ON metrics.StagingItem (Archived ASC)
GO

ALTER TABLE [metrics].[Map] DROP CONSTRAINT [DF_MetricsMap_uid]
GO
alter table metrics.Map drop column [uid]
GO

ALTER TABLE [metrics].[Map] DROP CONSTRAINT [FK_MetricMap_MetricItem]
GO
ALTER TABLE [metrics].[Map] ALTER COLUMN ItemID int not null
GO

ALTER TABLE [metrics].[Item] DROP CONSTRAINT [PK_MetricItem]
GO

ALTER TABLE [metrics].[Item] ALTER COLUMN ID int not null
GO

ALTER TABLE [metrics].[Item] ADD  CONSTRAINT [PK_MetricItem] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
GO

ALTER TABLE [metrics].[Map]  WITH CHECK ADD  CONSTRAINT [FK_MetricMap_MetricItem] FOREIGN KEY([ItemID]) REFERENCES [metrics].[Item] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [metrics].[Map] CHECK CONSTRAINT [FK_MetricMap_MetricItem]
GO

update	T
set		T.AssetTypeID = S.ID
from	metrics.Map T inner join AssetType S on S.Object = T.Object and S.ObjectID = T.ObjectID
GO
--alter table metrics.Map drop column [Object]
--alter table metrics.Map drop column [ObjectID]
--alter table metrics.Map drop column [EffectiveStartDate]
--alter table metrics.Map drop column [EffectiveEndDate]
GO


-- GOV-5387
ALTER procedure [dbo].[ResponsibilityRuleShouldRun]
	@id int-- = 70
as
begin
	set nocount on;

	--update ResponsibilityTypeRelationRule set LastRunOn = '7/20/2018 9:00:00 PM' where ID = 70
	declare @shouldRun bit = 0 ,
			@lastRunOn datetime,
			@o varchar(50),
			@oid int--,
			--@ruleUpdatedOn datetime

	select	@lastRunOn = coalesce(LastRunOn, '1/1/2000'),
			@o = Object,
			@oid = ObjectID--,
	--		@ruleUpdatedOn = UpdatedOn
	from	ResponsibilityTypeRelationRule
	where	ID = @id

	declare @assetMaxDate datetime,
			@assetFieldMaxDate datetime,
			@newUsers bit = 0,
			@newAssets bit = 0--,
	--		@ruleUpdated bit = 0

	--if @ruleUpdatedOn > @lastRunOn
	--begin
	--	set	@ruleUpdated = 1
	--end
	select	@newUsers = IIF(count(1) > 0, 1, 0)
	from	reporting.Global_Resource
	where	CreatedOn > @lastRunOn

	select	@assetMaxDate = max(A.CreatedOn)
	from	Asset A
			inner join AssetType T on T.ID = A.AssetTypeID and T.Object = @o and T.ObjectID = @oid 

	select	@assetMaxDate = IIF(max(A.UpdatedOn) > @assetMaxDate, max(A.UpdatedOn), @assetMaxDate)
	from	Asset A
			inner join AssetType T on T.ID = A.AssetTypeID and T.Object = @o and T.ObjectID = @oid 

	if @assetMaxDate > @lastRunOn
	begin
		set @newAssets = 1
	end

	if @newAssets = 0
	begin
		declare @fIDs table (FieldTypeID int)
		insert into @fIDs
			select	WF.FieldTypeID
			from	ResponsibilityTypeRelationRule R
					cross apply OPENJSON(R.Definition, '$.When') D--with ([When] nvarchar(max) '$.When', [Then] nvarchar(max) '$.Then') D
					cross apply OPENJSON(D.value) with (
							CheckType nvarchar(1) '$.CheckType',
							FieldTypeID int '$.FieldTypeID'--,
							--FieldTypeName nvarchar(250) '$.FieldTypeName' 
						) WF
			where	R.ID = @id
					and WF.CheckType = 'F'

		if exists(select 1 from @fIDs)
		begin
			select	@assetFieldMaxDate = max(F.UpdatedOn)
			from	Field F 
					inner join @fIDs FT on FT.FieldTypeID = F.FieldTypeID
					inner join Asset A on A.ID = F.AssetID
					inner join AssetType T on T.ID = A.AssetTypeID and T.Object = @o and T.ObjectID = @oid 
		end
		else
		begin
			-- slow 3 seconds or so for 400k rows
			/*select	@assetFieldMaxDate = max(F.EffectiveStartDate)
			from	Field F 
					inner join Asset A on A.ID = F.AssetID
					inner join AssetType T on T.ID = A.AssetTypeID and T.Object = @o and T.ObjectID = @oid */
			-- less than 1 sec for 400k rows
			select	@assetFieldMaxDate = max(F.UpdatedOn)
			from	Field F 
					inner join FieldType FT on F.FieldTypeID = FT.id and FT.Object = @o and FT.ObjectID = @oid
		end

		if @assetFieldMaxDate > @lastRunOn
		begin
			set @newAssets = 1
		end	
	end

	if @newUsers = 1 or @newAssets = 1
	begin
		set @shouldRun = 1
	end

	select @shouldRun
end
GO
