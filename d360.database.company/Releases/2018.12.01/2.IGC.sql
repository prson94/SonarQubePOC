-- ExecutionAsset table changes.
alter table [integration].[ExecutionAsset] add [Uid] uniqueidentifier constraint DF_IntegrationExecutionAsset_Uid default(newid()) not null
GO;

ALTER TABLE [integration].[ExecutionAsset] DROP CONSTRAINT [PK_IntegrationExecutionAsset]
GO;

DROP INDEX [CIX_IntegrationExecutionAsset] ON [integration].[ExecutionAsset] WITH ( ONLINE = OFF )
GO;

ALTER TABLE [integration].[ExecutionAsset] ADD  CONSTRAINT [PK_IntegrationExecutionAsset] PRIMARY KEY CLUSTERED ( [Uid] ASC )
GO;

ALTER TABLE [integration].[ExecutionAsset] ADD  CONSTRAINT [UQ_IntegrationExecutionAsset] UNIQUE ( [ExecutionID] DESC, [SynchedAssetTypeID] ASC, [SourceID] ASC )
GO;


-- Role table changes.
alter table [integration].[SynchedAssetTypeRoleItem] add ResponsibilityTypeID int null
GO;
update	T
set		T.ResponsibilityTypeID = S.ID
from	[integration].[SynchedAssetTypeRoleItem] T
		inner join ResponsibilityType S on S.Name = T.RoleName
GO;
alter table [integration].[SynchedAssetTypeRoleItem] alter column ResponsibilityTypeID int not null
GO;
alter table [integration].[SynchedAssetTypeRoleItem] drop column RoleName
GO;

ALTER TABLE [integration].[SynchedAssetTypeRoleItem] WITH CHECK ADD CONSTRAINT [FK_IntegrationSynchedAssetTypeRoleItem_ResponsibilityType] FOREIGN KEY([ResponsibilityTypeID]) REFERENCES [dbo].[ResponsibilityType] ([ID])
ALTER TABLE [integration].[SynchedAssetTypeRoleItem] CHECK CONSTRAINT [FK_IntegrationSynchedAssetTypeRoleItem_ResponsibilityType]
GO;

-- Addition of ExecutionAssetField table
create table integration.ExecutionAssetField (
	[Uid] uniqueidentifier NOT NULL, 
	Section int not null, 
	FieldName nvarchar(250) NOT NULL, 
	FieldValue nvarchar(max) null
);
ALTER TABLE [integration].[ExecutionAssetField] ADD  CONSTRAINT [PK_IntegrationExecutionAssetField] PRIMARY KEY CLUSTERED ( [Uid] ASC, Section ASC, FieldName ASC );
CREATE NONCLUSTERED INDEX [IX_IntegrationExecutionAsset_FieldName] ON [integration].[ExecutionAssetField] ( [FieldName] ASC );
CREATE NONCLUSTERED INDEX IX_IntegrationExecutionAsset_Section_Include ON [integration].[ExecutionAssetField] ([Section]) INCLUDE ([FieldValue]);
CREATE NONCLUSTERED INDEX IX_IntegrationExecutionAssetField_Section_FieldName_Include ON [integration].[ExecutionAssetField] ([Section],[FieldName]) INCLUDE ([FieldValue]);
GO;

-- Addition of ExecutionAssetTypeMetricRelationshipLog table
CREATE TABLE [integration].[ExecutionAssetTypeMetricRelationshipLog](
	[Uid] uniqueidentifier constraint DF_IntegrationExecutionAssetTypeMetricRelationshipLog_Uid default(newid()) not null,
	[ExecutionID] [bigint] NOT NULL,
	[SynchedAssetTypeID] [int] NOT NULL,
	[Action] varchar(1) not null,
	SubjectSourceID nvarchar(250),
	ObjectSourceID nvarchar(250), 
	IntersectID int
	CONSTRAINT [PK_IntegrationExecutionAssetTypeMetricLog] PRIMARY KEY CLUSTERED ( [Uid] ASC )
)
GO;

--Index changes
DROP INDEX [IX_IntegrationExecutionUnresolvedRelationItem_ObjectInfo] ON [integration].[ExecutionUnresolvedRelationItem]
DROP INDEX [IX_IntegrationExecutionUnresolvedRelationItem_SubjectInfo] ON [integration].[ExecutionUnresolvedRelationItem]
GO;

CREATE NONCLUSTERED INDEX [IX_IntegrationExecutionUnresolvedRelationItem_SubjectAssetID_Include] ON [integration].[ExecutionUnresolvedRelationItem] ([SubjectAssetID]) INCLUDE ([IntersectTypeID],[SubjectAssetTypeID],[SubjectSourceID])
CREATE NONCLUSTERED INDEX [IX_IntegrationExecutionUnresolvedRelationItem_ObjectAssetID_Include] ON [integration].[ExecutionUnresolvedRelationItem] ([ObjectAssetID]) INCLUDE ([IntersectTypeID],[ObjectAssetTypeID],[ObjectSourceID])
GO;

ALTER procedure [integration].[ProcessExecutionAssetType]
--declare	
	@ExecutionID bigint,
	@SynchedAssetTypeID int,
	@AssetTypeID int,
	@ResourceID int,
	@Section int --0 = Asset, 1 = Field, 2 = Relationships, 3 = Responsibilities
--set @ExecutionID = 34606
--set @SynchedAssetTypeID = 13
--set @AssetTypeID = 51
--set @ResourceID = 0
--set @Section = 0
as
begin
	set nocount on;

	--line below used for testing.
	--declare	 @ExecutionID bigint = 34606, @SynchedAssetTypeID int = 13, @AssetTypeID int = 51, @ResourceID int = 0, @Section int = 0

	declare @archived bit = 0

	select	@archived = Archived from integration.Execution where ID = @ExecutionID

	if @archived = 1 
	begin
		RAISERROR (N'This exection is marked as Archived and can no longer be processed.', 10, 1);
	end

	-- BEGIN CORE ASSET
	if @Section = 0
	begin
		--declare	 @ExecutionID bigint = 34606, @SynchedAssetTypeID int = 13, @AssetTypeID int = 51, @ResourceID int = 0, @Section int = 0

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
		create table #Assets (AssetTypeID int, ExecutionAssetUid uniqueidentifier, AssetID bigint, [Object] varchar(50), ObjectID int, [Type] varchar(50), TypeID int, SourceID nvarchar(250), ParentSourceID nvarchar(250), [Action] char(1), Error nvarchar(max));
		CREATE CLUSTERED INDEX CIX_TempAssets ON #Assets (SourceID)
		CREATE NONCLUSTERED INDEX [IX_TempAssets_Action] ON #Assets ( [Action] ASC )
		CREATE NONCLUSTERED INDEX [IX_TempAssets_ExecutionAssetUid-Action] ON #Assets ( ExecutionAssetUid ASC, [Action] ASC )

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
		insert into #Assets (AssetTypeID, ExecutionAssetUid, AssetID, [Object], ObjectID, [Type], TypeID, SourceID)
			select		A.AssetTypeID, 
						R.Uid,
						A.ID,
						A.Object,
						A.ObjectID,
						@Object,
						@ObjectID,
						R.SourceID 
			from		integration.ExecutionAsset R
						left join Asset A on A.AssetTypeID = @AssetTypeID and A.SourceID = R.SourceID
			where		R.ExecutionID = @ExecutionID 
						and R.SynchedAssetTypeID = @SynchedAssetTypeID
			group by	A.AssetTypeID, 
						R.Uid,
						A.ID,
						A.Object,
						A.ObjectID,
						R.SourceID;

		drop table if exists #Context;
		create table #Context (SourceID nvarchar(250), RawValue nvarchar(max), [ParentContextPosition] int);

		insert into #Context
			select	A.SourceID,
					RF.FieldValue,
					F.[ParentContextPosition]
			from	integration.ExecutionAsset A
					inner join integration.ExecutionAssetField RF on RF.Uid = A.Uid and RF.FieldName = '_context'
					inner join [integration].[SynchedAssetTypeFieldItem] F on F.SynchedAssetTypeID = A.SynchedAssetTypeID and F.SourceField = RF.FieldName
					inner join integration.SynchedAssetType SAT on SAT.ID = A.SynchedAssetTypeID
			where	A.ExecutionID = @ExecutionID
					and A.SynchedAssetTypeID = @SynchedAssetTypeID
					and F.ArrayValueDelimiter is null;

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
			--insert into #Assets
				select	D.AssetTypeID,
						NULL,
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
						NULL,
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
						NULL,
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
						NULL,
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
						RF.FieldValue as Name
				from	#Assets C
						inner join integration.ExecutionAssetField RF on RF.Uid = C.ExecutionAssetUid and RF.Section = 1 and RF.FieldName = '_name' and C.[Action] = 'A'
						left join Asset P on P.SourceID = C.ParentSourceID

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
						RF.FieldValue,
						1
				from	#Assets C
						inner join integration.ExecutionAssetField RF on RF.Uid = C.ExecutionAssetUid and RF.Section = 1 and RF.FieldName = '_name' and C.[Action] = 'A'
						left join Asset P on P.SourceID = C.ParentSourceID

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
		CREATE NONCLUSTERED INDEX IX_TempField_Step1 ON #Field_Step1 ( [SourceFieldName], [ArrayValueDelimiter], [ArrayValueFieldName]) INCLUDE ([SourceID],[FieldTypeID],[RawValue]);
		CREATE NONCLUSTERED INDEX IX_TempField_Step1_FieldType_Object ON #Field_Step1 (FieldTypeID ASC, Object ASC, ObjectID ASC) INCLUDE (NewValue);

		insert into #Field_Step1
			select	@AssetTypeID as AssetTypeID,
					EA.SourceID,
					A.ID, A.Object, A.ObjectID,
					FT.ID,
					RF.FieldName,
					RF.FieldValue,
					F.[ParentContextPosition], 
					F.[IsArray], 
					F.DefaultValue, 
					F.[ArrayValueDelimiter], 
					F.[ArrayValueFieldName],
					NULL, NULL
			from	integration.ExecutionAsset EA
					inner join integration.ExecutionAssetField RF on RF.Uid = EA.Uid and RF.Section = 1
					inner join [integration].[SynchedAssetTypeFieldItem] F on F.SynchedAssetTypeID = EA.SynchedAssetTypeID and F.SourceField = RF.FieldName
					inner join Asset A on A.AssetTypeID = @AssetTypeID and A.SourceID = EA.SourceID
					left join FieldType FT on FT.AssetTypeID = @AssetTypeID and FT.Name = F.TargetField
			where	EA.ExecutionID = @ExecutionID
					and EA.SynchedAssetTypeID = @SynchedAssetTypeID;

		BEGIN	-- Process array value-delimited fields
			update	T
			set		NewValue = S.NewValue
			from	#Field_Step1 T
					inner join	(
								select		J.SourceID,
											J.FieldTypeID,
											STRING_AGG(P.Val, ' / ')  as NewValue
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
			CREATE CLUSTERED INDEX CIX_TempEnumValueTable ON #EnumValueTable (PropertyName, Code);
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
		using		(
					select	*
					from	#Field_Step1
					where FieldTypeID is not null
					) S
		on          (
						T.FieldTypeID = S.FieldTypeID and 
						T.ObjectType = S.Object and
						T.ObjectID = S.ObjectID
					)
		when matched and ( (T.Value <> S.NewValue) OR (T.Value is null) OR (S.NewValue is null and T.Value is not null) ) then
			update set
					T.Value = S.NewValue,
					T.FormattedValue = S.NewValue
		when not matched by target then
			insert  (FieldTypeID, ObjectType, ObjectID, Value, FormattedValue)
			values  (S.FieldTypeID, S.Object, S.ObjectID, S.NewValue, S.NewValue);

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
					,A.SourceID
					,RIIF._type
					,RIIF._id
			from	integration.ExecutionAsset A
					inner join integration.ExecutionAssetField RF on RF.Uid = A.Uid and RF.Section = 2
					inner join [integration].[SynchedAssetTypeRelationItem] R on R.SynchedAssetTypeID = A.SynchedAssetTypeID and R.[SourceField] = RF.FieldName
					cross apply OPENJSON(RF.FieldValue) with (_type nvarchar(max) '$._type', _id nvarchar(max) '$._id') RIIF
			where	A.ExecutionID = @ExecutionID
					and A.SynchedAssetTypeID = @SynchedAssetTypeID
					and RIIF._type is not null;

		drop table if exists #Rel_Step2;
		create table #Rel_Step2 (
			SourceID nvarchar(250),
			IntersectTypeID int,
			SubjectAssetTypeID int, SubjectSourceID nvarchar(250), SubjectAssetID bigint, Subject varchar(50), SubjectID int,
			ObjectAssetTypeID int, ObjectSourceID nvarchar(250), ObjectAssetID bigint, Object varchar(50), ObjectID int,
			IntersectID int, [Action] char(1)
		);
		CREATE NONCLUSTERED INDEX IX_TempRel_Step2_SubjectObject_Include ON #Rel_Step2 ([Action],[Subject],[SubjectID],[Object],[ObjectID]) INCLUDE ([IntersectTypeID]);
		--34454
		
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
		set		[Action] = 'A'
		where	IntersectID is null;

--select SubjectAssetID, ObjectAssetID from #Rel_Step2 where [Action] = 'A' group by SubjectAssetID, ObjectAssetID having count(1) > 1
--select * from #Rel_Step2 where [Action] = 'A' and SubjectAssetID = 1088 and ObjectAssetID = 886

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
					left join #Rel_Step2 SI on SI.IntersectID = I.ID
			where	SI.IntersectID is null;

		--END Query for records we need to delete.

		BEGIN	-- Try to process previously unresolved relationships.

			-- Resolve the missing subject information from these as-yet unresolved relationships.
			update	U
			set		U.SubjectAssetID = A.ID,
					U.Subject = A.Object,
					U.SubjectID = A.ObjectID
			from	[integration].[ExecutionUnresolvedRelationItem] U
					inner join #IntersectTypes IT on IT.ID = U.IntersectTypeID and U.SubjectAssetID is null 
					inner join Asset A on A.AssetTypeID = U.SubjectAssetTypeID and A.SourceID = U.SubjectSourceID;

			-- Resolve the missing object information form these as-yet unresolved relationships.
			update	U
			set		U.ObjectAssetID = A.ID,
					U.Object = A.Object,
					U.ObjectID = A.ObjectID
			from	[integration].[ExecutionUnresolvedRelationItem] U
					inner join #IntersectTypes IT on IT.ID = U.IntersectTypeID and U.ObjectAssetID is null 
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
				from	[integration].[ExecutionUnresolvedRelationItem] U
						inner join #IntersectTypes IT on IT.ID = U.IntersectTypeID 
														and SubjectAssetID is not null 
														and ObjectAssetID is not null;
		END

		--begin Add
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, CreatedBy, UpdatedBy)
			select	distinct
					T.IntersectTypeID,
					T.Subject, T.SubjectID, T.Object, T.ObjectID,
					@ResourceID, @ResourceID
			from	#Rel_Step2 T
					left join [Intersect] S on T.[Action] = 'A' and S.IntersectTypeID = T.IntersectTypeID and S.Subject = T.Subject and S.SubjectID = T.SubjectID and S.Object = T.Object and S.ObjectID = T.ObjectID
			where	T.[Action] = 'A'
					and T.Subject is not null and T.SubjectID is not null and T.Object is not null and T.ObjectID is not null
					and S.ID is null;

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

		-- Clean up the unresolved table with items where both sides have been resolved.
		delete	[integration].[ExecutionUnresolvedRelationItem]
		where	SubjectAssetID is not null 
				and ObjectAssetID is not null;

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

			-- Log the specific intersects that are to be removed.
			insert into integration.ExecutionAssetTypeMetricRelationshipLog (ExecutionID, SynchedAssetTypeID, [Action], SubjectSourceID, ObjectSourceID, IntersectID)
				select	@ExecutionID, 
						@SynchedAssetTypeID,
						[Action],
						SubjectSourceID, 
						ObjectSourceID, 
						IntersectID
				from	#Rel_Step2
				where	[Action] = 'D';

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
					,R.ResponsibilityTypeID
					,substring(rtrim(ltrim(J.FieldValue)), 1, 250)
			from	integration.ExecutionAsset E
					inner join integration.ExecutionAssetField J on J.Uid = E.Uid and J.Section = 3
					inner join Asset A on A.AssetTypeID = @AssetTypeID and A.SourceID = E.SourceID
					inner join [integration].[SynchedAssetTypeRoleItem] R on R.SynchedAssetTypeID = E.SynchedAssetTypeID and R.SourceIdField = J.FieldName
			where	E.ExecutionID = @ExecutionID
					and E.SynchedAssetTypeID = @SynchedAssetTypeID;

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
		declare @IGCAssetRelationshipBreakdown nvarchar(2500),
				@GovernAssetRelationshipBreakdown nvarchar(2500),
				@IGCAssetResponsibilityBreakdown nvarchar(2500),
				@GovernAssetResponsibilityBreakdown nvarchar(2500),
				@GovernAssetRelationshipCount int = 0,
				@GovernAssetResponsibilityCount int = 0,
				@MisalignedResponsibilities int = 0,
				@RetrievedAssetRelationshipCount int = 0,
				@RetrievedAssetResponsibilityCount int = 0,
				@GovernAssetCount int = 0;

		set @IGCAssetRelationshipBreakdown		=	(
													select		O.intersectTypeId,
																O.[type],
																count(1) as [count]
													from		(
																select		RIT.intersectTypeId,
																			case when RI.IsSubject = 1 then A.SourceID else R.[id] end as SubjectID,
																			case when RI.IsSubject = 0 then A.SourceID else R.[id] end as ObjectID,
																			RIT.SourceAssetType as [type]
																from		integration.ExecutionAsset A
																			inner join integration.ExecutionAssetField C on C.Uid = A.Uid and C.Section = 2
																			cross apply OPENJSON(C.FieldValue) with (id nvarchar(500) '$._id', [type] nvarchar(500) '$._type') R
																			inner join integration.SynchedAssetTypeRelationItem RI on RI.SynchedAssetTypeID = A.SynchedAssetTypeID and RI.[SourceField] = C.FieldName
																			inner join [integration].[SynchedAssetTypeRelationItemTarget] RIT on RIT.SynchedAssetTypeRelationItemID = RI.ID and R.[type] COLLATE DATABASE_DEFAULT like RIT.SourceAssetType + '%'
																where		A.ExecutionID = @ExecutionID
																			and A.SynchedAssetTypeID = @SynchedAssetTypeID
																			and C.FieldName in (select SourceField from [integration].[SynchedAssetTypeRelationItem] where SynchedAssetTypeID = @SynchedAssetTypeID)
																group by	RIT.IntersectTypeID,
																			case when RI.IsSubject = 1 then A.SourceID else R.[id] end,
																			case when RI.IsSubject = 0 then A.SourceID else R.[id] end,
																			RIT.SourceAssetType
																) O
													group by	O.intersectTypeId,
																O.[type]
													for json path
													);

		set @GovernAssetRelationshipBreakdown	=	(
													select		O.IntersectTypeID as 'intersectTypeId',
																O.SourceAssetType as 'type',
																count(1) as 'count'
													from		(
																select		RIT.IntersectTypeID,
																			RIT.SourceAssetType
																from		integration.SynchedAssetTypeRelationItem RI
																			inner join [integration].[SynchedAssetTypeRelationItemTarget] RIT on RIT.SynchedAssetTypeRelationItemID = RI.ID
																where		RI.SynchedAssetTypeID = @SynchedAssetTypeID 
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
																					) and A.AssetTypeID = @AssetTypeID
													group by	O.IntersectTypeID,
																O.SourceAssetType
													for json path
													);

		set @IGCAssetResponsibilityBreakdown	=	(
													select		RT.Name as 'role',
																count(1) as 'count'
													from		integration.ExecutionAsset A
																inner join integration.ExecutionAssetField C on C.Uid = A.Uid and C.Section = 3
																inner join integration.SynchedAssetTypeRoleItem RI on RI.SynchedAssetTypeID = A.SynchedAssetTypeID and C.FieldName = RI.[SourceIdField] and C.FieldValue is not null and C.FieldValue <> ''
																inner join ResponsibilityType RT on RT.ID = RI.ResponsibilityTypeID
													where		A.ExecutionID = @ExecutionID  
																and A.SynchedAssetTypeID = @SynchedAssetTypeID 
													group by	RT.Name
													for json path
													);

		set @GovernAssetResponsibilityBreakdown =	(
													select		I_RT.Name as 'role',
																count(1) as 'count'
													from		integration.ExecutionAsset I_EA
																inner join Asset I_A on I_A.AssetTypeID = @AssetTypeID and I_A.SourceID = I_EA.SourceID
																inner join ResponsibilityTypeRelationOverrideItem I_R on I_R.AssetID = I_A.ID
																inner join ResponsibilityType I_RT on I_RT.ID = I_R.ResponsibilityTypeID
																inner join integration.SynchedAssetTypeRoleItem I_RI on I_RI.SynchedAssetTypeID = @SynchedAssetTypeID and I_RI.ResponsibilityTypeID = I_RT.ID
													where		I_EA.ExecutionID = @ExecutionID  
																and I_EA.SynchedAssetTypeID = @SynchedAssetTypeID 
													group by	I_RT.Name
													for json path
													);

		select	@GovernAssetRelationshipCount	= count(1)
		from	(
				select		RIT.IntersectTypeID
				from		integration.SynchedAssetTypeRelationItem RI
							inner join [integration].[SynchedAssetTypeRelationItemTarget] RIT on RI.SynchedAssetTypeID = @SynchedAssetTypeID 
																								and RIT.SynchedAssetTypeRelationItemID = RI.ID
				group by	RIT.IntersectTypeID
				) O
				inner join [Intersect] I on I.IntersectTypeID = O.IntersectTypeID and I.State not in (2,3);

		select	@GovernAssetResponsibilityCount = count(1)
		from	integration.SynchedAssetTypeRoleItem RO
				inner join ResponsibilityTypeRelationOverrideItem O on O.ResponsibilityTypeID = RO.ResponsibilityTypeID and RO.SynchedAssetTypeID = @SynchedAssetTypeID and RO.[Active] = 1 and O.SecurityAsset = 'R' and O.AssetID > 0
				inner join Asset A on A.ID = O.AssetID
				inner join integration.SynchedAssetType SAT on SAT.ID = RO.SynchedAssetTypeID and SAT.AssetTypeID = A.AssetTypeID;

		select	@MisalignedResponsibilities		= count(1)
		from	integration.SynchedAssetTypeRoleItem RO
				inner join ResponsibilityTypeRelationOverrideItem O on O.ResponsibilityTypeID = RO.ResponsibilityTypeID 
																	and RO.SynchedAssetTypeID = @SynchedAssetTypeID 
																	and RO.[Active] = 1 
																	and O.SecurityAsset = 'R' 
																	and O.AssetID > 0
				inner join Asset A on A.ID = O.AssetID
				inner join integration.SynchedAssetType SAT on SAT.ID = RO.SynchedAssetTypeID and SAT.AssetTypeID = A.AssetTypeID	
				inner join integration.ExecutionAsset EA on EA.ExecutionID = @ExecutionID  and EA.SynchedAssetTypeID = @SynchedAssetTypeID and A.SourceID = EA.SourceID
				inner join integration.ExecutionAssetField C on C.Uid = EA.Uid and C.Section = 2
				inner join FieldType RFT on RFT.Object = 'ResourceType' and RFT.Name = 'UserId'
				inner join Field RF on RF.FieldTypeID = RFT.ID and RF.ObjectType = 'Resource' and RF.ObjectID = O.SecurityAssetID
		where	C.FieldName = RO.SourceIdField
				and RF.Value <> C.FieldValue;

		select	@RetrievedAssetRelationshipCount = count(1)
		from	(
				select		RIT.intersectTypeId,
							case when RI.IsSubject = 1 then A.SourceID else R.[id] end as SubjectID,
							case when RI.IsSubject = 0 then A.SourceID else R.[id] end as ObjectID,
							RIT.SourceAssetType as [type]
				from		integration.ExecutionAsset A
						    inner join integration.ExecutionAssetField C on C.Uid = A.Uid and C.Section = 2
							cross apply OPENJSON(C.FieldValue) with (id nvarchar(500) '$._id', [type] nvarchar(500) '$._type') R
							inner join integration.SynchedAssetTypeRelationItem RI on RI.SynchedAssetTypeID = A.SynchedAssetTypeID and RI.[SourceField] = C.FieldName
							inner join [integration].[SynchedAssetTypeRelationItemTarget] RIT on RIT.SynchedAssetTypeRelationItemID = RI.ID and R.[type] COLLATE DATABASE_DEFAULT like RIT.SourceAssetType+'%'
				where		A.ExecutionID = @ExecutionID  
							and A.SynchedAssetTypeID = @SynchedAssetTypeID
				group by	RIT.IntersectTypeID,
							case when RI.IsSubject = 1 then A.SourceID else R.[id] end,
							case when RI.IsSubject = 0 then A.SourceID else R.[id] end,
							RIT.SourceAssetType
				) O;

		select	@RetrievedAssetResponsibilityCount = count(1)
		from	integration.ExecutionAsset A
				inner join integration.ExecutionAssetField C on C.Uid = A.Uid and C.Section = 3
		where	A.ExecutionID = @ExecutionID  
				and A.SynchedAssetTypeID = @SynchedAssetTypeID 
				and C.FieldName in (select [SourceIdField] from integration.SynchedAssetTypeRoleItem where SynchedAssetTypeID = A.SynchedAssetTypeID)
				and C.FieldValue <> '';

		declare @hasLevel bit = 0,
				@hasCriteria bit = 0;
		
		select	@hasLevel = IIF([Level] is null, 0, 1)
		from	integration.SynchedAssetType
		where	ID = @SynchedAssetTypeID;
		
		select	@hasCriteria = IIF(count(1) > 0, 1, 0)
		from	integration.SynchedAssetTypeFieldItem 
		where	SynchedAssetTypeID = @SynchedAssetTypeID
				and ConsiderWhenDeleting = 1;

		if @hasCriteria = 1 and @hasLevel = 1
		begin
			select		@GovernAssetCount = count(1)
			from		Asset A
						inner join integration.SynchedAssetType S on S.AssetTypeID = A.AssetTypeID and S.ID = @SynchedAssetTypeID
						cross apply dbo.GetAssetLevelById(A.ID) L
						cross apply (
							select		F.AssetID
							from		integration.SynchedAssetTypeFieldItem SF
										inner join FieldType T on T.Name = SF.TargetField and SF.ConsiderWhenDeleting = 1 and SF.SynchedAssetTypeID = S.ID and T.AssetTypeID = S.AssetTypeID
										inner join Field F on F.FieldTypeID = T.ID and F.Value = SF.DefaultValue and F.AssetID = A.ID
							group by	F.AssetID
						) D
			where		A.AssetTypeID = @AssetTypeID
						and L.[Level] = S.[Level]
		end;

		if @hasCriteria = 1 and @hasLevel = 0
		begin
			select		@GovernAssetCount = count(1)
			from		Asset A
						inner join integration.SynchedAssetType S on S.AssetTypeID = A.AssetTypeID and S.ID = @SynchedAssetTypeID
						cross apply (
							select		F.AssetID
							from		integration.SynchedAssetTypeFieldItem SF
										inner join FieldType T on T.Name = SF.TargetField and SF.ConsiderWhenDeleting = 1 and SF.SynchedAssetTypeID = S.ID and T.AssetTypeID = S.AssetTypeID
										inner join Field F on F.FieldTypeID = T.ID and F.Value = SF.DefaultValue and F.AssetID = A.ID
							group by	F.AssetID
						) D
			where		A.AssetTypeID = @AssetTypeID
		end;

		if @hasLevel = 1 and @hasCriteria = 0
		begin
			select		@GovernAssetCount = count(1)
			from		Asset A
						inner join integration.SynchedAssetType S on S.AssetTypeID = A.AssetTypeID and S.ID = @SynchedAssetTypeID
						cross apply dbo.GetAssetLevelById(A.ID) L
			where		A.AssetTypeID = @AssetTypeID
						and L.[Level] = S.[Level]
		end;

		if @hasLevel = 0 and @hasCriteria = 0
		begin
			select		@GovernAssetCount = count(1)
			from		Asset A
						inner join integration.SynchedAssetType S on S.AssetTypeID = A.AssetTypeID and S.ID = @SynchedAssetTypeID
			where		A.AssetTypeID = @AssetTypeID
		end
		
		merge	integration.ExecutionAssetTypeMetric as T
		using	(
				select		A.AssetTypeID,
							ET.ExecutionID,
							ET.SynchedAssetTypeID,
							ET.CurrentSourceAssetCount,
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
				) S
		on		(T.ExecutionID = S.ExecutionID and T.SynchedAssetTypeID = S.SynchedAssetTypeID)
		when matched then
			update set
			T.AssetsWithMissingDefinition = S.AssetsWithMissingDefinition,
			T.AssetsWithMissingRelationships = S.AssetsWithMissingRelationships,
			T.AssetsWithMissingResponsibilities = S.AssetsWithMissingResponsibilities,
			T.AssetsWithErrors = S.AssetsWithErrors,
			T.RetrievedAssetCount = S.RetrievedAssetCount,
			T.RetrievedAssetRelationshipCount = @RetrievedAssetRelationshipCount,
			T.RetrievedAssetResponsibilityCount = @RetrievedAssetResponsibilityCount,
			T.GovernAssetCount = @GovernAssetCount,
			T.GovernAssetRelationshipCount = @GovernAssetRelationshipCount,
			T.GovernAssetResponsibilityCount = @GovernAssetResponsibilityCount,
			T.IGCAssetRelationshipBreakdown = @IGCAssetRelationshipBreakdown,
			T.GovernAssetRelationshipBreakdown = @GovernAssetRelationshipBreakdown,
			T.IGCAssetResponsibilityBreakdown = @IGCAssetResponsibilityBreakdown,
			T.GovernAssetResponsibilityBreakdown = @GovernAssetResponsibilityBreakdown,
			T.MisalignedResponsibilities = @MisalignedResponsibilities
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
					GovernAssetResponsibilityBreakdown,
					MisalignedResponsibilities
					)
			values  (
					S.ExecutionID,
					S.SynchedAssetTypeID,
					S.AssetsWithMissingDefinition, 
					S.AssetsWithMissingRelationships,
					S.AssetsWithMissingResponsibilities,
					S.AssetsWithErrors,
					S.RetrievedAssetCount,
					@RetrievedAssetRelationshipCount,
					@RetrievedAssetResponsibilityCount,
					@GovernAssetCount,
					@GovernAssetRelationshipCount,
					@GovernAssetResponsibilityCount,
					@IGCAssetRelationshipBreakdown,
					@GovernAssetRelationshipBreakdown,
					@IGCAssetResponsibilityBreakdown,
					@GovernAssetResponsibilityBreakdown,
					@MisalignedResponsibilities
					);
	end
	--END METRICS CAPTURE
end
GO;