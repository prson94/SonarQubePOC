
CREATE procedure [utility].[AddAuditEntry]
--declare
	@Object varchar(50),
	@ObjectID int,
	@ResourceID int,
	@Date datetime,
	@Action varchar(15),
	@ActionObject varchar(50),
	@ActionObjectID int
--set @Object = 'Taxonomy'--'Artifact'
--set @ObjectID = 229--733
--set @ResourceID = 1
--set @Action = 'Updated'
--set @ActionObject = 'Taxonomy' --'Artifact'
--set @ActionObjectID = 229 --733
as
begin
	set nocount on;
	declare @objectName nvarchar(250),
			@actionObjectTypeName nvarchar(250),
			@actionObjectName nvarchar(250),
			@actionDescription nvarchar(max)
	
	declare @tbl table (ID int identity, FieldTypeID int, FieldName nvarchar(250), NewValue nvarchar(max), MostRecentVersion int, Updated bit)

	-- Object Resolution --------------------------------------------------
	if @Object = 'Artifact'				begin		select @objectName = Name from Artifact where ID = @ObjectID				end
	if @Object = 'ArtifactType'			begin		select @objectName = Name from ArtifactType where ID = @ObjectID			end
	if @Object = 'AttributeType'		begin		select @objectName = Name from AttributeType where ID = @ObjectID			end
	if @Object = 'Domain'				begin		select @objectName = Name from Domain where ID = @ObjectID					end
	if @Object = 'DomainGroup'			begin		select @objectName = Name from DomainGroup where ID = @ObjectID				end
	if @Object = 'DomainType'			begin		select @objectName = Name from DomainType where ID = @ObjectID				end
	if @Object = 'Fusion'				begin		select @objectName = Name from Fusion where ID = @ObjectID					end
	if @Object = 'FusionAttribute'		begin		select @objectName = TextPath from FusionAttribute where ID = @ObjectID		end
	if @Object = 'FusionAttributeType'	begin		select @objectName = Name from FusionAttributeType where ID = @ObjectID		end
	if @Object = 'FusionType'			begin		select @objectName = Name from FusionType where ID = @ObjectID				end
	if @Object = 'Group'				begin		select @objectName = Name from [Group] where ID = @ObjectID					end
	if @Object = 'Intersect'			begin		select @objectName = Name from [Intersect] where ID = @ObjectID				end
	if @Object = 'IntersectType'		begin		select @objectName = Name from IntersectType where ID = @ObjectID			end
	if @Object = 'LoadType'				begin		select @objectName = Name from LoadType where ID = @ObjectID				end
	if @Object = 'LookupType'			begin		select @objectName = Name from LookupType where ID = @ObjectID				end
	if @Object = 'Policy'				begin		select @objectName = Name from Policy where ID = @ObjectID					end
	if @Object = 'Report'				begin		select @objectName = Name from Report where ID = @ObjectID					end
	if @Object = 'ResponsibilityType'	begin		select @objectName = Name from ResponsibilityType where ID = @ObjectID		end
	if @Object = 'Rule'					begin		select @objectName = Name from [Rule] where ID = @ObjectID					end
	if @Object = 'StatisticType'		begin		select @objectName = Name from StatisticType where ID = @ObjectID			end
	if @Object = 'SurveyType'			begin		select @objectName = Name from SurveyType where ID = @ObjectID				end
	if @Object = 'Taxonomy'				begin		select @objectName = Name from Taxonomy where ID = @ObjectID				end
	if @Object = 'TaxonomyType'			begin		select @objectName = Name from TaxonomyType where ID = @ObjectID			end
	----------------------------------------------------------------------

	-- Action Object Resolution ------------------------------------------

	-- Relevant ONLY to: Artifact, ArtifactType
	if @ActionObject = 'Artifact'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.TextPath
		from	Artifact O
				inner join ArtifactType T on T.ID = O.ArtifactTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from Artifact where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentID', ParentID, 0, 0 from Artifact where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from Artifact where ID = @ActionObjectID
		insert into @tbl  select 0, 'TaxonomyTypeID', TaxonomyTypeID, 0, 0 from Artifact where ID = @ActionObjectID
		insert into @tbl  select 0, 'Status', Status, 0, 0 from Artifact where ID = @ActionObjectID
		insert into @tbl  select 0, 'DateLastCertified', DateLastCertified, 0, 0 from Artifact where ID = @ActionObjectID
	end

	-- Relevant ONLY to: ArtifactType
	if @ActionObject = 'ArtifactType'
	begin
		select	@actionObjectTypeName = 'Artifact Type',
				@actionObjectName = O.Name 
		from	ArtifactType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from ArtifactType where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentID', ParentID, 0, 0 from ArtifactType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from ArtifactType where ID = @ActionObjectID
		insert into @tbl  select 0, 'CanOwnFusion', CanOwnFusion, 0, 0 from ArtifactType where ID = @ActionObjectID
		--insert into @tbl  select 0, 'SourcingApplies', SourcingApplies, 0, 0 from ArtifactType where ID = @ActionObjectID
		insert into @tbl  select 0, 'AllowRelatedArtifacts', AllowRelatedArtifacts, 0, 0 from ArtifactType where ID = @ActionObjectID
	end
	
	-- Relevant ONLY to: Artifact, Domain, Fusion, FusionAttribute, Intersect, Taxonomy
	if @ActionObject = 'Attribute'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = T.Name + ' Attribute ' + cast(O.ID as nvarchar(15)) 
		from	Attribute O
				inner join AttributeType T on T.ID = O.AttributeTypeID
		where	O.ID = @ActionObjectID
	end

	-- Relevant ONLY to: AttributeType
	if @ActionObject = 'AttributeType'
	begin
		select	@actionObjectTypeName = 'Attribute Type',
				@actionObjectName = O.Name
		from	AttributeType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from AttributeType where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentID', ParentID, 0, 0 from AttributeType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from AttributeType where ID = @ActionObjectID
		insert into @tbl  select 0, 'TextFormatString', TextFormatString, 0, 0 from AttributeType where ID = @ActionObjectID
		insert into @tbl  select 0, 'AttributeTypeCategoryID', AttributeTypeCategoryID, 0, 0 from AttributeType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Domain
	if @ActionObject = 'DomainItem'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	DomainItem O
				inner join Domain T on T.ID = O.DomainID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from DomainItem where ID = @ActionObjectID
		insert into @tbl  select 0, 'Code', Code, 0, 0 from DomainItem where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from DomainItem where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Domain, DomainGroup, DomainType
	if @ActionObject = 'Domain'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	Domain O
				inner join DomainType T on T.ID = O.DomainTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from Domain where ID = @ActionObjectID
		--insert into @tbl  select 0, 'ParentID', ParentID, 0, 0 from Domain where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from Domain where ID = @ActionObjectID
		insert into @tbl  select 0, 'DomainGroupID', DomainGroupID, 0, 0 from Domain where ID = @ActionObjectID
	end

	-- Relevant ONLY to: DomainGroup, DomainType
	if @ActionObject = 'DomainGroup'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	DomainGroup O
				inner join DomainType T on T.ID = O.DomainTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from DomainGroup where ID = @ActionObjectID
		insert into @tbl  select 0, 'MasterListID', MasterListID, 0, 0 from DomainGroup where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from DomainGroup where ID = @ActionObjectID
	end

	-- Relevant ONLY to: DomainType
	if @ActionObject = 'DomainType'
	begin
		select	@actionObjectTypeName = 'Domain Type',
				@actionObjectName = O.Name
		from	DomainType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from DomainType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from DomainType where ID = @ActionObjectID
	end
	
	-- Relevant ONLY to: Rule
	if @ActionObject = 'EventGroup'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	EventGroup O
				inner join [Rule] T on T.ID = O.RuleID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from EventGroup where ID = @ActionObjectID
		insert into @tbl  select 0, 'PublicID', PublicID, 0, 0 from EventGroup where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Fusion
	if @ActionObject = 'Fusion'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	Fusion O
				inner join FusionType T on T.ID = O.FusionTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from Fusion where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from Fusion where ID = @ActionObjectID
		insert into @tbl  select 0, 'Enabled', Enabled, 0, 0 from Fusion where ID = @ActionObjectID
		insert into @tbl  select 0, 'Manual', Manual, 0, 0 from Fusion where ID = @ActionObjectID
		insert into @tbl  select 0, 'LockPromotedItems', LockPromotedItems, 0, 0 from Fusion where ID = @ActionObjectID
		insert into @tbl  select 0, 'IntervalType', IntervalType, 0, 0 from Fusion where ID = @ActionObjectID
		insert into @tbl  select 0, 'Interval', Interval, 0, 0 from Fusion where ID = @ActionObjectID
		insert into @tbl  select 0, 'ForceRefresh', ForceRefresh, 0, 0 from Fusion where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Fusion
	if @ActionObject = 'FusionAttributeOwnerRule'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = T.Name + ' Ownership Rule ' + cast(O.ID as nvarchar(15))
		from	FusionAttributeOwnerRule O
				inner join Fusion T on T.ID = O.FusionID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'ObjectType', ObjectType, 0, 0 from FusionAttributeOwnerRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectID', ObjectID, 0, 0 from FusionAttributeOwnerRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentObjectType', ParentObjectType, 0, 0 from FusionAttributeOwnerRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentObjectID', ParentObjectID, 0, 0 from FusionAttributeOwnerRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'RelationshipOwnerObjectType', RelationshipOwnerObjectType, 0, 0 from FusionAttributeOwnerRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'RelationshipOwnerObjectID', RelationshipOwnerObjectID, 0, 0 from FusionAttributeOwnerRule where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Fusion
	if @ActionObject = 'FusionAttributePromotionRule'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = T.Name + ' Promotion Rule ' + cast(O.ID as nvarchar(15))
		from	FusionAttributePromotionRule O
				inner join Fusion T on T.ID = O.FusionID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'ObjectType', ObjectType, 0, 0 from FusionAttributePromotionRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectID', ObjectID, 0, 0 from FusionAttributePromotionRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentObjectType', ParentObjectType, 0, 0 from FusionAttributePromotionRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentObjectID', ParentObjectID, 0, 0 from FusionAttributePromotionRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'PromotionObjectType', PromotionObjectType, 0, 0 from FusionAttributePromotionRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'PromotionObjectID', PromotionObjectID, 0, 0 from FusionAttributePromotionRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'PromotionParentObjectType', PromotionParentObjectType, 0, 0 from FusionAttributePromotionRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'PromotionParentObjectID', PromotionParentObjectID, 0, 0 from FusionAttributePromotionRule where ID = @ActionObjectID
	end

	-- Relevant ONLY to: FusionAttributeType, FusionType
	if @ActionObject = 'FusionAttributeType'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	FusionAttributeType O
				inner join FusionType T on T.ID = O.FusionTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from FusionAttributeType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Assignable', Assignable, 0, 0 from FusionAttributeType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: FusionType
	if @ActionObject = 'FusionType'
	begin
		select	@actionObjectTypeName = 'Fusion Type',
				@actionObjectName = O.Name 
		from	FusionType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from FusionType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from FusionType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Group
	if @ActionObject = 'Group'
	begin
		select	@actionObjectTypeName = 'Group',
				@actionObjectName = O.Name 
		from	[Group] O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from [Group] where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from [Group] where ID = @ActionObjectID
		insert into @tbl  select 0, 'PrimaryOwnerResourceID', PrimaryOwnerResourceID, 0, 0 from [Group] where ID = @ActionObjectID
		insert into @tbl  select 0, 'SecondaryOwnerResourceID', SecondaryOwnerResourceID, 0, 0 from [Group] where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Artifact, Domain, FusionAttribute, Intersect, Taxonomy, Policy, Rule
	if @ActionObject = 'Intersect'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	[Intersect] O
				inner join [IntersectType] T on T.ID = O.IntersectTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Classification', Classification, 0, 0 from [Intersect] where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from [Intersect] where ID = @ActionObjectID
	end

	-- Relevant ONLY to: IntersectType
	if @ActionObject = 'IntersectType'
	begin
		select	@actionObjectTypeName = 'Intersect Type',
				@actionObjectName = O.Name 
		from	IntersectType O
		where	O.ID = @ActionObjectID

		--insert into @tbl  select 0, 'ReadOnly', [ReadOnly], 0, 0 from IntersectType where ID = @ActionObjectID
		--insert into @tbl  select 0, 'IsTechnical', IsTechnical, 0, 0 from IntersectType where ID = @ActionObjectID
		--insert into @tbl  select 0, 'AllowContext', AllowContext, 0, 0 from IntersectType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: LoadType
	if @ActionObject = 'LoadType'
	begin
		select	@actionObjectTypeName = 'Load Type',
				@actionObjectName = O.Name 
		from	LoadType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from LoadType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: LoadType
	if @ActionObject = 'LoadTypeField'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	LoadTypeField O
				inner join LoadType T on T.ID = O.LoadTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from LoadTypeField where ID = @ActionObjectID
		insert into @tbl  select 0, 'SortOrder', SortOrder, 0, 0 from LoadTypeField where ID = @ActionObjectID
		insert into @tbl  select 0, 'LookupObjectType', LookupObjectType, 0, 0 from LoadTypeField where ID = @ActionObjectID
		insert into @tbl  select 0, 'LookupObjectID', LookupObjectID, 0, 0 from LoadTypeField where ID = @ActionObjectID
		insert into @tbl  select 0, 'LookupFieldName', LookupFieldName, 0, 0 from LoadTypeField where ID = @ActionObjectID
	end

	-- Relevant ONLY to: LoadType
	if @ActionObject = 'LoadTypeRule'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = T.Name + ' Rule ' + cast(O.ID as nvarchar(15))
		from	LoadTypeRule O
				inner join LoadType T on T.ID = O.LoadTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'LoadTypeRuleGroup', case LoadTypeRuleGroup when 1 then 'Promotion' when 2 then 'Relation' else 'Unknown' end, 0, 0 from LoadTypeRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'SortOrder', SortOrder, 0, 0 from LoadTypeRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectType', ObjectType, 0, 0 from LoadTypeRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectID', ObjectID, 0, 0 from LoadTypeRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'UniqueLoadTypeFieldID', UniqueLoadTypeFieldID, 0, 0 from LoadTypeRule where ID = @ActionObjectID
	end

	-- Relevant ONLY to: LoadType
	if @ActionObject = 'LoadTypeRuleItem'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = 'Rule Field ' + cast(O.ID as nvarchar(15))
		from	LoadTypeRuleItem O
				inner join LoadTypeRule R on R.ID = O.LoadTypeRuleID
				inner join LoadType T on T.ID = R.LoadTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'SourceLoadTypeFieldID', SourceLoadTypeFieldID, 0, 0 from LoadTypeRuleItem where ID = @ActionObjectID
		insert into @tbl  select 0, 'TargetFieldName', TargetFieldName, 0, 0 from LoadTypeRuleItem where ID = @ActionObjectID
		insert into @tbl  select 0, 'IsCustomField', IsCustomField, 0, 0 from LoadTypeRuleItem where ID = @ActionObjectID
	end

	-- Relevant ONLY to: LookupType
	if @ActionObject = 'Lookup'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = T.Name + ' Lookup ' + cast(O.ID as nvarchar(15))
		from	[Lookup] O
				inner join LookupType T on T.ID = O.LookupTypeID
		where	O.ID = @ActionObjectID
	end

	-- Relevant ONLY to: LookupType
	if @ActionObject = 'LookupType'
	begin
		select	@actionObjectTypeName = 'Lookup Type',
				@actionObjectName = O.Name 
		from	LookupType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from LookupType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Policy
	if @ActionObject = 'Policy'
	begin
		select	@actionObjectTypeName = 'Policy',
				@actionObjectName = O.Name 
		from	[Policy] O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from [Policy] where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentID', ParentID, 0, 0 from [Policy] where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from [Policy] where ID = @ActionObjectID
	end

	-- Relevant ONLY to: SurveyType
	if @ActionObject = 'QuestionType'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	QuestionType O
				inner join SurveyType T on T.ID = O.SurveyTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from QuestionType where ID = @ActionObjectID
		insert into @tbl  select 0, 'ResponseTypeID', ResponseTypeID, 0, 0 from QuestionType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from QuestionType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Report
	if @ActionObject = 'Report'
	begin
		select	@actionObjectTypeName = 'Report',
				@actionObjectName = O.Name
		from	Report O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from Report where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from Report where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectType', ObjectType, 0, 0 from Report where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectID', ObjectID, 0, 0 from Report where ID = @ActionObjectID
		insert into @tbl  select 0, 'ReportLayoutID', ReportLayoutID, 0, 0 from Report where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Report
	if @ActionObject = 'ReportTile'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	ReportTile O
				inner join Report T on T.ID = O.ReportID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from ReportTile where ID = @ActionObjectID
		insert into @tbl  select 0, 'ReportTileType', ReportTileType, 0, 0 from ReportTile where ID = @ActionObjectID
		insert into @tbl  select 0, 'ContentAreaNumber', ContentAreaNumber, 0, 0 from ReportTile where ID = @ActionObjectID
		insert into @tbl  select 0, 'CommandText', CommandText, 0, 0 from ReportTile where ID = @ActionObjectID
		insert into @tbl  select 0, 'Settings', cast(Settings as nvarchar(max)), 0, 0 from ReportTile where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Artifact, ArtifactType, DomainType, Intersect, Policy, Rule, Taxonomy, TaxonomyType, Vocabulary
	if @ActionObject = 'Responsibility'
	begin
		select	@actionObjectTypeName = 'Responsibility',
				@actionObjectName = T.Name 
		from	Responsibility O
				inner join ResponsibilityType T on T.ID = O.ResponsibilityTypeID

		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Context', (
				select	D.Name + ': ' + I.Code + ' - ' + I.Name + '; '
				from	ResponsibilityContextItem C
						inner join DomainItem I on C.ObjectType = 'DomainItem' and C.ObjectID = I.ID
						inner join Domain D on D.ID = I.DomainID
				where	ResponsibilityID = @ActionObjectID
				for xml path ('')--, root('items')
				), 0, 0 from Responsibility where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectType', ObjectType, 0, 0 from Responsibility where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectID', ObjectID, 0, 0 from Responsibility where ID = @ActionObjectID
		insert into @tbl  select 0, 'ResponsibleObjectType', ResponsibleObjectType, 0, 0 from Responsibility where ID = @ActionObjectID
		insert into @tbl  select 0, 'ResponsibleObjectID', ResponsibleObjectID, 0, 0 from Responsibility where ID = @ActionObjectID
	end

	-- Relevant ONLY to: ResponsibilityType
	if @ActionObject = 'ResponsibilityType'
	begin
		select	@actionObjectTypeName = 'Responsibility Type',
				@actionObjectName = O.Name 
		from	ResponsibilityType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from ResponsibilityType where ID = @ActionObjectID
		insert into @tbl  select 0, 'ResponsibilityTypeGroup', ResponsibilityTypeGroup, 0, 0 from ResponsibilityType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from ResponsibilityType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Rule
	if @ActionObject = 'Rule'
	begin
		select	@actionObjectTypeName = 'Rule',
				@actionObjectName = O.Name 
		from	[Rule] O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from [Rule] where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from [Rule] where ID = @ActionObjectID
		insert into @tbl  select 0, 'RuleType', RuleType, 0, 0 from [Rule] where ID = @ActionObjectID
	end

	-- Relevant ONLY to: StatisticType
	if @ActionObject = 'StatisticType'
	begin
		select	@actionObjectTypeName = 'Statistic Type',
				@actionObjectName = O.Name 
		from	StatisticType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from StatisticType where ID = @ActionObjectID
		insert into @tbl  select 0, 'CheckType', CheckType, 0, 0 from StatisticType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from StatisticType where ID = @ActionObjectID
		insert into @tbl  select 0, 'PartOfScore', PartOfScore, 0, 0 from StatisticType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Configuration', cast(Configuration as nvarchar(max)), 0, 0 from StatisticType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: SurveyType
	if @ActionObject = 'SurveyType'
	begin
		select	@actionObjectTypeName = 'Survey Type',
				@actionObjectName = O.Name 
		from	SurveyType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from SurveyType where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectType', ObjectType, 0, 0 from SurveyType where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectID', ObjectID, 0, 0 from SurveyType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Taxonomy, TaxonomyType
	if @ActionObject = 'Taxonomy'
	begin
		select	@actionObjectTypeName = T.Name + ' model',
				@actionObjectName = O.TextPath
		from	Taxonomy O
				inner join TaxonomyType T on T.ID = O.TaxonomyTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from Taxonomy where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentID', ParentID, 0, 0 from Taxonomy where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from Taxonomy where ID = @ActionObjectID
		insert into @tbl  select 0, 'Level', [Level], 0, 0 from Taxonomy where ID = @ActionObjectID
	end

	-- Relevant ONLY to: TaxonomyType
	if @ActionObject = 'TaxonomyType'
	begin
		select	@actionObjectTypeName = 'Model Type',
				@actionObjectName = O.Name
		from	TaxonomyType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from TaxonomyType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from TaxonomyType where ID = @ActionObjectID
		insert into @tbl  select 0, 'MaximumDepth', MaximumDepth, 0, 0 from TaxonomyType where ID = @ActionObjectID
		--insert into @tbl  select 0, 'Class', Class, 0, 0 from TaxonomyType where ID = @ActionObjectID
	end

	-- Get the dynamic fields for the actional object, if available for this type.
	if @ActionObject in ('Artifact', 'Attribute', 'Event', 'Fusion', 'FusionAttribute', 'Lookup', 'Resource', 'Taxonomy') 
	begin
		insert into @tbl  
			select	FieldTypeID, 
					FriendlyName, 
					FormattedValue, 
					0, 
					0 
			from	FieldWithRelation
			where	ObjectType = @ActionObject 
					and ObjectID = @ActionObjectID
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
								inner join reporting.Global_FieldAudit F on F.AuditID = A.ID and A.[Object] = @Object and A.ObjectID = @ObjectID and A.ActionObject = @ActionObject and A.ActionObjectID = @ActionObjectID
								inner join (
											select		F.FieldTypeID,
														F.FieldName,
														max([Version]) as [Version]
											from		reporting.Global_Audit A
														inner join reporting.Global_FieldAudit F on F.AuditID = A.ID and A.[Object] = @Object and A.ObjectID = @ObjectID and A.ActionObject = @ActionObject and A.ActionObjectID = @ActionObjectID
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
			set @actionDescription = @actionObjectTypeName + ' created.'
		end
	else
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
					set @actionDescription = coalesce(@actionDescription + ', ', '') + @fieldName + case when @version > 0 then ' updated' else ' added' end
				end

				set @current = @current + 1
			end
		end
	
	--select @Object, @ObjectID, @ObjectName, @ResourceID, @Date, @Action, @ActionObject, @ActionObjectID, @actionObjectTypeName, @actionObjectName, @actionDescription

	if @actionDescription is not null and @objectName is not null
	begin
		set @actionDescription = @actionDescription + '.'

		insert into [reporting].[Global_Audit] values (@Object, @ObjectID, @objectName, @ResourceID, @Date, @Action, @ActionObject, @ActionObjectID, @actionObjectTypeName, @actionObjectName, @actionDescription)
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
