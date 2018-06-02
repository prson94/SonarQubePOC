ALTER TRIGGER [dbo].[Asset_AfterInsert]
   ON  [dbo].[Asset] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Object], [ObjectID], [Custom])
        select	'Add', 
				I.Object, 
				I.ObjectID,
				[queue].WriteIndexXml('', I.[Object], I.ObjectID, coalesce(I.CreatedBy, 0)) 
		from	inserted I where  I.Object not in('FusionAttribute');
GO

ALTER TRIGGER [dbo].[Asset_AfterUpdate]
   ON  [dbo].[Asset] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Object], [ObjectID], [Custom])
        select	'Update', 
				I.Object, 
				I.ObjectID,
				[queue].WriteIndexXml('', I.[Object], I.ObjectID, coalesce(I.UpdatedBy, 0)) 
		from	inserted I where I.Object not in('FusionAttribute')
GO

CREATE TRIGGER [dbo].[AssetType_AfterInsert]
   ON  [dbo].[AssetType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', [Object], ObjectID, coalesce(CreatedBy, 0)), [Object], ObjectID from inserted where [Object] != 'FusionAttributeType'
GO

CREATE TRIGGER [dbo].[AssetType_AfterUpdate]
   ON  [dbo].[AssetType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', [Object], ObjectID, coalesce(UpdatedBy, 0)), [Object], ObjectID from inserted where [Object] != 'FusionAttributeType'
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

CREATE TRIGGER [dbo].[Intersect_AfterUpdate]
	ON [dbo].[Intersect]
	FOR UPDATE
AS
BEGIN
	SET NOCOUNT ON;
		
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', Subject, SubjectID, UpdatedBy), 'Intersect', ID from inserted;

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', Object, ObjectID, UpdatedBy), 'Intersect', ID from inserted;
END
GO

create TRIGGER [dbo].[IntersectType_AfterInsert]
	ON [dbo].[IntersectType]
	FOR INSERT
AS
BEGIN
	SET NOCOUNT ON;
		
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'IntersectType', ID, coalesce(UpdatedBy, 0)), 'IntersectType', ID from inserted
END
GO

create TRIGGER [dbo].[IntersectType_AfterUpdate]
	ON [dbo].[IntersectType]
	FOR UPDATE
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'IntersectType', ID, coalesce(UpdatedBy, 0)), 'IntersectType', ID from inserted
		
END
GO

create TRIGGER [dbo].[IssueType_AfterInsert]
	ON [dbo].[IssueType]
	FOR INSERT
AS
BEGIN
	SET NOCOUNT ON;
		
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'IssueType', ID, UpdatedBy), 'IssueType', ID from inserted;

END
GO

create TRIGGER [dbo].[IssueType_AfterUpdate]
	ON [dbo].[IssueType]
	FOR UPDATE
AS
BEGIN
	SET NOCOUNT ON;
		
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'IssueType', ID, UpdatedBy), 'IssueType', ID from inserted;
END
GO

create TRIGGER [dbo].[LookupType_AfterInsert]
   ON  [dbo].[LookupType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'LookupType', ID, coalesce(UpdatedBy, 0)), 'LookupType', ID from inserted
GO

create TRIGGER [dbo].[LookupType_AfterUpdate]
   ON  [dbo].[LookupType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'LookupType', ID, coalesce(UpdatedBy, 0)), 'LookupType', ID from inserted
GO

--CREATE NONCLUSTERED INDEX [IX_MapItem_SourceIntersectID]
--    ON [dbo].[MapItem]([SourceIntersectID] ASC);
--GO

--CREATE NONCLUSTERED INDEX [IX_MapItem_TargetIntersectID]
--    ON [dbo].[MapItem]([TargetIntersectID] ASC);
--GO

--CREATE NONCLUSTERED INDEX [IX_MapRuleItem_SourceFusionAttributeID_TargetFusionAttributeID]
--    ON [dbo].[MapRuleItem]([SourceFusionAttributeID] ASC, [TargetFusionAttributeID] ASC);
--GO

alter table [dbo].[ResponsibilityTypeRelationRule] add IsVisible bit constraint DF_ResponsibilityTypeRelationRule_IsVisible default(1) not null
alter table [dbo].[ResponsibilityTypeRelationRule] add ApplyToType bit constraint DF_ResponsibilityTypeRelationRule_ApplyToType default(0) not null
GO

ALTER VIEW [dbo].[FieldLookupValue]
AS
	SELECT	T.ID as FieldTypeID,
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
			AND COALESCE(A.ID, R.ResourceID, L.ID, RI.ID, RIT.ID,TAX.ID,TAXTYPE.ID) IS NOT NULL
GO

ALTER procedure [dbo].[DeleteObject]
	@ObjTemp varchar(50),
	@ObjectIDTemp int,
	@ResourceIDTemp int
as
begin
	set nocount on;

	-- Wierd StackOverflow about SQL Server using parameter sniffing, which can potentially slow down executing of procs from an application. See GOV-3316 for more details.
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
			@IsType bit = 0;

	declare @h table (IntersectID int, ID bigint, ObjectID int, Processed bit null);
	declare @ht table (IntersectTypeID int, ID int, ObjectID int, Processed bit null);

	declare @ClearAttributes bit = 0,
			@ClearComments bit = 0,
			@ClearIntersects bit = 0,
			@ClearFavorites bit = 0,
			@ClearFields bit = 0,
			@ClearFollows bit = 0,
			@ClearIssues bit = 0,
			@ClearNyms bit = 0,
			@ClearResponsibilities bit = 0,
			@ClearSiteNav bit = 0;

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
					inner join @h I on O.ID = I.ID;

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
					inner join @ht I on O.ID = I.ID;

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
					inner join @ht h on h.ObjectID = T.ID;

			delete	Artifact
			where	ID in (select ObjectID from @h);

			delete	ArtifactType			
			where	ID in (select ObjectID from @ht);
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
						inner join AssetType A on A.Object = R.ObjectType and A.ObjectID = R.ObjectID;

			update	T
			set		T.UpdatedBy = @ResourceID,
					T.UpdatedOn = @CurrentDate
			from	Asset T
					inner join [Attribute] S on S.ObjectType = T.Object and S.ObjectID = T.ObjectID and S.ID in (select ID from @a);

			update	T
			set		T.UpdatedBy = @ResourceID,
					T.UpdatedOn = @CurrentDate
			from	AssetType T
					inner join AttributeTypeRelation S on S.ObjectType = T.Object and S.ObjectID = T.ObjectID 
					inner join AttributeType A on A.ID = S.AttributeTypeID and A.ID in (select ID from @at);

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
						inner join AssetType A on A.Object = O.Object and A.ObjectID = O.ObjectID and O.ID = @ObjectID;

			update	T
			set		T.UpdatedBy = @ResourceID,
					T.UpdatedOn = @CurrentDate
			from	Asset T
					inner join Field S on S.ObjectType = T.Object and S.ObjectID = T.ObjectID and S.FieldTypeID = @ObjectID;

			update	T
			set		T.UpdatedBy = @ResourceID,
					T.UpdatedOn = @CurrentDate
			from	AssetType T
					inner join FieldType S on S.Object = T.Object and S.ObjectID = T.ObjectID and S.ID = @ObjectID;

			delete	Field 
			where	FieldTypeID = @ObjectID;
			
			update	FieldType 
			set		ParentFieldTypeID = null 
			where	ParentFieldTypeID = @ObjectID;

			delete	FieldType 
			where	ID = @ObjectID;
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
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID;

			delete	F
			from	RuleResultQualifierType F
					inner join RuleImplementation I on I.ID = F.RuleImplementationID
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID;

			delete	F
			from	RuleResultFusionAttribute F
					inner join RuleResult S on S.ID = F.RuleResultID
					inner join RuleImplementation I on I.ID = S.RuleImplementationID
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID;

			delete	S
			from	RuleResult S
					inner join RuleImplementation I on I.ID = S.RuleImplementationID
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID;

			delete [RuleImplementation] where RuleID in (select ID from [Rule] where RuleTypeID = @ObjectID);

			delete [Rule] where RuleTypeID = @ObjectID;

			delete RuleType where ID = @ObjectID;
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
			delete	Comment
			where	OwnerObjectType = @Object 
					and OwnerObjectID in (select ObjectID from @h)

			delete	CommentRelation
			where	ObjectType = @Object 
					and ObjectID in (select ObjectID from @h)
		END

		-- Site menu deletion
		IF @ClearSiteNav = 1
		BEGIN
			delete sitenav where objectid = @ObjectID and [object] = @Object
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
		DECLARE @ErrorMessage NVARCHAR(4000);
		DECLARE @ErrorSeverity INT;
	    DECLARE @ErrorState INT;

		SELECT 
			@ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE();

		-- Use RAISERROR inside the CATCH block to return error
		-- information about the original error that caused
		-- execution to jump to the CATCH block.
		RAISERROR (@ErrorMessage, -- Message text.
				   @ErrorSeverity, -- Severity.
				   @ErrorState -- State.
				   );

		rollback transaction @trans
	end catch
end
GO

ALTER PROCEDURE [dbo].[GetReferenceItemValues]	
	@listid int,
	@resourceID int	= 0,
	@useApiName bit = 0
AS
BEGIN
	SET NOCOUNT ON;
	
	create table #fieldtypes (ID int, Name nvarchar(250))
	create table #parentTypes (IntersectTypeID int, Name nvarchar(250), ReferenceListTypeID int, ParentLevel int)

	-- load the fields for this item
	if @useApiName = 1
		begin
			insert into #fieldtypes
				select ID, [Name] from fieldtype where object = 'ReferenceItemType' and objectid = @listid order by ColumnOrder
		end
	else
		begin
			insert into #fieldtypes
				select ID, 'Field' + cast(id as varchar(100)) as [Name] from fieldtype where object = 'ReferenceItemType' and objectid = @listid order by ColumnOrder
		end

	declare @parentLevel int = 0;
	declare @currentReferenceListID int = @listid;	
	-- load the parents for this reference item type
	while exists (select 1 from intersecttypedetail where [object] = 'ReferenceItemType' and objectid = @currentReferenceListID and predicatetype = 3 and @parentLevel < 20)
	begin
		-- need to loop through parent / child relations till we get to the lowest one or loop to many times
		insert into #parentTypes 
			select id, subjectname, subjectid, @parentLevel from intersecttypedetail where [object] = 'ReferenceItemType' and objectid = @currentReferenceListID and predicatetype = 3;

		select @currentReferenceListID =subjectid from intersecttypedetail where [object] = 'ReferenceItemType' and objectid = @currentReferenceListID and predicatetype = 3;
		
		set @parentLevel = @parentLevel +1;
	end
	
	
	DECLARE @tsqlSelect nvarchar(max);
	DECLARE @tsqlFrom nvarchar(max);
	DECLARE @tsqlWhere nvarchar(max);
	DECLARE @tsql nvarchar(max);

	set @tsqlSelect = 'select ri.id as [ID] ,ri.code as [Code],o.id as [AssetID]';
	set @tsqlFrom = ' from [dbo].[referenceitem] ri  inner join Asset O on O.Object = ''ReferenceItem'' and O.ObjectID = ri.ID ';
	set @tsqlWhere = ' where ri.visible = 1 and ri.referenceitemtypeid = ' + cast(@listid as nvarchar(20));
	if @resourceID > 0
	begin
		set @tsqlFrom = @tsqlFrom  + ' left join AssetWithoutReadPermission RP on RP.ResourceID = ' +  cast(@resourceID as varchar) + ' and RP.AssetID = O.ID ';
		set @tsqlWhere = @tsqlWhere + ' and RP.AssetID is null ';
	end	

	DECLARE @name nvarchar(250);
	DECLARE @id int = 0;
	DECLARE @intersectTypeId int;
	DECLARE @parentName nvarchar(250);
	DECLARE @parentListTypeID int = 0;	
	DECLARE @index int = 0;
	DECLARE @previousRelation varchar(200) = 'ri.ID';

	-- generate dynamic sql for each relationship
	DECLARE relCur CURSOR FOR SELECT IntersectTypeId, Name, ReferenceListTypeID, ParentLevel FROM #parentTypes
	OPEN relCur

	FETCH NEXT FROM relCur INTO @intersectTypeId, @parentName, @parentListTypeID, @parentLevel

	WHILE @@FETCH_STATUS = 0 BEGIN
	
		SET @tsqlSelect = @tsqlSelect + ',REL_' + cast(@index as nvarchar(10)) + '.DisplayValue as [Rel' + cast(@parentListTypeID as varchar(20)) + ']';
        SET @tsqlFrom = @tsqlFrom +' outer apply (
				    select	ID.DisplayValue, I.SubjectID                            
				    from	[PredicateIntersect] I
                            inner join Asset IA on I.Object = ''ReferenceItem'' and I.ObjectID = ' + @previousRelation + ' and IA.Object = ''ReferenceItem'' and IA.ObjectID = I.SubjectID and I.PredicateType = 3
                            inner join AssetType IAT on IAT.ID = IA.AssetTypeID
                            cross apply dbo.GetAssetDisplayValueById(IA.ID) ID
				    ) REL_' + cast(@index as nvarchar(10));

		set @previousRelation = 'REL_' + cast(@index as nvarchar(10)) + '.SubjectID';
		SET @index = @index + 1;
		FETCH NEXT FROM relCur INTO @intersectTypeId, @parentName, @parentListTypeID, @parentLevel
	END

	CLOSE relCur    
	DEALLOCATE relCur

	set @index = 0;
	-- generate dynamic sql for each field
	DECLARE cur CURSOR FOR SELECT id, name FROM #fieldtypes
	OPEN cur

	FETCH NEXT FROM cur INTO @id, @name

	WHILE @@FETCH_STATUS = 0 BEGIN
		
		SET @tsqlSelect = @tsqlSelect + ',f'+ cast(@index as nvarchar(10)) + '.formattedvalue as [' + @name + ']';
		SET @tsqlFrom = @tsqlFrom + ' left outer join [dbo].[field] f' + cast(@index as nvarchar(10)) + ' on (ri.id = f' + cast(@index as nvarchar(10)) + '.objectid and f' + cast(@index as nvarchar(10)) + '.[objecttype] = ''ReferenceItem'' and f' + cast(@index as nvarchar(10)) + '.fieldtypeid = ' + cast(@id as nvarchar(20)) + ')';

		SET @index = @index + 1;
		FETCH NEXT FROM cur INTO @id, @name
	END

	CLOSE cur    
	DEALLOCATE cur

	SET @tsql = @tsqlSelect + @tsqlFrom + @tsqlWhere;
	print @tsql
	EXEC sp_executesql @tsql;

END
GO

ALTER procedure [utility].[AddAuditEntry]
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
		
		if @DependentObject = 'ReferenceItemType'	begin		select @DependentObjectName = Name from ReferenceItemType where ID = @DependentObjectID		end		
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
				cross apply dbo.getIntersectTypeNames(O.ID) ITyName
		where	O.ID = @MainObjectID
	end
	
	-- Relevant ONLY to: IntersectType
	if @MainObject = 'IntersectType'
	begin
		select	@MainObjectTypeName = 'Intersect Type',
				@MainObjectName = O.Name 
		from	IntersectType O
				cross apply dbo.getIntersectTypeNames(O.ID) ITyName
		where	O.ID = @MainObjectID
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
	*/
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
		set @MainDescription = coalesce(@MainDescription,'') + '.'

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

ALTER procedure [utility].[GetFieldTypeLookupList]
--declare 
	@type varchar(50), --= 'ArtifactType',
	@id int --= 1
as
begin
	--select	type,
	--		value,
	--		title 
	--from	utility.GetIntersectTypesByType(@type, @id)

	--union

	--select	'A' as type,
	--		'AttributeType|' + cast(ID as varchar) as value,
	--		Name as title
	--from	AttributeType
	--where	ParentID is null

	--union

	--select	'F' as type,
	--		'FusionAttributeType|' + cast(ID as varchar) as value,
	--		TextPath as title
	--from	FusionAttributeType

	--union

	SELECT	'L' as type,
			'Artifact|' + cast(ID as varchar) as value,
			'Artifact : ' + Name as title
	FROM	ArtifactType
	UNION
	SELECT	'L' as type,
			'ReferenceItemType|0'  as value,
			'Reference List' as title
	UNION
	SELECT	'L' as type,
			'ReferenceItem|' + cast(ID as varchar) as value,
			'Reference List Item: ' + Name as title
	FROM	ReferenceItemType
	UNION
	SELECT	'L' as type,
			'Resource|1' as value,
			'Resource : User' as title
	UNION
	SELECT	'L' as type,
			'Taxonomy|' + cast(ID as varchar) as value,
			'Model : ' + Name as title
	FROM	TaxonomyType
	UNION
	SELECT	'L' as type,
			'TaxonomyType|0'  as value,
			'Model Type' as title
	UNION
	SELECT	'L' as type,
			'Lookup|' + cast(ID as varchar) as value,
			'Lookup : ' + Name as title
	FROM	LookupType

	--union

	--select	'FL' as type,
	--		'Lookup|' + cast(L.ID as varchar) as value,
	--		L.Name as title
	--from	LookupType L
	--		cross apply (
	--					select	count(1) as [Count]
	--					from	FieldType
	--					where	Object = 'LookupType' 
	--							and ObjectID = L.ID
	--							and [Type] = 'Lookup'
	--							and LookupObjectType = REPLACE(@type, 'Type','') 
	--							and LookupObjectID = @id
	--					) F
	--where	F.[Count] > 0
end
GO

ALTER FUNCTION [dbo].[GetAssetUrl]
(	
	@Type varchar(50),
	@TypeID int,
	@ObjectID int = 0
)
RETURNS TABLE 
AS
RETURN 
(
	-- Add the SELECT statement with parameter references here
	SELECT CASE @Type
		WHEN 'Artifact' THEN 'artifact/' +  + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)
		WHEN 'ArtifactType' THEN 'artifact/' + CAST(@TypeID as varchar)
		WHEN 'Domain' THEN 'domain/' +  + CAST(@TypeID as varchar) + '/' +  + CAST(@ObjectID as varchar)
		WHEN 'DomainType' THEN 'domain/' + CAST(@TypeID as varchar)
		WHEN 'ReferenceItem' THEN 'reference/' +  + CAST(@TypeID as varchar)-- + '/' +  + CAST(@ObjectID as varchar)
		WHEN 'ReferenceItemType' THEN 'reference/' + CAST(@TypeID as varchar)
		WHEN 'FusionAttribute' THEN 'fusion/fusionattribute/' + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)		
		WHEN 'Fusion' THEN 'fusion/' + CAST(@TypeID as varchar) + '/' + + CAST(@ObjectID as varchar)
		WHEN 'FusionType' THEN 'fusion/' + CAST(@TypeID as varchar)
		WHEN 'Group' THEN 'group/' + CAST(@ObjectID as varchar)	
		WHEN 'Lookup' THEN 'admin/lookups/' + CAST(@TypeID as varchar) + '/' + + CAST(@ObjectID as varchar)
		WHEN 'LookupType' THEN 'admin/lookups/' + CAST(@TypeID as varchar)
		WHEN 'Policy' THEN 'policy/' + CAST(@TypeID as varchar(15)) + '/id/' + CAST(@ObjectID as varchar)
		WHEN 'PolicyType' THEN 'policy/' + CAST(@TypeID as varchar) + '/structure'		
		WHEN 'Resource' THEN 'resource/' + CAST(@ObjectID as varchar)
		WHEN 'ResourceType' THEN 'resource/list/' + CAST(@TypeID as varchar)
		WHEN 'Rule' THEN 'quality/rule/' + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)
		WHEN 'RuleType' THEN 'quality/rule/' + CAST(@TypeID as varchar)	
		WHEN 'Taxonomy' THEN 'model/' + CAST(@TypeID as varchar) + '/id/' + CAST(@ObjectID as varchar)
		WHEN 'TaxonomyType' THEN 'model/' + CAST(@TypeID as varchar) + '/structure'	
		WHEN 'ShoppingCartType' THEN 'cart/' + CAST(@ObjectID as varchar)	
	end as Url
)
GO

alter function [dbo].[CheckIfObjectExistsWithParent]
(
	@ObjectType varchar(50), -- = 'ArtifactType'
	@ObjectTypeID int, -- = 1
	@ObjectID int, -- = 4651
	@Fields nvarchar(max), -- = '[{"id": 53072, "value":"Country Of Risk"}, {"id": 53096, "value":"Description for Country Of Risk"}]'	
	@ParentID int = -1
)
returns bit
as
begin
	declare @exists bit = 0;
	declare @numberOfKeyFields int = 0;
	declare @numberOfKeyMatches int = 0;
	declare @parentIntersectType int = 0;	
	declare @tbl table (ID int, Value nvarchar(max))
	
	insert into @tbl
		select	F.*
		from	openjson(@Fields) with (ID int 'strict $.ID', Value nvarchar(max) '$.Value') as F
				inner join FieldType T on T.ID = F.ID and T.Object = @ObjectType and T.ObjectID = @ObjectTypeID and T.IsPartOfKey = 1

	declare @results table (ID int, ObjectID int)
	
	-- do we only need to check items on the same level as the existing object?
	if (@ObjectType = 'PolicyType' or @ObjectType = 'TaxonomyType')
	begin
		select @parentIntersectType = IT.id 
			from 
				IntersectType IT
				inner join [Predicate] P on (IT.PredicateID = P.ID)
			where 
				[subject] = @ObjectType and [object] = @ObjectType and [subjectid] = @ObjectTypeID and [ObjectId] = @ObjectTypeID  and P.[Type] = 4;

		if ( @ParentID is null or @ParentID <=0 ) and @ObjectID is not null
		begin					
			select @ParentId = [subjectid] 
			from 
				[Intersect] I 
			where 
				I.IntersectTypeId = @parentIntersectType and I.ObjectID = @ObjectID;
		end;

			-- if it doesnt have a parent only consider top level items
			if ( @ParentId is not null and @ParentId > 0)
			begin
				if @ObjectID is not null -- edit existing item not top level
				begin
					insert into @results
						select	T.ID,
								F.ObjectID 
						from	@tbl T										
								inner join Field F on F.FieldTypeID = T.ID and F.Value = T.Value and (F.ObjectID <> @ObjectID)
								inner join [Intersect] I on (I.IntersectTypeID = @parentIntersectType and I.ObjectID = F.ObjectID and I.SubjectID = @ParentId)
				end
				else-- new item item not top level
				begin
					insert into @results
						select	T.ID,
								F.ObjectID 
						from	@tbl T										
								inner join Field F on F.FieldTypeID = T.ID and F.Value = T.Value
								inner join [Intersect] I on (I.IntersectTypeID = @parentIntersectType and I.ObjectID = F.ObjectID and I.SubjectID = @ParentId)
				end
			end
			else
			begin
				if @ObjectID is not null -- edit existing item top level
				begin
					insert into @results
						select	T.ID,
								F.ObjectID 
						from	@tbl T										
								inner join Field F on F.FieldTypeID = T.ID and F.Value = T.Value and (F.ObjectID <> @ObjectID)
						where 
							not exists (select 1 from [Intersect] I where (I.IntersectTypeID = @parentIntersectType and I.ObjectID = F.ObjectID)	)					
				end
				else
				begin -- new item item top level
					insert into @results
						select	T.ID,
								F.ObjectID 
						from	@tbl T										
								inner join Field F on F.FieldTypeID = T.ID and F.Value = T.Value
						where 
							not exists (select 1 from [Intersect] I where (I.IntersectTypeID = @parentIntersectType and I.ObjectID = F.ObjectID)	)					
				end
			end				
	end	-- end policy / model
	else if (@ObjectType = 'ArtifactType')
	begin
		begin
			select top 1 @parentIntersectType = IT.id 
				from 
					IntersectType IT
					inner join [Predicate] P on (IT.PredicateID = P.ID)
				where 
					[subject] = @ObjectType and [object] = @ObjectType and [ObjectId] = @ObjectTypeID  and P.[Type] = 3;
		end

		if (@parentIntersectType is not null and @parentIntersectType > 0)
		begin
			-- has parent

			if ( @ParentID is null or @ParentID <=0 ) and @ObjectID is not null
			begin					
				select @ParentId = [subjectid] 
				from 
					[Intersect] I 
				where 
					I.IntersectTypeId = @parentIntersectType and I.ObjectID = @ObjectID;
			end;

			insert into @results
						select	T.ID,
								F.ObjectID 
						from	@tbl T										
								inner join Field F on F.FieldTypeID = T.ID and F.Value = T.Value
								inner join [Intersect] I on (I.IntersectTypeID = @parentIntersectType and I.ObjectID = F.ObjectID and I.SubjectID = @ParentId)
		end
		else
		begin
			-- no parent
			insert into @results
			select	T.ID,
					F.ObjectID 
			from	@tbl T
					left join Field F on F.FieldTypeID = T.ID and F.Value = T.Value and ( (@ObjectID is null) OR (@ObjectID is not null and F.ObjectID <> @ObjectID) )
		end
	end
	else
	begin
		insert into @results
			select	T.ID,
					F.ObjectID 
			from	@tbl T
					left join Field F on F.FieldTypeID = T.ID and F.Value = T.Value and ( (@ObjectID is null) OR (@ObjectID is not null and F.ObjectID <> @ObjectID) )
	end

	if exists(select 1 from @results)
		begin
			if exists(select 1 from @results where ObjectID is null)
				begin
					set	@exists = 0
				end
			else
				begin
					-- need to check if there are multiple keys does the same object have all? so check that the count of key fields in tbl matches the count in results for that object
					select @numberOfKeyMatches = a.maxcount from (select top 1 objectid, count(1) as maxcount from @results group by objectid order by 2 desc) a ;
					select @numberOfKeyFields = count(1) from @tbl

					if (@numberOfKeyMatches = @numberOfKeyFields)
					begin
						set @exists = 1
					end
					else
					begin
						set @exists = 0
					end
				end
		end
	else
		begin
			set @exists = 0
		end

	return @exists
end
GO

ALTER FUNCTION [dbo].[GetWorkflowConditionLabels]
(
	@conditions xml
)
RETURNS xml
AS
BEGIN
	declare @recordCount int;

	declare @results table (id int, FieldTypeID int, ValueType varchar(max), [Value] nvarchar(max), Operator varchar(max), VersionStepID int, FormInputID varchar(max), ContextualFieldID varchar(max), ValueLabel varchar(max));

	select 
		 @recordCount = count(*)
	from 
		@conditions.nodes('/Conditions/Condition') c(x);

		insert into @results (id, FieldTypeID, VersionStepID, FormInputID, ValueType, [Value], Operator, ContextualFieldID, ValueLabel)
			select
			row_number() over (order by x.value('@FieldTypeID', 'int'), x.value('@VersionStepID', 'int'), x.value('@FormInputID', 'varchar(max)')) as id,
			 x.value('@FieldTypeID', 'int') as FieldTypeID
			,x.value('@VersionStepID', 'int') as VersionStepID  
			,x.value('@FormInputID', 'varchar(max)') as FormInputID
			,x.value('@ValueType', 'varchar(max)') as ValueType  
			,x.value('@Value', 'varchar(max)') as [Value]  
			,x.value('@Operator', 'varchar(max)') as [Operator] 
			,x.value('@ContextualFieldID', 'varchar(max)') as ContextualFieldID
			,null as ValueLabel
		from 
			@conditions.nodes('/Conditions/Condition') c(x)
		left join FieldType FT on FT.ID = x.value('@FieldTypeID', 'int')
		left join workflow.VersionStep VS on VS.ID = x.value('@VersionStepID', 'int')

		
	while(@recordCount > 0)
	begin
		if (select top 1 ValueType from @results where id = @recordCount) in ('U', 'L')
		begin
		
			if ((select FieldTypeID from @results where id = @recordCount) is not null)
			begin
				declare @valueLabel varchar(max);

				select @valueLabel = coalesce(RI.DisplayValue, R.[Value])
				from 
					FieldType FT
				inner join @results R on R.id = @recordCount and FT.ID = R.FieldTypeID
				left join LookupType LT on FT.LookupObjectType = 'Lookup' and LT.ID = FT.LookupObjectID
				left join [Lookup] L on L.ID = (select top 1 [Value] from @results where id = @recordCount)
				left join ReferenceItem RI on FT.LookupObjectType = 'ReferenceItem' and FT.LookupObjectID = RI.ReferenceItemTypeID and RI.ID = R.[Value]

				update r
				set r.ValueLabel = @valueLabel
				from @results r
				where r.id = @recordCount;

			end
			
			if ((select FormInputID from @results where id = @recordCount) is not null)
			begin
				declare @fields xml, @valueLabel2 varchar(max);

				select @fields = VS.fields from 
				workflow.VersionStep VS
				inner join @results R on R.id = @recordCount and VS.ID = R.VersionStepID;


				select 
					@valueLabel2 = coalesce(RI.DisplayValue, R.[Value])
				from @fields.nodes('fields/form/field') f(x)
				inner join @results R on R.id = @recordCount
				inner join FieldType FT on FT.ID = x.value('@referenceFieldId', 'int')
				left join LookupType LT on FT.LookupObjectType = 'Lookup' and LT.ID = FT.LookupObjectID
				left join [Lookup] L on L.ID = (select top 1 [Value] from @results where id = @recordCount)
				left join ReferenceItem RI on FT.LookupObjectType = 'ReferenceItem' and FT.LookupObjectID = RI.ReferenceItemTypeID and RI.ID = R.[Value]
				where x.value('@id', 'varchar(max)') = R.FormInputID;


				update r
				set r.ValueLabel = @valueLabel2
				from @results r
				where r.id = @recordCount;


			end
		end	
		else
		begin
			update r
			set r.ValueLabel = r.[Value]
			from @results r
			where r.id = @recordCount;
		end


		set @recordCount = @recordCount - 1;
	end

	RETURN 
		coalesce(
		 (select 
			r.FieldTypeID as 'Condition/@FieldTypeID',
			r.VersionStepID as 'Condition/@VersionStepID',
			r.FormInputID as 'Condition/@FormInputID',
			r.ValueType as 'Condition/@ValueType',
			r.[Value] as 'Condition/@Value',
			r.Operator as 'Condition/@Operator',
			r.ContextualFieldID as 'Condition/@ContextualFieldID',
			r.ValueLabel as 'Condition/@ValueLabel' 
		from @results r
		for xml path(''), root('Conditions'))
		,
		'<Conditions />');
END
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
											/*SELECT	ID as AID,
													CAST(ID as nvarchar(max)) as ID,
													CAST(DisplayValue as nvarchar(max)) as TextPath
											FROM	Artifact A
											WHERE	A.ID = CAST(@Value as int)
													and L.ObjectType = 'Artifact'*/
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
											/*SELECT	ID,
													CAST(TextPath as nvarchar(max)) as TextPath
											FROM	Taxonomy A
											WHERE	A.ID = CAST(@Value as int)
													and L.ObjectType = 'Taxonomy'*/
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

ALTER FUNCTION [utility].[GetFormattedFieldLookupValueWithMultiple]
(
	@Type varchar(25),
	@DisplayFormat nvarchar(250),
	@LookupObjectType varchar(25),
	@LookupObjectID int,
	@Value nvarchar(max),
	@SupportsMultipleValues bit	
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
		if @SupportsMultipleValues = 1
		begin	
			set @formattedValue =  utility.GenerateFormattedMultipleValue (@DisplayFormat, @LookupObjectType, @LookupObjectID, @Value)
		end
		else if @LookupObjectType = 'ReferenceItemType'
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
											SELECT	ID as AID,
													CAST(ID as nvarchar(max)) as ID,
													CAST(DisplayValue as nvarchar(max)) as TextPath
											FROM	Artifact A
											WHERE	A.ID = CAST(@Value as int)
													and L.ObjectType = 'Artifact'
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
											SELECT	ID,
													CAST(TextPath as nvarchar(max)) as TextPath
											FROM	Taxonomy A
											WHERE	A.ID = CAST(@Value as int)
													and L.ObjectType = 'Taxonomy'
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
						select @currentValue = utility.GetFormattedFieldLookupValueWithMultiple(@Type, @lkpFormat, @lkpType, @lkpID, @currentValue, @SupportsMultipleValues)
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

CREATE FUNCTION [dbo].[GetWorkflowResponsibleUsers]
(
	@itemStepId int,
	@firstResponse bit
)
RETURNS varchar(max)
AS
BEGIN
RETURN (
	
	select coalesce(string_agg(X.ResponsibleUsers, ', '), '[unknown]') as ResponsibleUsers from
	(
		select distinct
			case when @firstResponse = 1 then
					GR.FirstName + ' ' + GR.LastName
			else
				coalesce(
					GR2.FirstName + ' ' + GR2.LastName,
					GR.FirstName + ' ' + GR.LastName, 
					NULL)
			end as ResponsibleUsers
		from	workflow.ItemStep IST
		left join workflow.Item I on I.ID = IST.ItemID
		left join workflow.ItemAssignment IA on IA.ItemID = I.ID	
		left join reporting.Global_resource GR on GR.ResourceID = IST.CompletedBy
		left join reporting.Global_resource GR2 on GR2.ResourceID = IA.ResourceObjectID
		where
			IST.ID = @itemStepId
		group by GR.FirstName, GR.LastName, GR2.FirstName, GR2.LastName, IST.ID ,IST.ItemID, IST.StepID, IA.ID
	) X		
)
END
GO

CREATE CLUSTERED INDEX CIX_IntegrationSynchedAssetTypeFieldItem ON integration.SynchedAssetTypeFieldItem ( [SynchedAssetTypeID] ASC, [SourceField] ASC )
GO

alter table [Intersect] add SourceID nvarchar(500) null
GO

alter table [ReferenceItem] add SourceID nvarchar(500) null
GO

CREATE TABLE [integration].[SynchedIntersectType](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[IntegrationSettingID] [int] NOT NULL,
	[SourceIntersectTypeID] [int] NOT NULL,
	[TargetAssetTypeName] [varchar](500) NOT NULL,
	[Active] [bit] NOT NULL,
	[LastSynchOn] [datetime] NULL,
	CONSTRAINT [PK_IntegrationSynchedIntersectType] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [integration].[SynchedIntersectType] ADD  CONSTRAINT [DF_IntegrationSynchedIntersectType_Active]  DEFAULT ((0)) FOR [Active]
GO

ALTER TABLE [integration].[SynchedIntersectType]  WITH CHECK ADD  CONSTRAINT [FK_IntegrationSynchedIntersectType_IntegrationSetting] FOREIGN KEY([IntegrationSettingID]) REFERENCES [integration].[Setting] ([ID])
GO

ALTER TABLE [integration].[SynchedIntersectType] CHECK CONSTRAINT [FK_IntegrationSynchedIntersectType_IntegrationSetting]
GO

ALTER TRIGGER [dbo].[ReferenceItem_AfterInsert]
   ON  [dbo].[ReferenceItem] 
   AFTER INSERT
AS 
	SET NOCOUNT ON
	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],SourceID,[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, O.SourceID,'ReferenceItem', O.ID, O.[CreatedOn], O.[CreatedBy], O.[UpdatedOn], O.[UpdatedBy]
		FROM	inserted O inner join  AssetType T on T.Object = 'ReferenceItemType' and T.ObjectID = O.ReferenceItemTypeID
GO

ALTER TRIGGER [dbo].[ReferenceItem_AfterUpdate]
   ON  [dbo].[ReferenceItem] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn,
			T.SourceID = S.SourceID
	from	Asset T
			inner join inserted S on T.Object = 'ReferenceItem' and T.ObjectID = S.ID
GO

alter table [integration].[SynchedAssetType] add AllowChangeDetection bit constraint DF_IntegrationSynchedAssetType_AllowChangeDetection default(1) not null
GO


ALTER TABLE [dbo].[ResponsibilityTypeRelationRule] ADD [IsVisible]            BIT            CONSTRAINT [DF_ResponsibilityTypeRelationRule_IsVisible] DEFAULT ((1)) NOT NULL
ALTER TABLE [dbo].[ResponsibilityTypeRelationRule] ADD [ApplyToType]          BIT            CONSTRAINT [DF_ResponsibilityTypeRelationRule_ApplyToType] DEFAULT ((0)) NOT NULL
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
							--NULL,
					
							O.SourceIntersectID,
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

							O.TargetIntersectID,
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

					from	@tBusinessPoints O
							inner join IntersectDetail SI on SI.ID = O.SourceIntersectID
							inner join IntersectDetail TI ON TI.ID = O.TargetIntersectID

							--select * from @tItems;

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

ALTER PROCEDURE [dbo].[GetReferenceItemValues]	
	@listid int,
	@resourceID int	= 0,
	@useApiName bit = 0
AS
BEGIN
	SET NOCOUNT ON;
	
	create table #fieldtypes (ID int, Name nvarchar(250))
	create table #parentTypes (IntersectTypeID int, Name nvarchar(250), ReferenceListTypeID int, ParentLevel int)

	-- load the fields for this item
	if @useApiName = 1
		begin
			insert into #fieldtypes
				select ID, [Name] from fieldtype where object = 'ReferenceItemType' and objectid = @listid order by ColumnOrder
		end
	else
		begin
			insert into #fieldtypes
				select ID, 'Field' + cast(id as varchar(100)) as [Name] from fieldtype where object = 'ReferenceItemType' and objectid = @listid order by ColumnOrder
		end

	declare @parentLevel int = 0;
	declare @currentReferenceListID int = @listid;	
	-- load the parents for this reference item type
	while exists (select 1 from intersecttypedetail where [object] = 'ReferenceItemType' and objectid = @currentReferenceListID and predicatetype = 3 and @parentLevel < 20)
	begin
		-- need to loop through parent / child relations till we get to the lowest one or loop to many times
		insert into #parentTypes 
			select id, subjectname, subjectid, @parentLevel from intersecttypedetail where [object] = 'ReferenceItemType' and objectid = @currentReferenceListID and predicatetype = 3;

		select @currentReferenceListID =subjectid from intersecttypedetail where [object] = 'ReferenceItemType' and objectid = @currentReferenceListID and predicatetype = 3;
		
		set @parentLevel = @parentLevel +1;
	end
	
	
	DECLARE @tsqlSelect nvarchar(max);
	DECLARE @tsqlFrom nvarchar(max);
	DECLARE @tsqlWhere nvarchar(max);
	DECLARE @tsql nvarchar(max);

	set @tsqlSelect = 'select ri.id as [ID] ,ri.code as [Code],o.id as [AssetID]';
	set @tsqlFrom = ' from [dbo].[referenceitem] ri  inner join Asset O on O.Object = ''ReferenceItem'' and O.ObjectID = ri.ID ';
	set @tsqlWhere = ' where ri.visible = 1 and ri.referenceitemtypeid = ' + cast(@listid as nvarchar(20));
	if @resourceID > 0
	begin
		set @tsqlFrom = @tsqlFrom  + ' left join AssetWithoutReadPermission RP on RP.ResourceID = ' +  cast(@resourceID as varchar) + ' and RP.AssetID = O.ID ';
		set @tsqlWhere = @tsqlWhere + ' and RP.AssetID is null ';
	end	

	DECLARE @name nvarchar(250);
	DECLARE @id int = 0;
	DECLARE @intersectTypeId int;
	DECLARE @parentName nvarchar(250);
	DECLARE @parentListTypeID int = 0;	
	DECLARE @index int = 0;
	DECLARE @previousRelation varchar(200) = 'ri.ID';

	-- generate dynamic sql for each relationship
	DECLARE relCur CURSOR FOR SELECT IntersectTypeId, Name, ReferenceListTypeID, ParentLevel FROM #parentTypes
	OPEN relCur

	FETCH NEXT FROM relCur INTO @intersectTypeId, @parentName, @parentListTypeID, @parentLevel

	WHILE @@FETCH_STATUS = 0 BEGIN
	
		SET @tsqlSelect = @tsqlSelect + ',REL_' + cast(@index as nvarchar(10)) + '.DisplayValue as [Rel' + cast(@parentListTypeID as varchar(20)) + ']';
        SET @tsqlFrom = @tsqlFrom +' outer apply (
				    select	ID.DisplayValue, I.SubjectID                            
				    from	[PredicateIntersect] I
                            inner join Asset IA on I.Object = ''ReferenceItem'' and I.ObjectID = ' + @previousRelation + ' and IA.Object = ''ReferenceItem'' and IA.ObjectID = I.SubjectID and I.PredicateType = 3
                            inner join AssetType IAT on IAT.ID = IA.AssetTypeID
                            cross apply dbo.GetAssetDisplayValueById(IA.ID) ID
				    ) REL_' + cast(@index as nvarchar(10));

		set @previousRelation = 'REL_' + cast(@index as nvarchar(10)) + '.SubjectID';
		SET @index = @index + 1;
		FETCH NEXT FROM relCur INTO @intersectTypeId, @parentName, @parentListTypeID, @parentLevel
	END

	CLOSE relCur    
	DEALLOCATE relCur

	set @index = 0;
	-- generate dynamic sql for each field
	DECLARE cur CURSOR FOR SELECT id, name FROM #fieldtypes
	OPEN cur

	FETCH NEXT FROM cur INTO @id, @name

	WHILE @@FETCH_STATUS = 0 BEGIN
		
		SET @tsqlSelect = @tsqlSelect + ',f'+ cast(@index as nvarchar(10)) + '.formattedvalue as [' + @name + ']';
		SET @tsqlFrom = @tsqlFrom + ' left outer join [dbo].[field] f' + cast(@index as nvarchar(10)) + ' on (ri.id = f' + cast(@index as nvarchar(10)) + '.objectid and f' + cast(@index as nvarchar(10)) + '.[objecttype] = ''ReferenceItem'' and f' + cast(@index as nvarchar(10)) + '.fieldtypeid = ' + cast(@id as nvarchar(20)) + ')';

		SET @index = @index + 1;
		FETCH NEXT FROM cur INTO @id, @name
	END

	CLOSE cur    
	DEALLOCATE cur

	SET @tsql = @tsqlSelect + @tsqlFrom + @tsqlWhere;
	print @tsql
	EXEC sp_executesql @tsql;

END
GO

alter procedure [fusion].[ProcessFusionCacheInQueue]
--declare
	@FusionID int
--set @FusionID = 15
as
begin
	SET NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	UPDATE  FusionAttribute
	SET		TextPath = utility.GetBreadcrumbStringWrapper('FusionAttribute', ID, '.')
	FROM	FusionAttribute 
	WHERE	FusionID = @FusionID and deleted = 0

	-- if this is a markit fusion update the lineage
	declare @fusionTypeId int;

	select 
		@fusionTypeId = fusiontypeid
	from
		fusion
	where
		id = @FusionID;

	if @fusionTypeId = 13
	begin		
		exec fusion.GenerateMarkitMapLineageData @FusionID
	end
	else if @fusionTypeId = 16
	begin
		exec [fusion].[GenerateEagleLineageData] @FusionID
	end
	else if @fusionTypeId = 111
	begin
		exec [fusion].[GenerateFoundationLineage]
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
		set @MainDescription = coalesce(@MainDescription,'') + '.'

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

alter procedure [utility].[GetFieldTypeLookupList]
--declare 
	@type varchar(50), --= 'ArtifactType',
	@id int --= 1
as
begin
	--select	type,
	--		value,
	--		title 
	--from	utility.GetIntersectTypesByType(@type, @id)

	--union

	--select	'A' as type,
	--		'AttributeType|' + cast(ID as varchar) as value,
	--		Name as title
	--from	AttributeType
	--where	ParentID is null

	--union

	--select	'F' as type,
	--		'FusionAttributeType|' + cast(ID as varchar) as value,
	--		TextPath as title
	--from	FusionAttributeType

	--union

	SELECT	'L' as type,
			'Artifact|' + cast(ID as varchar) as value,
			'Artifact : ' + Name as title
	FROM	ArtifactType
	UNION
	SELECT	'L' as type,
			'ReferenceItemType|0'  as value,
			'Reference List' as title
	UNION
	SELECT	'L' as type,
			'ReferenceItem|' + cast(ID as varchar) as value,
			'Reference List Item: ' + Name as title
	FROM	ReferenceItemType
	UNION
	SELECT	'L' as type,
			'Resource|1' as value,
			'Resource : User' as title
	UNION
	SELECT	'L' as type,
			'Taxonomy|' + cast(ID as varchar) as value,
			'Model : ' + Name as title
	FROM	TaxonomyType
	UNION
	SELECT	'L' as type,
			'TaxonomyType|0'  as value,
			'Model Type' as title
	UNION
	SELECT	'L' as type,
			'Lookup|' + cast(ID as varchar) as value,
			'Lookup : ' + Name as title
	FROM	LookupType

	--union

	--select	'FL' as type,
	--		'Lookup|' + cast(L.ID as varchar) as value,
	--		L.Name as title
	--from	LookupType L
	--		cross apply (
	--					select	count(1) as [Count]
	--					from	FieldType
	--					where	Object = 'LookupType' 
	--							and ObjectID = L.ID
	--							and [Type] = 'Lookup'
	--							and LookupObjectType = REPLACE(@type, 'Type','') 
	--							and LookupObjectID = @id
	--					) F
	--where	F.[Count] > 0
end
GO

ALTER FUNCTION [dbo].[GetAssetKeyHashById](
	@Id bigint
)
RETURNS TABLE 
AS
RETURN 
(
	

select		A.AssetTypeID,
				A.ID,
				CONVERT(
					varchar(32), 
					SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
					2) as KeyHash
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
				CONVERT(
					varchar(32), 
					SUBSTRING(HASHBYTES('SHA1', STRING_AGG(r.code, char(59))), 3, 32), 
					2) as KeyHash
	from		Asset A
				inner join referenceitem r on (a.object = 'ReferenceItem' and r.id = a.objectid)
	where a.id = @id
	group by	A.AssetTypeID, A.ID

)
GO

ALTER FUNCTION [dbo].[GetAssetUrl]
(	
	@Type varchar(50),
	@TypeID int,
	@ObjectID int = 0
)
RETURNS TABLE 
AS
RETURN 
(
	-- Add the SELECT statement with parameter references here
	SELECT CASE @Type
		WHEN 'Artifact' THEN 'artifact/' +  + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)
		WHEN 'ArtifactType' THEN 'artifact/' + CAST(@TypeID as varchar)
		WHEN 'Domain' THEN 'domain/' +  + CAST(@TypeID as varchar) + '/' +  + CAST(@ObjectID as varchar)
		WHEN 'DomainType' THEN 'domain/' + CAST(@TypeID as varchar)
		WHEN 'ReferenceItem' THEN 'reference/' +  + CAST(@TypeID as varchar)-- + '/' +  + CAST(@ObjectID as varchar)
		WHEN 'ReferenceItemType' THEN 'reference/' + CAST(@TypeID as varchar)
		WHEN 'FusionAttribute' THEN 'fusion/fusionattribute/' + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)		
		WHEN 'Fusion' THEN 'fusion/' + CAST(@TypeID as varchar) + '/' + + CAST(@ObjectID as varchar)
		WHEN 'FusionType' THEN 'fusion/' + CAST(@TypeID as varchar)
		WHEN 'Group' THEN 'group/' + CAST(@ObjectID as varchar)	
		WHEN 'Lookup' THEN 'admin/lookups/' + CAST(@TypeID as varchar) + '/' + + CAST(@ObjectID as varchar)
		WHEN 'LookupType' THEN 'admin/lookups/' + CAST(@TypeID as varchar)
		WHEN 'Policy' THEN 'policy/' + CAST(@TypeID as varchar(15)) + '/id/' + CAST(@ObjectID as varchar)
		WHEN 'PolicyType' THEN 'policy/' + CAST(@TypeID as varchar) + '/structure'		
		WHEN 'Resource' THEN 'resource/' + CAST(@ObjectID as varchar)
		WHEN 'ResourceType' THEN 'resource/list/' + CAST(@TypeID as varchar)
		WHEN 'Rule' THEN 'quality/rule/' + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)
		WHEN 'RuleType' THEN 'quality/rule/' + CAST(@TypeID as varchar)	
		WHEN 'Taxonomy' THEN 'model/' + CAST(@TypeID as varchar) + '/id/' + CAST(@ObjectID as varchar)
		WHEN 'TaxonomyType' THEN 'model/' + CAST(@TypeID as varchar) + '/structure'	
		WHEN 'ShoppingCartType' THEN 'cart/' + CAST(@ObjectID as varchar)	
	end as Url
)
GO

ALTER FUNCTION [dbo].[GetReferenceItemDisplayValue]
(
	@referenceItemId int,
	@fieldTypeId int
)
RETURNS TABLE 
AS
RETURN 
(
	select		top 1
				string_agg(coalesce(D.FormattedValue, D.value), '') as DisplayValue
	from		FieldType FT_1 
				inner join referenceitemtype RIT on RIT.ID = FT_1.LookupObjectID
				inner join referenceitem RI on RI.ReferenceItemTypeID = RIT.ID and RI.ID = @referenceItemId
				outer apply (
							select	TL.value,
									coalesce(F.FormattedValue, RI.Code) as FormattedValue
							from	string_split(replace(FT_1.LookupDisplayFormat, '{', '|'), '|') TF
									cross apply string_split(replace(TF.[value], '}', '|'), '|') TL
									left join FieldType FT on FT.Object = 'ReferenceItemType' and FT.ObjectID = RIT.ID and FT.Name like TL.Value
									left join Field F on F.FieldTypeID = FT.ID and F.ObjectID = RI.ID and F.objecttype = 'ReferenceItem'
									left join ReferenceItem RI2 on TL.Value = 'Code' and RI2.ID = RI.ID									
							where	RTRIM(TF.value) <> ''
									and RTRIM(TL.value) <> ''
							) D
	where FT_1.ID = @fieldTypeId
)
GO

ALTER FUNCTION [dbo].[ArtifactNgSiteNavigation](@id int)
RETURNS XML
WITH RETURNS NULL ON NULL INPUT
BEGIN 
	RETURN 
	(
	SELECT	name,
			url,
			'Menu_AT' + cast(id as varchar(15)) as menuID,
			0 as feature,
			case when @@NESTLEVEL > 24 then null else  dbo.ArtifactNgSiteNavigation(id) end as items
	FROM	(				
					select
						FAT.ID,
						FAT.Name,
						AUrl.[Url] as [Url]
					from	    ArtifactType FAT					
					inner join AssetType T on T.Object = 'ArtifactType' and T.ObjectID = FAT.ID
					outer apply (
							select	IT.SubjectID
							from	IntersectType IT 
									inner join [Predicate] P on IT.Object = T.Object and IT.ObjectID = FAT.ID and P.ID = IT.PredicateID and P.Type = 3
							) IT
					cross apply [dbo].[GetAssetUrl]('ArtifactType', FAT.ID, 0) AUrl
					where IT.SubjectID = @id and FAT.ID <> @id
			--		) A
			) BG
			FOR XML PATH('nav'), TYPE
	)
END
GO

ALTER function [dbo].[CheckIfObjectExistsWithParent]
(
	@ObjectType varchar(50), -- = 'ArtifactType'
	@ObjectTypeID int, -- = 1
	@ObjectID int, -- = 4651
	@Fields nvarchar(max), -- = '[{"id": 53072, "value":"Country Of Risk"}, {"id": 53096, "value":"Description for Country Of Risk"}]'	
	@ParentID int = -1
)
returns bit
as
begin
	declare @exists bit = 0;
	declare @numberOfKeyFields int = 0;
	declare @numberOfKeyMatches int = 0;
	declare @parentIntersectType int = 0;	
	declare @tbl table (ID int, Value nvarchar(max))
	
	insert into @tbl
		select	F.*
		from	openjson(@Fields) with (ID int 'strict $.ID', Value nvarchar(max) '$.Value') as F
				inner join FieldType T on T.ID = F.ID and T.Object = @ObjectType and T.ObjectID = @ObjectTypeID and T.IsPartOfKey = 1

	declare @results table (ID int, ObjectID int)
	
	-- do we only need to check items on the same level as the existing object?
	if (@ObjectType = 'PolicyType' or @ObjectType = 'TaxonomyType')
	begin
		select @parentIntersectType = IT.id 
			from 
				IntersectType IT
				inner join [Predicate] P on (IT.PredicateID = P.ID)
			where 
				[subject] = @ObjectType and [object] = @ObjectType and [subjectid] = @ObjectTypeID and [ObjectId] = @ObjectTypeID  and P.[Type] = 4;

		if ( @ParentID is null or @ParentID <=0 ) and @ObjectID is not null
		begin					
			select @ParentId = [subjectid] 
			from 
				[Intersect] I 
			where 
				I.IntersectTypeId = @parentIntersectType and I.ObjectID = @ObjectID;
		end;

			-- if it doesnt have a parent only consider top level items
			if ( @ParentId is not null and @ParentId > 0)
			begin
				if @ObjectID is not null -- edit existing item not top level
				begin
					insert into @results
						select	T.ID,
								F.ObjectID 
						from	@tbl T										
								inner join Field F on F.FieldTypeID = T.ID and F.Value = T.Value and (F.ObjectID <> @ObjectID)
								inner join [Intersect] I on (I.IntersectTypeID = @parentIntersectType and I.ObjectID = F.ObjectID and I.SubjectID = @ParentId)
				end
				else-- new item item not top level
				begin
					insert into @results
						select	T.ID,
								F.ObjectID 
						from	@tbl T										
								inner join Field F on F.FieldTypeID = T.ID and F.Value = T.Value
								inner join [Intersect] I on (I.IntersectTypeID = @parentIntersectType and I.ObjectID = F.ObjectID and I.SubjectID = @ParentId)
				end
			end
			else
			begin
				if @ObjectID is not null -- edit existing item top level
				begin
					insert into @results
						select	T.ID,
								F.ObjectID 
						from	@tbl T										
								inner join Field F on F.FieldTypeID = T.ID and F.Value = T.Value and (F.ObjectID <> @ObjectID)
						where 
							not exists (select 1 from [Intersect] I where (I.IntersectTypeID = @parentIntersectType and I.ObjectID = F.ObjectID)	)					
				end
				else
				begin -- new item item top level
					insert into @results
						select	T.ID,
								F.ObjectID 
						from	@tbl T										
								inner join Field F on F.FieldTypeID = T.ID and F.Value = T.Value
						where 
							not exists (select 1 from [Intersect] I where (I.IntersectTypeID = @parentIntersectType and I.ObjectID = F.ObjectID)	)					
				end
			end				
	end	-- end policy / model
	else if (@ObjectType = 'ArtifactType')
	begin
		begin
			select top 1 @parentIntersectType = IT.id 
				from 
					IntersectType IT
					inner join [Predicate] P on (IT.PredicateID = P.ID)
				where 
					[subject] = @ObjectType and [object] = @ObjectType and [ObjectId] = @ObjectTypeID  and P.[Type] = 3;
		end

		if (@parentIntersectType is not null and @parentIntersectType > 0)
		begin
			-- has parent

			if ( @ParentID is null or @ParentID <=0 ) and @ObjectID is not null
			begin					
				select @ParentId = [subjectid] 
				from 
					[Intersect] I 
				where 
					I.IntersectTypeId = @parentIntersectType and I.ObjectID = @ObjectID;
			end;

			insert into @results
						select	T.ID,
								F.ObjectID 
						from	@tbl T										
								inner join Field F on F.FieldTypeID = T.ID and F.Value = T.Value
								inner join [Intersect] I on (I.IntersectTypeID = @parentIntersectType and I.ObjectID = F.ObjectID and I.SubjectID = @ParentId)
		end
		else
		begin
			-- no parent
			insert into @results
			select	T.ID,
					F.ObjectID 
			from	@tbl T
					left join Field F on F.FieldTypeID = T.ID and F.Value = T.Value and ( (@ObjectID is null) OR (@ObjectID is not null and F.ObjectID <> @ObjectID) )
		end
	end
	else
	begin
		insert into @results
			select	T.ID,
					F.ObjectID 
			from	@tbl T
					left join Field F on F.FieldTypeID = T.ID and F.Value = T.Value and ( (@ObjectID is null) OR (@ObjectID is not null and F.ObjectID <> @ObjectID) )
	end

	if exists(select 1 from @results)
		begin
			if exists(select 1 from @results where ObjectID is null)
				begin
					set	@exists = 0
				end
			else
				begin
					-- need to check if there are multiple keys does the same object have all? so check that the count of key fields in tbl matches the count in results for that object
					select @numberOfKeyMatches = a.maxcount from (select top 1 objectid, count(1) as maxcount from @results group by objectid order by 2 desc) a ;
					select @numberOfKeyFields = count(1) from @tbl

					if (@numberOfKeyMatches = @numberOfKeyFields)
					begin
						set @exists = 1
					end
					else
					begin
						set @exists = 0
					end
				end
		end
	else
		begin
			set @exists = 0
		end

	return @exists
end
GO


ALTER FUNCTION [dbo].[GetWorkflowConditionLabels]
(
	@conditions xml
)
RETURNS xml
AS
BEGIN
	declare @recordCount int;

	declare @results table (id int, FieldTypeID int, ValueType varchar(max), [Value] nvarchar(max), Operator varchar(max), VersionStepID int, FormInputID varchar(max), ContextualFieldID varchar(max), ValueLabel varchar(max));

	select 
		 @recordCount = count(*)
	from 
		@conditions.nodes('/Conditions/Condition') c(x);

		insert into @results (id, FieldTypeID, VersionStepID, FormInputID, ValueType, [Value], Operator, ContextualFieldID, ValueLabel)
			select
			row_number() over (order by x.value('@FieldTypeID', 'int'), x.value('@VersionStepID', 'int'), x.value('@FormInputID', 'varchar(max)')) as id,
			 x.value('@FieldTypeID', 'int') as FieldTypeID
			,x.value('@VersionStepID', 'int') as VersionStepID  
			,x.value('@FormInputID', 'varchar(max)') as FormInputID
			,x.value('@ValueType', 'varchar(max)') as ValueType  
			,x.value('@Value', 'varchar(max)') as [Value]  
			,x.value('@Operator', 'varchar(max)') as [Operator] 
			,x.value('@ContextualFieldID', 'varchar(max)') as ContextualFieldID
			,null as ValueLabel
		from 
			@conditions.nodes('/Conditions/Condition') c(x)
		left join FieldType FT on FT.ID = x.value('@FieldTypeID', 'int')
		left join workflow.VersionStep VS on VS.ID = x.value('@VersionStepID', 'int')

		
	while(@recordCount > 0)
	begin
		if (select top 1 ValueType from @results where id = @recordCount) in ('U', 'L')
		begin
		
			if ((select FieldTypeID from @results where id = @recordCount) is not null)
			begin
				declare @valueLabel varchar(max);

				select @valueLabel = coalesce(RI.DisplayValue, R.[Value])
				from 
					FieldType FT
				inner join @results R on R.id = @recordCount and FT.ID = R.FieldTypeID
				left join LookupType LT on FT.LookupObjectType = 'Lookup' and LT.ID = FT.LookupObjectID
				left join [Lookup] L on L.ID = (select top 1 [Value] from @results where id = @recordCount)
				left join ReferenceItem RI on FT.LookupObjectType = 'ReferenceItem' and FT.LookupObjectID = RI.ReferenceItemTypeID and RI.ID = R.[Value]

				update r
				set r.ValueLabel = @valueLabel
				from @results r
				where r.id = @recordCount;

			end
			
			if ((select FormInputID from @results where id = @recordCount) is not null)
			begin
				declare @fields xml, @valueLabel2 varchar(max);

				select @fields = VS.fields from 
				workflow.VersionStep VS
				inner join @results R on R.id = @recordCount and VS.ID = R.VersionStepID;


				select 
					@valueLabel2 = coalesce(RI.DisplayValue, R.[Value])
				from @fields.nodes('fields/form/field') f(x)
				inner join @results R on R.id = @recordCount
				inner join FieldType FT on FT.ID = x.value('@referenceFieldId', 'int')
				left join LookupType LT on FT.LookupObjectType = 'Lookup' and LT.ID = FT.LookupObjectID
				left join [Lookup] L on L.ID = (select top 1 [Value] from @results where id = @recordCount)
				left join ReferenceItem RI on FT.LookupObjectType = 'ReferenceItem' and FT.LookupObjectID = RI.ReferenceItemTypeID and RI.ID = R.[Value]
				where x.value('@id', 'varchar(max)') = R.FormInputID;


				update r
				set r.ValueLabel = @valueLabel2
				from @results r
				where r.id = @recordCount;


			end
		end	
		else
		begin
			update r
			set r.ValueLabel = r.[Value]
			from @results r
			where r.id = @recordCount;
		end


		set @recordCount = @recordCount - 1;
	end

	RETURN 
		coalesce(
		 (select 
			r.FieldTypeID as 'Condition/@FieldTypeID',
			r.VersionStepID as 'Condition/@VersionStepID',
			r.FormInputID as 'Condition/@FormInputID',
			r.ValueType as 'Condition/@ValueType',
			r.[Value] as 'Condition/@Value',
			r.Operator as 'Condition/@Operator',
			r.ContextualFieldID as 'Condition/@ContextualFieldID',
			r.ValueLabel as 'Condition/@ValueLabel' 
		from @results r
		for xml path(''), root('Conditions'))
		,
		'<Conditions />');
END
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
											/*SELECT	ID as AID,
													CAST(ID as nvarchar(max)) as ID,
													CAST(DisplayValue as nvarchar(max)) as TextPath
											FROM	Artifact A
											WHERE	A.ID = CAST(@Value as int)
													and L.ObjectType = 'Artifact'*/
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
											/*SELECT	ID,
													CAST(TextPath as nvarchar(max)) as TextPath
											FROM	Taxonomy A
											WHERE	A.ID = CAST(@Value as int)
													and L.ObjectType = 'Taxonomy'*/
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

ALTER FUNCTION [utility].[GetFormattedFieldLookupValueWithMultiple]
(
	@Type varchar(25),
	@DisplayFormat nvarchar(250),
	@LookupObjectType varchar(25),
	@LookupObjectID int,
	@Value nvarchar(max),
	@SupportsMultipleValues bit	
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
		if @SupportsMultipleValues = 1
		begin	
			set @formattedValue =  utility.GenerateFormattedMultipleValue (@DisplayFormat, @LookupObjectType, @LookupObjectID, @Value)
		end
		else if @LookupObjectType = 'ReferenceItemType'
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
											SELECT	ID as AID,
													CAST(ID as nvarchar(max)) as ID,
													CAST(DisplayValue as nvarchar(max)) as TextPath
											FROM	Artifact A
											WHERE	A.ID = CAST(@Value as int)
													and L.ObjectType = 'Artifact'
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
											SELECT	ID,
													CAST(TextPath as nvarchar(max)) as TextPath
											FROM	Taxonomy A
											WHERE	A.ID = CAST(@Value as int)
													and L.ObjectType = 'Taxonomy'
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
						select @currentValue = utility.GetFormattedFieldLookupValueWithMultiple(@Type, @lkpFormat, @lkpType, @lkpID, @currentValue, @SupportsMultipleValues)
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

alter procedure [fusion].[GenerateFoundationLineage]
as
begin
	--select	ST.FormattedValue as SourceFusionAttributeTypeID,
	--		S.TextPath as SourceName,
	--		TT.FormattedValue as TargetFusionAttributeTypeID,
	--		T.TextPath as TargetName,
	--		IT.ID
	--from	FusionAttribute MA
	--		inner join FieldWithRelation ST on ST.ObjectType = 'FusionAttribute' and ST.ObjectID = MA.ID and ST.Name = 'SourceTypeID'
	--		inner join FusionAttributeType S on S.ID = ST.FormattedValue
	--		inner join FieldWithRelation TT on TT.ObjectType = 'FusionAttribute' and TT.ObjectID = MA.ID and TT.Name = 'TargetTypeID'
	--		inner join FusionAttributeType T on T.ID = TT.FormattedValue
	--		left join IntersectType IT on IT.Subject = 'FusionAttributeType' and IT.SubjectID = ST.FormattedValue and IT.Object = 'FusionAttributeType' and IT.ObjectID = TT.FormattedValue
	--where	MA.FusionID = @fusionID and MA.FusionAttributeTypeID = 50032
	--group by ST.FormattedValue, S.TextPath, TT.FormattedValue, T.TextPath, IT.ID

	DROP TABLE IF EXISTS #Maps

	--	select	ST.FormattedValue as SourceFusionAttributeTypeID,
	--			TT.FormattedValue as TargetFusionAttributeTypeID
	--from		FusionAttribute MA
	--			inner join FieldWithRelation ST on ST.ObjectType = 'FusionAttribute' and ST.ObjectID = MA.ID and ST.Name = 'SourceTypeID'
	--			inner join FieldWithRelation S on S.ObjectType = 'FusionAttribute' and S.ObjectID = MA.ID and S.Name = 'SourceObjectID'
	--			inner join FieldWithRelation TT on TT.ObjectType = 'FusionAttribute' and TT.ObjectID = MA.ID and TT.Name = 'TargetTypeID'
	--			inner join FieldWithRelation T on T.ObjectType = 'FusionAttribute' and T.ObjectID = MA.ID and T.Name = 'TargetObjectID'
	--			inner join FusionAttribute SA on SA.FusionID = MA.FusionID and SA.SourceID = S.FormattedValue --and SA.FusionAttributeTypeID = ST.FormattedValue
	--			inner join FusionAttribute TA on TA.FusionID = MA.FusionID and TA.SourceID = T.FormattedValue --and TA.FusionAttributeTypeID = TT.FormattedValue
	--			left join IntersectTypeDetail IT on IT.Subject = 'FusionAttributeType' and IT.SubjectID = SA.FusionAttributeTypeID and IT.Object = 'FusionAttributeType' and IT.ObjectID = TA.FusionAttributeTypeID and IT.PredicateType = 1
	--where		MA.FusionAttributeTypeID = 1476 --and SA.ID <> TA.ID (slows down query quite a bit)
	--group by	ST.FormattedValue, TT.FormattedValue

	--select * from Predicate
	--update IntersectType set PredicateID = 10 where Subject = 'FusionAttributeType' and Object = 'FusionAttributeType' and PredicateID is null and SubjectID > 1400
	--insert into IntersectType (Subject, SubjectID, Object, ObjectID, IsSystem, State,PredicateID, SubjectCardinality,ObjectCardinality) values ('FusionAttributeType', 1492, 'FusionAttributeType', 1490, 1, 1, 10, 2,2)

	select		IT.ID as IntersectTypeID,
				'FusionAttribute' as Subject,
				--ST.FormattedValue as SourceFusionAttributeTypeID,
				--S.FormattedValue as SourceObjectID,
				SA.ID as SubjectID,
				--TT.FormattedValue as TargetFusionAttributeTypeID,
				--T.FormattedValue as TargetObjectID,
				'FusionAttribute' as Object,
				TA.ID as ObjectID
	into		#Maps
	from		FusionAttribute MA
				inner join FieldWithRelation ST on ST.ObjectType = 'FusionAttribute' and ST.ObjectID = MA.ID and ST.Name = 'SourceTypeID'
				inner join FieldWithRelation S on S.ObjectType = 'FusionAttribute' and S.ObjectID = MA.ID and S.Name = 'SourceObjectID'
				inner join FieldWithRelation TT on TT.ObjectType = 'FusionAttribute' and TT.ObjectID = MA.ID and TT.Name = 'TargetTypeID'
				inner join FieldWithRelation T on T.ObjectType = 'FusionAttribute' and T.ObjectID = MA.ID and T.Name = 'TargetObjectID'
				inner join FusionAttribute SA on SA.FusionID = MA.FusionID and SA.SourceID = S.FormattedValue --and SA.FusionAttributeTypeID = ST.FormattedValue
				inner join FusionAttribute TA on TA.FusionID = MA.FusionID and TA.SourceID = T.FormattedValue --and TA.FusionAttributeTypeID = TT.FormattedValue
				left join IntersectTypeDetail IT on IT.Subject = 'FusionAttributeType' and IT.SubjectID = SA.FusionAttributeTypeID and IT.Object = 'FusionAttributeType' and IT.ObjectID = TA.FusionAttributeTypeID and IT.PredicateType = 1
	where		MA.FusionAttributeTypeID = 1476 --and SA.ID <> TA.ID (slows down query quite a bit)
	group by	SA.ID, TA.ID, IT.ID

--	select * from FusionAttribute where ID in (64479)
--select top 1000 * from [Intersect] where [Owner] = 'FOUNDATION'

	DROP TABLE IF EXISTS #Types

	select	distinct
			IntersectTypeID 
	into	#Types
	from	#Maps 
	where	IntersectTypeID is not null

	delete	T
	from	[Intersect] T
			inner join #Types I on	I.IntersectTypeID = T.IntersectTypeID
			left join #Maps M on	M.IntersectTypeID = T.IntersectTypeID 
									and M.Subject = T.Subject and M.SubjectID = T.SubjectID 
									and M.Object = T.Object and M.ObjectID = T.ObjectID
	where	M.IntersectTypeID is null

	insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, [Owner])
		select	M.IntersectTypeID, 
				M.Subject, M.SubjectID, 
				M.Object, M.ObjectID, 
				'FOUNDATION'
		from	#Maps M 
				left join [Intersect] T on	M.IntersectTypeID = T.IntersectTypeID 
											and M.Subject = T.Subject and M.SubjectID = T.SubjectID 
											and M.Object = T.Object and M.ObjectID = T.ObjectID
		where	M.IntersectTypeID is not null 
				and T.ID is null;

	--merge	[Intersect] T
	--using	(
	--		select	distinct
	--				* 
	--		from	#Maps 
	--		where	IntersectTypeID is not null
	--		) S
	--		on (T.IntersectTypeID = S.IntersectTypeID and T.Subject = S.Subject and T.Object = S.Object and T.SubjectID = S.SubjectID and T.ObjectID = S.ObjectID)
	----when	not matched by source and T.IntersectTypeID = S.IntersectTypeID
	----then	delete
	--when	not matched by target
	--then	INSERT (IntersectTypeID, Subject, SubjectID, Object, ObjectID, [Owner])
	--		VALUES (S.IntersectTypeID, S.Subject, S.SubjectID, S.Object, S.ObjectID, 'FOUNDATION');
end
GO

alter table [integration].[SynchedAssetType] add LastSuccessfulCount int null
GO

ALTER view [dbo].[SiteNavAvailable] as
	select
		u.ID as ObjectID,
		u.Name,
		u.url as Route,
		u.Object,
		null as SortOrder,
		u.ParentID as ParentID
	from
	(
		select
		ID,
		ParentID,
		Name,
		dbo.GenerateNgObjectUrl('ArtifactType', ID, 0) As url,
		'ArtifactType' as [Object]
		FROM ArtifactType
		
		UNION ALL
		
		SELECT
		ID,
		null as ParentID,
		Name,
		dbo.GenerateNgObjectUrl('TaxonomyType', ID, 0)  As url,
		'TaxonomyType' as [Object]
		FROM TaxonomyType
		
		UNION ALL

		SELECT
		ID,
		null as ParentID,
		Name,
		dbo.GenerateNgObjectUrl('PolicyType', ID, 0)  As url,
		'PolicyType' as [Object]
		FROM PolicyType
		
		UNION ALL

		SELECT
		ID,
		null as ParentID,
		Name,
		dbo.GenerateNgObjectUrl('RuleType', ID, 0)  As url,
		'RuleType' as [Object]
		FROM RuleType
	) u
	left join SiteNav v on v.Object = u.Object and v.ObjectID = u.ID
	where v.ObjectID is null 
GO