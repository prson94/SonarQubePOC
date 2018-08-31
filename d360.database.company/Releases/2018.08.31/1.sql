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

alter table metrics.StagingResult add Processing bit constraint DF_MetricsStagingResult_Processing default(0) not null
GO

alter table [workflow].[VersionStepTransition] add [TimerLastRunDate] DATETIME NULL
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

		if @Obj = 'Artifact' or @Obj = 'ArtifactType' or @Obj = 'FusionAttribute' or @Obj = 'FusionAttributeType' or @Obj = 'ReferenceItem' or @Obj = 'ReferenceItemType' or @Obj = 'Rule'
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
									inner join IntersectType I on I.Subject = @Obj and I.Object = @Obj and I.ObjectID = C.ObjectID and C.[Object] = @Obj
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
		INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID],[AssetID])
			select	'ObjectIndex', 
					'D',
					O.Object, 
					O.ObjectID,
					O.ID
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
			delete ResponsibilityTypeRelationRuleResult where ResponsibilityTypeID = @ObjectID
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
			
			--remove child entries
			delete nav
			from sitenav nav
			inner join @ht t on t.ObjectID = nav.ObjectID and nav.Object = @Object;
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
				END
			ELSE
				BEGIN
					delete	T
					from	ResponsibilityTypeRelationOverrideItem T
					where   T.AssetID  in (select ID from @h)

					delete	T
					from	ResponsibilityTypeRelationRuleResult T
					where   T.AssetID in (select ID from @h)
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

alter procedure [dbo].[ResponsibilityRuleShouldRun]
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
			select	@assetFieldMaxDate = max(F.EffectiveStartDate)
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
			select	@assetFieldMaxDate = max(F.EffectiveStartDate)
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
	--select	--@assetMaxDate as AssetMaxDate,
	--		--@assetFieldMaxDate as AssetFieldMaxDate,
	--		@newUsers as NewUser,
	--		@newAssets as NewAsset--,
	--	--	@ruleUpdated as RuleUpdated

	select @shouldRun
end
GO

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
				select @issueId = id, @objectType = [object], @objectId = [objectid] from Issue where id = @objectId
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
GO

alter FUNCTION [dbo].[GetOwnersListForWorkflow]
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

--select * from workflow.itemstep
GO

CREATE TABLE [dbo].[AssetCrossReference] (
    [uid]        VARCHAR (250) NOT NULL,
    [DataSource] VARCHAR (250) NOT NULL,
    [Type]       VARCHAR (50)  NOT NULL,
    [ExternalID] VARCHAR (250) NOT NULL,
    CONSTRAINT [PK_AssetCrossReference] PRIMARY KEY CLUSTERED ([DataSource] ASC, [Type] ASC, [ExternalID] ASC, [uid] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_AssetCrossReference_uid]
    ON [dbo].[AssetCrossReference]([uid] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_AssetCrossReference_uid_DataSource]
    ON [dbo].[AssetCrossReference]([uid] ASC, [DataSource] ASC);
GO

