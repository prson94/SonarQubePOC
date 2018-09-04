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

/*
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
*/
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

CREATE UNIQUE CLUSTERED INDEX [IDEX_IntersectAsset_ObjectAsset_Predicate_IntersectType] ON [utility].[IntersectAsset] ([ID], [ObjectAssetID], [PredicateType], [IntersectTypeID])
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
/*
alter table metrics.StagingResult add Processing bit constraint DF_MetricsStagingResult_Processing default(0) not null
GO

alter table [workflow].[VersionStepTransition] add [TimerLastRunDate] DATETIME NULL
GO
*/
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

CREATE procedure [relation].[BulkUpsert]
--declare 
	@uid uniqueidentifier,-- = 'B56999A9-CBCD-4091-8317-89C80F0AF3D1', --App stores BT
	@r int-- = 1
as
begin
	set nocount on;
	/*	TEST: insert some records	*/
	--BEGIN
	--	drop table if exists #RelationshipTable;
	--	create table #RelationshipTable (
	--		ItemNumber int not null,
	--		SubjectUid uniqueidentifier null,
	--		Subject varchar(50) null,
	--		SubjectID int null,
	--		ObjectUid uniqueidentifier null,
	--		Object varchar(50) null,
	--		ObjectID int null,
	--		IntersectID int null,
	--		[Message] nvarchar(2500) null,
	--		Success bit null,
	--		IsNew bit null
	--	);
	--	drop table if exists #RelationshipFieldTable;
	--	create table #RelationshipFieldTable (
	--		ItemNumber int not null,
	--		FieldName nvarchar(250) not null,
	--		FieldValue nvarchar(max) null,
	--		FieldTypeID int null,
	--		LookupValue nvarchar(250) null
	--	);

	--	insert into #RelationshipTable (ItemNumber, [SubjectUid], [ObjectUid]) values (1, '858f5605-a72e-4c15-84bb-3ee619c3f2cf', '66cc34fa-126f-4cf8-964a-1aeaddb72d37');
	--	insert into #RelationshipFieldTable (ItemNumber, FieldName, FieldValue) values (1, 'AuthoritativeSource', 'true');
	--	--insert into #RelationshipFieldTable (ItemNumber, FieldName, FieldValue) values (1, 'DataType', 'true');
	--END;
	/********************************/

	--Get Identifier values that we will need for this relationship type.
	declare @st varchar(50),
			@stid int,
			@ot varchar(50),
			@otid int,
			@it int

	select	@st = Subject,
			@stid = SubjectID,
			@ot = Object,
			@otid = ObjectID,
			@it = ID
	from	IntersectType
	where	[uid] = @uid

	-- Resolve the FieldTypeIDs for the fields you have added.
	update	T
	set		T.FieldTypeID = S.ID
	from	#RelationshipFieldTable T
			inner join FieldType S on S.Object = 'IntersectType' and S.ObjectID = @it and S.Name = T.FieldName
	----------------------------------------------------------

	BEGIN 
		-- Validation checks ----------

		-- 1. Does relationship have all the key fields defined?
		update	T
		set		T.Success = 0,
				T.[Message] = coalesce(T.[Message] + '; ', '') + 'Relationship is missing key field(s): [' + S.Names + ']'
		from	#RelationshipTable T
				inner join	(
							select	A.ItemNumber,
									STRING_AGG(FT.Name, ', ') as Names
							from	#RelationshipTable A
									inner join FieldType FT on FT.Object = 'IntersectType' 
																and FT.ObjectID = @it
																and FT.IsPartOfKey = 1
									left join #RelationshipFieldTable F on F.ItemNumber = A.ItemNumber and F.FieldTypeID = FT.ID
							where	F.ItemNumber is null
							group by A.ItemNumber
							) S on S.ItemNumber = T.ItemNumber;

		-- 2. Does relationship have all required fields defined?
		update	T
		set		T.Success = 0,
				T.[Message] = coalesce(T.[Message] + '; ', '') + 'Relationship is missing required field(s): [' + S.Names + ']'
		from	#RelationshipTable T
				inner join	(
							select	A.ItemNumber,
									STRING_AGG(FT.Name, ', ') as Names
							from	#RelationshipTable A
									inner join FieldType FT on FT.Object = 'IntersectType' 
																and FT.ObjectID = @it
																and FT.IsRequired = 1
									left join #RelationshipFieldTable F on F.ItemNumber = A.ItemNumber and F.FieldTypeID = FT.ID 
							where	F.ItemNumber is null
							group by A.ItemNumber
							) S on S.ItemNumber = T.ItemNumber;

		-- 3. Are all lookup fields valid, based on field's LookupEditFormat, or LookupDisplayFormat?

		--- A. Get the valid lookup values.
		update	T
		set		T.LookupValue = S.[Value]
		from	#RelationshipFieldTable T
				inner join FieldType F on F.ID = T.FieldTypeID and F.[Type] = 'Lookup'
				inner join FieldLookupValue S on S.FieldTypeID = F.ID and S.[Text] = T.FieldValue

		--- B. Check which fields do not have a valid from from query above.
		update	T
		set		T.Success = 0,
				T.[Message] = coalesce(T.[Message] + '; ', '') + 'Relationship contains one or more fields with invalid lookup values: [' + S.Names + ']'
		from	#RelationshipTable T
				inner join	(
							select		A.ItemNumber,
										STRING_AGG(FT.Name+'='+F.FieldValue, ', ') as Names
							from		#RelationshipTable A
										inner join FieldType FT on FT.Object = 'IntersectType' 
																	and FT.ObjectID = @it
																	and FT.[Type] = 'Lookup'
										inner join #RelationshipFieldTable F on F.ItemNumber = A.ItemNumber and F.FieldTypeID = FT.ID and F.LookupValue is null
							group by	A.ItemNumber
							) S on S.ItemNumber = T.ItemNumber;

		-- 4. Are all values valid based on field's data type?
		update	T
		set		T.Success = 0,
				T.[Message] = coalesce(T.[Message] + '; ', '') + 'Relationship contains one or more field that are invalid based on their data types: [' + S.Names + ']'
		from	#RelationshipTable T
				inner join	(
							select	A.ItemNumber,
									STRING_AGG(FT.Name + ' is ' + FT.[Type] + ' but has a value of ' + F.FieldValue, ', ') as Names
							from	#RelationshipTable A
									inner join FieldType FT on FT.Object = 'IntersectType' and FT.ObjectID = @it
									inner join #RelationshipFieldTable F on F.ItemNumber = A.ItemNumber 
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
				T.[Message] = coalesce(T.[Message] + '; ', '') + 'Relationship contains one or more field that have an invalid length: [' + S.Names + ']'
		from	#RelationshipTable T
				inner join	(
							select	A.ItemNumber,
									STRING_AGG(FT.Name + ' must have an exact length of ' + cast(FT.[Length] as nvarchar), ', ') as Names
							from	#RelationshipTable A
									inner join FieldType FT on FT.Object = 'IntersectType' and FT.ObjectID = @it
									inner join #RelationshipFieldTable F on F.ItemNumber = A.ItemNumber 
																	and F.FieldTypeID = FT.ID 
																	and FT.[Length] is not null
																	and FT.[Length] <> LEN(F.FieldValue)
							group by A.ItemNumber
							) S on S.ItemNumber = T.ItemNumber;

		-- 6. Check if minimum length populated, if so is the field's minimum legnth valid?
		update	T
		set		T.Success = 0,
				T.[Message] = coalesce(T.[Message] + '; ', '') + 'Relationship contains one or more field that have an invalid minimum length: [' + S.Names + ']'
		from	#RelationshipTable T
				inner join	(
							select	A.ItemNumber,
									STRING_AGG(FT.Name + ' must have a minimum length of ' + cast(FT.[MinimumLength] as nvarchar), ', ') as Names
							from	#RelationshipTable A
									inner join FieldType FT on FT.Object = 'IntersectType' and FT.ObjectID = @it
									inner join #RelationshipFieldTable F on F.ItemNumber = A.ItemNumber 
																	and F.FieldTypeID = FT.ID 
																	and FT.[MinimumLength] is not null
																	and FT.[MinimumLength] > LEN(F.FieldValue)
							group by A.ItemNumber
							) S on S.ItemNumber = T.ItemNumber;

		-- 7. Check if maximum length populated, if so is the field's maximum length valid?
		update	T
		set		T.Success = 0,
				T.[Message] = coalesce(T.[Message] + '; ', '') + 'Relationship contains one or more field that have an invalid maximum length: [' + S.Names + ']'
		from	#RelationshipTable T
				inner join	(
							select	A.ItemNumber,
									STRING_AGG(FT.Name + ' must have a maximum length of ' + cast(FT.[MaximumLength] as nvarchar), ', ') as Names
							from	#RelationshipTable A
									inner join FieldType FT on FT.Object = 'IntersectType' and FT.ObjectID = @it
									inner join #RelationshipFieldTable F on F.ItemNumber = A.ItemNumber 
																	and F.FieldTypeID = FT.ID 
																	and FT.[MaximumLength] is not null
																	and FT.[MaximumLength] < LEN(F.FieldValue)
							group by A.ItemNumber
							) S on S.ItemNumber = T.ItemNumber;

		-- 8. If regex defined, validate against the Pattern field as defined on FieldType.
		-- TODO: perhaps implement a CLR function here.
		-- https://stackoverflow.com/questions/194652/sql-server-regular-expressions-in-t-sql

	END	-------------------------------

	-- Now upsert the valid relationships.
	drop table if exists #ObjectMergeTableResult;
	create table #ObjectMergeTableResult (ID int, ItemNumber int, [Action] nvarchar(10));
	CREATE NONCLUSTERED INDEX IX_TempObjectMergeTableResult ON #ObjectMergeTableResult ( ItemNumber ASC );

	--Resolve the Object/ObjectID combination from asset table.
	update	T
	set		T.Subject = S.Object,
			T.SubjectID = S.ObjectID,
			T.Object = O.Object,
			T.ObjectID = O.ObjectID
	from	#RelationshipTable T
			left join AssetWithType S on S.[Type] = @st and S.TypeID = @stid and S.[uid] = T.SubjectUid
			left join AssetWithType O on O.[Type] = @ot and O.TypeID = @otid and O.[uid] = T.ObjectUid;

	--Resolve the Object/ObjectID combination from asset type table (for reference lists).
	update	T
	set		T.Subject = S.Object,
			T.SubjectID = S.ObjectID
	from	#RelationshipTable T
			inner join AssetType S on @st = 'ReferenceItemType' and @stid = 0 and S.[uid] = T.SubjectUid and T.Subject is null;

	update	T
	set		T.Object = O.Object,
			T.ObjectID = O.ObjectID
	from	#RelationshipTable T
			inner join AssetType O on @ot = 'ReferenceItemType' and @otid = 0 and O.[uid] = T.ObjectUid and T.Object is null;

	-- Validate.
	update	#RelationshipTable
	set		Success = 0,
			[Message] = coalesce([Message] + '; ', '') + 'Not able to resolve subject of this relationship to a valid asset.'
	where	Subject is null or SubjectID is null;

	-- Validate.
	update	#RelationshipTable
	set		Success = 0,
			[Message] = coalesce([Message] + '; ', '') + 'Not able to resolve object of this relationship to a valid asset.'
	where	Object is null or ObjectID is null;

	merge into  [Intersect] T
	using		(
				select      *
				from        #RelationshipTable
				where		Success is null	-- We have not failed in validation.
            ) S
	on      ( T.IntersectTypeID = @it and T.Subject = S.Subject and T.SubjectID = S.SubjectID and T.Object = S.Object and T.ObjectID = S.ObjectID )
	when matched then
		update set
				T.UpdatedBy = @r,
				T.UpdatedOn = getutcdate()
	when not matched by target then
		insert  (IntersectTypeID, Subject, SubjectID, Object, ObjectID, [State], CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		values  (@it, S.Subject, S.SubjectID, S.Object, S.ObjectID, 1, @r, getutcdate(), 1, getutcdate(), 'BULK_API')
	output inserted.ID, S.ItemNumber, $action into #ObjectMergeTableResult;

	update	T
	set		T.IntersectID = S.ID,
			T.IsNew = IIF(S.[Action] = 'I', 1, 0)
	from	#RelationshipTable T
			inner join #ObjectMergeTableResult S on S.ItemNumber = T.ItemNumber;

	-- Merge field data ---------------------------
	merge into  Field T
    using       (
                select  distinct 
                        A.IntersectID as ObjectID, 
                        F.FieldTypeID,
                        coalesce(F.LookupValue, F.FieldValue) as Value
                from    #RelationshipFieldTable F
                        inner join #RelationshipTable A on A.ItemNumber = F.ItemNumber 
                            and A.ObjectID is not null 
                            and F.FieldTypeID is not null
							and A.Success is null	-- We have not failed in validation.
                ) S
    on          (
                    T.FieldTypeID = S.FieldTypeID and 
                    T.ObjectType = 'Intersect' and 
					T.ObjectID = S.ObjectID
                )
    when		matched then
	update		set
					T.Value = S.Value
    when		not matched by target then
	insert		(FieldTypeID, ObjectType, ObjectID, Value)
    values		(S.FieldTypeID, 'Intersect', S.ObjectID, S.Value);
	-----------------------------------------------

	update	#RelationshipTable
	set		Success = 1
	where	Success is null
			and IntersectID is not null;

end
GO
