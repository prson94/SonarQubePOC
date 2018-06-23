CREATE TABLE [integration].[ExecutionRoleItem](
	[ID] [uniqueidentifier] NOT NULL,
	[ExecutionID] [bigint] NOT NULL,
	[SourceID] [nvarchar](250) NOT NULL,
	[RoleName] [nvarchar](250) NOT NULL,
	[UserIdentifier] [nvarchar](250) NOT NULL,
	[SynchedAssetTypeID] [int] NOT NULL,
	CONSTRAINT [PK_IntegrationExecutionRoleItem] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [integration].[ExecutionRoleItem] ADD  CONSTRAINT [DF_IntegrationExecutionRoleItem_ID]  DEFAULT (newid()) FOR [ID]
GO

ALTER TABLE [integration].[ExecutionRoleItem] ADD  CONSTRAINT [DF_IntegrationExecutionRoleItem_SynchedAssetTypeID]  DEFAULT ((0)) FOR [SynchedAssetTypeID]
GO

ALTER TABLE [integration].[ExecutionRoleItem]  WITH CHECK ADD  CONSTRAINT [FK_IntegrationExecutionRoleItem_IntegrationExecution] FOREIGN KEY([ExecutionID]) REFERENCES [integration].[Execution] ([ID])
GO

ALTER TABLE [integration].[ExecutionRoleItem] CHECK CONSTRAINT [FK_IntegrationExecutionRoleItem_IntegrationExecution]
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
--where		E.SynchedAssetTypeID = 1
		group by	E.ExecutionID,
					E.SynchedAssetTypeID,
					E.CurrentSourceAssetCount
	/*
	select	* 
	from	#fullSynched
	*/
	DROP TABLE IF EXISTS #roles

	create table #roles (ResponsibilityTypeID int, AssetID bigint, SecurityAssetID int)
	insert into #roles 
		select	distinct
				RT.ID as ResponsibilityTypeID,
				A.ID as AssetID,
				F.ObjectID as SecurityAssetID
		from	integration.ExecutionRoleItem R
				inner join	(
							select		max(ExecutionID) as ExecutionID,
										SynchedAssetTypeID,
										SourceID,
										RoleName
							from		integration.ExecutionRoleItem
							group by	SynchedAssetTypeID,
										SourceID,
										RoleName
							) MR on MR.ExecutionID = R.ExecutionID and MR.SynchedAssetTypeID = R.SynchedAssetTypeID and MR.SourceID = R.SourceID and MR.RoleName = R.RoleName
				inner join Asset A on A.SourceID = R.SourceID
				inner join Field F on F.ObjectType = 'Resource' and F.Value = R.UserIdentifier and F.FieldTypeID in (select ID from FieldType where Object = 'ResourceType' and ObjectID = 1 and Name = 'UserId')
				inner join ResponsibilityType RT on RT.Name = R.RoleName

	delete	T
	from	ResponsibilityTypeRelationOverrideItem T
			left join #roles S on S.ResponsibilityTypeID = T.ResponsibilityTypeID and S.AssetID = T.AssetID and T.SecurityAsset = 'R' and S.SecurityAssetID = T.SecurityAssetID
	where	S.AssetID is null
			and T.AssetID in (select AssetID from #roles)

	--Get the Intersect.ID to delete. These are the ones that are no longer present after a full refresh.
	delete [Intersect] where ID in (
		select	I.ID
		from	[Intersect] I
				inner join	(
							select	R.IntersectTypeID
							from	integration.ExecutionRelationItem R
									inner join #fullSynched E on E.ExecutionID = R.ExecutionID			
							group by R.IntersectTypeID
							) E on E.IntersectTypeID = I.IntersectTypeID
				inner join Asset S on S.Object = I.Subject and S.ObjectID = I.SubjectID
				inner join Asset O on O.Object = I.Object and O.ObjectID = I.ObjectID
				left join (
					select	R.IntersectTypeID,
							R.SubjectSourceID,
							R.ObjectSourceID
					from	integration.ExecutionRelationItem R
							inner join #fullSynched E on E.ExecutionID = R.ExecutionID			
				) SI on SI.IntersectTypeID = I.IntersectTypeID and SI.SubjectSourceID = S.SourceID and SI.ObjectSourceID = O.SourceID 
		where SI.IntersectTypeID is null
	)

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
	select	@max = coalesce(max(ID),0) from #deletes
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

CREATE NONCLUSTERED INDEX [IX_CacheAssetResponsibility_RuleID_Include] ON [cache].[AssetResponsibility]([RuleID] ASC) INCLUDE([OverrideItemID]);
GO

ALTER TABLE [dbo].[CommentVote] DROP CONSTRAINT [FK_Comment_ID]
GO

alter table Field alter column UpdatedBy int not null
GO

ALTER TRIGGER [dbo].[FieldType_AfterUpsert]
   ON  [dbo].[FieldType] 
   AFTER INSERT,UPDATE
AS 

		IF ((COLUMNS_UPDATED() & 1073741824)=1073741824)
		begin
			insert into [dbo].[Testing_FieldTypeIn]
				select  * from  inserted
		end
		else
		begin
			insert into [dbo].[Testing_FieldTypeOut]
				select  * from inserted
		end 

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


ALTER TABLE [fusion].[RulePromotion] DROP CONSTRAINT [DF_RulePromotion_CreatedOn]
GO

ALTER TABLE [fusion].[RulePromotion] ADD  CONSTRAINT [DF_RulePromotion_CreatedOn]  DEFAULT (getutcdate()) FOR [CreatedOn]
GO

ALTER TABLE [fusion].[RulePromotion] DROP CONSTRAINT [DF_RulePromotion_UpdatedOn]
GO

ALTER TABLE [fusion].[RulePromotion] ADD  CONSTRAINT [DF_RulePromotion_UpdatedOn]  DEFAULT (getutcdate()) FOR [UpdatedOn]
GO



declare @schema_name nvarchar(256)
declare @table_name nvarchar(256)
declare @col_name nvarchar(256)
declare @Command  nvarchar(1000)

set @schema_name = N'fusion'
set @table_name = N'RulePromotion'
set @col_name = N'ObjectTypeID'

select	@Command = 'ALTER TABLE ' + @schema_name + '.' + @table_name + ' drop constraint ' + d.name
from	sys.tables t
		join sys.default_constraints d on d.parent_object_id = t.object_id
		join sys.columns c on c.object_id = t.object_id and c.column_id = d.parent_column_id
where	t.name = @table_name
		and t.schema_id = schema_id(@schema_name)
		and c.name = @col_name

--select @Command
if @Command is not null
begin
	execute (@Command)
end
GO

ALTER TABLE [fusion].[RulePromotion] ADD  CONSTRAINT [DF_RulePromotion_ObjectTypeID]  DEFAULT ((-1)) FOR [ObjectTypeID]
GO


ALTER VIEW [dbo].[FieldLookupValue]
AS
	/*SELECT	T.ID as FieldTypeID,
			T.LookupObjectType,
			T.LookupObjectID,
			COALESCE(A.ID, R.ResourceID, L.ID, RI.ID, RIT.ID,TAX.ID,TAXTYPE.ID) as Value,	
			utility.GetFormattedFieldLookupValue(T.Type, coalesce(T.LookupEditFormat, T.LookupDisplayFormat), T.LookupObjectType, T.LookupObjectID, COALESCE(A.ID, R.ResourceID, L.ID, RI.ID, RIT.ID,TAX.ID,TAXTYPE.ID)) as Text
	FROM	FieldType T 
			LEFT JOIN Artifact A ON T.LookupObjectType = 'Artifact' AND T.LookupObjectID = A.ArtifactTypeID
			LEFT JOIN reporting.Global_Resource R ON T.LookupObjectType = 'Resource' --AND T.LookupObjectID = R.ResourceTypeID
			LEFT JOIN Lookup L ON T.LookupObjectType = 'Lookup' AND T.LookupObjectID = L.LookupTypeID
			LEFT JOIN ReferenceItem RI ON T.LookupObjectType = 'ReferenceItem' AND T.LookupObjectID = RI.ReferenceItemTypeID
			LEFT JOIN ReferenceItemType RIT ON T.LookupObjectType = 'ReferenceItemType' --AND T.LookupObjectID = RIT.ID
			LEFT JOIN Taxonomy TAX ON T.LookupObjectType = 'Taxonomy' AND T.LookupObjectID = TAX.TaxonomyTypeID
			LEFT JOIN TaxonomyType TAXTYPE ON T.LookupObjectType = 'TaxonomyType'
	WHERE	T.LookupObjectType is not null
			AND COALESCE(A.ID, R.ResourceID, L.ID, RI.ID, RIT.ID,TAX.ID,TAXTYPE.ID) IS NOT NULL*/

	/* Artifacts with no parents and matching display value just show the display value */
	SELECT	T.ID as FieldTypeID,
			T.LookupObjectType,
			T.LookupObjectID,
			A.ID as Value,			
			A.DisplayValue as Text
	FROM	FieldType T 			
			INNER JOIN AssetDetail A on T.LookupObjectType = 'Artifact' AND T.LookupObjectID = A.TypeID			
			INNER JOIN AssetType ATT on (A.AssetTypeID = ATT.ID)
	WHERE	T.LookupObjectType is not null AND A.ID IS NOT NULL and not exists ( select 1 from intersecttype it inner join [predicate] p on it.predicateid = p.id and p.[type] = 3 and it.[subject] = 'ArtifactType' and it.[object] = 'ArtifactType' and it.objectid = A.TypeID)
			and  coalesce(T.LookupEditFormat, T.LookupDisplayFormat) = ATT.DisplayFormat

	UNION ALL

	/* Artifacts with no parents and different display value just show the display value */
	SELECT	T.ID as FieldTypeID,
			T.LookupObjectType,
			T.LookupObjectID,
			A.ID as Value,	
			utility.GetFormattedFieldLookupValue(T.Type, coalesce(T.LookupEditFormat, T.LookupDisplayFormat), T.LookupObjectType, T.LookupObjectID, A.ID) as Text		
	FROM	FieldType T 
			INNER JOIN Artifact A ON T.LookupObjectType = 'Artifact' AND T.LookupObjectID = A.ArtifactTypeID						
			INNER JOIN AssetType ATT on (Att.[Object] = 'ArtifactType' and ATT.ObjectID = A.ArtifactTypeID)
	WHERE	T.LookupObjectType is not null AND A.ID IS NOT NULL and not exists ( select 1 from intersecttype it inner join [predicate] p on it.predicateid = p.id and p.[type] = 3 and it.[subject] = 'ArtifactType' and it.[object] = 'ArtifactType' and it.objectid = A.ArtifactTypeID)
			and  coalesce(T.LookupEditFormat, T.LookupDisplayFormat) <> ATT.DisplayFormat

	UNION ALL
	/* Artifacts with parents need to show the path which is slower */
	SELECT	T.ID as FieldTypeID,
			T.LookupObjectType,
			T.LookupObjectID,
			A.ID as Value,	
			utility.GetFormattedFieldLookupValue(T.Type, coalesce(T.LookupEditFormat, T.LookupDisplayFormat), T.LookupObjectType, T.LookupObjectID, A.ID) as Text		
	FROM	FieldType T 
			INNER JOIN Artifact A ON T.LookupObjectType = 'Artifact' AND T.LookupObjectID = A.ArtifactTypeID						
	WHERE	T.LookupObjectType is not null AND A.ID IS NOT NULL and exists ( select 1 from intersecttype it inner join [predicate] p on it.predicateid = p.id and p.[type] = 3 and it.[subject] = 'ArtifactType' and it.[object] = 'ArtifactType' and it.objectid = A.ArtifactTypeID)

	UNION ALL

	SELECT	T.ID as FieldTypeID,
			T.LookupObjectType,
			T.LookupObjectID,
			R.ResourceID as Value,	
			utility.GetFormattedFieldLookupValue(T.Type, coalesce(T.LookupEditFormat, T.LookupDisplayFormat), T.LookupObjectType, T.LookupObjectID, R.ResourceID) as Text
	FROM	FieldType T 
			INNER JOIN reporting.Global_Resource R ON T.LookupObjectType = 'Resource'			
	WHERE	T.LookupObjectType is not null and R.ResourceID IS NOT NULL

	UNION ALL

	SELECT	T.ID as FieldTypeID,
			T.LookupObjectType,
			T.LookupObjectID,
			L.ID as Value,	
			utility.GetFormattedFieldLookupValue(T.Type, coalesce(T.LookupEditFormat, T.LookupDisplayFormat), T.LookupObjectType, T.LookupObjectID, L.ID) as Text
	FROM	FieldType T 			
			INNER JOIN Lookup L ON T.LookupObjectType = 'Lookup' AND T.LookupObjectID = L.LookupTypeID			
	WHERE	T.LookupObjectType is not null AND L.ID IS NOT NULL

	UNION ALL

	SELECT	T.ID as FieldTypeID,
			T.LookupObjectType,
			T.LookupObjectID,
			RI.ID as Value,	
			utility.GetFormattedFieldLookupValue(T.Type, coalesce(T.LookupEditFormat, T.LookupDisplayFormat), T.LookupObjectType, T.LookupObjectID, RI.ID) as Text
	FROM	FieldType T 			
			INNER JOIN ReferenceItem RI ON T.LookupObjectType = 'ReferenceItem' AND T.LookupObjectID = RI.ReferenceItemTypeID
	WHERE	T.LookupObjectType is not null
			AND RI.ID IS NOT NULL

	UNION ALL

	SELECT	T.ID as FieldTypeID,
			T.LookupObjectType,
			T.LookupObjectID,
			RIT.ID as Value,	
			utility.GetFormattedFieldLookupValue(T.Type, coalesce(T.LookupEditFormat, T.LookupDisplayFormat), T.LookupObjectType, T.LookupObjectID, RIT.ID) as Text
	FROM	FieldType T 			
			INNER JOIN ReferenceItemType RIT ON T.LookupObjectType = 'ReferenceItemType'
	WHERE	T.LookupObjectType is not null AND RIT.ID IS NOT NULL

	UNION ALL

	SELECT	T.ID as FieldTypeID,
			T.LookupObjectType,
			T.LookupObjectID,
			TAX.ID as Value,	
			utility.GetFormattedFieldLookupValue(T.Type, coalesce(T.LookupEditFormat, T.LookupDisplayFormat), T.LookupObjectType, T.LookupObjectID, TAX.ID) as Text
	FROM	FieldType T 			
			INNER JOIN Taxonomy TAX ON T.LookupObjectType = 'Taxonomy' AND T.LookupObjectID = TAX.TaxonomyTypeID			
	WHERE	T.LookupObjectType is not null AND TAX.ID IS NOT NULL

	UNION ALL

	SELECT	T.ID as FieldTypeID,
			T.LookupObjectType,
			T.LookupObjectID,
			TAXTYPE.ID as Value,	
			utility.GetFormattedFieldLookupValue(T.Type, coalesce(T.LookupEditFormat, T.LookupDisplayFormat), T.LookupObjectType, T.LookupObjectID, TAXTYPE.ID) as Text
	FROM	FieldType T 			
			INNER JOIN TaxonomyType TAXTYPE ON T.LookupObjectType = 'TaxonomyType'
	WHERE	T.LookupObjectType is not null
			AND TAXTYPE.ID IS NOT NULL
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

			--insert into @h
			--	select	I.ID, null, F.ID, null 
			--	from	[IntersectDetail] I
			--			inner join FusionAttribute F on I.Subject = 'FusionAttribute' 
			--											and I.Object = 'FusionAttribute' 
			--											and (F.ID = I.SubjectID OR F.ID = I.ObjectID) 
			--											and F.FusionID = @ObjectID
			--											and I.PredicateType = 3

			insert into @h								
				select I.ID, null, F.ID, null 
				from [Intersect] I
				inner join IntersectType T on T.ID = I.IntersectTypeID
				inner join [Predicate] P on P.ID = T.PredicateID and P.[Type] = 3
				inner join FusionAttribute F on I.[Subject] = 'FusionAttribute' and I.[Object] = 'FusionAttribute'
					and (I.SubjectID = F.ID or I.ObjectID = F.ID) and F.FusionID = @ObjectID;

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

		if @Object = 'Intersect'
		begin
			update [Intersect] set Deleted = 1 where ID = @ObjectID
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
			and not exists (select 1 from #items i where i.SourceIntersectID = r.SourceIntersectID and i.TargetIntersectID = r.TargetIntersectID);
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

			insert into @links
					select	distinct
							S.SourceSubject + '.' + cast(S.SourceSubjectID as varchar) as [from],
							S.TargetSubject + '.' + cast(S.TargetSubjectID as varchar) as [to],
							'' as category
					from	#items S
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
					from	#items I
					left join Asset A on A.[Object] = I.SourceSubject and A.ObjectID = I.SourceSubjectID;;

					--perform this update separately to avoid duplicate in the above query
					update n
					set n.HasSourceRules = 1
					from @nodes n
					inner join #items i on n.[key] = (i.SourceSubject + '.' + cast(i.SourceSubjectID as varchar)) and i.SourceHasSourceRules = 1;

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
					from	#items I
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
GO

CREATE TABLE [dbo].[AssetDisplayFormatFieldTypes] (
    [AssetTypeID] INT            NOT NULL,
    [ObjectID]    INT            NOT NULL,
    [ObjectType]  VARCHAR (20)   NOT NULL,
    [FieldTypeID] INT            DEFAULT ((0)) NOT NULL,
    [Token]       NVARCHAR (300) NULL
);
GO

CREATE PROCEDURE UpdateAssetDisplayFormatFieldTypes	
AS
BEGIN
BEGIN TRANSACTION;
    SAVE TRANSACTION MySavePoint;
    
	BEGIN TRY
	delete from dbo.AssetDisplayFormatFieldTypes;

    INSERT INTO dbo.AssetDisplayFormatFieldTypes
             select	distinct
								T.ID as AssetTypeID,
								T.ObjectID,
								T.[Object] as ObjectType,
								COALESCE(FT.ID,0) as FieldTypeID,
								TF.Value as Token		
							from	
								dbo.assettype T
								cross apply string_split(replace(replace(T.DisplayFormat, '{', '|'),'}','|'), '|') TF									
								left join dbo.FieldType FT on FT.AssetTypeID = T.ID and FT.Name = TF.Value	
							where	(TF.value) <> ''
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION MySavePoint; -- rollback to MySavePoint
        END
    END CATCH
    COMMIT TRANSACTION 
END
GO

create view ArtifactTypeDisplayFormatFieldTypes
as
select	
		T.ID as AssetTypeID,
		T.ObjectID as ArtifactTypeID,
		FT.ID as FieldTypeID--,
		--0 as DisplayOrder
from	
	dbo.assettype T
	cross apply string_split(replace(replace(T.DisplayFormat, '{', '|'),'}','|'), '|') TF									
	inner join dbo.FieldType FT on FT.AssetTypeID = T.ID and FT.Name = TF.Value	
where	(TF.value) <> '' and T.Object = 'ArtifactType'
GO

CREATE FUNCTION [dbo].[GetArtifactDisplayValue]
(
	@Id bigint
)
RETURNS TABLE 

AS
RETURN 
(	
	/* support multiple items in display format */
	/*select		string_agg(ISNULL(F.FormattedValue, ' '), '') as DisplayValue
	from		dbo.Asset A				
				inner join ArtifactTypeDisplayFormatFieldTypes ATDF on ATDF.AssetTypeId = A.AssetTypeID
				inner join dbo.Field F on ATDF.FieldTypeID = F.FieldTypeID and F.AssetID = A.ID	
	where A.ID = @id*/

	/* support single item in display format */
	select		F.FormattedValue as DisplayValue
	from		dbo.Asset A				
				inner join ArtifactTypeDisplayFormatFieldTypes ATDF on ATDF.AssetTypeId = A.AssetTypeID
				inner join dbo.Field F on ATDF.FieldTypeID = F.FieldTypeID and F.AssetID = A.ID	
	where A.ID = @id
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
	select	IAD.ObjectAssetID as ID,
			IAD.ObjectID as ObjectID,
			IAD.SubjectID as ParentID,
            ID.DisplayValue as ParentDisplayValue,						
			PUrl.Url as ParentUrl							
				    from	[utility].IntersectAsset IAD							
                            inner join dbo.Asset IA on IA.Object = 'Artifact' and IA.ObjectID = IAD.SubjectID and IAD.PredicateType = 3
                            inner join dbo.AssetType IAT on IAT.ID = IA.AssetTypeID
                            cross apply [dbo].[GetArtifactDisplayValue](IA.ID) ID
							cross apply dbo.GetAssetUrl('Artifact', IAT.ObjectID, IAD.SubjectID) PUrl
					where IAD.[Object] = 'Artifact' and IAD.ObjectAssetID = @Id
)
GO

ALTER FUNCTION [dbo].[GetAssetDisplayValueById]
(
	@Id bigint
)
RETURNS TABLE 
AS
RETURN 
(
	/*select 
		coalesce(F.FormattedValue, F.value) as DisplayValue
	from dbo.Asset A
		inner join dbo.AssetType T on T.ID = A.AssetTypeID
		inner join dbo.FieldType FT on FT.AssetTypeID = T.ID and FT.Name = replace(replace(T.DisplayFormat, '{', ''),'}','')
		inner join dbo.Field F on F.FieldTypeID = FT.ID and F.AssetID = A.ID
	where A.ID = @id and T.DisplayFormat = '{Name}'
	union	*/
		select	top 1	
					string_agg(coalesce(D.FormattedValue, D.value), '') as DisplayValue
		from		dbo.Asset A
					inner join dbo.AssetType T on T.ID = A.AssetTypeID 					
					outer apply (
								select	TF.value,
										coalesce(case when TF.Value = 'FirstName' then R.FirstName + ' ' else R.LastName end, F.FormattedValue, RI.Code, FA.Name) as FormattedValue
								from	string_split(replace(replace(T.DisplayFormat, '{', '|'),'}','|'), '|') TF									
										left join dbo.FieldType FT on FT.AssetTypeID = T.ID and FT.Name = TF.Value
										left join dbo.Field F on F.FieldTypeID = FT.ID and F.AssetID = A.ID
										left join dbo.ReferenceItem RI on TF.Value = 'Code' and A.Object = 'ReferenceItem' and RI.ID = A.ObjectID
										left join dbo.FusionAttribute FA on TF.Value = 'Name' and A.Object = 'FusionAttribute' and FA.ID = A.ObjectID
										left join reporting.Global_resource R on TF.Value in ('FirstName', 'LastName') and A.Object = 'Resource' and R.ResourceID = A.ObjectID
								where	RTRIM(TF.value) <> ''									
								) D
		where A.ID = @Id --and T.DisplayFormat <>  '{Name}'
					
)
GO

ALTER FUNCTION [utility].[GetFormattedFieldLookupValue]
(
	@Type varchar(25),
	@DisplayFormat nvarchar(250),
	@LookupObjectType varchar(25),
	@LookupObjectID int,
	@Value nvarchar(max)	
)
RETURNS nvarchar(max)
AS
BEGIN
	declare @formattedValue nvarchar(max)
	
	if @Value is null
	begin
		return null
	end

	if @LookupObjectType is null
	begin
		set @formattedValue  = @Value

		if @Type = 'Link' OR @Type = 'UncLink'
		begin
			declare @linkName nvarchar(max),
					@linkUrl nvarchar(max)

			if charindex('|', @Value, 1) > 1
				begin
					SELECT @linkName = SUBSTRING(@Value, 1, PATINDEX('%|%', @Value)-1)
					SELECT @linkUrl = SUBSTRING(@Value, PATINDEX('%|%', @Value)+1, LEN(@Value))

					set @formattedValue = '<a href="' + @linkUrl + '" target="_blank">' + @linkName + '</a>'
				end
			else
				begin
					if @Value <> '' AND @Value <> '|' AND @Value IS NOT NULL
						begin
							if LEFT(@Value, 1) = '|'
								begin
									--no name, default to url
									set @formattedValue = '<a href="' + SUBSTRING(@Value,2, LEN(@Value)) + '" target="_blank">' + SUBSTRING(@Value,2, LEN(@Value)) + '</a>'
								end
							else
								begin
									set @formattedValue = '<a href="' + @Value + '" target="_blank">' + @Value + '</a>'
								end
						end
					else
						begin
							set @formattedValue = null
						end
				end
		end

	end	
	else
	begin
		if @LookupObjectType = 'ReferenceItemType'
		begin
			select @formattedValue = Name from ReferenceItemType where id = @Value;		
		end
		else if @LookupObjectType = 'TaxonomyType'
		begin
			select @formattedValue = Name from TaxonomyType where id = @Value;		
		end
		else
		begin
			declare @tokens table(ID int identity(1,1), Token nvarchar(100), Field nvarchar(100))
			declare @fieldValues table(Field nvarchar(100), Value nvarchar(max), LookupObjectType nvarchar(250), LookupObjectID int, LookupDisplayFormat nvarchar(250))

			set @formattedValue = @DisplayFormat
	
			while patindex('%{%',@formattedValue) > 0
			 begin
				declare @txt nvarchar(100) = SUBSTRING(@formattedValue, patindex('%{%',@formattedValue), PATINDEX('%}%', @formattedValue))
				insert into @tokens Values (@txt, REPLACE(REPLACE(@txt,'{',''),'}',''))
				set @formattedValue = replace(@formattedValue, @txt, '')
			end

			insert into @fieldValues
				select	distinct
						V.Name,
						V.Value,
						V.LookupObjectType,
						V.LookupObjectID,
						V.LookupDisplayFormat
				from	(
						SELECT	ID,
								Name,
								'Artifact' as ObjectType
						FROM	ArtifactType
						WHERE	@LookupObjectType = 'Artifact' and ID = @LookupObjectID
						UNION
						SELECT	ID,
								Name,
								'Lookup' as ObjectType
						FROM	[LookupType]
						WHERE	@LookupObjectType = 'Lookup' and ID = @LookupObjectID
						UNION
						SELECT	ID,
								Name,
								'ReferenceItem' as ObjectType
						FROM	[ReferenceItemType]
						WHERE	@LookupObjectType = 'ReferenceItem' and ID = @LookupObjectID
						UNION										
						SELECT	1 as ID,
								'User' as Name,
								'Resource' as ObjectType
						WHERE	@LookupObjectType = 'Resource'-- and ID = @LookupObjectID
						UNION
						SELECT	ID,
								Name,
								'Taxonomy' as ObjectType
						FROM	TaxonomyType
						WHERE	@LookupObjectType = 'Taxonomy' and ID = @LookupObjectID
						) L
						outer apply (

									SELECT	IT.Name,
											[IF].Value,
											[IT].LookupObjectType,
											COALESCE([IT].LookupObjectID, 0) as LookupObjectID,
											[IT].LookupDisplayFormat
									FROM	Field [IF]
											inner join FieldType IT ON [IF].FieldTypeID = IT.ID 
																	and [IF].ObjectType = L.ObjectType
																	/*and [IF].ObjectID = case 
																							when dbo.IsInteger(@Value) = 1 then @Value
																							else 0
																						end*/
																	and [IF].ObjectID = case 
																							when TRY_CAST(@Value AS int) IS NULL  then 0 --not an int
																							else @Value -- int
																						end
																							
								
									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	A.ObjectID as AID,
													CAST(A.ObjectID as nvarchar(max)) as ID,
													CAST(TP.TextPath as nvarchar(max)) as TextPath
											FROM	asset A											
													cross apply dbo.GetAssetTextPathById(A.ID, '/') TP 
											WHERE	A.ObjectID = CAST(@Value as int) and A.[Object] = 'Artifact' and L.ObjectType = 'Artifact'																								
											) A
											unpivot	(
													FieldValue for FieldName in (ID, TextPath)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(											
											SELECT	A.ObjectID as ID,													
													CAST(TP.TextPath as nvarchar(max)) as TextPath
											FROM	asset A													
													cross apply dbo.GetAssetTextPathById(A.ID, '/') TP 
											WHERE	A.ObjectID = CAST(@Value as int) and A.[Object] = 'Taxonomy' and L.ObjectType = 'Taxonomy'
											) A
											unpivot	(
													FieldValue for FieldName in (TextPath)
													) p
																						
									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID,
													CAST(Code as nvarchar(max)) as Code
											FROM	ReferenceItem A
											WHERE	A.ID = @Value
													and L.ObjectType = 'ReferenceItem'
											) A
											unpivot	(
													FieldValue for FieldName in (Code)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID,
													CAST(Name as nvarchar(max)) as Name,
													CAST(Description as nvarchar(max)) as Description
											FROM	ReferenceItemType A
											WHERE	A.ID = @Value
													and L.ObjectType = 'ReferenceItemType'
											) A
											unpivot	(
													FieldValue for FieldName in (Name, Description)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ResourceID as ID,
													CAST(FirstName as nvarchar(max)) as FirstName,
													CAST(LastName as nvarchar(max)) as LastName,
													CAST(Email as nvarchar(max)) as Email
											FROM	reporting.Global_Resource A
											WHERE	A.ResourceID = @Value
													and L.ObjectType = 'Resource'
											) A
											unpivot	(
													FieldValue for FieldName in (FirstName, LastName, Email)
													) p
									) V

			declare @current int,
					@max int

			set @current = 1
			select @max = Max(ID) from @tokens

			set @formattedValue = @DisplayFormat

			while(@current <= @max)
			begin
				declare @currentToken nvarchar(100) = null,
						@currentField nvarchar(100) = null,
						@currentValue nvarchar(max) = null,
						@lkpType nvarchar(250) = null, 
						@lkpID int = null, 
						@lkpFormat nvarchar(250) = null

				select	@currentField = Field, 
						@currentToken = Token 
				from	@tokens
				where	ID = @current

				select	@currentValue = Value,
						@lkpType = LookupObjectType,
						@lkpID = LookupObjectID,
						@lkpFormat = LookupDisplayFormat
				from	@fieldValues 
				where	Field = @currentField

				if @currentValue is not null
				begin
					if @lookupObjectType is not null and @lkpID is not null
					begin
						select @currentValue = utility.GetFormattedFieldLookupValue(@Type, @lkpFormat, @lkpType, @lkpID, @currentValue)
					end

					SET @formattedValue = REPLACE(@formattedValue, @currentToken, @currentValue)
				end
				else
				begin
					SET @formattedValue = REPLACE(@formattedValue, @currentToken, '')
				end

				SET @current = @current + 1
			end
		end
	end

	return @formattedValue
END
GO

CREATE TABLE [api].[Namespace] (
    [ID]        INT           IDENTITY (1, 1) NOT NULL,
    [ServiceID] INT           NOT NULL,
    [Node]      VARCHAR (250) NOT NULL,
    [Namespace] VARCHAR (250) NOT NULL,
    CONSTRAINT [PK_Api_Namespace] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Namespace_Service] FOREIGN KEY ([ServiceID]) REFERENCES [api].[Service] ([ID])
);
GO

CREATE TABLE [dbo].[FusionProfile] (
    [ID]                                                BIGINT                                      IDENTITY (1, 1) NOT NULL,
    [RowCount]                                          INT                                         NOT NULL,
    [UniqueCount]                                       INT                                         NOT NULL,
    [Uniqueness]                                        REAL                                        NULL,
    [NullCount]                                         INT                                         NOT NULL,
    [EmptyCount]                                        INT                                         NOT NULL,
    [MinValue]                                          VARCHAR (255)                               NULL,
    [MaxValue]                                          VARCHAR (255)                               NULL,
    [DataType]                                          VARCHAR (255)                               NULL,
    [Completeness]                                      REAL                                        NULL,
    [FormatCount]                                       INT                                         NULL,
    [OverallDataType]                                   VARCHAR (255)                               NULL,
    [DominantDataType]                                  VARCHAR (255)                               NULL,
    [Precision]                                         INT                                         NULL,
    [Scale]                                             INT                                         NULL,
    [MinLength]                                         INT                                         NULL,
    [MaxLenght]                                         INT                                         NULL,
    [TotalSum]                                          FLOAT (53)                                  NULL,
    [StandardDeviation]                                 FLOAT (53)                                  NULL,
    [AlphanumericAVGLen]                                REAL                                        NULL,
    [AlphanumericChecksum]                              REAL                                        NULL,
    [AlphanumericCompleteness]                          INT                                         NULL,
    [AlphanumericCount]                                 INT                                         NULL,
    [AverageFormatFrequency]                            REAL                                        NULL,
    [AverageFrequency]                                  REAL                                        NULL,
    [STDevFormatFrequency real null,
	[STDevFrequency] REAL                                        NULL,
    [BlankCount]                                        INT                                         NULL,
    [ByteLength]                                        INT                                         NULL,
    [AverageCount]                                      REAL                                        NULL,
    [LeastCommonFormatCount]                            INT                                         NULL,
    [LeastCommonValueCount]                             INT                                         NULL,
    [MostCommonValueCount]                              INT                                         NULL,
    [DateAverageLength]                                 REAL                                        NULL,
    [DateChecksum]                                      VARCHAR (255)                               NULL,
    [DateCompletness]                                   INT                                         NULL,
    [DateCount]                                         INT                                         NULL,
    [DateFormatCount]                                   INT                                         NULL,
    [DateLeastCommonValue]                              DATETIME                                    NULL,
    [DateLeastCommonFormat]                             VARCHAR (255)                               NULL,
    [DateLeastCommonFormatCount]                        VARCHAR (255)                               NULL,
    [DateLeastCommonCount]                              INT                                         NULL,
    [DateMostCommonValue]                               DATETIME                                    NULL,
    [DateMostCommonFormat]                              VARCHAR (255)                               NULL,
    [DateMostCommonFormatCount]                         VARCHAR (255)                               NULL,
    [DateMostCommonCount]                               INT                                         NULL,
    [DateMaxValue]                                      DATETIME                                    NULL,
    [DateMaxCount]                                      INT                                         NULL,
    [DateMaxLength]                                     INT                                         NULL,
    [DateMinValue]                                      DATETIME                                    NULL,
    [DateMinCount]                                      INT                                         NULL,
    [DateMinLength]                                     INT                                         NULL,
    [DateLengthDeviation]                               REAL                                        NULL,
    [DateUniqueCount]                                   INT                                         NULL,
    [DecimalAverage]                                    REAL                                        NULL,
    [DecimalAverageLength]                              REAL                                        NULL,
    [DecimalCompleteness]                               INT                                         NULL,
    [DecimalCount]                                      INT                                         NULL,
    [DecimalFormats]                                    INT                                         NULL,
    [DecimalLeastCommon]                                REAL                                        NULL,
    [DecimalLeastCommonCount]                           INT                                         NULL,
    [DecimalLeastCommonFormat]                          VARCHAR (255)                               NULL,
    [DecimalLeastCommonFormatCount]                     INT                                         NULL,
    [DecimalLengthDeviation]                            REAL                                        NULL,
    [DecimalMaxLength]                                  INT                                         NULL,
    [DecimalMaximum]                                    REAL                                        NULL,
    [DecimalMaxCount]                                   INT                                         NULL,
    [DecimalMinLength]                                  INT                                         NULL,
    [DecimalMinimum]                                    REAL                                        NULL,
    [DecimalMinCount]                                   INT                                         NULL,
    [DecimalMostCommon]                                 REAL                                        NULL,
    [DecimalMostCommonCount]                            INT                                         NULL,
    [DecimalMostCommonFormat]                           VARCHAR (255)                               NULL,
    [DecimalMostCommonFormatCount]                      INT                                         NULL,
    [DecimalPrecision]                                  INT                                         NULL,
    [DecimalScale]                                      INT                                         NULL,
    [DecimalTotalSum]                                   REAL                                        NULL,
    [DecimalUniqueCount]                                INT                                         NULL,
    [DecimalValueDeviation]                             REAL                                        NULL,
    [DeviationOfLength]                                 REAL                                        NULL,
    [DocumentedFormat]                                  VARCHAR (255)                               NULL,
    [DocumentedLength]                                  INT                                         NULL,
    [DocumentedMaxValue]                                VARCHAR (255)                               NULL,
    [DocumentedMinValue]                                VARCHAR (255)                               NULL,
    [DocumentedNullable]                                VARCHAR (255)                               NULL,
    [DocumentedPrecision]                               INT                                         NULL,
    [DocumentedScale]                                   INT                                         NULL,
    [DocumentedDataType]                                VARCHAR (255)                               NULL,
    [EncodingType]                                      VARCHAR (255)                               NULL,
    [ExternalName]                                      VARCHAR (255)                               NULL,
    [FailedMeasures]                                    BIT                                         NULL,
    [FailedRows]                                        BIT                                         NULL,
    [FrequentValues]                                    BIT                                         NULL,
    [HasNulls]                                          BIT                                         NULL,
    [HighAmounts]                                       BIT                                         NULL,
    [IgnoredRows]                                       INT                                         NULL,
    [ImplicitDecimalPoint]                              BIT                                         NULL,
    [IntegerAverage]                                    REAL                                        NULL,
    [IntegerAverageLength]                              INT                                         NULL,
    [IntegerCompleteness]                               INT                                         NULL,
    [IntegerCount]                                      INT                                         NULL,
    [IntegerFormatCount]                                INT                                         NULL,
    [IntegerLeastCommonValue]                           INT                                         NULL,
    [IntegerLeastCommonCount]                           INT                                         NULL,
    [IntegerLeastCommonFormat]                          VARCHAR (155)                               NULL,
    [IntegerLeastCommonFormatCount]                     INT                                         NULL,
    [IntegerLengthDeviation]                            REAL                                        NULL,
    [IntegerMaxLength]                                  INT                                         NULL,
    [IntegerMaxValue]                                   INT                                         NULL,
    [IntegerMaxValueCount]                              INT                                         NULL,
    [IntegerMinLength]                                  INT                                         NULL,
    [IntegerMinValue]                                   INT                                         NULL,
    [IntegerMinValueCount]                              INT                                         NULL,
    [IntegerMostCommonValue]                            INT                                         NULL,
    [IntegerMostCommonCount]                            INT                                         NULL,
    [IntegerMostCommonFormat]                           VARCHAR (155)                               NULL,
    [IntegerMostCommonFormatCount]                      INT                                         NULL,
    [IntegerPrecision]                                  INT                                         NULL,
    [IntegerTotalSum]                                   INT                                         NULL,
    [IntegerUniqueCount]                                INT                                         NULL,
    [IntegerValueDeviation]                             REAL                                        NULL,
    [IsASequence]                                       BIT                                         NULL,
    [KeyCheck]                                          BIT                                         NULL,
    [Language]                                          VARCHAR (255)                               NULL,
    [LastValidated]                                     DATETIME                                    NULL,
    [LastValidatedBy]                                   VARCHAR (255)                               NULL,
    [LeastCommonFormat]                                 VARCHAR (255)                               NULL,
    [LeastCommonValue]                                  VARCHAR (255)                               NULL,
    [LengthAtStart]                                     VARCHAR (255)                               NULL,
    [LongValues]                                        BIT                                         NULL,
    [LowValues]                                         BIT                                         NULL,
    [MaxExpectedFormatFrequency]                        INT                                         NULL,
    [MaxExpectedFrequency]                              INT                                         NULL,
    [MaxExpectedLength]                                 INT                                         NULL,
    [MaxExpectedNumber]                                 REAL                                        NULL,
    [MaxCount]                                          INT                                         NULL,
    [MinExpectedFormatFrequency]                        INT                                         NULL,
    [MinExpectedFrequency]                              INT                                         NULL,
    [MinExpectedLength]                                 INT                                         NULL,
    [MinExpectedNumber]                                 REAL                                        NULL,
    [MinimumCount]                                      INT                                         NULL,
    [MissingValues]                                     BIT                                         NULL,
    [ModifiedDate]                                      DATETIME                                    NULL,
    [ModifiedBy]                                        VARCHAR (255)                               NULL,
    [ModifiedReason]                                    VARCHAR (255)                               NULL,
    [MoneyAverageValue]                                 REAL                                        NULL,
    [MoneyAverageLength]                                INT                                         NULL,
    [MoneyCompleteness]                                 INT                                         NULL,
    [MoneyCountValue]                                   INT                                         NULL,
    [MoneyFormatCount]                                  INT                                         NULL,
    [MoneyLeastCommonValue]                             REAL                                        NULL,
    [MoneyLeastCommonCount]                             INT                                         NULL,
    [MoneyLeastCommonFormatCount]                       INT                                         NULL,
    [MoneyLengthDeviation]                              REAL                                        NULL,
    [MoneyMaxLength]                                    INT                                         NULL,
    [MoneyMaximumValue]                                 REAL                                        NULL,
    [MoneyMaximumCount]                                 INT                                         NULL,
    [MoneyMinLength]                                    INT                                         NULL,
    [MoneyMinimumValue]                                 REAL                                        NULL,
    [MoneyMinimumCount]                                 INT                                         NULL,
    [MoneyMostCommon]                                   REAL                                        NULL,
    [MoneyMostCommonCount]                              INT                                         NULL,
    [MoneyMostCommonFormat]                             VARCHAR (255)                               NULL,
    [MoneyMostCommonFormatCount]                        INT                                         NULL,
    [MoneyPrecision]                                    INT                                         NULL,
    [MoneyScale]                                        INT                                         NULL,
    [MoneyTotalSum]                                     REAL                                        NULL,
    [MoneyUniqueCount]                                  INT                                         NULL,
    [MoneyValueDeviation]                               REAL                                        NULL,
    [MostCommonFormat]                                  VARCHAR (255)                               NULL,
    [MostCommonFormatCount]                             INT                                         NULL,
    [MostCommonValue]                                   VARCHAR (255)                               NULL,
    [NativeType]                                        VARCHAR (255)                               NULL,
    [NegativeCount]                                     INT                                         NULL,
    [NegativeValues]                                    BIT                                         NULL,
    [NoteCount]                                         INT                                         NULL,
    [NullType]                                          VARCHAR (255)                               NULL,
    [PassedMeasure]                                     VARCHAR (255)                               NULL,
    [PassedRows]                                        INT                                         NULL,
    [Position]                                          INT                                         NULL,
    [RareFormats]                                       BIT                                         NULL,
    [RareValues]                                        BIT                                         NULL,
    [ReferenceID]                                       VARCHAR (255)                               NULL,
    [RelationshipCount]                                 INT                                         NULL,
    [RuleCount]                                         INT                                         NULL,
    [Schema]                                            VARCHAR (255)                               NULL,
    [SchemaExternalName]                                VARCHAR (255)                               NULL,
    [ShortValues]                                       BIT                                         NULL,
    [SignType]                                          VARCHAR (255)                               NULL,
    [StandardDeviationOfFormatFrequency]                REAL                                        NULL,
    [StandardDeviationOfFrequency]                      REAL                                        NULL,
    [StandardDeviationofValues]                         REAL                                        NULL,
    [TableConnection]                                   VARCHAR (255)                               NULL,
    [TableExternalName]                                 VARCHAR (255)                               NULL,
    [TableID]                                           VARCHAR (255)                               NULL,
    [Version]                                           REAL                                        NULL,
    [ZeroCount]                                         INT                                         NULL,
    [CreatedOn]                                         DATETIME                                    NULL,
    [CreatedBy]                                         INT                                         NULL,
    [UpdatedOn]                                         DATETIME                                    NULL,
    [UpdatedBy]                                         INT                                         NULL,
    [EffectiveStartDate]                                DATETIME2 (0) GENERATED ALWAYS AS ROW START NOT NULL,
    [EffectiveEndDate]                                  DATETIME2 (0) GENERATED ALWAYS AS ROW END   NOT NULL,
    CONSTRAINT [PK_FusionProfile] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    PERIOD FOR SYSTEM_TIME ([EffectiveStartDate], [EffectiveEndDate])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE=[dbo].[FusionProfile_History], DATA_CONSISTENCY_CHECK=ON));
GO

CREATE FUNCTION [dbo].[GetArtifactParentDisplayValue]
(
	@Id bigint
)
RETURNS TABLE 

AS
RETURN 
(
	/*select		top 1
				string_agg(coalesce(D.FormattedValue, D.value), '') as DisplayValue
	from		dbo.Asset A
				inner join dbo.AssetType T on T.ID = A.AssetTypeID 
				outer apply (
							select	TF.value,
									F.FormattedValue as FormattedValue
							from	string_split(replace(replace(T.DisplayFormat, '{', '|'),'}','|'), '|') TF									
									left join dbo.FieldType FT on FT.AssetTypeID = T.ID and FT.Name = TF.Value
									left join dbo.Field F on F.FieldTypeID = FT.ID and F.AssetID = A.ID									
							where	RTRIM(TF.value) <> ''									
							) D
	where A.ID = @Id*/
	Select Left(Main.DisplayValue,Len(Main.DisplayValue)) As "DisplayValue"
From
    (
	select		F.FormattedValue as DisplayValue
	from		dbo.Asset A				
				inner join ArtifactTypeDisplayFormatFieldTypes ATDF on ATDF.AssetTypeId = A.AssetTypeID
				inner join dbo.Field F on ATDF.FieldTypeID = F.FieldTypeID and F.AssetID = A.ID	
	where A.ID = @id
) [Main]
)
GO

-- update fusion rule promotion steps where old name was hard coded name field
    update rsm
    set rsm.TargetFieldTypeID = ft.id
from 
    fusion.rulestepmapping rsm
    inner join fusion.rulestepsetting rss_o on rss_o.rulestepid = rsm.rulestepid and rss_o.name = 'Object' and rss_o.name = 'Object' and rsm.targetfieldtypeid = 0 and rsm.targetfieldname = 'Name'
    inner join fusion.rulestepsetting rss_oi on rss_oi.rulestepid = rsm.rulestepid and rss_oi.name = 'ObjectID'
    inner join fieldtype ft on ft.object = rss_o.value and ft.objectid = rss_oi.value and ft.name = 'Name'
GO
-- update fusion rule promotion steps where old desc was hard coded name field
    update rsm
    set rsm.TargetFieldTypeID = ft.id
from 
    fusion.rulestepmapping rsm
    inner join fusion.rulestepsetting rss_o on rss_o.rulestepid = rsm.rulestepid and rss_o.name = 'Object' and rss_o.name = 'Object' and rsm.targetfieldtypeid = 0 and rsm.targetfieldname = 'Description'
    inner join fusion.rulestepsetting rss_oi on rss_oi.rulestepid = rsm.rulestepid and rss_oi.name = 'ObjectID'
    inner join fieldtype ft on ft.object = rss_o.value and ft.objectid = rss_oi.value and ft.name = 'Description'
GO
-- update fusion rule promotion steps where old taxonomytypeid was hard coded
update rsm 
    set rsm.TargetFieldTypeID = ft.id 
from 
    fusion.rulestepmapping rsm 
    inner join fusion.rulestepsetting rss_o on rss_o.rulestepid = rsm.rulestepid and rss_o.name = 'Object' and rss_o.name = 'Object' and rsm.targetfieldtypeid = 0 and rsm.targetfieldname = 'TaxonomyTypeID' 
    inner join fusion.rulestepsetting rss_oi on rss_oi.rulestepid = rsm.rulestepid and rss_oi.name = 'ObjectID' 
    inner join fieldtype ft on ft.object = rss_o.value and ft.objectid = rss_oi.value and ft.name = 'SubjectArea' 
GO
-- update constant values for subject area to there list values
update rsm 
set rsm.constantvalue = flv.[value] 
from 
fusion.rulestepmapping rsm 
inner join fusion.rulestepsetting rss_o on rss_o.rulestepid = rsm.rulestepid and rss_o.name = 'Object' and rss_o.name = 'Object' and rsm.targetfieldname = 'TaxonomyTypeID'  and rsm.IsConstantValue = 1
inner join fusion.rulestepsetting rss_oi on rss_oi.rulestepid = rsm.rulestepid and rss_oi.name = 'ObjectID' 
inner join fieldtype ft on ft.object = rss_o.value and ft.objectid = rss_oi.value and ft.name = 'SubjectArea' 
inner join fieldlookupvalue flv on ft.id = flv.fieldtypeid and flv.text = rsm.constantvalue
GO

create VIEW [utility].[ArtifactAssetParentIntermediate]
WITH SCHEMABINDING  
AS  
    select    a_o.ID as AssetID,         
                     I.ObjectID as ParentArtifactID
       from
              dbo.[Intersect] I
              inner join dbo.Asset a_o on I.[Object] = a_o.[Object] and I.[ObjectID] = a_o.ObjectID       
              inner join dbo.[IntersectType] IT on I.IntersectTypeID = IT.ID
              inner join dbo.[Predicate] P on P.ID = IT.PredicateID and P.[Type] = 3            
       where I.[Object] = 'Artifact'
GO


CREATE UNIQUE CLUSTERED INDEX [INDX_ArtifactAssetParentIntermediate_AssetID_ParentArtifactID] ON [utility].[ArtifactAssetParentIntermediate]
(
       [AssetID] ASC,       
       [ParentArtifactID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
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

alter table integration.SynchedAssetType add [Level] int null
GO
alter table integration.ExecutionAssetType alter column [ErrorMessage] nvarchar(max) null
GO
ALTER TABLE [integration].[ExecutionRelationItem] add [SynchedAssetTypeID] INT CONSTRAINT [DF_IntegrationExecutionRelationItem_SynchedAssetTypeID] DEFAULT ((0)) NOT NULL
GO

----54 only
DROP INDEX [IX_Asset_AssetTypeID] ON [dbo].[Asset];
GO

CREATE NONCLUSTERED INDEX [IX_Asset_AssetTypeID_Include] ON [dbo].[Asset]([AssetTypeID] ASC) INCLUDE([ID], [Object], [ObjectID]);
GO

CREATE NONCLUSTERED INDEX [IX_Intersect_Subject_Object_Include]
    ON [dbo].[Intersect]([Subject] ASC, [Object] ASC, [SubjectID] ASC)
    INCLUDE([ID]);
GO

CREATE NONCLUSTERED INDEX [IX_ResponsibilityTypeRelationItem_SecurityAsset]
    ON [dbo].[ResponsibilityTypeRelationItem]([SecurityAsset] ASC)
    INCLUDE([SecurityAssetID]);
GO

CREATE UNIQUE CLUSTERED INDEX [IDEX_IntersectAsset_ObjectAsset_Predicate_IntersectType]
    ON [utility].[IntersectAsset]([ID] ASC, [ObjectAssetID] ASC, [PredicateType] ASC, [IntersectTypeID] ASC);
GO

drop view [dbo].[Relationship_test]
GO
--------------

DROP INDEX [IX_CacheAssetResponsibility_Asset] ON [cache].[AssetResponsibility];
GO

CREATE NONCLUSTERED INDEX [IX_CacheAssetResponsibility_RuleID_OverrideItemID]
    ON [cache].[AssetResponsibility]([RuleID] ASC, [OverrideItemID] ASC)
    INCLUDE([Overriden]);
GO

CREATE NONCLUSTERED INDEX [IX_Intersect_Subject_Object_Include]
    ON [dbo].[Intersect]([Subject] ASC, [Object] ASC, [SubjectID] ASC)
    INCLUDE([ID]);
GO

CREATE NONCLUSTERED INDEX [IX_ResponsibilityTypeRelationItem_SecurityAsset]
    ON [dbo].[ResponsibilityTypeRelationItem]([SecurityAsset] ASC)
    INCLUDE([SecurityAssetID]);
GO

