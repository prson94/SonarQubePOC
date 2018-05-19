UPDATE	T
SET		T.AssetTypeID = A.ID
FROM	FieldType T 
		inner join AssetType A on A.Object = T.Object and A.ObjectID = T.ObjectID and T.AssetTypeID is null
GO

create proc integration.ProcessUnresolvedRelationships
as
begin
	delete	[integration].[UnresolvedRelationItem]
	where	ID in	(
					select  U.ID
					from    [integration].[UnresolvedRelationItem] U
							inner join IntersectType IT on IT.ID = U.IntersectTypeID
							inner join AssetType ST on ST.Object = IT.Subject and ST.ObjectID = IT.SubjectID
							inner join AssetType OT on OT.Object = IT.Object and OT.ObjectID = IT.ObjectID
							inner join Asset S on S.AssetTypeID = ST.ID and S.SourceID = U.SubjectSourceID
							inner join Asset O on O.AssetTypeID = OT.ID and O.SourceID = U.ObjectSourceID
							inner join [Intersect] I on I.IntersectTypeID = IT.ID and I.Subject = S.Object and I.SubjectID = S.ObjectID and I.Object = O.Object and I.ObjectID = O.ObjectID		
					);

	merge into  [Intersect] T
	using       (
				select  U.IntersectTypeID,
						S.Object as Subject, 
						S.ObjectID as SubjectID, 
						O.Object, 
						O.ObjectID 
				from    [integration].[UnresolvedRelationItem] U
						inner join IntersectType IT on IT.ID = U.IntersectTypeID
						inner join AssetType ST on ST.Object = IT.Subject and ST.ObjectID = IT.SubjectID
						inner join AssetType OT on OT.Object = IT.Object and OT.ObjectID = IT.ObjectID
						inner join Asset S on S.AssetTypeID = ST.ID and S.SourceID = U.SubjectSourceID
						inner join Asset O on O.AssetTypeID = OT.ID and O.SourceID = U.ObjectSourceID
				) S
	on          (
					T.IntersectTypeID = S.IntersectTypeID and 
					T.Subject = S.Subject and 
					T.SubjectID = S.SubjectID and 
					T.Object = S.Object and 
					T.ObjectID = S.ObjectID
				)
	when not matched then
		insert  (IntersectTypeID, Subject, SubjectID, Object, ObjectID, CreatedBy, UpdatedBy)
		values  (S.IntersectTypeID, S.Subject, S.SubjectID, S.Object, S.ObjectID, 0, 0);
end
GO

DROP FUNCTION [utility].[GetObjectName] 
GO

ALTER TRIGGER [dbo].[Policy_AfterUpdate]
   ON  [dbo].[Policy] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn,
			T.SourceID = S.SourceID
	from	Asset T
			inner join inserted S on T.Object = 'Policy' and T.ObjectID = S.ID
GO

ALTER TRIGGER [dbo].[Policy_AfterInsert]
   ON  [dbo].[Policy] 
   AFTER INSERT
AS 
	SET NOCOUNT ON
	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],SourceID,[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, O.SourceID,'Policy', O.ID, O.[UpdatedOn], O.[UpdatedBy], O.[UpdatedOn], O.[UpdatedBy]
		FROM	inserted O inner join  AssetType T on T.Object = 'PolicyType' and T.ObjectID = O.PolicyTypeID
GO

ALTER TRIGGER [dbo].[Rule_AfterUpdate]
   ON  [dbo].[Rule] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn,
			T.SourceID = S.SourceID
	from	Asset T
			inner join inserted S on T.Object = 'Rule' and T.ObjectID = S.ID
GO

ALTER TRIGGER [dbo].[Rule_AfterInsert]
   ON  [dbo].[Rule] 
   AFTER INSERT
AS 
	SET NOCOUNT ON
	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],SourceID,[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, O.SourceID,'Rule', O.ID, O.[CreatedOn], O.[CreatedBy], O.[UpdatedOn], O.[UpdatedBy]
		FROM	inserted O inner join  AssetType T on T.Object = 'RuleType' and T.ObjectID = O.RuleTypeID
GO

UPDATE	[Rule] SET SourceID = SourceID
UPDATE	[Policy] SET SourceID = SourceID
GO

CREATE CLUSTERED INDEX [CIX_Asset] ON [dbo].[Asset]([AssetTypeID] ASC, [State] ASC, [ID] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Field_FieldTypeID_Include] ON [dbo].[Field]([FieldTypeID] ASC) INCLUDE([FormattedValue], [Value]);
GO

EXEC sp_rename N'dbo.Field_History.ix_FieldNew_History', N'IX_Field_History', N'INDEX';  
GO

EXEC sp_rename N'dbo.FieldType_History.ix_FieldTypeNew_History', N'IX_FieldType_History', N'INDEX';  
GO

CREATE NONCLUSTERED INDEX [IX_FusionAttribute_Fusion_Include] ON [dbo].[FusionAttribute]([FusionAttributeTypeID] ASC, [FusionID] ASC, [Deleted] ASC) INCLUDE([Name], [ParentID]);
GO

CREATE NONCLUSTERED INDEX [IX_Intersect_Subject_Object_Include] ON [dbo].[Intersect]([Subject] ASC, [Object] ASC, [SubjectID] ASC) INCLUDE([ID]);
GO

CREATE TRIGGER [dbo].[Intersect_AfterInsert]
	ON [dbo].[Intersect]
	FOR INSERT
AS
BEGIN
	SET NOCOUNT ON;
		
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', Subject, SubjectID, UpdatedBy), 'Intersect', ID from inserted;

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', Object, ObjectID, UpdatedBy), 'Intersect', ID from inserted;
END
GO

EXEC sp_rename N'dbo.IntersectType_History.ix_IntersectTypeNew_History', N'IX_IntersectType_History', N'INDEX';  
GO

drop TRIGGER [dbo].[Map_AfterDelete]
GO

drop TRIGGER [dbo].[MapType_AfterDelete]
GO
drop TRIGGER [dbo].[MapType_AfterInsert]
GO
drop TRIGGER [dbo].[MapType_AfterUpdate]
GO

alter table MapType alter column MapClass int not null
GO

ALTER TRIGGER [dbo].[PolicyType_AfterInsert]
   ON  [dbo].[PolicyType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON
	insert into AssetType (Name, Description, Class, DisplayFormat, [State], [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
		select Name, Description, 6, coalesce(DisplayFormat, '{Name}'), 1, 1, MaximumDepth, 'PolicyType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from inserted
GO

ALTER TRIGGER [dbo].[PolicyType_AfterUpdate]
   ON  [dbo].[PolicyType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.Name = S.Name,
			T.Description = S.Description,
			T.DisplayFormat = S.DisplayFormat,
			T.HierarchyMaximumDepth = S.MaximumDepth,
			T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	AssetType T
			inner join inserted S on T.Object = 'PolicyType' and T.ObjectID = S.ID
GO

CREATE NONCLUSTERED INDEX [IX_ResponsibilityTypeRelationItem_OverrideItemID] ON [dbo].[ResponsibilityTypeRelationItem]([OverrideItemID] ASC);
GO

ALTER TABLE [integration].[SynchedAssetType] DROP CONSTRAINT [DF_IntegrationSynchedAssetType_TriggerTopicMessage]
GO
alter table integration.SynchedAssetType drop column TriggerTopicMessage
alter table integration.SynchedAssetType drop column PageSize
GO
alter table integration.SynchedAssetType add [Level] int null
alter table integration.SynchedAssetType add TriggerTopicMessage bit constraint DF_IntegrationSynchedAssetType_TriggerTopicMessage default(0) not null
alter table integration.SynchedAssetType add PageSize int null
GO

ALTER VIEW [dbo].[FieldDetail]
AS
	SELECT	T.ID as FieldTypeID,
			T.Name,
			T.FriendlyName,
			A.AssetTypeID,
			A.ID as AssetID,
			A.Object,
			A.ObjectID,
			T.Type,
			coalesce(F.Value, T.DefaultValue) as Value,
			case
				when T.AllowAllValue = 1 and F.FormattedValue = '0' then cast(T.AllowAllLabel as nvarchar(max))
				when F.FormattedValue is not null then F.FormattedValue
				when T.DefaultFormattedValue is not null then cast(T.DefaultFormattedValue as nvarchar(max))
				else null
			end as FormattedValue
	FROM	Asset A
			inner join FieldType T on T.AssetTypeID = A.AssetTypeID
			left join Field F on F.FieldTypeID = T.ID and F.ObjectType = A.Object and F.ObjectID = A.ObjectID
	WHERE	(F.Value is not null OR T.DefaultValue is not null)
GO

ALTER procedure [dbo].[DeleteObject]
	@ObjTemp varchar(50),
	@ObjectIDTemp int,
	@ResourceIDTemp int
as
begin
	set nocount on

	declare
		@Obj varchar(50) = @ObjTemp,
		@ObjectID int = @ObjectIDTemp,
		@ResourceID int = @ResourceIDTemp

	
	declare @Object varchar(50) = @Obj,
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

ALTER FUNCTION [utility].[GetIntersectTypesByType]  
(   
--declare  
 @type varchar(50),-- = 'ArtifactType',  
 @id int-- = 2  
)  
RETURNS TABLE   
AS  
RETURN   
(  
 select top 100000  
   *  
 from (  
   select 'I' as type,  
     I.ID,  
     cast(I.ID as varchar) + '|' + I.Subject + '|' + cast(I.SubjectID as varchar) + '|2' as value,  
     coalesce(  
      UPPER(LEFT(P.Name, 1)) + LOWER(SUBSTRING(P.Name, 2, LEN(P.NAME))),   
      'Relates to'  
     ) + ' ' + I.SubjectName + '(->)' as title  
   from IntersectTypeDetail I  
     left join [Predicate] P on P.ID = I.PredicateID  
   where Subject = @type and SubjectID = @id and Object = @type and ObjectID = @id  
   union  
   select 'I' as type,  
     I.ID,  
     cast(I.ID as varchar) + '|' + I.Object + '|' + cast(I.ObjectID as varchar) + '|1' as value,  
     coalesce(UPPER(LEFT(P.Inverse, 1)) + LOWER(SUBSTRING(P.Inverse, 2, LEN(P.Inverse))), 'Related to') + ' ' + I.ObjectName + '(<-)' as title  
   from IntersectTypeDetail I  
     left join [Predicate] P on P.ID = I.PredicateID  
   where Subject = @type and SubjectID = @id and Object = @type and ObjectID = @id  
   union  
   select 'I' as type,  
     I.ID,  
     cast(I.ID as varchar) +   
     '|' +  
     case   
      when (Subject = @type and SubjectID = @id) then I.Object + '|' + cast(I.ObjectID as varchar) + '|2'  
      else I.Subject + '|' + cast(I.SubjectID as varchar) + '|1'  
     end as value,  
     case   
      when (Subject = @type and SubjectID = @id) then coalesce(UPPER(LEFT(P.Name, 1)) + LOWER(SUBSTRING(P.Name, 2, LEN(P.Name))), 'Relates to') +' ' + I.ObjectName + '(->)'  
      else coalesce(UPPER(LEFT(P.Inverse, 1)) + LOWER(SUBSTRING(P.Inverse, 2, LEN(P.Inverse))), 'Related to') + ' ' + I.SubjectName + '(<-)'  
     end as title  
   from IntersectTypeDetail I  
     left join [Predicate] P on P.ID = I.PredicateID  
   where (  
     (Subject = @type and SubjectID = @id) or (Object = @type and ObjectID = @id)  
     )  
     and  Subject + cast(SubjectID as varchar) <> Object + cast(ObjectID as varchar)  
   union  
   select 'I' as type,  
     I.ID,  
     cast(I.ID as varchar) + '|' +  
     case   
      when (Subject = @type and SubjectID = @id) then I.Object + '|' + cast(I.ObjectID as varchar)  
      else I.Subject + '|' + cast(I.SubjectID as varchar)  
     end + '|0' as value,  
	 case 
	  when (Subject = @type and SubjectID = @id) then coalesce(UPPER(LEFT(P.Name, 1)) + LOWER(SUBSTRING(P.Name, 2, LEN(P.Name))) + ' / ' + UPPER(LEFT(P.Inverse, 1)) + LOWER(SUBSTRING(P.Inverse, 2, LEN(P.Inverse))), 'Relates to / Related to') + ' ' + I.ObjectName + '(<->)' 
	  else  coalesce(UPPER(LEFT(P.Name, 1)) + LOWER(SUBSTRING(P.Name, 2, LEN(P.Name))) + ' / ' + UPPER(LEFT(P.Inverse, 1)) + LOWER(SUBSTRING(P.Inverse, 2, LEN(P.Inverse))), 'Relates to / Related to') + ' ' + I.SubjectName + '(<->)' 
     end as title  
   from IntersectTypeDetail I  
     left join [Predicate] P on P.ID = I.PredicateID  
   where (Subject = @type and SubjectID = @id) or (Object = @type and ObjectID = @id)  
   ) O order by ID  
)
GO

CREATE TABLE [dbo].[IssueTypeRelation] (
    [IssueTypeID] INT NOT NULL,
    [AssetTypeID] INT NOT NULL,
    CONSTRAINT [PK_IssueTypeRelation] PRIMARY KEY CLUSTERED ([IssueTypeID] ASC, [AssetTypeID] ASC)
);
GO

