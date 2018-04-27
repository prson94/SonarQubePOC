--do something with [cache].[ResponsibilityItem]

-- ARTIFACT FIELD GENERATION

INSERT INTO [dbo].[FieldType]
           ([Name]
           ,[FriendlyName]
           ,[Description]
           ,[Type]
           ,[MinimumLength]
           ,[MaximumLength]
           ,[Object]
           ,[ObjectID]
           ,[SortOrder]
           ,[IsRequired]
           ,[IsListable]
           ,[IsDisplayable]
           ,[IsEditable]
           ,[AllowAllValue]
           ,[IsPrimaryFilter]
           ,[IsPartOfKey])
select	'Name', 'Name', 'The artifact''s name', 'Text', 3, 1000, 
		'ArtifactType', ID as ObjectID, 
		1 as SortOrder, 1 as IsRequired, 1 as IsListable, 1 as IsDisplayable, 1 as IsEditable, 0 as AllowAllValue, 0 as IsPrimaryFilter, 1 as IsPartOfKey
from	ArtifactType
GO

insert into Field (ObjectType, ObjectID, FieldTypeID, Value, FormattedValue)
	select	'Artifact', A.ID, FT.ID, A.Name, A.Name 
	from	Artifact A 
			inner join FieldType FT on FT.Object = 'ArtifactType' and FT.ObjectID = A.ArtifactTypeID and FT.Name = 'Name'
			left join Field F on F.FieldTypeID = FT.ID and F.ObjectType = 'Artifact' and F.ObjectID = A.ID
	where	F.FieldTypeID is null
GO

INSERT INTO [dbo].[FieldType]
           ([Name]
           ,[FriendlyName]
           ,[Description]
           ,[Type]
           ,[Object]
           ,[ObjectID]
           ,[SortOrder]
           ,[IsRequired]
           ,[IsListable]
           ,[IsDisplayable]
           ,[IsEditable]
           ,[AllowAllValue]
           ,[IsPrimaryFilter]
           ,[IsPartOfKey])
select	'Description', 'Description', 'The artifact''s description', 'Html', 
		'ArtifactType', ID as ObjectID, 
		2 as SortOrder, 0 as IsRequired, 0 as IsListable, 1 as IsDisplayable, 1 as IsEditable, 0 as AllowAllValue, 0 as IsPrimaryFilter, 0 as IsPartOfKey
from	ArtifactType
GO

insert into Field (ObjectType, ObjectID, FieldTypeID, Value, FormattedValue)
	select	'Artifact', A.ID, FT.ID, A.Description, A.Description 
	from	Artifact A 
			inner join FieldType FT on FT.Object = 'ArtifactType' and FT.ObjectID = A.ArtifactTypeID and FT.Name = 'Description' and A.Description is not null and A.Description <> ''
			left join Field F on F.FieldTypeID = FT.ID and F.ObjectType = 'Artifact' and F.ObjectID = A.ID
	where	F.FieldTypeID is null
GO

-- Artifact Status
declare @rt int

INSERT INTO ReferenceItemType	( [Name], [DisplayFormat], [Description], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
VALUES							( 'Artifact Status', '{Code}', 'The status flag for an artifact.', getutcdate(), 0, getutcdate(), 0)

set @rt = SCOPE_IDENTITY()

INSERT INTO [dbo].[ReferenceItem]( [ReferenceItemTypeID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy], [Code], [Visible])
VALUES (@rt, getutcdate(), 0, getutcdate(), 0, 'Draft', 1)
INSERT INTO [dbo].[ReferenceItem]( [ReferenceItemTypeID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy], [Code], [Visible])
VALUES (@rt, getutcdate(), 0, getutcdate(), 0, 'Under Review', 1)
INSERT INTO [dbo].[ReferenceItem]( [ReferenceItemTypeID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy], [Code], [Visible])
VALUES (@rt, getutcdate(), 0, getutcdate(), 0, 'Certified', 1)

INSERT INTO [dbo].[FieldType]
([Name],[FriendlyName],[Description],[Type],[Object],[ObjectID],[SortOrder],[IsRequired],[IsListable],[IsDisplayable],[IsEditable],[AllowAllValue],[IsPrimaryFilter],[IsPartOfKey], LookupObjectType, LookupObjectID, LookupDisplayFormat)
select	'Status', 'Status', 'The artifact''s status', 'Lookup', 
		'ArtifactType', ID as ObjectID, 
		3 as SortOrder, 0 as IsRequired, 1 as IsListable, 1 as IsDisplayable, 1 as IsEditable, 0 as AllowAllValue, 0 as IsPrimaryFilter, 0 as IsPartOfKey, 'ReferenceItem', @rt, '{Code}'
from	ArtifactType
GO

insert into Field (ObjectType, ObjectID, FieldTypeID, Value, FormattedValue)
	select	'Artifact', A.ID, FT.ID, R.ID, R.Code--, A.Status, A.ArtifactTypeID
	from	Artifact A 
			inner join FieldType FT on FT.Object = 'ArtifactType' and FT.ObjectID = A.ArtifactTypeID and FT.Name = 'Status'
			inner join ReferenceItem R on R.ReferenceItemTypeID = FT.LookupObjectID and ltrim(rtrim(R.Code)) = ltrim(rtrim(A.Status))
GO

-- Artifact Subject Area
declare @rt int

INSERT INTO ReferenceItemType	( [Name], [DisplayFormat], [Description], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
VALUES							( 'Artifact Subject Area', '{Code}', 'The subject area for an artifact.', getutcdate(), 0, getutcdate(), 0)

set @rt = SCOPE_IDENTITY()

INSERT INTO [dbo].[ReferenceItem]( [ReferenceItemTypeID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy], [Code], [Visible])
select	@rt, getutcdate(), 0, getutcdate(), 0, Name, 1
from	TaxonomyType

INSERT INTO [dbo].[FieldType]
([Name],[FriendlyName],[Description],[Type],[Object],[ObjectID],[SortOrder],[IsRequired],[IsListable],[IsDisplayable],[IsEditable],[AllowAllValue],[IsPrimaryFilter],[IsPartOfKey], LookupObjectType, LookupObjectID, LookupDisplayFormat)
select	'SubjectArea', 'Subject Area', 'The artifact''s subject area', 'Lookup', 
		'ArtifactType', ID as ObjectID, 
		4 as SortOrder, 0 as IsRequired, 1 as IsListable, 1 as IsDisplayable, 1 as IsEditable, 0 as AllowAllValue, 0 as IsPrimaryFilter, 0 as IsPartOfKey, 'ReferenceItem', @rt, '{Code}'
from	ArtifactType

insert into Field (ObjectType, ObjectID, FieldTypeID, Value, FormattedValue)
	select	'Artifact', A.ID, FT.ID, R.ID, R.Code--, A.Status, A.ArtifactTypeID
	from	Artifact A 
			inner join TaxonomyType T on T.ID = A.TaxonomyTypeID
			inner join FieldType FT on FT.Object = 'ArtifactType' and FT.ObjectID = A.ArtifactTypeID and FT.Name = 'SubjectArea'
			inner join ReferenceItem R on R.ReferenceItemTypeID = FT.LookupObjectID and ltrim(rtrim(R.Code)) = ltrim(rtrim(T.Name))
GO

UPDATE	TF
SET		TF.FormattedValue = utility.GetFormattedFieldLookupValue(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, TF.Value)
from	Field TF
		inner join FieldType FT on FT.ID = TF.FieldTypeID and FT.LookupObjectType = 'ReferenceItem'

-- POLICY DYNAMIC FIELD GENERATION ---------------------------------------

INSERT INTO [dbo].[FieldType]
           ([Name]
           ,[FriendlyName]
           ,[Description]
           ,[Type]
           ,[MinimumLength]
           ,[MaximumLength]
           ,[Object]
           ,[ObjectID]
           ,[SortOrder]
           ,[IsRequired]
           ,[IsListable]
           ,[IsDisplayable]
           ,[IsEditable]
           ,[AllowAllValue]
           ,[IsPrimaryFilter]
           ,[IsPartOfKey])
select	'Name', 'Name', 'The policy''s name', 'Text', 3, 1000, 
		'PolicyType', ID as ObjectID, 
		1 as SortOrder, 1 as IsRequired, 1 as IsListable, 1 as IsDisplayable, 1 as IsEditable, 0 as AllowAllValue, 0 as IsPrimaryFilter, 1 as IsPartOfKey
from	PolicyType
GO

insert into Field (ObjectType, ObjectID, FieldTypeID, Value, FormattedValue)
	select 'Policy', A.ID, FT.ID, A.Name, A.Name from [Policy] A inner join FieldType FT on FT.Object = 'PolicyType' and FT.ObjectID = A.PolicyTypeID and FT.Name = 'Name'
GO

INSERT INTO [dbo].[FieldType]
           ([Name]
           ,[FriendlyName]
           ,[Description]
           ,[Type]
           ,[Object]
           ,[ObjectID]
           ,[SortOrder]
           ,[IsRequired]
           ,[IsListable]
           ,[IsDisplayable]
           ,[IsEditable]
           ,[AllowAllValue]
           ,[IsPrimaryFilter]
           ,[IsPartOfKey])
select	'Description', 'Description', 'The policy''s description', 'Html', 
		'PolicyType', ID as ObjectID, 
		2 as SortOrder, 0 as IsRequired, 0 as IsListable, 1 as IsDisplayable, 1 as IsEditable, 0 as AllowAllValue, 0 as IsPrimaryFilter, 0 as IsPartOfKey
from	PolicyType
GO

insert into Field (ObjectType, ObjectID, FieldTypeID, Value, FormattedValue)
	select	'Policy', A.ID, FT.ID, A.Description, A.Name 
	from	[Policy] A 
			inner join FieldType FT on FT.Object = 'PolicyType' and FT.ObjectID = A.PolicyTypeID and FT.Name = 'Description' and A.Description is not null and A.Description <> ''
GO

-- RULE DYNAMIC FIELD GENERATION -----------------------------------------

INSERT INTO [dbo].[FieldType]
           ([Name]
           ,[FriendlyName]
           ,[Description]
           ,[Type]
           ,[MinimumLength]
           ,[MaximumLength]
           ,[Object]
           ,[ObjectID]
           ,[SortOrder]
           ,[IsRequired]
           ,[IsListable]
           ,[IsDisplayable]
           ,[IsEditable]
           ,[AllowAllValue]
           ,[IsPrimaryFilter]
           ,[IsPartOfKey])
select	'Name', 'Name', 'The rule''s name', 'Text', 3, 1000, 
		'RuleType', ID as ObjectID, 
		1 as SortOrder, 1 as IsRequired, 1 as IsListable, 1 as IsDisplayable, 1 as IsEditable, 0 as AllowAllValue, 0 as IsPrimaryFilter, 1 as IsPartOfKey
from	RuleType
GO

insert into Field (ObjectType, ObjectID, FieldTypeID, Value, FormattedValue)
	select 'Rule', A.ID, FT.ID, A.Name, A.Name from [Rule] A inner join FieldType FT on FT.Object = 'RuleType' and FT.ObjectID = A.RuleTypeID and FT.Name = 'Name'
GO

INSERT INTO [dbo].[FieldType]
           ([Name]
           ,[FriendlyName]
           ,[Description]
           ,[Type]
           ,[Object]
           ,[ObjectID]
           ,[SortOrder]
           ,[IsRequired]
           ,[IsListable]
           ,[IsDisplayable]
           ,[IsEditable]
           ,[AllowAllValue]
           ,[IsPrimaryFilter]
           ,[IsPartOfKey])
select	'Description', 'Description', 'The rule''s description', 'Html', 
		'RuleType', ID as ObjectID, 
		2 as SortOrder, 0 as IsRequired, 0 as IsListable, 1 as IsDisplayable, 1 as IsEditable, 0 as AllowAllValue, 0 as IsPrimaryFilter, 0 as IsPartOfKey
from	RuleType
GO

insert into Field (ObjectType, ObjectID, FieldTypeID, Value, FormattedValue)
	select	'Rule', A.ID, FT.ID, A.Description, A.Name 
	from	[Rule] A 
			inner join FieldType FT on FT.Object = 'RuleType' and FT.ObjectID = A.RuleTypeID and FT.Name = 'Description' and A.Description is not null and A.Description <> ''
GO

INSERT INTO [dbo].[FieldType]
           ([Name]
           ,[FriendlyName]
           ,[Description]
           ,[Type]
           ,[Object]
           ,[ObjectID]
           ,[SortOrder]
           ,[IsRequired]
           ,[IsListable]
           ,[IsDisplayable]
           ,[IsEditable]
           ,[AllowAllValue]
           ,[IsPrimaryFilter]
           ,[IsPartOfKey])
select	'Purpose', 'Purpose', 'The rule''s Purpose', 'Html', 
		'RuleType', ID as ObjectID, 
		2 as SortOrder, 0 as IsRequired, 0 as IsListable, 1 as IsDisplayable, 1 as IsEditable, 0 as AllowAllValue, 0 as IsPrimaryFilter, 0 as IsPartOfKey
from	RuleType
GO

insert into Field (ObjectType, ObjectID, FieldTypeID, Value, FormattedValue)
	select	'Rule', A.ID, FT.ID, A.Description, A.Name 
	from	[Rule] A 
			inner join FieldType FT on FT.Object = 'RuleType' and FT.ObjectID = A.RuleTypeID and FT.Name = 'Purpose' and A.Purpose is not null and A.Purpose <> ''
GO

INSERT INTO [dbo].[FieldType]
           ([Name]
           ,[FriendlyName]
           ,[Description]
           ,[Type]
           ,[Object]
           ,[ObjectID]
           ,[SortOrder]
           ,[IsRequired]
           ,[IsListable]
           ,[IsDisplayable]
           ,[IsEditable]
           ,[AllowAllValue]
           ,[IsPrimaryFilter]
           ,[IsPartOfKey])
select	'Measurement', 'Measurement', 'The rule''s Measurement', 'Html', 
		'RuleType', ID as ObjectID, 
		2 as SortOrder, 0 as IsRequired, 0 as IsListable, 1 as IsDisplayable, 1 as IsEditable, 0 as AllowAllValue, 0 as IsPrimaryFilter, 0 as IsPartOfKey
from	RuleType
GO

insert into Field (ObjectType, ObjectID, FieldTypeID, Value, FormattedValue)
	select	'Rule', A.ID, FT.ID, A.Description, A.Name 
	from	[Rule] A 
			inner join FieldType FT on FT.Object = 'RuleType' and FT.ObjectID = A.RuleTypeID and FT.Name = 'Measurement' and A.Purpose is not null and A.Purpose <> ''
GO

INSERT INTO [dbo].[FieldType]
           ([Name]
           ,[FriendlyName]
           ,[Description]
           ,[Type]
           ,[Object]
           ,[ObjectID]
           ,[SortOrder]
           ,[IsRequired]
           ,[IsListable]
           ,[IsDisplayable]
           ,[IsEditable]
           ,[AllowAllValue]
           ,[IsPrimaryFilter]
           ,[IsPartOfKey])
select	'Resolution', 'Resolution', 'The rule''s Resolution', 'Html', 
		'RuleType', ID as ObjectID, 
		2 as SortOrder, 0 as IsRequired, 0 as IsListable, 1 as IsDisplayable, 1 as IsEditable, 0 as AllowAllValue, 0 as IsPrimaryFilter, 0 as IsPartOfKey
from	RuleType
GO

insert into Field (ObjectType, ObjectID, FieldTypeID, Value, FormattedValue)
	select	'Rule', A.ID, FT.ID, A.Description, A.Name 
	from	[Rule] A 
			inner join FieldType FT on FT.Object = 'RuleType' and FT.ObjectID = A.RuleTypeID and FT.Name = 'Resolution' and A.Resolution is not null and A.Resolution <> ''
GO

-- TAXONOMY DYNAMIC FIELD GENERATION -------------------------------------

INSERT INTO [dbo].[FieldType]
           ([Name]
           ,[FriendlyName]
           ,[Description]
           ,[Type]
           ,[MinimumLength]
           ,[MaximumLength]
           ,[Object]
           ,[ObjectID]
           ,[SortOrder]
           ,[IsRequired]
           ,[IsListable]
           ,[IsDisplayable]
           ,[IsEditable]
           ,[AllowAllValue]
           ,[IsPrimaryFilter]
           ,[IsPartOfKey])
select	'Name', 'Name', 'The model''s name', 'Text', 3, 1000, 
		'TaxonomyType', ID as ObjectID, 
		1 as SortOrder, 1 as IsRequired, 1 as IsListable, 1 as IsDisplayable, 1 as IsEditable, 0 as AllowAllValue, 0 as IsPrimaryFilter, 1 as IsPartOfKey
from	TaxonomyType
GO

insert into Field (ObjectType, ObjectID, FieldTypeID, Value, FormattedValue)
	select 'Taxonomy', A.ID, FT.ID, A.Name, A.Name from [Taxonomy] A inner join FieldType FT on FT.Object = 'TaxonomyType' and FT.ObjectID = A.TaxonomyTypeID and FT.Name = 'Name'
GO

INSERT INTO [dbo].[FieldType]
           ([Name]
           ,[FriendlyName]
           ,[Description]
           ,[Type]
           ,[Object]
           ,[ObjectID]
           ,[SortOrder]
           ,[IsRequired]
           ,[IsListable]
           ,[IsDisplayable]
           ,[IsEditable]
           ,[AllowAllValue]
           ,[IsPrimaryFilter]
           ,[IsPartOfKey])
select	'Description', 'Description', 'The model''s description', 'Html', 
		'TaxonomyType', ID as ObjectID, 
		2 as SortOrder, 0 as IsRequired, 0 as IsListable, 1 as IsDisplayable, 1 as IsEditable, 0 as AllowAllValue, 0 as IsPrimaryFilter, 0 as IsPartOfKey
from	TaxonomyType
GO

insert into Field (ObjectType, ObjectID, FieldTypeID, Value, FormattedValue)
	select	'Taxonomy', A.ID, FT.ID, A.Description, A.Name 
	from	[Taxonomy] A 
			inner join FieldType FT on FT.Object = 'TaxonomyType' and FT.ObjectID = A.TaxonomyTypeID and FT.Name = 'Description' and A.Description is not null and A.Description <> ''
GO

-- INSERT INTO ASSET TYPE-------------------------------------------------
insert into AssetType (Name, Description, Class, DisplayFormat, [State], [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
	select Name, Description, 1, '{Name}', 1, 0, 1, 'ArtifactType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from ArtifactType
GO
insert into AssetType (Name, Description, Class, DisplayFormat, [State], [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
	select Name, Description, 2, '{Name}', 1, 1, MaximumDepth, 'TaxonomyType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from TaxonomyType
GO
insert into AssetType (Name, Description, Class, DisplayFormat, [State], [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
	select Name, Description, 3, '{Name}', 1, 0, 1, 'FusionType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from FusionType
GO
insert into AssetType (Name, Description, Class, DisplayFormat, [State], [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
	select Name, null, 4, '{Name}', 1, 0, 1, 'FusionAttributeType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from FusionAttributeType
GO
insert into AssetType (Name, Description, Class, DisplayFormat, [State], [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
	select Name, null, 4, '{Name}', 1, 0, 1, 'FusionQueryAttributeType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from FusionQueryAttributeType
GO
INSERT into AssetType (Name, Description, Class, DisplayFormat, [State], [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
	select Name, Description, 5, coalesce(DisplayFormat, '{Name}'), 1, 0, 1, 'AttributeType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from AttributeType
GO
insert into AssetType (Name, Description, Class, DisplayFormat, [State], [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
	select Name, Description, 6, '{Name}', 1, 1, MaximumDepth, 'PolicyType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from PolicyType
GO
insert into AssetType (Name, Description, Class, DisplayFormat, [State], [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
	select Name, Description, 7, '{Name}', 1, 0, 1, 'RuleType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from RuleType
GO
insert into AssetType (Name, Description, Class, DisplayFormat, [State], [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
	select Name, Description, 8, '{Name}', 1, 0, 1, 'MapType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from MapType
GO
insert into AssetType (Name, Description, Class, DisplayFormat, [State],  [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
	select Name, Description, 9, coalesce(DisplayFormat, '{Name}'), 1, 0, 1, 'ReferenceItemType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from ReferenceItemType
GO

-- LOAD ASSETS INTO NEW TABLE -----------------------------------------------------------------------------------------------------------------------
INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
	SELECT	T.ID, 1, 'Artifact', O.ID, O.[CreatedOn], O.[CreatedBy], O.[UpdatedOn], O.[UpdatedBy]
	FROM	Artifact O inner join  AssetType T on T.Object = 'ArtifactType' and T.ObjectID = O.ArtifactTypeID
GO

INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
	SELECT	T.ID, 1, 'Taxonomy', O.ID, O.[UpdatedOn], O.[UpdatedBy], O.[UpdatedOn], O.[UpdatedBy]
	FROM	Taxonomy O inner join  AssetType T on T.Object = 'TaxonomyType' and T.ObjectID = O.TaxonomyTypeID
GO

INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
	SELECT	T.ID, 1, 'Fusion', O.ID, O.[UpdatedOn], O.[UpdatedBy], O.[UpdatedOn], O.[UpdatedBy]
	FROM	Fusion O inner join  AssetType T on T.Object = 'FusionType' and T.ObjectID = O.FusionTypeID
GO

INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
	SELECT	T.ID, 1, 'FusionAttribute', O.ID, getutcdate(), 0, getutcdate(), 0
	FROM	FusionAttribute O inner join  AssetType T on T.Object = 'FusionAttributeType' and T.ObjectID = O.FusionAttributeTypeID
GO

INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
	SELECT	T.ID, 1, 'FusionQueryAttribute', O.ID, getutcdate(), 0, getutcdate(), 0
	FROM	FusionQueryAttribute O inner join  AssetType T on T.Object = 'FusionQueryAttributeType' and T.ObjectID = O.FusionQueryAttributeTypeID
GO

INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
	SELECT	T.ID, 1, 'Attribute', O.ID, O.[UpdatedOn], O.[UpdatedBy], O.[UpdatedOn], O.[UpdatedBy]
	FROM	Attribute O inner join  AssetType T on T.Object = 'AttributeType' and T.ObjectID = O.AttributeTypeID
GO

INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
	SELECT	T.ID, 1, 'Policy', O.ID, O.[UpdatedOn], O.[UpdatedBy], O.[UpdatedOn], O.[UpdatedBy]
	FROM	[Policy] O inner join  AssetType T on T.Object = 'PolicyType' and T.ObjectID = O.PolicyTypeID
GO

INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
	SELECT	T.ID, 1, 'Rule', O.ID, O.[CreatedOn], O.[CreatedBy], O.[UpdatedOn], O.[UpdatedBy]
	FROM	[Rule] O inner join  AssetType T on T.Object = 'RuleType' and T.ObjectID = O.RuleTypeID
GO

--INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
--	SELECT	T.ID, 1, 'Map', O.ID, O.[CreatedOn], O.[CreatedBy], O.[UpdatedOn], O.[UpdatedBy]
--	FROM	[Map] O inner join  AssetType T on T.Object = 'MapType' and T.ObjectID = O.MapTypeID
--GO

INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
	SELECT	T.ID, 1, 'ReferenceItem', O.ID, O.[CreatedOn], O.[CreatedBy], O.[UpdatedOn], O.[UpdatedBy]
	FROM	ReferenceItem O inner join  AssetType T on T.Object = 'ReferenceItemType' and T.ObjectID = O.ReferenceItemTypeID
GO


insert into OrganizationType (Name) values ('General Organization')
GO
alter table Organization add OrganizationTypeID int constraint DF_Organization_OrganizationType default(1) not null
GO
ALTER TABLE [dbo].[Organization]  WITH CHECK ADD  CONSTRAINT [FK_Organization_OrganizationType] FOREIGN KEY([OrganizationTypeID]) REFERENCES [dbo].[OrganizationType] ([ID])
GO
ALTER TABLE [dbo].[Organization] CHECK CONSTRAINT [FK_Organization_OrganizationType]
GO

DROP INDEX [IX_Artifact_ArtifactTypeID] ON [dbo].[Artifact]
GO

DROP INDEX [IX_Artifact_ArtifactTypeID_TaxonomyTypeID] ON [dbo].[Artifact]
GO

DROP INDEX [IX_Artifact_ArtifactTypeID-Status] ON [dbo].[Artifact]
GO

DROP INDEX [IX_Artifact_TaxonomyTypeID] ON [dbo].[Artifact]
GO

ALTER TABLE [dbo].[Artifact] DROP CONSTRAINT [DF_Artifact_TaxonomyTypeID]
GO

ALTER TABLE [dbo].[Artifact] DROP CONSTRAINT [FK_Artifact_TaxonomyType]
GO



ALTER TABLE Artifact DROP COLUMN Name
ALTER TABLE Artifact DROP COLUMN Description
ALTER TABLE Artifact DROP COLUMN Status
ALTER TABLE Artifact DROP COLUMN TextPath
ALTER TABLE Artifact DROP COLUMN DateLastCertified
ALTER TABLE Artifact DROP COLUMN TaxonomyTypeID
ALTER TABLE Artifact DROP COLUMN [KeyHash]
ALTER TABLE Artifact DROP COLUMN [FieldHash]
ALTER TABLE Artifact DROP COLUMN [DisplayValue]
GO

ALTER TABLE Artifact ADD [KeyHash]        AS             ([utility].[GetObjectHashWrapper]('Artifact',[ID],[ArtifactTypeID],(1)))
ALTER TABLE Artifact ADD [FieldHash]      AS             ([utility].[GetObjectHashWrapper]('Artifact',[ID],[ArtifactTypeID],(0)))
ALTER TABLE Artifact ADD [DisplayValue]   NVARCHAR (MAX) DEFAULT ('<INVALID NO LONGER USED>') NOT NULL
GO



CREATE NONCLUSTERED INDEX [IX_Artifact_ArtifactTypeID] ON [dbo].[Artifact]([ArtifactTypeID] ASC);
GO

ALTER TRIGGER [dbo].[Artifact_AfterDelete]
	ON [dbo].[Artifact]
	AFTER DELETE
AS
	--SET TRANSACTION ISOLATION LEVEL SNAPSHOT
	SET NOCOUNT ON
	update	Asset 
	set		[State] = 3
	where	Object = 'Artifact' and ObjectID in (select ID from deleted);

	delete Asset
	where Object = 'Artifact' and ObjectID in (select ID from deleted);
GO

DROP TRIGGER [dbo].[Artifact_AfterUpsert]
GO

CREATE TRIGGER [dbo].[Artifact_AfterInsert]
   ON  [dbo].[Artifact] 
   AFTER INSERT
AS 
	SET NOCOUNT ON
	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],SourceID,[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, O.SourceID,'Artifact', O.ID, O.[CreatedOn], O.[CreatedBy], O.[UpdatedOn], O.[UpdatedBy]
		FROM	inserted O inner join  AssetType T on T.Object = 'ArtifactType' and T.ObjectID = O.ArtifactTypeID;
GO

CREATE TRIGGER [dbo].[Artifact_AfterUpdate]
   ON  [dbo].[Artifact] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	Asset T
			inner join inserted S on T.Object = 'Artifact' and T.ObjectID = S.ID
GO

ALTER TRIGGER [dbo].[ArtifactType_AfterDelete]
   ON  [dbo].[ArtifactType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	update	AssetType
	set		[State] = 3
	where	Object = 'ArtifactType' and ObjectID in (select ID from deleted)

	delete AssetType where Object = 'ArtifactType' and ObjectID in (select ID from deleted)
GO

ALTER TRIGGER [dbo].[ArtifactType_AfterInsert]
   ON  [dbo].[ArtifactType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON
	insert into AssetType (Name, Description, Class, DisplayFormat, [State], [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
		select Name, Description, 1, DisplayFormat, 1, 0, 1, 'ArtifactType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from inserted
GO

ALTER TRIGGER [dbo].[ArtifactType_AfterUpdate]
   ON  [dbo].[ArtifactType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.Name = S.Name,
			T.Description = S.Description,
			T.DisplayFormat = S.DisplayFormat,
			T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	AssetType T
			inner join inserted S on T.Object = 'ArtifactType' and T.ObjectID = S.ID
GO

ALTER TABLE ArtifactTypeExportTemplateStyle ADD [BackgroundColorValueFieldTypeID] INT CONSTRAINT [DF_ArtifactTypeExportTemplateStyle_BackgroundColorValueFieldTypeID] DEFAULT ((0)) NOT NULL
ALTER TABLE ArtifactTypeExportTemplateStyle ADD [ColorValueFieldTypeID]           INT      CONSTRAINT [DF_ArtifactTypeExportTemplateStyle_ColorValueFieldTypeID] DEFAULT ((0)) NOT NULL
GO

ALTER TABLE Attribute DROP COLUMN DisplayValue
GO
ALTER TABLE Attribute ADD [DisplayValue] AS ([utility].[GetObjectDisplayValueWrapper]('Attribute',[ID],[AttributeTypeID]))
GO

ALTER TRIGGER [dbo].[Attribute_AfterDelete]
   ON  [dbo].[Attribute] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	update	Asset
	set		[State] = 3
	where	Object = 'Attribute' and ObjectID in (select ID from deleted)

	delete Asset where Object = 'Attribute' and ObjectID in (select ID from deleted)
GO

ALTER TRIGGER [dbo].[Attribute_AfterInsert]
   ON  [dbo].[Attribute] 
   AFTER INSERT
AS 
	SET NOCOUNT ON
	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, 'Attribute', O.ID, O.[UpdatedOn], O.[UpdatedBy], O.[UpdatedOn], O.[UpdatedBy]
		FROM	inserted O inner join  AssetType T on T.Object = 'AttributeType' and T.ObjectID = O.AttributeTypeID
GO

ALTER TRIGGER [dbo].[Attribute_AfterUpdate]
   ON  [dbo].[Attribute] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	Asset T
			inner join inserted S on T.Object = 'Attribute' and T.ObjectID = S.ID
GO

ALTER TABLE AttributeType DROP COLUMN TextFormatString
GO

ALTER TRIGGER [dbo].[AttributeType_AfterDelete]
   ON  [dbo].[AttributeType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	update	AssetType
	set		[State] = 3
	where	Object = 'AttributeType' and ObjectID in (select ID from deleted)

	delete AssetType where Object = 'AttributeType' and ObjectID in (select ID from deleted)
GO

ALTER TRIGGER [dbo].[AttributeType_AfterInsert]
   ON  [dbo].[AttributeType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into AssetType (Name, Description, Class, DisplayFormat, [State], [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
		select Name, Description, 5, DisplayFormat, 1, 0, 1, 'AttributeType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from inserted
GO

ALTER TRIGGER [dbo].[AttributeType_AfterUpdate]
   ON  [dbo].[AttributeType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	update	T
	set		T.Name = S.Name,
			T.Description = S.Description,
			T.DisplayFormat = S.DisplayFormat,
			T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	AssetType T
			inner join inserted S on T.Object = 'AttributeType' and T.ObjectID = S.ID
GO

CREATE CLUSTERED INDEX [CIX_Field] ON [dbo].[Field]([ObjectType] ASC, [ObjectID] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Field_AssetID] ON [dbo].[Field]([AssetID] ASC) INCLUDE([FieldTypeID], [Value]);
GO

CREATE NONCLUSTERED INDEX [IX_Field_AssetID_Include_FormatedValue_FieldTypeID] ON [dbo].[Field]([AssetID] ASC) INCLUDE([FieldTypeID], [FormattedValue]);
GO

CREATE NONCLUSTERED INDEX [IX_Field_FieldTypeID] ON [dbo].[Field]([FieldTypeID] ASC) INCLUDE([ObjectType], [ObjectID]);
GO

DROP INDEX [IX_Field_FieldTypeID_ObjectType] ON Field
GO

DROP INDEX [IX_Field_ObjectType-ObjectID] ON Field
GO

DROP TRIGGER [dbo].[Field_AfterUpsert]
GO
CREATE TRIGGER [dbo].[Field_AfterUpsert]
	ON [dbo].[Field]
	FOR INSERT, UPDATE
AS
	SET NOCOUNT ON;

	UPDATE	T
	SET		T.FormattedValue = utility.GetFormattedFieldLookupValueWithMultiple(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value, FT.AllowMultipleValues)
	FROM	Field T 
			inner join inserted F on F.FieldTypeID = T.FieldTypeID and F.ObjectType = T.ObjectType and F.ObjectID = T.ObjectID and F.ObjectType <> 'FusionAttribute' and F.ObjectType <> 'FusionQueryAttribute'
			INNER JOIN FieldType FT ON FT.ID = T.FieldTypeID;

	-- the below section can be slow
	if exists(select 1 from Field TF inner join FieldType FT on FT.ID = TF.FieldTypeID inner join inserted SF on FT.LookupObjectType = SF.ObjectType and FT.LookupObjectID = SF.ObjectID and SF.ObjectType <> 'FusionAttribute' and SF.ObjectType <> 'FusionQueryAttribute')
	begin
		
		UPDATE	TF
		SET		TF.FormattedValue = utility.GetFormattedFieldLookupValueWithMultiple(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, TF.Value, FT.AllowMultipleValues)
		from	Field TF
				inner join FieldType FT on FT.ID = TF.FieldTypeID
				inner join	inserted SF on FT.LookupObjectType = SF.ObjectType and FT.LookupObjectID = SF.ObjectID and SF.ObjectType <> 'FusionAttribute' and SF.ObjectType <> 'FusionQueryAttribute';
	end

	UPDATE	T
	SET		T.AssetID = A.ID
	FROM	Field T 
			inner join inserted F on F.FieldTypeID = T.FieldTypeID and F.ObjectType = T.ObjectType and F.ObjectID = T.ObjectID and T.AssetID is null
			inner join Asset A on A.Object = F.ObjectType and A.ObjectID = F.ObjectID;
GO

DROP INDEX [IX_FieldType_Object] ON FieldType
GO

CREATE NONCLUSTERED INDEX [IX_FieldType_AssetTypeID] ON [dbo].[FieldType]([AssetTypeID] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_FieldType_LookupObjectType_LookupObjectID] ON [dbo].[FieldType]([LookupObjectType] ASC, [LookupObjectID] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_FieldType_Object-ObjectID] ON [dbo].[FieldType]([Object] ASC, [ObjectID] ASC);
GO

DROP TRIGGER [dbo].[FieldType_AfterDelete]
GO

CREATE TRIGGER [dbo].[FieldType_AfterInsert]
	ON [dbo].[FieldType]
	FOR INSERT
AS
	SET NOCOUNT ON;

	UPDATE	T
	SET		T.AssetTypeID = A.ID
	FROM	FieldType T 
			inner join inserted F on F.ID = T.ID and T.AssetTypeID is null
			inner join AssetType A on A.Object = F.Object and A.ObjectID = F.ObjectID
GO

DROP TRIGGER [dbo].[FieldType_AfterUpsert]
GO
CREATE TRIGGER [dbo].[FieldType_AfterUpsert]
   ON  [dbo].[FieldType] 
   AFTER INSERT, UPDATE
AS 
	
		UPDATE	F
		set		F.FormattedValue = utility.GetFormattedFieldLookupValueWithMultiple(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value, FT.AllowMultipleValues)
		FROM	Field F
				inner join inserted FT on FT.ID = F.FieldTypeID

		update	FT	
		set		FT.defaultformattedvalue  = [utility].[GetFormattedFieldLookupValueWrapper](FT.[Type],FT.[LookupDisplayFormat],FT.[LookupObjectType],FT.[LookupObjectID],FT.[DefaultValue])
		from	FieldType FT
				inner join inserted ins on ins.ID = FT.ID
GO

ALTER TRIGGER [dbo].[Fusion_AfterDelete]
   ON  [dbo].[Fusion] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	update	Asset
	set		[State] = 3
	where	Object = 'Fusion' and ObjectID in (select ID from deleted)

	delete Asset where Object = 'Fusion' and ObjectID in (select ID from deleted)
GO

ALTER TRIGGER [dbo].[Fusion_AfterInsert]
   ON  [dbo].[Fusion] 
   AFTER INSERT
AS 
BEGIN
	SET NOCOUNT ON
	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, 'Fusion', O.ID, O.[UpdatedOn], O.[UpdatedBy], O.[UpdatedOn], O.[UpdatedBy]
		FROM	inserted O inner join  AssetType T on T.Object = 'FusionType' and T.ObjectID = O.FusionTypeID
END
GO

ALTER TRIGGER [dbo].[Fusion_AfterUpdate]
   ON  [dbo].[Fusion] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	Asset T
			inner join inserted S on T.Object = 'Fusion' and T.ObjectID = S.ID
GO

CREATE TRIGGER [dbo].[FusionAttribute_AfterDelete]
   ON  [dbo].[FusionAttribute] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	update	Asset
	set		[State] = 3
	where	Object = 'FusionAttribute' and ObjectID in (select ID from deleted)

	delete Asset where Object = 'FusionAttribute' and ObjectID in (select ID from deleted)
GO

CREATE TRIGGER [dbo].[FusionAttribute_AfterInsert]
   ON  [dbo].[FusionAttribute] 
   AFTER INSERT
AS 
BEGIN
	SET NOCOUNT ON
	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],SourceID,[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, O.SourceID, 'FusionAttribute', O.ID, getutcdate(), 0, getutcdate(), 0
		FROM	inserted O inner join  AssetType T on T.Object = 'FusionAttributeType' and T.ObjectID = O.FusionAttributeTypeID
END
GO

CREATE TRIGGER [dbo].[FusionAttribute_AfterUpdate]
   ON  [dbo].[FusionAttribute] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.UpdatedBy = 0,
			T.UpdatedOn = getutcdate()
	from	Asset T
			inner join inserted S on T.Object = 'FusionAttribute' and T.ObjectID = S.ID
GO

ALTER TRIGGER [dbo].[FusionAttributeType_AfterDelete]
   ON  [dbo].[FusionAttributeType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	update	AssetType
	set		[State] = 3
	where	Object = 'FusionAttributeType' and ObjectID in (select ID from deleted)

	delete AssetType where Object = 'FusionAttributeType' and ObjectID in (select ID from deleted)
GO

ALTER TRIGGER [dbo].[FusionAttributeType_AfterInsert]
   ON  [dbo].[FusionAttributeType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into AssetType (Name, Description, Class, DisplayFormat, [State], [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
		select Name, null, 4, '{Name}', 1, 0, 1, 'FusionAttributeType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from inserted
GO

ALTER TRIGGER [dbo].[FusionAttributeType_AfterUpdate]
   ON  [dbo].[FusionAttributeType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	update	T
	set		T.Name = S.Name,
			T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	AssetType T
			inner join inserted S on T.Object = 'FusionAttributeType' and T.ObjectID = S.ID
GO

ALTER TABLE FusionQueryAttribute DROP COLUMN DisplayValue
ALTER TABLE FusionQueryAttribute ADD [DisplayValue] AS ([utility].[GetObjectDisplayValueWrapper]('FusionQueryAttribute',[ID],[FusionQueryAttributeTypeID]))
GO

CREATE NONCLUSTERED INDEX [IX_FusionQueryAttribute_FusionQueryAttributeTypeID]
    ON [dbo].[FusionQueryAttribute]([FusionQueryAttributeTypeID] ASC);
GO

CREATE TRIGGER [dbo].[FusionQueryAttribute_AfterDelete]
	ON [dbo].[FusionQueryAttribute]
	AFTER DELETE
AS
	SET NOCOUNT ON;
	update	Asset 
	set		[State] = 3
	where	Object = 'FusionQueryAttribute' and ObjectID in (select ID from deleted);

	delete Asset
	where Object = 'FusionQueryAttribute' and ObjectID in (select ID from deleted);
GO

CREATE TRIGGER [dbo].[FusionQueryAttribute_AfterInsert]
   ON  [dbo].[FusionQueryAttribute] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],SourceID,[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, O.ID, 'FusionQueryAttribute', O.ID, O.CreatedOn, O.CreatedBy, O.UpdatedOn, O.UpdatedBy
		FROM	inserted O inner join  AssetType T on T.Object = 'FusionQueryAttribute' and T.ObjectID = O.FusionQueryAttributeTypeID;
GO

CREATE TRIGGER [dbo].[FusionQueryAttribute_AfterUpdate]
   ON  [dbo].[FusionQueryAttribute] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	Asset T
			inner join inserted S on T.Object = 'FusionQueryAttribute' and T.ObjectID = S.ID
GO

ALTER TRIGGER [dbo].[FusionQueryAttributeType_AfterDelete]
   ON  [dbo].[FusionQueryAttributeType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	update	AssetType
	set		[State] = 3
	where	Object = 'FusionQueryAttributeType' and ObjectID in (select ID from deleted)

	delete AssetType where Object = 'FusionQueryAttributeType' and ObjectID in (select ID from deleted)
GO

CREATE TRIGGER [dbo].[FusionQueryAttributeType_AfterInsert]
   ON  [dbo].[FusionQueryAttributeType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON
	insert into AssetType (Name, Description, Class, DisplayFormat, [State], [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
		select Name, null, 13, DisplayFormat, 1, 0, 1, 'FusionQueryAttributeType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from inserted
GO

CREATE TRIGGER [dbo].[FusionQueryAttributeType_AfterUpdate]
   ON  [dbo].[FusionQueryAttributeType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.Name = S.Name,
			T.Description = null,
			T.DisplayFormat = S.DisplayFormat,
			T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	AssetType T
			inner join inserted S on T.Object = 'FusionQueryAttributeType' and T.ObjectID = S.ID
GO

DROP TRIGGER [dbo].[FusionQueryAttributeType_AfterUpsert]
GO

ALTER TRIGGER [dbo].[FusionType_AfterDelete]
   ON  [dbo].[FusionType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	update	AssetType
	set		[State] = 3
	where	Object = 'FusionType' and ObjectID in (select ID from deleted)

	delete AssetType where Object = 'FusionType' and ObjectID in (select ID from deleted)
GO

ALTER TRIGGER [dbo].[FusionType_AfterInsert]
   ON  [dbo].[FusionType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into AssetType (Name, Description, Class, DisplayFormat, [State], [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
		select Name, Description, 3, '{Name}', 1, 0, 1, 'FusionType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from inserted
GO

ALTER TRIGGER [dbo].[FusionType_AfterUpdate]
   ON  [dbo].[FusionType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	update	T
	set		T.Name = S.Name,
			T.Description = S.Description,
			T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	AssetType T
			inner join inserted S on T.Object = 'FusionType' and T.ObjectID = S.ID
GO

ALTER TABLE [dbo].[Group] ADD  CONSTRAINT [ucGroupName] UNIQUE NONCLUSTERED ( [Name] ASC )
GO

ALTER TRIGGER [dbo].[Group_AfterDelete]
   ON  [dbo].[Group] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'Group', ID, coalesce(UpdatedBy, 0)), 'Group', ID from deleted;

	update	Asset 
	set		[State] = 3
	where	Object = 'Group' and ObjectID in (select ID from deleted);

	delete Asset
	where Object = 'Group' and ObjectID in (select ID from deleted);
GO

ALTER TRIGGER [dbo].[Group_AfterInsert]
   ON  [dbo].[Group] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'Group', ID, coalesce(UpdatedBy, 0)), 'Group', ID from inserted;

	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],SourceID,[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, O.ID, 'Group', O.ID, O.UpdatedOn, coalesce(O.UpdatedBy, 0), O.UpdatedOn, coalesce(O.UpdatedBy, 0)
		FROM	inserted O inner join  AssetType T on T.Object = 'GroupType' and T.ObjectID = 1;
GO

ALTER TRIGGER [dbo].[Group_AfterUpdate]
   ON  [dbo].[Group] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'Group', ID, coalesce(UpdatedBy, 0)), 'Group', ID from inserted;

	update	T
	set		T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	Asset T
			inner join inserted S on T.Object = 'Group' and T.ObjectID = S.ID;
GO

DROP TRIGGER [dbo].[Lookup_AfterInsert]
DROP TRIGGER [dbo].[Lookup_AfterUpdate]
GO

DROP TRIGGER [dbo].[LookupType_AfterInsert]
DROP TRIGGER [dbo].[LookupType_AfterUpdate]
GO

DROP TRIGGER [dbo].[Map_AfterDelete]
DROP TRIGGER [dbo].[Map_AfterUpsert]
GO

DROP TRIGGER [dbo].[MapType_AfterDelete]
DROP TRIGGER [dbo].[MapType_AfterUpsert]
GO

ALTER TABLE [Policy] drop column [DisplayValue] 
ALTER TABLE [Policy] add [DisplayValue] NVARCHAR (MAX)  DEFAULT ('<INVALID VALUE>') NOT NULL
GO

CREATE TRIGGER [dbo].[Organization_AfterDelete]
	ON [dbo].[Organization]
	AFTER DELETE
AS
	SET NOCOUNT ON;
	update	Asset 
	set		[State] = 3
	where	Object = 'Organization' and ObjectID in (select ID from deleted);

	delete Asset
	where Object = 'Organization' and ObjectID in (select ID from deleted);
GO

CREATE TRIGGER [dbo].[Organization_AfterInsert]
   ON  [dbo].[Organization] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],SourceID,[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, O.ID, 'Organization', O.ID, getutcdate(), 0, getutcdate(), 0
		FROM	inserted O inner join  AssetType T on T.Object = 'OrganizationType' and T.ObjectID = O.OrganizationTypeID;
GO

CREATE TRIGGER [dbo].[Organization_AfterUpdate]
   ON  [dbo].[Organization] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.UpdatedBy = 0,
			T.UpdatedOn = getutcdate()
	from	Asset T
			inner join inserted S on T.Object = 'Organization' and T.ObjectID = S.ID
GO

ALTER TRIGGER [dbo].[Policy_AfterDelete]
   ON  [dbo].[Policy] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	update	Asset
	set		[State] = 3
	where	Object = 'Policy' and ObjectID in (select ID from deleted)

	delete Asset where Object = 'Policy' and ObjectID in (select ID from deleted)
GO

ALTER TRIGGER [dbo].[Policy_AfterInsert]
   ON  [dbo].[Policy] 
   AFTER INSERT
AS 
	SET NOCOUNT ON
	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, 'Policy', O.ID, O.[UpdatedOn], O.[UpdatedBy], O.[UpdatedOn], O.[UpdatedBy]
		FROM	inserted O inner join  AssetType T on T.Object = 'PolicyType' and T.ObjectID = O.PolicyTypeID
GO

ALTER TRIGGER [dbo].[Policy_AfterUpdate]
   ON  [dbo].[Policy] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	Asset T
			inner join inserted S on T.Object = 'Policy' and T.ObjectID = S.ID
GO


ALTER TRIGGER [dbo].[PolicyType_AfterDelete]
   ON  [dbo].[PolicyType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	update	AssetType
	set		[State] = 3
	where	Object = 'PolicyType' and ObjectID in (select ID from deleted);

	delete AssetType where Object = 'PolicyType' and ObjectID in (select ID from deleted);
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


CREATE NONCLUSTERED INDEX [IX_Predicate_Type]
    ON [dbo].[Predicate]([Type] ASC);
GO

alter table ReferenceItem drop column DisplayValue
alter table ReferenceItem add [DisplayValue]        AS             ([utility].[GetObjectDisplayValueWrapper]('ReferenceItem',[ID],[ReferenceItemTypeID]))
alter table ReferenceItem add [KeyHash]             AS             ([utility].[GetObjectHashWrapper]('ReferenceItem',[ID],[ReferenceItemTypeID],(1)))
alter table ReferenceItem add [FieldHash]           AS             ([utility].[GetObjectHashWrapper]('ReferenceItem',[ID],[ReferenceItemTypeID],(0)))

ALTER TRIGGER [dbo].[ReferenceItem_AfterDelete]
   ON  [dbo].[ReferenceItem] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	update	Asset
	set		[State] = 3
	where	Object = 'ReferenceItem' and ObjectID in (select ID from deleted)

	delete Asset where Object = 'ReferenceItem' and ObjectID in (select ID from deleted)
GO

CREATE TRIGGER [dbo].[ReferenceItem_AfterInsert]
   ON  [dbo].[ReferenceItem] 
   AFTER INSERT
AS 
	SET NOCOUNT ON
	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, 'ReferenceItem', O.ID, O.[CreatedOn], O.[CreatedBy], O.[UpdatedOn], O.[UpdatedBy]
		FROM	inserted O inner join  AssetType T on T.Object = 'ReferenceItemType' and T.ObjectID = O.ReferenceItemTypeID
GO

CREATE TRIGGER [dbo].[ReferenceItem_AfterUpdate]
   ON  [dbo].[ReferenceItem] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	Asset T
			inner join inserted S on T.Object = 'ReferenceItem' and T.ObjectID = S.ID
GO

DROP TRIGGER [dbo].[ReferenceItem_AfterUpsert]
GO

ALTER TABLE [ReferenceItemType] add [SourceNotes]   NVARCHAR (MAX) NULL
GO

ALTER TRIGGER [dbo].[ReferenceItemType_AfterDelete]
   ON  [dbo].[ReferenceItemType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	update	AssetType
	set		[State] = 3
	where	Object = 'ReferenceItemType' and ObjectID in (select ID from deleted);

	delete AssetType where Object = 'ReferenceItemType' and ObjectID in (select ID from deleted);
GO

ALTER TRIGGER [dbo].[ReferenceItemType_AfterInsert]
   ON  [dbo].[ReferenceItemType]
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into AssetType (Name, Description, Class, DisplayFormat, [State],  [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
		select Name, Description, 9, coalesce(DisplayFormat, '{Name}'), 1, 0, 1, 'ReferenceItemType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from inserted;
GO

ALTER TRIGGER [dbo].[ReferenceItemType_AfterUpdate]
   ON  [dbo].[ReferenceItemType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	update	T
	set		T.Name = S.Name,
			T.Description = S.Description,
			T.DisplayFormat = S.DisplayFormat,
			T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	AssetType T
			inner join inserted S on T.Object = 'ReferenceItemType' and T.ObjectID = S.ID;
GO

ALTER TABLE ResponsibilityType ALTER COLUMN [ResponsibilityTypeGroup] INT NULL
GO

ALTER TABLE [dbo].[ResponsibilityTypeRelation] ADD [ReadObject]           BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_ReadObject] DEFAULT ((1)) NOT NULL
ALTER TABLE [dbo].[ResponsibilityTypeRelation] ADD [ReadAttributes]       BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_ReadAttributes] DEFAULT ((1)) NOT NULL
ALTER TABLE [dbo].[ResponsibilityTypeRelation] ADD [ReadAudit]            BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_ReadAudit] DEFAULT ((1)) NOT NULL
ALTER TABLE [dbo].[ResponsibilityTypeRelation] ADD [ReadDashboards]       BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_ReadDashboards] DEFAULT ((1)) NOT NULL
ALTER TABLE [dbo].[ResponsibilityTypeRelation] ADD [ReadRelationships]    BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_ReadRelationships] DEFAULT ((1)) NOT NULL
ALTER TABLE [dbo].[ResponsibilityTypeRelation] ADD [ReadSocial]           BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_ReadSocial] DEFAULT ((1)) NOT NULL
ALTER TABLE [dbo].[ResponsibilityTypeRelation] ADD [ModifyObject]         BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_ModifyObject] DEFAULT ((0)) NOT NULL
ALTER TABLE [dbo].[ResponsibilityTypeRelation] ADD [ModifyAttributes]     BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_ModifyAttributes] DEFAULT ((0)) NOT NULL
ALTER TABLE [dbo].[ResponsibilityTypeRelation] ADD [ModifyRelationships]  BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_ModifyRelationships] DEFAULT ((0)) NOT NULL
ALTER TABLE [dbo].[ResponsibilityTypeRelation] ADD [ModifySocial]         BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_ModifySocial] DEFAULT ((0)) NOT NULL
ALTER TABLE [dbo].[ResponsibilityTypeRelation] ADD [DeleteObject]         BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_DeleteObject] DEFAULT ((0)) NOT NULL
ALTER TABLE [dbo].[ResponsibilityTypeRelation] ADD [DeleteAttributes]     BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_DeleteAttributes] DEFAULT ((0)) NOT NULL
ALTER TABLE [dbo].[ResponsibilityTypeRelation] ADD [DeleteRelationships]  BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_DeleteRelationships] DEFAULT ((0)) NOT NULL
ALTER TABLE [dbo].[ResponsibilityTypeRelation] ADD [DeleteSocial]         BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_DeleteSocial] DEFAULT ((0)) NOT NULL
GO

CREATE NONCLUSTERED INDEX [IX_ResponsibilityTypeRelationItem_AssetID_SecurityAsset_Overriden_include]
    ON [dbo].[ResponsibilityTypeRelationItem]([AssetID] ASC, [SecurityAsset] ASC, [Overriden] ASC)
    INCLUDE([SecurityAssetID]);
GO

CREATE TRIGGER ResponsibilityTypeRelationOverrideItem_AfterDelete
   ON  dbo.ResponsibilityTypeRelationOverrideItem
   AFTER DELETE
AS 
BEGIN
	SET NOCOUNT ON;

	update	T
	set		T.Overriden = 0
	from	ResponsibilityTypeRelationItem T
			inner join deleted S on T.RuleID > 0 and S.AssetID = T.AssetID and S.ResponsibilityTypeID = T.ResponsibilityTypeID and T.Overriden = 1
			left join ResponsibilityTypeRelationItem E on E.RuleID = 0 and E.AssetID = S.AssetID and E.ResponsibilityTypeID = S.ResponsibilityTypeID and E.OverrideItemID <> S.ID
	where	E.AssetID is null;

	delete	T
	from	ResponsibilityTypeRelationItem T
			inner join deleted S on T.OverrideItemID = S.ID;
END
GO

CREATE TRIGGER ResponsibilityTypeRelationOverrideItem_AfterInsert
   ON  dbo.ResponsibilityTypeRelationOverrideItem
   AFTER INSERT
AS 
BEGIN
	SET NOCOUNT ON;

	insert into ResponsibilityTypeRelationItem (RuleID, ResponsibilityTypeID, AssetID, SecurityAsset, SecurityAssetID, OverrideItemID) 
		select	0, 
				ResponsibilityTypeID, 
				AssetID, 
				SecurityAsset, 
				SecurityAssetID, 
				ID
		from	inserted;

	update	T
	set		T.Overriden = 1
	from	ResponsibilityTypeRelationItem T
			inner join inserted S on T.RuleID > 0 and S.AssetID = T.AssetID and S.ResponsibilityTypeID = T.ResponsibilityTypeID and T.Overriden = 0;
END
GO

CREATE TRIGGER ResponsibilityTypeRelationOverrideItem_AfterUpdate
   ON  dbo.ResponsibilityTypeRelationOverrideItem
   AFTER UPDATE
AS 
BEGIN
	SET NOCOUNT ON;
	update	T
	set		T.AssetID = S.AssetID,
			T.ResponsibilityTypeID = S.ResponsibilityTypeID,
			T.SecurityAsset = S.SecurityAsset,
			T.SecurityAssetID = S.SecurityAssetID
	from	ResponsibilityTypeRelationItem T
			inner join inserted S on S.ID = T.OverrideItemID
END
GO

alter table [Rule] drop column [DisplayValue]
alter table [Rule] add [DisplayValue] AS ([utility].[GetObjectDisplayValueWrapper]('Rule',[ID],[RuleTypeID]))
GO

alter table [Rule] add [CreatedBy]       INT            NULL
alter table [Rule] add [CreatedOn]       DATETIME       NULL
alter table [Rule] add [UpdatedBy]       INT            NULL
alter table [Rule] add [UpdatedOn]       DATETIME       NULL
 GO

 ALTER TRIGGER [dbo].[Rule_AfterDelete]
   ON  [dbo].[Rule] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	update	Asset
	set		[State] = 3
	where	Object = 'Rule' and ObjectID in (select ID from deleted)

	delete Asset where Object = 'Rule' and ObjectID in (select ID from deleted)
GO

ALTER TRIGGER [dbo].[Rule_AfterInsert]
   ON  [dbo].[Rule] 
   AFTER INSERT
AS 
	SET NOCOUNT ON
	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, 'Rule', O.ID, O.[CreatedOn], O.[CreatedBy], O.[UpdatedOn], O.[UpdatedBy]
		FROM	inserted O inner join  AssetType T on T.Object = 'RuleType' and T.ObjectID = O.RuleTypeID
GO

ALTER TRIGGER [dbo].[Rule_AfterUpdate]
   ON  [dbo].[Rule] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	Asset T
			inner join inserted S on T.Object = 'Rule' and T.ObjectID = S.ID
GO


ALTER TRIGGER [dbo].[RuleType_AfterDelete]
   ON  [dbo].[RuleType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	update	AssetType
	set		[State] = 3
	where	Object = 'RuleType' and ObjectID in (select ID from deleted);
	delete AssetType where Object = 'RuleType' and ObjectID in (select ID from deleted);
GO

ALTER TRIGGER [dbo].[RuleType_AfterInsert]
   ON  [dbo].[RuleType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into AssetType (Name, Description, Class, DisplayFormat, [State], [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
		select Name, Description, 7, coalesce(DisplayFormat, '{Name}'), 1, 0, 1, 'RuleType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from inserted;
GO

ALTER TRIGGER [dbo].[RuleType_AfterUpdate]
   ON  [dbo].[RuleType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	update	T
	set		T.Name = S.Name,
			T.Description = S.Description,
			T.DisplayFormat = S.DisplayFormat,
			T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	AssetType T
			inner join inserted S on T.Object = 'RuleType' and T.ObjectID = S.ID;
GO

alter table Shortcut add [DisplayOrder]    INT            DEFAULT ((100)) NOT NULL
alter table Shortcut add [LinkTarget]      INT            DEFAULT ((0)) NOT NULL

ALTER TRIGGER [dbo].[Taxonomy_AfterDelete]
	ON [dbo].[Taxonomy]
	AFTER DELETE
AS
	SET NOCOUNT ON;
	update	Asset
	set		[State] = 3
	where	Object = 'Taxonomy' and ObjectID in (select ID from deleted);

	delete Asset where Object = 'Taxonomy' and ObjectID in (select ID from deleted);
GO

CREATE TRIGGER [dbo].[Taxonomy_AfterInsert]
   ON  [dbo].[Taxonomy] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],SourceID,[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, 'Taxonomy', O.ID, O.SourceID,O.[UpdatedOn], O.[UpdatedBy], O.[UpdatedOn], O.[UpdatedBy]
		FROM	inserted O inner join  AssetType T on T.Object = 'TaxonomyType' and T.ObjectID = O.TaxonomyTypeID;
GO

CREATE TRIGGER [dbo].[Taxonomy_AfterUpdate]
   ON  [dbo].[Taxonomy] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	update	T
	set		T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	Asset T
			inner join inserted S on T.Object = 'Taxonomy' and T.ObjectID = S.ID;
GO

DROP TRIGGER [dbo].[Taxonomy_AfterUpsert]

ALTER TRIGGER [dbo].[TaxonomyType_AfterDelete]
   ON  [dbo].[TaxonomyType] 
   AFTER DELETE
AS 
BEGIN
	SET NOCOUNT ON;
	update	AssetType
	set		[State] = 3
	where	Object = 'TaxonomyType' and ObjectID in (select ID from deleted);
	delete AssetType where Object = 'TaxonomyType' and ObjectID in (select ID from deleted);
END
GO

ALTER TRIGGER [dbo].[TaxonomyType_AfterInsert]
   ON  [dbo].[TaxonomyType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into AssetType (Name, Description, Class, DisplayFormat, [State], [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
		select Name, Description, 2, DisplayFormat, 1, 1, MaximumDepth, 'TaxonomyType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from inserted;
GO

ALTER TRIGGER [dbo].[TaxonomyType_AfterUpdate]
   ON  [dbo].[TaxonomyType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	update	T
	set		T.Name = S.Name,
			T.Description = S.Description,
			T.DisplayFormat = S.DisplayFormat,
			T.HierarchyMaximumDepth = S.MaximumDepth,
			T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	AssetType T
			inner join inserted S on T.Object = 'TaxonomyType' and T.ObjectID = S.ID;
GO

drop table metrics.ConditionValue
drop table metrics.Condition

CREATE TABLE [metrics].[Condition] (
    [MapID]       BIGINT         NOT NULL,
    [FieldTypeID] INT            NOT NULL,
    [AndOr]       VARCHAR (1)    NOT NULL,
    [Operator]    VARCHAR (10)   NOT NULL,
    [Value]       NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_MetricCondition] PRIMARY KEY NONCLUSTERED ([MapID] ASC, [FieldTypeID] ASC),
    CONSTRAINT [FK_MetricCondition_MetricMap] FOREIGN KEY ([MapID]) REFERENCES [metrics].[Map] ([ID])
);
GO

CREATE TABLE [metrics].[ConditionValue] (
    [MapID]       BIGINT         NOT NULL,
    [FieldTypeID] INT            NOT NULL,
    [Value]       NVARCHAR (250) NOT NULL,
    CONSTRAINT [PK_MetricConditionValue] PRIMARY KEY NONCLUSTERED ([MapID] ASC, [FieldTypeID] ASC, [Value] ASC),
    CONSTRAINT [FK_MetricConditionValue_MetricCondition] FOREIGN KEY ([MapID], [FieldTypeID]) REFERENCES [metrics].[Condition] ([MapID], [FieldTypeID])
);
GO

DROP TABLE [metrics].[StagingResult]
GO

CREATE TABLE [metrics].[StagingResult] (
    [MapID]         BIGINT         NOT NULL,
    [EffectiveDate] DATETIME       NOT NULL,
    [AssetID]       BIGINT         NOT NULL,
    [Value]         BIT            NOT NULL,
    [Score]         DECIMAL (5, 3) NOT NULL,
    CONSTRAINT [PK_MetricStagingResult] PRIMARY KEY NONCLUSTERED ([MapID] ASC, [EffectiveDate] DESC, [AssetID] ASC),
    CONSTRAINT [FK_StagingResult_Map] FOREIGN KEY ([MapID]) REFERENCES [metrics].[Map] ([ID]) ON DELETE CASCADE
);
GO


ALTER TRIGGER [reporting].[ReportingGlobalResource_AfterDelete]
	ON [reporting].[Global_Resource]
	FOR DELETE
AS
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'Resource', ResourceID, 0), 'Resource', ResourceID from deleted;

	update	Asset 
	set		[State] = 3
	where	Object = 'Resource' and ObjectID in (select ID from deleted);

	delete Asset
	where Object = 'Resource' and ObjectID in (select ID from deleted);
GO

ALTER TRIGGER [reporting].[ReportingGlobalResource_AfterInsert]
	ON [reporting].[Global_Resource]
	FOR INSERT
AS
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Add', [queue].WriteIndexXml('', 'Resource', ResourceID, 0), 'Resource', ResourceID from inserted;

	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],SourceID,[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, O.ResourceID, 'Resource', O.ResourceID, getutcdate(), 0, getutcdate(), 0
		FROM	inserted O inner join  AssetType T on T.Object = 'ResourceType' and T.ObjectID = 1;
GO

CREATE NONCLUSTERED INDEX [IX_WorkflowItem_VersionObjectObjectID]
    ON [workflow].[Item]([VersionID] ASC, [Object] ASC, [ObjectID] ASC);
GO

ALTER view [cache].[ObjectDetails]
as
select
	T.Object,
	T.ObjectID,
	T.Name,
	T.Name as TextPath,
	cast(null as nvarchar) as Description,		
	T.Url,
	T.Url as NgUrl,
	cast(null as varchar) as Parent,
	cast(null as int) as ParentID,
	cast(null as nvarchar) as ParentName,
	T.ObjectType,
	T.ObjectTypeID,
	T.ObjectTypeName,
	T.IconBackColor,
	T.IconForeColor,
	T.IconText
from
	( select	A.Object as Object,
		A.ObjectID as ObjectID,
		AName.DisplayValue as Name,						
		AUrl.[Url] as [Url],
		AST.Object as ObjectType,
		AST.ObjectID as ObjectTypeID,
		AST.Name as ObjectTypeName,
		coalesce(S.IconBackColor, '#000') as IconBackColor,
		coalesce(S.IconForeColor, '#fff') as IconForeColor,
		coalesce(S.IconText, 'leaf') as IconText
	from	AssetType AST
		left join ObjectStyle S on S.ObjectType = AST.Object and S.ObjectID = AST.ObjectID
		left join Asset A on A.AssetTypeID = AST.ID
		cross apply [dbo].[GetAssetUrl](A.[Object], AST.ObjectID, A.ObjectID) AUrl
		cross apply [dbo].[GetAssetDisplayValueById](A.ID) AName
			) T		
union -- types
select
	T_t.Object,
	T_t.ObjectID,
	T_t.Name,
	T_t.Name as TextPath,
	cast(null as nvarchar) as Description,		
	T_t.Url,
	T_t.Url as NgUrl,
	cast(null as varchar) as Parent,
	cast(null as int) as ParentID,
	cast(null as nvarchar) as ParentName,
	T_t.ObjectType,
	T_t.ObjectTypeID,
	T_t.ObjectTypeName,
	T_t.IconBackColor,
	T_t.IconForeColor,
	T_t.IconText
from
( select	AST.Object as Object,
		AST.ObjectID as ObjectID,
		AST.Name as Name,						
		AUrl.[Url] as [Url],
		AST.Object as ObjectType,
		AST.ObjectID as ObjectTypeID,
		null as ObjectTypeName,
		coalesce(S.IconBackColor, '#000') as IconBackColor,
		coalesce(S.IconForeColor, '#fff') as IconForeColor,
		coalesce(S.IconText, 'leaf') as IconText
	from	AssetType AST
		left join ObjectStyle S on S.ObjectType = AST.Object and S.ObjectID = AST.ObjectID		
		cross apply [dbo].[GetAssetUrl](AST.[Object], AST.ObjectID, AST.ObjectID) AUrl
			) T_t
union -- intersects
select	'Intersect' as Object,
		I.ID as ObjectID,
		IName.Name as Name,
		IName.Name as TextPath,		
		cast(null as nvarchar) as Description,
		null as Url,
		null as NgUrl,
		cast(null as varchar) as Parent,
		cast(null as int) as ParentID,
		cast(null as nvarchar) as ParentName,
		'IntersectType' as ObjectType,
		IT.ID as ObjectTypeID,
		ITypeName.Name as ObjectTypeName,
		coalesce(S.IconBackColor, '#000') as IconBackColor,
		coalesce(S.IconForeColor, '#fff') as IconForeColor,
		coalesce(S.IconText, 'leaf') as IconText
from	IntersectType IT		
		inner join [Intersect] I on I.IntersectTypeID = IT.ID		
		left join ObjectStyle S on S.ObjectType = 'IntersectType' and S.ObjectID = IT.ID		
		cross apply dbo.GetIntersectNames(I.ID) IName	
		cross apply dbo.GetIntersectTypeNames(IT.ID) ITypeName

union -- intersect types
select	'IntersectType' as Object,
		I_T.ID as ObjectID,
		ITypeName.Name as Name,
		ITypeName.Name as TextPath,		
		cast(null as nvarchar) as Description,
		null as Url,
		null as NgUrl,
		cast(null as varchar) as Parent,
		cast(null as int) as ParentID,
		cast(null as nvarchar) as ParentName,
		'IntersectType' as ObjectType,
		0 as ObjectTypeID,
		null as ObjectTypeName,
		coalesce(S.IconBackColor, '#000') as IconBackColor,
		coalesce(S.IconForeColor, '#fff') as IconForeColor,
		coalesce(S.IconText, 'leaf') as IconText
from	IntersectType I_T				
		left join ObjectStyle S on S.ObjectType = 'IntersectType' and S.ObjectID = I_T.ID				
		cross apply dbo.GetIntersectTypeNames(I_T.ID) ITypeName
GO

ALTER VIEW [dbo].[AttributeDetail]
AS
	select	A.ObjectType,
			A.ObjectID,
			A.AttributeTypeID,
			A.ID,
			A.ParentID,
			T.Name,
			C.Name as AttributeTypeCategory,
			T.ShowNameInTree,
			A.DisplayValue as FormattedValue
	from	Attribute A
			inner join AttributeType T on A.AttributeTypeID = T.ID
			left join AttributeTypeCategory C on C.ID = T.AttributeTypeCategoryID
GO

ALTER VIEW [dbo].[FieldLookupValue]
AS
	SELECT	T.ID as FieldTypeID,
			T.LookupObjectType,
			T.LookupObjectID,
			COALESCE(A.ID, R.ResourceID, L.ID, RI.ID, RIT.ID,TAX.ID) as Value,	
			utility.GetFormattedFieldLookupValue(T.Type, coalesce(T.LookupEditFormat, T.LookupDisplayFormat), T.LookupObjectType, T.LookupObjectID, COALESCE(A.ID, R.ResourceID, L.ID, RI.ID, RIT.ID,TAX.ID)) as Text
	FROM	FieldType T 
			LEFT JOIN Artifact A ON T.LookupObjectType = 'Artifact' AND T.LookupObjectID = A.ArtifactTypeID
			LEFT JOIN reporting.Global_Resource R ON T.LookupObjectType = 'Resource' --AND T.LookupObjectID = R.ResourceTypeID
			LEFT JOIN Lookup L ON T.LookupObjectType = 'Lookup' AND T.LookupObjectID = L.LookupTypeID
			LEFT JOIN ReferenceItem RI ON T.LookupObjectType = 'ReferenceItem' AND T.LookupObjectID = RI.ReferenceItemTypeID
			LEFT JOIN ReferenceItemType RIT ON T.LookupObjectType = 'ReferenceItemType' --AND T.LookupObjectID = RIT.ID
			LEFT JOIN Taxonomy TAX ON T.LookupObjectType = 'Taxonomy' AND T.LookupObjectID = TAX.TaxonomyTypeID
	WHERE	T.LookupObjectType is not null
			AND COALESCE(A.ID, R.ResourceID, L.ID, RI.ID, RIT.ID,TAX.ID) IS NOT NULL
GO

ALTER VIEW [dbo].[FieldWithRelation]
AS
	SELECT	F.FieldTypeID,
			T.Name,
			T.FriendlyName,
			T.Category,
			T.Description,
			T.DisplayDescription,
			T.FormDescription,
			T.ValidationDescription,
			T.Type,
			T.LookupObjectType,
			T.LookupObjectID,
			T.LookupDisplayFormat,
			T.MinimumLength,
			T.MaximumLength,
			T.Length,
			T.Pattern,
			T.IsDisplayable,
			T.IsEditable,
			T.IsListable,
			T.IsRequired,
			T.SortOrder,
			T.AllowMultipleValues,
			F.ObjectType,
			F.ObjectID,
			F.Value,
			F.FormattedValue,
			case  
				when (T.AllowMultipleValues = 0 and T.LookupObjectType = 'ReferenceItemType') then [dbo].GenerateObjectUrl('ReferenceItemType', coalesce(F.Value, T.DefaultValue), T.LookupObjectID)
				when (T.AllowMultipleValues = 0 and T.LookupObjectType = 'ReferenceItem') then [dbo].GenerateObjectUrl('ReferenceItemType', T.LookupObjectID, coalesce(F.Value, T.DefaultValue))
				when (T.AllowMultipleValues = 0 and T.LookupObjectType = 'Resource') then [dbo].GenerateObjectUrl('ResourceType', 0, T.LookupObjectID)
				else null
			end as LookupUrl
	FROM	FieldType T
			left join Field F on F.FieldTypeID = T.ID
	WHERE	(F.Value is not null OR T.DefaultValue is not null)
GO

alter view [dbo].[IntersectDetail]
as
	
	select	I.IntersectID as ID,
			I.IntersectTypeID,
			I.State,
			I.Subject,
			I.SubjectID,
			S.Name as SubjectName,
			S.Name as SubjectShortName,
			dbo.GenerateNgObjectUrl(S.[ObjectType], S.ObjectTypeID, S.ObjectID) as SubjectUrl,
			S.ObjectType as SubjectType,
			S.ObjectTypeID as SubjectTypeID,
			S.ObjectTypeName as SubjectTypeName,
			S.IconBackColor as SubjectIconBackColor,
			S.IconForeColor as SubjectIconForeColor,
			S.IconText as SubjectIconText,

			I.Object,
			I.ObjectID,
			O.Name as ObjectName,
			O.Name as ObjectShortName,
			dbo.GenerateNgObjectUrl(O.[ObjectType], O.ObjectTypeID, O.ObjectID) as ObjectUrl,
			O.ObjectType as ObjectType,
			O.ObjectTypeID as ObjectTypeID,
			O.ObjectTypeName as ObjectTypeName,
			O.IconBackColor as ObjectIconBackColor,
			O.IconForeColor as ObjectIconForeColor,
			O.IconText as ObjectIconText,

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
			inner join cache.objectdetails S on S.Object = I.Subject and S.ObjectID = I.SubjectID
			inner join cache.objectdetails O on O.Object = I.Object and O.ObjectID = I.ObjectID
GO

alter view [dbo].[OrganizationDetail]
as
select 
	o.ID,
	o.Name,
	o.Accepted,
	o.AcceptedBy,
	o.DateAccepted,
	o.AdministratorEmail,
	r.FirstName + ' ' + r.LastName as AcceptedByName,
	o.OrganizationTypeID
from Organization o
left join reporting.Global_Resource r on r.ResourceID = o.AcceptedBy
where o.[State] = 1
GO

alter view [dbo].[SecurityDetail]
as
	select	'Resource' as ResponsibleObjectType,
			RD.ResourceID as ResponsibleObjectID,
			RD.Object as ObjectType,
			RD.ObjectID,
			RTC.Claim,
			RTC.ClaimObject
	from	ResponsibilityDetails RD
			inner join ResponsibilityTypeObjectClaim RTC	on RTC.ResponsibilityTypeID = RD.ResponsibilityTypeID 
																and RTC.ObjectType = RD.Type 
																and RTC.ObjectID = RD.TypeID
GO

alter view [dbo].[SiteNavAvailable] as
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
	) u
	left join SiteNav v on v.Object = u.Object and v.ObjectID = u.ID
	where v.ObjectID is null
GO

alter view [dbo].[SiteNavFlat] as

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
	) u
GO


alter procedure [bulkload].[BusinessLineage]
--declare
	@id int
--set @id = 237
as
begin
	set nocount on;

	declare @r int,
			@dt datetime = getutcdate(),
			@ActionColumn int = 1,
			@SourceIntersectTypeColumn int = 2,
			@SourceSubjectColumn int = 3,
			@SourceObjectColumn int = 4,
			@SourceFusionConfigColumn int = 5,
			@SourceFusionAttributeColumn int = 6,
			@TargetIntersectTypeColumn int = 7,
			@TargetSubjectColumn int = 8,
			@TargetObjectColumn int = 9,
			@TargetFusionConfigColumn int = 10,
			@TargetFusionAttributeColumn int = 11,
			@TransformationColumn int = 12

	select	@r = UpdatedBy from [Load] where ID = @id

	--Set the default Action to Add if blank or NULL.
	update	LoadItemColumn
	set		Value = 'Add'
	where	LoadID = @id and ColumnIndex = @ActionColumn and (Value is null or Value = '')

	exec bulkload.UpdateIntersectTypeColumn @id, @SourceIntersectTypeColumn																		-- source intersect type
	exec bulkload.UpdateIntersectTypeColumn @id, @TargetIntersectTypeColumn																		-- target intersect type

	exec bulkload.UpdateItemColumnByIntersectType @id, @SourceIntersectTypeColumn, 1, @SourceSubjectColumn		-- source subject
	exec bulkload.UpdateItemColumnByIntersectType @id, @SourceIntersectTypeColumn, 0, @SourceObjectColumn		-- source object
	exec bulkload.UpdateItemColumnByIntersectType @id, @TargetIntersectTypeColumn, 1, @TargetSubjectColumn		-- target subject
	exec bulkload.UpdateItemColumnByIntersectType @id, @TargetIntersectTypeColumn, 0, @TargetObjectColumn		-- target object

	exec bulkload.UpdateFusionConfigurationColumn @id, @SourceFusionConfigColumn																-- source fusion config
	exec bulkload.UpdateFusionConfigurationColumn @id, @TargetFusionConfigColumn																-- target fusion config

	exec bulkload.UpdateFusionAttributeColumn @id, @SourceFusionConfigColumn, @SourceFusionAttributeColumn										-- source fusion attribute
	exec bulkload.UpdateFusionAttributeColumn @id, @TargetFusionConfigColumn, @TargetFusionAttributeColumn										-- target fusion attribute

	drop table if exists #RemoveItems
	drop table if exists #AddItems
--select * from #RemoveItems
	BEGIN TRANSACTION [Tran1]

	BEGIN TRY
		-- HANDLE THE REMOVEs

		-- Load Temp table that we are going to work from
		select	SS.RowIndex,
		
				SIT.LookupObjectID as SourceIntersectTypeID,
				SS.LookupObject as SourceSubject,
				SS.LookupObjectID as SourceSubjectID,
				SO.LookupObject as SourceObject,
				SO.LookupObjectID as SourceObjectID,

				TIT.LookupObjectID as TargetIntersectTypeID,
				TS.LookupObject as TargetSubject,
				TS.LookupObjectID as TargetSubjectID,
				[TO].LookupObject as TargetObject,
				[TO].LookupObjectID as TargetObjectID,

				SI.ID as SourceIntersectID,
				TI.ID as TargetIntersectID,
				M.ID as MapItemID,

				MRI.ID as MapRuleItemID,

				cast(0 as bit) as Status,
				cast('' as nvarchar(500)) as StatusMessage,

				@r as ResourceID  --THE USER THAT ADDED THE LOAD
		into	#RemoveItems
		from	LoadItemColumn SS
				inner join LoadItemColumn SO	on SO.LoadID = SS.LoadID	and SO.RowIndex = SS.RowIndex 	and SS.ColumnIndex = @SourceSubjectColumn 	and SO.ColumnIndex = @SourceObjectColumn
				inner join LoadItemColumn SA	on SA.LoadID = SS.LoadID	and SA.RowIndex = SS.RowIndex 	and SA.ColumnIndex = @ActionColumn and SA.Value = 'Remove'
				inner join LoadItemColumn SIT	on SIT.LoadID = SS.LoadID	and SIT.RowIndex = SS.RowIndex 	and SIT.ColumnIndex = @SourceIntersectTypeColumn
				left join [Intersect] SI		on SIT.LookupObject = 'IntersectType' and SI.IntersectTypeID = SIT.LookupObjectID 
												and SI.Subject = SS.LookupObject and SI.SubjectID = SS.LookupObjectID 
												and SI.Object = SO.LookupObject and SI.ObjectID = SO.LookupObjectID

				inner join LoadItemColumn TS 	on TS.LoadID = SS.LoadID 	and TS.RowIndex = SS.RowIndex	and TS.ColumnIndex = @TargetSubjectColumn
				inner join LoadItemColumn [TO]	on [TO].LoadID = SS.LoadID	and [TO].RowIndex = SS.RowIndex	and [TO].ColumnIndex = @TargetObjectColumn
				inner join LoadItemColumn TIT	on TIT.LoadID = SS.LoadID	and TIT.RowIndex = SS.RowIndex 	and TIT.ColumnIndex = @TargetIntersectTypeColumn
				left join [Intersect] TI		on TIT.LookupObject = 'IntersectType' and TI.IntersectTypeID = TIT.LookupObjectID 
												and TI.Subject = TS.LookupObject and TI.SubjectID = TS.LookupObjectID 
												and TI.Object = [TO].LookupObject and TI.ObjectID = [TO].LookupObjectID

				left join MapItem M				on M.SourceIntersectID = SI.ID and M.TargetIntersectID = TI.ID

				left join LoadItemColumn SFA	on SFA.LoadID = SS.LoadID	and SFA.RowIndex = SS.RowIndex 	and SFA.ColumnIndex = @SourceFusionAttributeColumn
				left join LoadItemColumn TFA	on TFA.LoadID = SS.LoadID	and TFA.RowIndex = SS.RowIndex 	and TFA.ColumnIndex = @TargetFusionAttributeColumn
				left join MapRuleItem MRI		on	SFA.LookupObject = 'FusionAttribute' and MRI.SourceFusionAttributeID = SFA.LookupObjectID and
													TFA.LookupObject = 'FusionAttribute' and MRI.TargetFusionAttributeID = TFA.LookupObjectID

		where	SS.LoadID = @id


		-- Add indexes to temp table
		CREATE NONCLUSTERED INDEX [IX_TempRemoveItems_MapItem] ON #RemoveItems ( MapItemID ASC )
		CREATE NONCLUSTERED INDEX [IX_TempRemoveItems_MapRuleItem] ON #RemoveItems ( MapRuleItemID ASC )
		CREATE NONCLUSTERED INDEX [IX_TempRemoveItems_SourceIntersect] ON #RemoveItems ( SourceIntersectID ASC )
		CREATE NONCLUSTERED INDEX [IX_TempRemoveItems_TargetIntersect] ON #RemoveItems ( TargetIntersectID ASC )

		/*	BEGIN: REMOVE TECHNICAL MAPPINGS THAT ARE TIED TO FOUND MAP ITEMS */
		declare @mapRuleItems table(MapRuleItemID int, MapRuleID int)
		insert into @mapRuleItems
			select	T.MapRuleItemID,
					TJ.MapRuleID
			from	MapRuleItemMapItem T
					inner join #RemoveItems S on S.MapItemID = T.MapItemID
					left join MapRuleItemMapRule TJ on TJ.MapRuleItemID = T.MapRuleItemID

		delete	T
		from	MapRuleItemMapItem T
				inner join @mapRuleItems S on S.MapRuleItemID = T.MapRuleItemID

		delete	T
		from	MapRuleItemMapRule T
				inner join @mapRuleItems S on S.MapRuleItemID = T.MapRuleItemID

		delete	T
		from	MapRule T
				inner join @mapRuleItems S on S.MapRuleID = T.ID
				left join MapRuleItemMapRule NTJ on NTJ.MapRuleID = S.MapRuleID and NTJ.MapRuleItemID <> S.MapRuleItemID	--get all map rules that are used only once.
		where	NTJ.MapRuleID is null
		/*	END: REMOVE TECHNICAL MAPPINGS THAT ARE TIED TO FOUND MAP ITEMS */

		/*	BEGIN: REMOVE TECHNICAL MAPPING OPTIONALLY SPECIFIED IF NOT TIED ANYWHERE ELSE */
		declare @mapRuleItemIDs table(MapRuleItemID int)
		insert into @mapRuleItemIDs
			select	S.MapRuleItemID
			from	#RemoveItems S
					left join MapRuleItemMapItem J on J.MapRuleItemID = S.MapRuleItemID
			where	S.MapRuleItemID is not null;

		delete	T
		from	MapRuleItem T
				inner join @mapRuleItemIDs S on S.MapRuleItemID = T.ID;

		/*	END: REMOVE TECHNICAL MAPPING OPTIONALLY SPECIFIED IF NOT TIED ANYWHERE ELSE */

		/*	BEGIN: MAPPINGS FOUND MAP ITEMS */
		declare @mapItems table(MapItemID int, MapID int)
		insert into @mapItems
			select	S.MapItemID,
					J.MapID
			from	#RemoveItems S
					left join MapItemMap J on J.MapItemID = S.MapItemID;

		delete	T
		from	MapItemMap T
				inner join @mapItems S on S.MapItemID = T.MapItemID;

		delete	T
		from	MapSequence T
				inner join @mapItems S on S.MapItemID = T.MapItemID;

		delete	T
		from	MapItem T
				inner join @mapItems S on S.MapItemID = T.ID;

		delete	T
		from	MapRule T
				inner join @mapRuleItems S on S.MapRuleID = T.ID
				left join MapRuleItemMapRule NTJ on NTJ.MapRuleID = S.MapRuleID and NTJ.MapRuleItemID <> S.MapRuleItemID	--get all map rules that are used only once.
		where	NTJ.MapRuleID is null;
		/*	END: REMOVE FOUND MAP ITEMS */

		/*	BEGIN: REMOVE SOURCE AND TARGET INTERSECTS THAT ARE NOT REFERENCED ANYWHERE ELSE */
		delete	T
		from	[Intersect] T
				inner join #RemoveItems S on (S.SourceIntersectID = T.ID or S.TargetIntersectID = T.ID)
				left join MapItem CSM on CSM.SourceIntersectID = T.ID
				left join MapItem CTM on CTM.TargetIntersectID = T.ID
				left join [Intersect] CI on (CI.Subject = 'Intersect' and CI.SubjectID = T.ID) or (CI.Object = 'Intersect' and CI.ObjectID = T.ID)
		where	CSM.ID is null and 
				CTM.ID is null and
				CI.ID is null;
		/*	BEGIN: REMOVE SOURCE INTERSECTS THAT ARE NOT REFERENCED ANYWHERE ELSE */

		-- update status & status message for Items table
		
		-- SUCCESS STATUS
		update	T
		set		T.Status = 1,
				T.StatusMessage = coalesce(T.StatusMessage,'') + 'Business map removed. '
		from	#RemoveItems T
				left join MapItem S on S.ID = T.MapItemID
		where	T.MapItemID is not null and S.ID is null;

		update	T
		set		T.StatusMessage = coalesce(T.StatusMessage,'') + 'Source relationship removed. '
		from	#RemoveItems T
				left join [Intersect] S on S.ID = T.SourceIntersectID
		where	T.SourceIntersectID is not null and S.ID is null;

		update	T
		set		T.StatusMessage = coalesce(T.StatusMessage,'') + 'Target relationship removed. '
		from	#RemoveItems T
				left join [Intersect] S on S.ID = T.TargetIntersectID
		where	T.TargetIntersectID is not null and S.ID is null;

		-- FAILED STATUS
		update	T
		set		T.Status = 0,
				T.StatusMessage = coalesce(T.StatusMessage,'') + 'Could not find source relationship. '
		from	#RemoveItems T
		where	SourceIntersectID is null;

		update	T
		set		T.Status = 0,
				T.StatusMessage = coalesce(T.StatusMessage,'') + 'Could not find target relationship. '
		from	#RemoveItems T
		where	TargetIntersectID is null;

		update	T
		set		T.Status = 0,
				T.StatusMessage = coalesce(T.StatusMessage,'') + 'Could not find business map. '
		from	#RemoveItems T
		where	MapItemID is null;


		-- Now update LoadItems on original Load with status and messages created above
		update	T
		set		T.Status = S.Status,
				T.StatusMessage = S.StatusMessage,
				T.Object = case S.Status
							when 1 then 'MapItem'
							else NULL
						   end,
				T.ObjectID = case S.Status
							when 1 then S.MapItemID
							else NULL
						   end
		from	LoadItem T
				inner join #RemoveItems S on T.LoadID = @id and S.RowIndex = T.RowIndex;



		-- NOW HANDLE THE ADDs ---------------------------------------------------------------------------

		-- Load Temp table that we are going to work from
		select	SS.RowIndex,
		
				SIT.LookupObjectID as SourceIntersectTypeID,
				SS.LookupObject as SourceSubject,
				SS.LookupObjectID as SourceSubjectID,
				SO.LookupObject as SourceObject,
				SO.LookupObjectID as SourceObjectID,

				TIT.LookupObjectID as TargetIntersectTypeID,
				TS.LookupObject as TargetSubject,
				TS.LookupObjectID as TargetSubjectID,
				[TO].LookupObject as TargetObject,
				[TO].LookupObjectID as TargetObjectID,

				SFA.LookupObjectID as SourceFusionAttributeID,
				SFA.Value as SourceFusionAttributeRaw,
				TFA.LookupObjectID as TargetFusionAttributeID,
				TFA.Value as TargetFusionAttributeRaw,

				SI.ID as SourceIntersectID,
				TI.ID as TargetIntersectID,
				M.ID as MapItemID,
				MRI.ID as MapRuleItemID,

				SIFT.ID as SourceFusionIntersectTypeID,
				TIFT.ID as TargetFusionIntersectTypeID,
				SIF.ID as SourceFusionIntersectID,
				TIF.ID as TargetFusionIntersectID,

				cast(null as bit) as Status,
				cast('' as nvarchar(500)) as StatusMessage,

				@r as ResourceID  --THE USER THAT ADDED THE LOAD
		into	#AddItems
		from	LoadItemColumn SS
				inner join LoadItemColumn SO	on SO.LoadID = SS.LoadID	and SO.RowIndex = SS.RowIndex 	and SS.ColumnIndex = @SourceSubjectColumn 	and SO.ColumnIndex = @SourceObjectColumn
				inner join LoadItemColumn SA	on SA.LoadID = SS.LoadID	and SA.RowIndex = SS.RowIndex 	and SA.ColumnIndex = @ActionColumn and SA.Value = 'Add'
				inner join LoadItemColumn SIT	on SIT.LoadID = SS.LoadID	and SIT.RowIndex = SS.RowIndex 	and SIT.ColumnIndex = @SourceIntersectTypeColumn
				left join [Intersect] SI		on SIT.LookupObject = 'IntersectType' and SI.IntersectTypeID = SIT.LookupObjectID 
												and SI.Subject = SS.LookupObject and SI.SubjectID = SS.LookupObjectID 
												and SI.Object = SO.LookupObject and SI.ObjectID = SO.LookupObjectID

				inner join LoadItemColumn TS 	on TS.LoadID = SS.LoadID 	and TS.RowIndex = SS.RowIndex	and TS.ColumnIndex = @TargetSubjectColumn
				inner join LoadItemColumn [TO]	on [TO].LoadID = SS.LoadID	and [TO].RowIndex = SS.RowIndex	and [TO].ColumnIndex = @TargetObjectColumn
				inner join LoadItemColumn TIT	on TIT.LoadID = SS.LoadID	and TIT.RowIndex = SS.RowIndex 	and TIT.ColumnIndex = @TargetIntersectTypeColumn
				left join [Intersect] TI		on TIT.LookupObject = 'IntersectType' and TI.IntersectTypeID = TIT.LookupObjectID 
												and TI.Subject = TS.LookupObject and TI.SubjectID = TS.LookupObjectID 
												and TI.Object = [TO].LookupObject and TI.ObjectID = [TO].LookupObjectID

				left join MapItem M				on M.SourceIntersectID = SI.ID and M.TargetIntersectID = TI.ID

				left join LoadItemColumn SFA	on SFA.LoadID = SS.LoadID	and SFA.RowIndex = SS.RowIndex 	and SFA.ColumnIndex = @SourceFusionAttributeColumn
				left join LoadItemColumn TFA	on TFA.LoadID = SS.LoadID	and TFA.RowIndex = SS.RowIndex 	and TFA.ColumnIndex = @TargetFusionAttributeColumn

				left join MapRuleItem MRI		on	SFA.LookupObject = 'FusionAttribute' and MRI.SourceFusionAttributeID = SFA.LookupObjectID and
													TFA.LookupObject = 'FusionAttribute' and MRI.TargetFusionAttributeID = TFA.LookupObjectID

				left join FusionAttribute SFAO	on SFA.LookupObject = 'FusionAttribute' and SFAO.ID = SFA.LookupObjectID 
				outer apply (
						SELECT  MIN(ID) as ID
						FROM    IntersectType
						WHERE   Subject = 'IntersectType' and SubjectID = SIT.LookupObjectID and Object = 'FusionAttributeType' and ObjectID = SFAO.FusionAttributeTypeID
				) SIFT
				left join [Intersect] SIF		on	SIF.IntersectTypeID = SIFT.ID 
													and SIF.Subject = 'Intersect' and SIF.SubjectID = SI.ID
													and SIF.Object = SFA.LookupObject and SIF.ObjectID = SFA.LookupObjectID

				left join FusionAttribute TFAO	on TFA.LookupObject = 'FusionAttribute' and TFAO.ID = TFA.LookupObjectID 
				outer apply (
						SELECT  MIN(ID) as ID
						FROM    IntersectType
						WHERE   Subject = 'IntersectType' and SubjectID = TIT.LookupObjectID and Object = 'FusionAttributeType' and ObjectID = TFAO.FusionAttributeTypeID
				) TIFT
				left join [Intersect] TIF		on	TIF.IntersectTypeID = TIFT.ID 
													and TIF.Subject = 'Intersect' and TIF.SubjectID = TI.ID
													and TIF.Object = TFA.LookupObject and TIF.ObjectID = TFA.LookupObjectID

		where	SS.LoadID = @id

		-- Add indexes to temp table
		CREATE NONCLUSTERED INDEX [IX_SourceBusinessIntersect] ON #AddItems ( SourceIntersectTypeID ASC, SourceSubject ASC, SourceSubjectID ASC, SourceObject ASC, SourceObjectID ASC )
/*
update LoadItemColumn set Value = 'Bloomberg LP/Back Office Data License' where LoadID =  270 and RowIndex = 2 and ColumnIndex = 4
select * from LoadItemColumn where LoadID = 270
select * from #AddItems
select * from LoadItem where LoadID = 270

select I.LoadID, I.RowIndex, case I.[Status] when 1 then 'Complete' when 0 then 'Failed' else 'Queued' end as [Status], I.StatusMessage
from LoadItem I
where I.LoadID = 270
order by I.RowIndex
*/
		-- ERROR OUT THE ROWS THAT DO NOT HAVE THE APPROPRIATE FUSION INTERSECT TYPE IDs.
		update	#AddItems
		set		Status = 0,
				StatusMessage = coalesce(StatusMessage,'') +
								IIF(SourceFusionIntersectTypeID is null, 'Could not find source fusion relationship type. ', '') + 
								IIF(SourceFusionAttributeID is null, 'Could not find source fusion path. ', '') + 
								IIF(TargetFusionIntersectTypeID is null, 'Could not find target fusion relationship type. ', '') + 
								IIF(TargetFusionAttributeID is null, 'Could not find target fusion path. ', '')
		where	(SourceFusionAttributeRaw is not null and SourceFusionIntersectTypeID is null) OR (TargetFusionAttributeRaw is not null and TargetFusionIntersectTypeID is null);

		-- ERROR OUT THE ROWS THAT DO NOT HAVE THE APPROPRIATE SOURCEs.
		update	#AddItems
		set		Status = 0,
				StatusMessage = coalesce(StatusMessage,'') +
								IIF(SourceSubjectID is null, 'Could not find source subject. ', '') + 
								IIF(SourceObjectID is null, 'Could not find source object. ', '')
		where	(SourceSubjectID is null) OR (SourceObjectID is null);

		-- ERROR OUT THE ROWS THAT DO NOT HAVE THE APPROPRIATE TARGETs.
		update	#AddItems
		set		Status = 0,
				StatusMessage = coalesce(StatusMessage,'') +
								IIF(TargetSubjectID is null, 'Could not find target subject. ', '') + 
								IIF(TargetObjectID is null, 'Could not find target object. ', '')
		where	(TargetSubjectID is null) OR (TargetObjectID is null);




		/*	BEGIN: SOURCE BUSINESS INTERSECT LOGIC */

		-- insert source business relationships
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	SourceIntersectTypeID, 
					SourceSubject, SourceSubjectID, 
					SourceObject, SourceObjectID,
					0, ResourceID, @dt, ResourceID, @dt
			from	(
					select		SourceIntersectTypeID, SourceSubject, SourceSubjectID, SourceObject, SourceObjectID, ResourceID
					from		#AddItems
					where		Status is null 
								and SourceIntersectID is null
					group by	SourceIntersectTypeID, SourceSubject, SourceSubjectID, SourceObject, SourceObjectID, ResourceID
					) O


		-- update rows with existing source business intersect
		update	T
		set		T.SourceIntersectID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Source business relationship created.'
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.SourceIntersectTypeID 
											and T.SourceSubject = S.Subject and T.SourceSubjectID = S.SubjectID 
											and T.SourceObject = S.Object and T.SourceObjectID = S.ObjectID
											and T.SourceIntersectID is null
											and T.Status is null;
		
		-- update rows with existing target business intersect
		update	T
		set		T.TargetIntersectID = S.ID
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.TargetIntersectTypeID 
											and T.TargetSubject = S.Subject and T.TargetSubjectID = S.SubjectID 
											and T.TargetObject = S.Object and T.TargetObjectID = S.ObjectID
											and T.TargetIntersectID is null
											and T.Status is null;

		/*	END: SOURCE BUSINESS INTERSECT LOGIC */


		/*	BEGIN: TARGET BUSINESS INTERSECT LOGIC */

		-- insert target business relationships
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	TargetIntersectTypeID, 
					TargetSubject, TargetSubjectID, 
					TargetObject, TargetObjectID,
					0, ResourceID, @dt, ResourceID, @dt
			from	(
					select		TargetIntersectTypeID, TargetSubject, TargetSubjectID, TargetObject, TargetObjectID, ResourceID
					from		#AddItems
					where		Status is null 
								and TargetIntersectID is null
					group by	TargetIntersectTypeID, TargetSubject, TargetSubjectID, TargetObject, TargetObjectID, ResourceID
					) O

		-- update rows with existing target business intersect
		update	T
		set		T.TargetIntersectID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Target business relationship created.'
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.TargetIntersectTypeID 
											and T.TargetSubject = S.Subject and T.TargetSubjectID = S.SubjectID 
											and T.TargetObject = S.Object and T.TargetObjectID = S.ObjectID
											and T.TargetIntersectID is null
											and T.Status is null;

		/*	END: TARGET BUSINESS INTERSECT LOGIC */


		/*	BEGIN: SOURCE TECHNICAL INTERSECT LOGIC */

		-- insert source technical relationships
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	SourceFusionIntersectTypeID, 
					'Intersect', SourceIntersectID, 'FusionAttribute', SourceFusionAttributeID,
					0, ResourceID, @dt, ResourceID, @dt
			from	(
					select		SourceFusionIntersectTypeID, SourceIntersectID, SourceFusionAttributeID, ResourceID
					from		#AddItems
					where		Status is null
								and SourceFusionIntersectTypeID is not null
								and SourceFusionIntersectID is null
								and SourceIntersectID is not null
								and SourceFusionAttributeID is not null
					group by	SourceFusionIntersectTypeID, SourceIntersectID, SourceFusionAttributeID, ResourceID
					) O;

		-- update rows with new source technical intersect
		update	T
		set		T.SourceFusionIntersectID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Source technical relationship created.'
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.SourceFusionIntersectTypeID 
											and S.Subject = 'Intersect' and S.SubjectID = T.SourceIntersectID 
											and S.Object = 'FusionAttribute' and S.ObjectID = T.SourceFusionAttributeID
											and T.SourceFusionIntersectID is null 
											and T.Status is null;

		-- update rows with new target technical intersect
		update	T
		set		T.TargetFusionIntersectID = S.ID
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.TargetFusionIntersectTypeID 
											and S.Subject = 'Intersect' and S.SubjectID = T.TargetIntersectID 
											and S.Object = 'FusionAttribute' and S.ObjectID = T.TargetFusionAttributeID
											and T.TargetFusionIntersectID is null 
											and T.Status is null;

		/*	END: SOURCE TECHNICAL INTERSECT LOGIC */


		/*	BEGIN: TARGET TECHNICAL INTERSECT LOGIC */
		
		-- insert target technical relationships
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	TargetFusionIntersectTypeID, 
					'Intersect', TargetIntersectID, 'FusionAttribute', TargetFusionAttributeID,
					0, ResourceID, @dt, ResourceID, @dt
			from	(
					select		TargetFusionIntersectTypeID, TargetIntersectID, TargetFusionAttributeID, ResourceID
					from		#AddItems
					where		Status is null
								and TargetFusionIntersectTypeID is not null
								and TargetFusionIntersectID is null
								and TargetIntersectID is not null
								and TargetFusionAttributeID is not null			
					group by	TargetFusionIntersectTypeID, TargetIntersectID, TargetFusionAttributeID, ResourceID
					) O;

		-- update rows with new target technical intersect
		update	T
		set		T.TargetFusionIntersectID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Target technical relationship created.'
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.TargetFusionIntersectTypeID 
											and S.Subject = 'Intersect' and S.SubjectID = T.TargetIntersectID 
											and S.Object = 'FusionAttribute' and S.ObjectID = T.TargetFusionAttributeID
											and T.TargetFusionIntersectID is null 
											and T.Status is null;

		/*	END: TARGET TECHNICAL INTERSECT LOGIC */

		-- insert new map items
		insert into MapItem (SourceIntersectID, TargetIntersectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	distinct
					SourceIntersectID, 
					TargetIntersectID,
					ResourceID,
					@dt, 
					ResourceID,
					@dt
			from	#AddItems
			where	SourceIntersectID is not null 
					and TargetIntersectID is not null 
					and MapItemID is null
					and Status is null;

		-- update source data with newly created map item IDs
		update	T
		set		T.MapItemID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Business map created.'
		from	#AddItems T
				inner join [MapItem] S on	S.SourceIntersectID = T.SourceIntersectID 
											and S.TargetIntersectID = T.TargetIntersectID 
											and T.MapItemID is null 
											and T.Status is null;

		-- insert new map rule items
		insert into MapRuleItem (SourceFusionAttributeID, TargetFusionAttributeID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	distinct
					SourceFusionAttributeID, 
					TargetFusionAttributeID,
					ResourceID,
					@dt, 
					ResourceID,
					@dt
			from	#AddItems
			where	SourceIntersectID is not null 
					and TargetIntersectID is not null
					and SourceFusionAttributeID is not null 
					and TargetFusionAttributeID is not null
					and Status is null;

		-- update source data with newly created map rule item IDs
		update	T
		set		T.MapRuleItemID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Technical map created.'
		from	#AddItems T
				inner join [MapRuleItem] S on	S.SourceFusionAttributeID = T.SourceFusionAttributeID 
												and S.TargetFusionAttributeID = T.TargetFusionAttributeID 
												and T.MapRuleItemID is null 
												and Status is null;

		-- MERGE MapRuleItemMapItem with all the IDs above
		merge	MapRuleItemMapItem as T
		using	(
				select		MapItemID, 
							MapRuleItemID
				from		#AddItems
				where		MapItemID is not null
							and MapRuleItemID is not null
				group by	MapItemID, 
							MapRuleItemID
				) as S
		on		T.MapRuleItemID = S.MapRuleItemID and T.MapItemID = S.MapItemID
		when	not matched by target then
				insert (MapRuleItemID, MapItemID)
				values (S.MapRuleItemID, S.MapItemID);

		
		-- CALCULATE STATUS BASED ON POPULATED IDs
		update	#AddItems
		set		Status = 1
		where	MapItemID is not null 
				and (
					(SourceFusionAttributeRaw is not null and TargetFusionAttributeRaw is not null and MapRuleItemID is not null) 
					or 
					(SourceFusionAttributeRaw is null and TargetFusionAttributeRaw is null)
				);

		-- Now update LoadItems on original Load with status and messages created above
		update	T
		set		T.Status = S.Status,
				T.StatusMessage = S.StatusMessage,
				T.Object = case S.Status
							when 1 then 'MapItem'
							else NULL
						   end,
				T.ObjectID = case S.Status
							when 1 then S.MapItemID
							else NULL
						   end
		from	LoadItem T
				inner join #AddItems S on T.LoadID = @id and S.RowIndex = T.RowIndex;


--select *,  case [Status] when 1 then 'Complete' when 0 then 'Failed' else 'Queued' end as [Status] from LoadItem where LoadID = 270

		-- NOW, Close out the Load job ----------------------------------------------------------------------------------
		update	LoadItem
		set		Status = cast(0 as bit),
				StatusMessage = 'Incomplete : ' + coalesce(StatusMessage,''),
				Object = null,
				ObjectID = null
		where	LoadID = @id and Status is null;

		update	[Load]
		set		DateCompleted = getutcdate()
		where	ID = @id;

		COMMIT TRANSACTION [Tran1]
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION [Tran1]
		select ERROR_MESSAGE()
		update	[Load]
		set		Notes = Notes + '<br/> ' + ERROR_MESSAGE()
		where	ID = @id;
	END CATCH
end
GO

alter procedure [bulkload].[GetLoadColumns]
--declare	
	@action varchar(2),-- = 'P', --P = Promotion, R = Relation, O = Responsibilities, BL = Business Lineage, TL = Technical Lineage
	@type varchar(50),-- = 'ArtifactType',--'ArtifactType',--'IntersectType',--'ArtifactType',
	@id int,-- = 33,
	@getLookups bit = 1
as
begin
	declare @fields table (ID int identity, FieldTypeID int, Name nvarchar(250), Required bit, PartOfKey bit, AllowMultipleValues bit, IsLookup bit)
	declare @lookups table (ID int identity, FieldID int, Value nvarchar(max))
	declare @current int = 1,
			@max int,
			@isLookup bit = 0,
			@fieldTypeID int

	if @action = 'O'
	begin
		--	insert into @fields 
			--	select	-1, 'Owner Type', 1, 1, 1

		/*	insert into @lookups
				select	-1,
						'Glossary: ' + Name from ArtifactType order by Name

			insert into @lookups
				select	-1,
						'Model: ' + Name from TaxonomyType order by Name

			insert into @lookups
				select	-1,
						'Policy: ' + Name from PolicyType order by Name*/

		--	insert into @fields 
			--	select	0, 'Owner ID', 1, 1, 0

			insert into @fields 
				select	1, 'Responsibility', 1, 1, 0, 1

			insert into @lookups
				select	1,
						Name from ResponsibilityType order by Name

			insert into @fields 
				select	2, 'Resource', 1, 1, 0, 1

			insert into @lookups
				select	2,
						'User:' + email from reporting.Global_Resource order by email

			insert into @lookups
				select	2,
						'Group:' + Name from [Group] order by Name

			
			begin
				insert into @fields
					select		0,
								'Asset ID', 
								1,
								1,
								0,
								0	
			end
	end

	if @action = 'P'
	begin
		if @type = 'AttributeType'
		begin
			insert into @fields 
				select	-1, 'Owner Type', 1, 1, 0, 1

			insert into @lookups
				select	-1,
						'Glossary: ' + Name from ArtifactType order by Name
			insert into @lookups
				select	-1,
						'Model: ' + Name from TaxonomyType order by Name

			insert into @fields 
				select	0, 'Owner ID', 1, 1, 0, 0
		end --AttributeType

		if @type = 'IntersectType'
		begin
			declare @s varchar(50),
					@sid int,
					@o varchar(50),
					@oid int

			select	@s = Subject,
					@sid = SubjectID,
					@o = Object,
					@oid = ObjectID
			from	IntersectType
			where	ID = @id


			if @s = 'TaxonomyType'
			begin
				insert into @fields
					select	0, 
							'Subject Path', 
							1, 
							1, 
							0,
							0
			end
			else
			begin
				insert into @fields
					select	FT.ID, 
							'Subject ' + FT.Name, 
							FT.IsRequired, 
							FT.IsPartOfKey, 
							FT.AllowMultipleValues,
							case FT.Type
								when 'Lookup' then cast(1 as bit)
								else cast(0 as bit)
							end as IsLookup
					from	FieldType FT 
					where	FT.IsPartOfKey = 1 and FT.Object = @s and FT.ObjectID = @sid
			end

			if @s = 'TaxonomyType'
			begin
				insert into @fields
					select	0, 
							'Object Path', 
							1, 
							1, 
							0,
							0
			end
			else
			begin
				insert into @fields
					select	FT.ID, 
							'Object ' + FT.Name, 
							FT.IsRequired, 
							FT.IsPartOfKey, 
							FT.AllowMultipleValues,
							case FT.Type
								when 'Lookup' then cast(1 as bit)
								else cast(0 as bit)
							end as IsLookup
					from	FieldType FT 
					where	FT.IsPartOfKey = 1 and FT.Object = @o and FT.ObjectID = @oid
			end

		end --IntersectType

		if @type = 'ArtifactType'
		begin
			declare @parentTypeID int = null,
					@parentTypeName nvarchar(250) = null
			
			/*select	@parentTypeID = T.ParentID,
					@parentTypeName = P.Name
			from	ArtifactType T 
					inner join ArtifactType P on P.ID = T.ParentID
			where	T.ID = @id*/

			select 
				@parentTypeID = I.SubjectID,
				@parentTypeName = I.SubjectName
			from 
				intersecttypedetail I                
			where I.[PredicateType] = 3 and [Object] = @type and ObjectID = @id;

			if @parentTypeID is not null
			begin
				insert into @fields 
					values(	0, 
							@parentTypeName, 
							cast(1 as bit), 
							cast(1 as bit), 
							cast(0 as bit),
							cast(1 as bit) );
				
				insert into @lookups
					select	(select id from @fields where fieldtypeid = 0), DisplayValue from AssetDetail where typeid = @parentTypeID and [object] = 'Artifact' order by DisplayValue;

			end
		end --ArtifactType

		if @type = 'ReferenceItemType'
		begin
			insert into @fields values (0, 'Code', 1, 1, 0, 0)
		end --ReferenceItemType

		if @type = 'TaxonomyType'
		begin
			declare @initialDepth int = 1,
					@maxDepth int = 1
			select @maxDepth = MaximumDepth from TaxonomyType where ID = @id
			declare @levels table (Value int)
			while  @initialDepth <= @maxDepth
			begin
				insert into @levels values (@initialDepth)
				set @initialDepth = @initialDepth + 1
			end

			insert into @fields 
				select	FT.ID, 
						case
							when TTL.Name is not null then TTL.Name + ' ' + FT.Name
							else 'Level ' + cast(L.Value as nvarchar)  + ' ' + FT.Name
						end, 
						FT.IsRequired, 
						FT.IsPartOfKey, 
						FT.AllowMultipleValues,
						case FT.Type
							when 'Lookup' then cast(1 as bit)
							else cast(0 as bit)
						end as IsLookup
				from	@levels L 
						inner join FieldType FT on FT.IsPartOfKey = 1 and FT.Object = @type and FT.ObjectID = @id
						left join TaxonomyTypeLevel TTL on TTL.[Level] = L.Value and TaxonomyTypeID = @id
		end --TaxonomyType		
	end -- P	
	else if (@action = 'R' or @action = 'U')
	begin
		--relate / unrelate
		print 'relate / unrelate'
				
		-- look up the intersect type and get the source / target type
		
		declare @subjectType varchar(50),
				@subjectTypeName nvarchar(500),
				@subjectTypeID int,
				@objectType varchar(50),
				@objectTypeName nvarchar(500),
				@objectTypeID int
		select	@subjectType = Subject,
				@subjectTypeName = SubjectName,
				@subjectTypeID = SubjectID,
				@objectTypeName = ObjectName,
				@objectType = Object,
				@objectTypeID = ObjectID
		from	IntersectTypeDetail
		where	ID = @id
		

		-- if its a fusion attribute type we just use the name

		-- get the key fields for the target / source		

		if @objectType = 'FusionAttributeType' or @objectType = 'IntersectType'
		begin
			insert into @fields values (0, @objectTypeName, 1, 1, 0, 0)
		end		
		else
		begin
			--select * from fieldtype where [object] = 'ArtifactType' and objectid = 1 and IsPartOfKey = 1
			insert into @fields
				select		0,
							@objectTypeName + ' Asset ID', 
							1,
							1,
							0,
							0				
		end

		if @subjectType = 'FusionAttributeType' or @subjectType = 'IntersectType'
		begin
			insert into @fields values (0, @subjectTypeName, 1, 1, 0, 0)
		end
		else
		begin
			insert into @fields
				select		0,
							@subjectTypeName + ' Asset ID', 
							1,
							1,
							0,
							0				
		end
	end -- R or U

	-- fields on the item
	if (@action = 'R' or @action = 'P')
	begin
		insert into @fields
			select		ID,
						Name, 
						IsRequired,
						IsPartOfKey,
						AllowMultipleValues,
						case Type
							when 'Lookup' then cast(1 as bit)
							else cast(0 as bit)
						end as IsLookup
			from		FieldType 
			where		Object = @type 
						and ObjectID = @id 
						and Type not in ('Attribute', 'ComplexRelationLookup', 'FieldFromRelationship', 'FilteredLookup', 'FusionLookup', 'OwnershipLookup', 'RefListRelationship')
						and ( (@type = 'IntersectType' and IsPartOfKey = 0) OR (@type = 'TaxonomyType' and IsPartOfKey = 0) OR (@type <> 'TaxonomyType') )
						and IsEditable = 1
			order by	ColumnOrder
		
		select @max = max(ID) from @fields

		while @current <= @max
		begin
			select	@isLookup = IsLookup, 
					@fieldTypeID = FieldTypeID
			from	@fields 
			where	ID = @current

			if @isLookup = 1 and @getLookups = 1
			begin
				insert into @lookups
					select		@current,
								[Text]
					from		FieldLookupValue
					where		FieldTypeID = @fieldTypeID
					order by	[Text]
			end
			
			set @current = @current + 1
		end
	end

	
	if @action = 'BL'
	begin

			insert into @fields values (-4, 'Action', 1, 0, 0, 1)
			insert into @fields values (-2, 'Source Relation', 1, 1, 0, 1)
			insert into @fields values (0, 'Source Subject Asset ID', 1, 1, 0, 0)
			insert into @fields values (0, 'Source Object Asset ID', 1, 1, 0, 0)
			insert into @fields values (-3, 'Source Fusion Configuration', 0, 0, 0, 1)
			insert into @fields values (0, 'Source Fusion Path', 0, 0, 0, 0)

			insert into @fields values (-2, 'Target Relation', 1, 1, 0, 1)
			insert into @fields values (0, 'Target Subject Asset ID', 1, 1, 0, 0)
			insert into @fields values (0, 'Target Object Asset ID', 1, 1, 0, 0)
			insert into @fields values (-3, 'Target Fusion Configuration', 0, 0, 0, 1)
			insert into @fields values (0, 'Target Fusion Path', 0, 0, 0, 0)

			insert into @fields values (0, 'Transformation', 0, 0, 0, 0)

			insert into @lookups values (-4, 'Add')
			insert into @lookups values (-4, 'Remove')

			insert into @lookups
				select		-1,
							Name 
				from		TaxonomyType 
				order by	Name

			insert into @lookups
				select		-2,
							Name 
				from		IntersectType
				where		IsSystem = 0
				order by	Name

			insert into @lookups
				select		-3,
							Name 
				from		Fusion
				order by	Name
	end

	if @action = 'TL'
	begin
		insert into @fields values (-1, 'Source Fusion Configuration', 0, 0, 0, 1)
		insert into @fields values (0, 'Source Fusion Path', 0, 0, 0, 0)

		insert into @fields values (-1, 'Target Fusion Configuration', 0, 0, 0, 1)
		insert into @fields values (0, 'Target Fusion Path', 0, 0, 0, 0)

		insert into @fields values (0, 'Group', 0, 0, 0, 0)

		insert into @lookups
			select		-1,
						Name 
			from		Fusion
			order by	Name
	end

	--Return the data
	select	Name,
			Required,
			PartOfKey,
			AllowMultipleValues,
			IsLookup,
			(
			select	Value
			from	@lookups
			where	FieldID = F.ID
			for json path
			) as Lookups
	from	@fields F
	for json path
end
GO

create FUNCTION [dbo].[GetAssetKeyHashById](
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
	where a.assettypeid = @id
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
	where a.assettypeid = @id
	group by	A.AssetTypeID, A.ID

)
GO

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

	-- Process hashes for Load Items
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
													coalesce(IC.[Value],'') as [Value] 
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
											SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
											2) as KeyHash
							from		(
										select		top 100 percent
													I.RowIndex,
													FT.ID as FieldTypeID,
													coalesce(IC.Value, '') as Value
										from		LoadItem I
													inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
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
	
	-- Resolve Single-value LOOKUP fields
	exec [bulkload].[UpdateDynamicLookupFieldColumns] @id

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

	-- Resolve RELATIONSHIP fields
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

	update	T
	set		T.Object = A.Object,
			T.ObjectID = A.ObjectID
	from	LoadItem T
			inner join AssetType ST on ST.Object = @Object and ST.ObjectID = @ObjectID
			cross apply GetAssetKeyHashById(ST.ID) S 
			inner join Asset A on A.ID = S.ID
	where S.KeyHash = T.KeyHash and T.LoadID = @id
	
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
go

ALTER PROCEDURE [bulkload].[Unrelate]
--declare 
	@id int --= 297
AS
BEGIN
	SET NOCOUNT ON;


	declare @r int,
			@intersectTypeID int,
			@subjectHasSubjectArea bit,
			@subject varchar(50),
			@subjectID int,
			@objectHasSubjectArea bit,
			@object varchar(50),
			@objectID int,
			@dt datetime = getutcdate(),
			@columnCount int

	select	@r = UpdatedBy,
			@intersectTypeID = ObjectID
	from	[Load] 
	where	[Action] = 'U'
			and ID = @id

	select	@columnCount = count(1) from LoadColumn where LoadID = @id

	select	@subject = Subject,
			@subjectID = SubjectID,
			@object = Object,
			@objectID = ObjectID
	from	IntersectType
	where	ID = @intersectTypeID

	if @subject = 'ArtifactType'
		begin
			set @subjectHasSubjectArea = 1
			exec bulkload.UpdateSubjectAreaColumn @id, 1							-- subject subject area
			exec bulkload.UpdateItemColumnByType @id, @subject, @subjectID, 1, 2	-- subject
		end
	else
		begin
			set @subjectHasSubjectArea = 0
			exec bulkload.UpdateItemColumnByType @id, @subject, @subjectID, 0, 1	-- subject
		end

	if @object = 'ArtifactType'
		begin
			set @objectHasSubjectArea = 1

			if @subjectHasSubjectArea = 1
				begin
					exec bulkload.UpdateSubjectAreaColumn @id, 3							-- object subject area
					exec bulkload.UpdateItemColumnByType @id, @object, @objectID, 3, 4		-- object
				end
			else
				begin 
					exec bulkload.UpdateSubjectAreaColumn @id, 2							-- object subject area
					exec bulkload.UpdateItemColumnByType @id, @object, @objectID, 2, 3		-- object
				end
		end
	else
		begin
			set @objectHasSubjectArea = 0

			if @subjectHasSubjectArea = 1
				begin
					exec bulkload.UpdateItemColumnByType @id, @object, @objectID, 0, 3		-- object
				end
			else
				begin 
					exec bulkload.UpdateItemColumnByType @id, @object, @objectID, 0, 2		-- object
				end
		end

	drop table if exists #Items

	BEGIN TRANSACTION [Tran1]

	BEGIN TRY
		-- Load Temp table that we are going to work from
		select	S.RowIndex,
		
				S.LookupObject as Subject,
				S.LookupObjectID as SubjectID,

				O.LookupObject as Object,
				O.LookupObjectID as ObjectID,
				
				cast(0 as int) as IntersectID,

				cast(0 as bit) as Status,
				cast('' as nvarchar(500)) as StatusMessage,

				@r as ResourceID  --THE USER THAT ADDED THE LOAD
		into	#Items
		from	LoadItemColumn S
				inner join LoadItemColumn O on O.LoadID = S.LoadID 
											and O.RowIndex = S.RowIndex 
											and S.LoadID = @id
											and S.ColumnIndex = case 
												when @subjectHasSubjectArea = 1 then 2
												else 1
											end
											and O.ColumnIndex = case
																	when @subjectHasSubjectArea = 1 and @objectHasSubjectArea = 1 then 4
																	when @subjectHasSubjectArea = 1 and @objectHasSubjectArea = 0 then 3
																	when @subjectHasSubjectArea = 0 and @objectHasSubjectArea = 1 then 3
																	else 2
																end			

		-- Add indexes to temp table
		CREATE NONCLUSTERED INDEX [IX_Intersect] ON #Items ( Subject ASC, SubjectID ASC, Object ASC, ObjectID ASC )
--select * from #Items

		-- update rows with existing intersects
		update	T
		set		T.IntersectID = S.ID
		from	#Items T
				inner join [Intersect] S on S.IntersectTypeID = @intersectTypeID 
										and T.Subject = S.Subject 
										and T.SubjectID = S.SubjectID 
										and T.Object = S.Object 
										and T.ObjectID = S.ObjectID;

		-- delete relationships
		declare @tbl table (ID int)

		insert into @tbl
			select IntersectID from #Items where IntersectID > 0

		insert into @tbl
			select ID from [Intersect] where Subject = 'Intersect' and SubjectID in (select IntersectID from #Items where IntersectID > 0)

		insert into @tbl
			select ID from [Intersect] where Object = 'Intersect' and ObjectID in (select IntersectID from #Items where IntersectID > 0)

		-- Delete anywhere that the intersect is used.
		delete Field where ObjectType = 'Intersect'and ObjectID in (select ID from @tbl)
		delete [Attribute] where ObjectType = 'Intersect'and ObjectID in (select ID from @tbl)
		delete MapRuleItemMapItem where MapItemID in (
			select	M.ID 
			from	MapItem M
					inner join @tbl I on (I.ID = M.TargetIntersectID) OR (I.ID = M.SourceIntersectID)
		)
		delete MapItemMap where MapItemID in (
			select	M.ID 
			from	MapItem M
					inner join @tbl I on (I.ID = M.TargetIntersectID) OR (I.ID = M.SourceIntersectID)
		)
		delete	MapItem 
		where	SourceIntersectID in (select ID from @tbl)
				or TargetIntersectID in (select ID from @tbl)

		-- now delete the Intersects.
		delete [Intersect] where ID in (select ID from @tbl)

		-- SUCCESS STATUS
		update	#Items
		set		Status = 1,
				StatusMessage = 'Relationship removed. '
		where	IntersectID > 0;

		-- FAILED STATUS
		update	T
		set		T.Status = 0,
				T.StatusMessage = T.StatusMessage +
								'Relationship could not be removed. ' + 
								IIF(T.SubjectID is null, 'Could not find subject. ', '') + 
								IIF(T.ObjectID is null, 'Could not find object. ', '')
		from	#Items T
		where	IntersectID = 0;

		-- Now update LoadItems on original Load with status and messages created above
		update	T
		set		T.Status = S.Status,
				T.StatusMessage = S.StatusMessage,
				T.Object = case S.Status
							when 1 then 'Intersect'
							else NULL
						   end,
				T.ObjectID = case S.Status
							when 1 then S.IntersectID
							else NULL
						   end
		from	LoadItem T
				inner join #Items S on T.LoadID = @id and S.RowIndex = T.RowIndex;

		-- Now perform audit
		declare @current int = 2,
				@max int,
				@s varchar(50),
				@sid int,
				@o varchar(50),
				@oid int,
				@intersect int,
				@ct varchar(25)
		select	@max = max(Rowindex) from #Items

		while @current <= @max
		begin
			select	@s = Subject,
					@sid = SubjectID,
					@o = Object,
					@oid = ObjectID,
					@intersect = IntersectID,
					@ct = 'Delete'
			from	#items
			where	RowIndex = @current

			if @intersect > 0
			begin
				exec utility.AddAuditEntry @s, @sid, @r, @dt, @ct, 'Intersect', @intersect
				exec utility.AddAuditEntry @o, @oid, @r, @dt, @ct, 'Intersect', @intersect
			end

			set @current = @current + 1
		end

		-- Close out the Load job
		update	[Load]
		set		DateCompleted = getutcdate()
		where	ID = @id;

		COMMIT TRANSACTION [Tran1]
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION [Tran1]
	END CATCH
END
GO

alter procedure [bulkload].[UpdateDynamicLookupFieldColumns]
	@id int	
as
begin
	set nocount on;
	declare @startColumnIndex int = 0;
	declare @endColumnIndex int = 0;

	-- Artifact lookups
	update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case 
									when ( (L_A.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType in ('Artifact', 'ArtifactType')) ) then 'Artifact'									
									else NULL
								end as LookupObject,
								case 
									when L_A.ID is not null then L_A.ID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0
									else NULL
								end as LookupObjectID --coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_L.Value, L_T.ID) as LookupObjectID -- L_I.ID,
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex								
								inner join AssetDetail L_A on L_A.[Object] = 'Artifact' and L_A.TypeID = F.LookupObjectID and (L_A.DisplayValue = IC.Value)								
						where	F.AllowMultipleValues = 0 and F.LookupObjectType in ('Artifact', 'ArtifactType')
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex;

	-- Reference Item Type lookups
	update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case 									
									when ( (L_D.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'ReferenceItemType') ) then 'ReferenceItemType'									
									else NULL
								end as LookupObject,
								case 									
									when L_D.ID is not null then L_D.ID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0																		
									else NULL
								end as LookupObjectID --coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_L.Value, L_T.ID) as LookupObjectID -- L_I.ID,
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
								
								inner join ReferenceItemType L_D on L_D.[Name] = IC.Value								
						where	F.AllowMultipleValues = 0 and F.LookupObjectType = 'ReferenceItemType'
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

	-- Reference item
		update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,								
								case
									when ( (L_DI.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'ReferenceItem') ) then 'ReferenceItem'									
									else NULL
								end as LookupObject,
								case 									
									when L_DI.ID is not null then L_DI.ID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0
									else NULL
								end as LookupObjectID --coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_L.Value, L_T.ID) as LookupObjectID -- L_I.ID,
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
								inner join ReferenceItem L_DI on L_DI.ReferenceItemTypeID = F.LookupObjectID and L_DI.[DisplayValue] = IC.Value							
						where	F.AllowMultipleValues = 0 and F.LookupObjectType = 'ReferenceItem'
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

-- fusion attribute type
update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case
									when ( (L_F.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'FusionAttributeType') ) then 'FusionAttribute'									
									else NULL
								end as LookupObject,
								case 									
									when L_F.ID is not null then L_F.ID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0

									else NULL
								end as LookupObjectID --coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_L.Value, L_T.ID) as LookupObjectID -- L_I.ID,
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
								
								inner join FusionAttribute L_F on L_F.FusionAttributeTypeID = F.LookupObjectID and (L_F.[Name] = IC.Value OR L_F.TextPath = IC.Value)								
						where	F.AllowMultipleValues = 0 and F.LookupObjectType = 'FusionAttributeType'
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex
-- Lookup 

update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case 									
									when ( (L_L.Value is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'Lookup') ) then 'Lookup'									
									else NULL
								end as LookupObject,
								case 									
									when L_L.Value is not null then L_L.Value
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0

									else NULL
								end as LookupObjectID 
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
																
								left join [FieldLookupValue] L_L on F.ID = L_L.FieldTypeID and L_L.LookupObjectType = 'Lookup' and L_L.LookupObjectID = F.LookupObjectID and L_L.[Text] = IC.Value								
						where	F.AllowMultipleValues = 0 and F.LookupObjectType = 'Lookup'
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

-- taxonomy
update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case
									when ( (L_T.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel ) ) then 'Taxonomy'
									else NULL
								end as LookupObject,
								case 									
									when L_T.ID is not null then L_T.ID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0
									else NULL
								end as LookupObjectID --coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_L.Value, L_T.ID) as LookupObjectID -- L_I.ID,
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
																
								inner join AssetDetail L_T on L_T.[Object] = 'Taxonomy' and L_T.TypeID = F.LookupObjectID and (L_T.[DisplayValue] = IC.Value /*OR L_T.TextPath = IC.Value*/)
						where	F.AllowMultipleValues = 0 and F.LookupObjectType in ('Taxonomy', 'TaxonomyType')
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

	
	select @endColumnIndex = max(ColumnIndex) from LoadItemColumn where loadid = @id;

	while @startColumnIndex <= @endColumnIndex
	begin
		update	T
		set		T.StatusMessage = coalesce(T.StatusMessage, '') + S.StatusMessage
		from	LoadItem T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									case 
										when IC.LookupObjectID is null and IC.Value is not null and IC.Value <> '' then ' ' + F.Name + ' does not contain a valid value.'
										else ''
									end StatusMessage
							from	FieldType F
									inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
									inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex and IC.columnIndex = @startColumnIndex and IC.LookupObjectID is null
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex
		set @startColumnIndex = @startColumnIndex + 1
	end
end
GO

alter procedure [bulkload].[UpdateIntersectTypeColumn]
	@id int,
	@column int
as
begin
	set nocount on;
	update	T
	set		T.LookupObject = 'IntersectType',
			T.LookupObjectID = S.ID
	from	LoadItemColumn T
			inner join IntersectTypeDetail S on coalesce(S.SubjectName,'') + ' ' + coalesce(S.PredicateName,'/') + ' ' + coalesce(S.ObjectName,'') = lower(T.Value) 
				and T.ColumnIndex = @column and T.LoadID = @id
end
GO

alter procedure [bulkload].[UpdateItemColumn]
	@id int,
	@globalTypeColumn int, 
	@typeColumn int, 
	@subjectAreaColumn int, 
	@itemColumn int
as
begin
	set nocount on;
	update	T
	set		T.LookupObject = TTT.Value,
			T.LookupObjectID = coalesce(A.ID, D.ID, DI.ID, I.ID, P.ID, R.ID, TA.ID) --M.ID, 
	from	LoadItemColumn T
			inner join LoadItemColumn TT on TT.LoadID = T.LoadID and T.LoadID = @id and TT.RowIndex = T.RowIndex and TT.ColumnIndex = @typeColumn and T.ColumnIndex = @itemColumn
			inner join LoadItemColumn TS on TS.LoadID = T.LoadID and TS.RowIndex = T.RowIndex and TS.ColumnIndex = @subjectAreaColumn
			inner join LoadItemColumn TTT on TTT.LoadID = T.LoadID and TTT.RowIndex = T.RowIndex and TTT.ColumnIndex = @globalTypeColumn
			left join Artifact A on lower(A.DisplayValue) = lower(T.Value) and A.ArtifactTypeID = TT.LookupObjectID and TTT.Value = 'Artifact'
			left join ReferenceItemType D on lower(D.Name) = lower(T.Value) and TTT.Value = 'ReferenceItemType' and TT.LookupObjectID = 0
			left join ReferenceItem DI on lower(DI.DisplayValue) = lower(T.Value) and TTT.Value = 'ReferenceItem' and DI.ReferenceItemTypeID = TT.LookupObjectID
			left join [Intersect] I on lower(I.Name) = lower(T.Value) and I.IntersectTypeID = TT.LookupObjectID and TTT.Value = 'Intersect'
			--left join [Map] M on lower(M.Name) = lower(T.Value) and M.MapTypeID = TT.LookupObjectID and TTT.Value = 'Map'
			left join [Policy] P on lower(P.TextPath) = lower(T.Value) and P.PolicyTypeID = TT.LookupObjectID and TTT.Value = 'Policy'
			left join [Rule] R on lower(R.DisplayValue) = lower(T.Value) and R.RuleTypeID = TT.LookupObjectID and TTT.Value = 'Rule'
			left join [Taxonomy] TA on lower(TA.TextPath) = lower(T.Value) and TA.TaxonomyTypeID = TT.LookupObjectID and TTT.Value = 'Taxonomy'
	where	coalesce(A.ID, D.ID, DI.ID, I.ID, P.ID, R.ID, TA.ID) is not null   --M.ID, 
end
GO

alter procedure [bulkload].[UpdateItemColumnByIntersectType]
	@id int,
	@intersectTypeColumn int, 
	@isSubject bit, 
	@itemColumn int
as
begin
	set nocount on;
	update	T
	set		T.LookupObject = replace(case when @isSubject = 1 then IT.Subject else IT.Object end, 'Type', ''),
			T.LookupObjectID = A.ObjectID
	from	LoadItemColumn T
			inner join LoadItemColumn TI on TI.LoadID = T.LoadID and TI.RowIndex = T.RowIndex and TI.ColumnIndex = @intersectTypeColumn and T.ColumnIndex = @itemColumn
			inner join IntersectType IT on TI.LookupObject = 'IntersectType' and IT.ID = TI.LookupObjectID
			inner join Asset A on A.ID = cast(T.[Value] as bigint)
			inner join AssetType P on P.ID = A.AssetTypeID 
				and P.ObjectID = case when @isSubject = 1 then  IT.SubjectID else IT.ObjectID end
				and P.[Object] = case when @isSubject = 1 then IT.[Subject] else IT.[Object] end			
	where	T.LoadID = @id and A.ID is not null
end
GO

alter procedure [bulkload].[UpdateItemColumnByType]
	@id int,
	@ObjectType varchar(50), 
	@ObjectTypeID int,
	@subjectAreaColumn int, 
	@itemColumn int
as
begin
	set nocount on;

	if @ObjectType = 'ArtifactType'
	begin
			update	T
			set		T.LookupObject = replace(@ObjectType, 'Type', ''),
					T.LookupObjectID = S.ID
			from	LoadItemColumn T
					inner join LoadItemColumn TS on TS.LoadID = T.LoadID and TS.RowIndex = T.RowIndex and TS.ColumnIndex = @subjectAreaColumn
					inner join Artifact S on lower(S.DisplayValue) = lower(T.Value) and S.ArtifactTypeID = @ObjectTypeID
			where	T.LoadID = @id
	end
	if @ObjectType = 'FusionAttributeType'
	begin
		update	T
		set		T.LookupObject = replace(@ObjectType, 'Type', ''),
				T.LookupObjectID = S.ID
		from	LoadItemColumn T
				left join FusionAttribute S on lower(S.TextPath) = lower(T.Value) and S.FusionAttributeTypeID = @ObjectTypeID
		where	T.LoadID = @id
				and T.ColumnIndex = @itemColumn
	end
	if @ObjectType = 'IntersectType'
	begin
		update	T
		set		T.LookupObject = replace(@ObjectType, 'Type', ''),
				T.LookupObjectID = S.ID
		from	LoadItemColumn T
				left join [Intersect] S on lower(S.Name) = lower(T.Value) and S.IntersectTypeID = @ObjectTypeID
		where	T.LoadID = @id
				and T.ColumnIndex = @itemColumn
	end
	--if @ObjectType = 'MapType'
	--begin
	--	update	T
	--	set		T.LookupObject = replace(@ObjectType, 'Type', ''),
	--			T.LookupObjectID = S.ID
	--	from	LoadItemColumn T
	--			left join [Map] S on lower(S.Name) = lower(T.Value) and S.MapTypeID = @ObjectTypeID
	--	where	T.LoadID = @id
	--			and T.ColumnIndex = @itemColumn
	--end
	if @ObjectType = 'PolicyType'
	begin
		update	T
		set		T.LookupObject = replace(@ObjectType, 'Type', ''),
				T.LookupObjectID = S.ID
		from	LoadItemColumn T
				left join [Policy] S on lower(S.TextPath) = lower(T.Value) and S.PolicyTypeID = @ObjectTypeID
		where	T.LoadID = @id
				and T.ColumnIndex = @itemColumn
	end
	if @ObjectType = 'ReferenceItemType' and @ObjectTypeID = 0
	begin
		update	T
		set		T.LookupObject = @ObjectType,
				T.LookupObjectID = S.ID
		from	LoadItemColumn T
				left join ReferenceItemType S on lower(S.Name) = lower(T.Value)
		where	T.LoadID = @id
				and T.ColumnIndex = @itemColumn
	end
	if @ObjectType = 'ReferenceItemType' and @ObjectTypeID > 0
	begin
		update	T
		set		T.LookupObject = replace(@ObjectType, 'Type', ''),
				T.LookupObjectID = S.ID
		from	LoadItemColumn T
				left join ReferenceItem S on lower(S.DisplayValue) = lower(T.Value) and S.ReferenceItemTypeID = @ObjectTypeID
		where	T.LoadID = @id
				and T.ColumnIndex = @itemColumn
	end
	if @ObjectType = 'RuleType'
	begin
		update	T
		set		T.LookupObject = replace(@ObjectType, 'Type', ''),
				T.LookupObjectID = S.ID
		from	LoadItemColumn T
				left join [Rule] S on lower(S.DisplayValue) = lower(T.Value) and S.RuleTypeID = @ObjectTypeID
		where	T.LoadID = @id
				and T.ColumnIndex = @itemColumn
	end
	if @ObjectType = 'TaxonomyType'
	begin
		update	T
		set		T.LookupObject = replace(@ObjectType, 'Type', ''),
				T.LookupObjectID = S.ID
		from	LoadItemColumn T
				left join Taxonomy S on lower(S.TextPath) = lower(T.Value) and S.TaxonomyTypeID = @ObjectTypeID
		where	T.LoadID = @id
				and T.ColumnIndex = @itemColumn
	end
end
GO

alter procedure [bulkload].[UpdateTypeColumn]
	@id int,
	@typeColumn int,
	@typeNameColumn int
as
begin
	set nocount on;
	update	T2
	set		T2.LookupObject = T1.Value + 'Type',
			T2.LookupObjectID = coalesce(A.ID, D.ID, P.ID, T.ID, R.ID)
	from	LoadItemColumn T2
			inner join LoadItemColumn T1 on T1.LoadID = T2.LoadID and T1.RowIndex = T2.RowIndex and T1.ColumnIndex = @typeColumn and T2.LoadID = @id and T2.ColumnIndex = @typeNameColumn
			left join ArtifactType A on lower(A.Name) = lower(T2.Value) and T1.Value = 'Artifact'
			left join ReferenceItemType D on lower(D.Name) = lower(T2.Value) and T1.Value = 'ReferenceItemType'
			left join IntersectType I on lower(I.Name) = lower(T2.Value) and T1.Value = 'Intersect'
			left join PolicyType P on lower(P.Name) = lower(T2.Value) and T1.Value = 'Policy'
			left join TaxonomyType T on lower(T.Name) = lower(T2.Value) and T1.Value = 'Taxonomy'
			left join RuleType R on lower(R.Name) = lower(T2.Value) and T1.Value = 'Rule'
end
GO

ALTER PROCEDURE [dbo].[DeleteIntersect]
	@ID int,
	@ResourceID int
AS
BEGIN
	SET NOCOUNT ON;
	declare @trancount int;
    set @trancount = @@trancount;	
	
	BEGIN TRY
		if @trancount = 0
            begin transaction
        else
			save transaction DeleteIntersect

		IF NOT EXISTS(select 1 from [Intersect] where ID = @ID)
		BEGIN
			RAISERROR('Item does not exist.', 16, 1);
		END

		IF EXISTS(select 1 from [Intersect] where (Subject = 'Intersect' and SubjectID = @ID) OR (Object = 'Intersect' and ObjectID = @ID) )
		BEGIN
			RAISERROR('Item is used in other relationships.', 16, 1);
		END

		if exists(select 1 from [Attribute] where ObjectType = 'Intersect' and ObjectID = @ID)
		begin
			DELETE	[Attribute]
			WHERE	ObjectType = 'Intersect' and ObjectID = @ID
		end

		declare @oNodeID int,
				@date datetime,
				@Subject varchar(50),
				@SubjectID int,
				@Object varchar(50),
				@ObjectID int

		set @date = getutcdate()

		select	@Subject = Subject,
				@SubjectID = SubjectID,
				@Object = Object,
				@ObjectID = ObjectID
		from	[Intersect]
		where	ID = @ID

		exec utility.AddAuditEntry @Subject, @SubjectID, @ResourceID, @date, 'Removed', 'Intersect', @ID
		exec utility.AddAuditEntry @Object, @ObjectID, @ResourceID, @date, 'Removed', 'Intersect', @ID

		-- Now delete the actual record.
		delete	[Intersect]
		where	ID = @ID

		if @trancount = 0
			commit;
	END TRY
	BEGIN CATCH
		declare @message varchar(4000), @xstate int;
        select @message = ERROR_MESSAGE(), @xstate = XACT_STATE();
        if @xstate = -1
            rollback;
        if @xstate = 1 and @trancount = 0
            rollback
        if @xstate = 1 and @trancount > 0
            rollback transaction DeleteIntersect;

        raiserror ('Unable to remove relationship: %s', 16, 1, @message);
	END CATCH
END
GO

ALTER procedure [dbo].[DeleteObject]
--declare
	@ObjTemp varchar(50),
	@ObjectIDTemp int,
	@ResourceIDTemp int
--set @Obj = 'Artifact'
--set @ObjectID = 974223
--set @ResourceID = 1
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

ALTER PROCEDURE [dbo].[GetAvailableSiteNavigation]
AS
BEGIN
	SET NOCOUNT ON;

	select
		u.ID as ObjectID,
		u.Name,
		u.url as Route,
		u.Object,
		null as SortOrder,
		null as ParentID
	from
	(
		select
		ID,
		Name,
		dbo.GenerateNgObjectUrl('ArtifactType', ID, 0) As url,
		'ArtifactType' as [Object]
		FROM ArtifactType
		
		UNION ALL
		
		SELECT
		ID,
		Name,
		dbo.GenerateNgObjectUrl('TaxonomyType', ID, 0)  As url,
		'TaxonomyType' as [Object]
		FROM TaxonomyType
		
		UNION ALL
		
		SELECT
		ID,
		Name,
		dbo.GenerateNgObjectUrl('PolicyType', ID, 0)  As url,
		'PolicyType' as [Object]
		FROM PolicyType
	) u
	left join SiteNav v on v.Object = u.Object and v.ObjectID = u.ID
	where v.ObjectID is null 
END
GO

ALTER PROCEDURE [dbo].[GetAverageScoreByObjectType]
--declare
	@type varchar(50),-- = 'Artifact',
	@id int-- = 733
AS
begin
	declare 
			@oName nvarchar(250),
			@oTypeName nvarchar(250),
			@oType varchar(50),
			@oID int,
			@AveragePoints int,
			@MaxPoints int,
			@AverageScore int,
			@ObjectScore varchar(250)--int

	select	@oName = utility.GetAssetDisplayValue(A.ID),
			@oTypeName = T.Name,
			@oType = T.Object,
			@oID = T.ObjectID
	from	Asset A
			inner join AssetType T on T.ID = A.AssetTypeID  and A.[Object] = @type and A.ObjectID = @id

	select	@ObjectScore = cast(Value * 100 as int)
	from	metrics.Score
	where	getutcdate() between EffectiveStartDate and EffectiveEndDate
			and Object = @type and ObjectID = @id

	select	@AverageScore = avg(cast(Value * 100 as int))
	from	metrics.Score S
			inner join Asset A on A.Object = S.Object and A.ObjectID = S.ObjectID
			inner join AssetType T on T.ID = A.AssetTypeID and T.Object = @oType and T.ObjectID = @oID
	where	getutcdate() between S.EffectiveStartDate and S.EffectiveEndDate

	select	@type as [Object], @id as ObjectID, @oName as ObjectName, @ObjectScore as ObjectScore, 
			@oType as ObjectType, @oID as ObjectTypeID, @oTypeName as ObjectTypeName, @AverageScore as AverageScore 
end
GO

ALTER PROCEDURE [dbo].[GetCommentCategoriesByFollower]
	@FollowingResourceID int
AS
BEGIN	
	SELECT		CR.ObjectID,
				CR.ObjectType,
				O.Name,
				O.ObjectTypeName as Category
	FROM		Follow F
				INNER JOIN CommentRelation CR ON	(
													(CR.ObjectType = F.ObjectType AND CR.ObjectID = F.ObjectID) OR 
													(CR.ObjectType = 'Resource' AND CR.ObjectID = @FollowingResourceID)
													)
												 AND F.ResourceID = @FollowingResourceID
				inner join cache.ObjectDetails O on O.[Object] = CR.ObjectType and O.ObjectID = CR.ObjectID
	GROUP BY	CR.ObjectID,
				O.ObjectTypeName,
				CR.ObjectType,
				O.Name
END
GO

ALTER PROCEDURE [dbo].[GetCommentCountByFollower]
--declare
	@resourceID int,
	@dateStart datetime = null,
	@dateEnd datetime = null,
	@searchPhrase varchar(100) = ''
--set @resourceID = 1
AS
BEGIN
	SELECT	i.CommentType, 
			u.[Count], 
			u.CommentTypeName 
	FROM	(
			select	count(1) as [All],
					sum(case when c.commenttypeid = 2 then 1 else 0 end) as [Discussions],
					sum(case when c.commenttypeid = 5 then 1 else 0 end) as Issues,
					sum(case when c.commenttypeid = 6 then 1 else 0 end) as Tasks,
					sum(case when c.commenttypeid = 7 then 1 else 0 end) as [Red Flags],
					sum(case when c.commenttypeid = 8 then 1 else 0 end) as [Data Events],
					sum(case when c.commenttypeid = 9 then 1 else 0 end) as [Challenges]
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
							inner join	ResponsibilityDetails o on o.ResourceID = @resourceID and o.Object = c2.OwnerObjectType and o.ObjectID = c2.OwnerObjectID
					)
			AND C.isdeleted = 0
			AND (
					(C.DateCreated between @dateStart and @dateEnd and @dateStart is not null and @dateEnd is not null) or
					(@dateStart is null and @dateEnd is null)
				)
			AND C.ParentID is null
			AND (
				coalesce(ltrim(rtrim(@searchPhrase)),'')='' or 
				lower(Body) like lower('%'+@searchPhrase+'%')
				)
			AND case 
					when c.CreatingResourceID = @resourceID then 1
					when c.VisibilityID = 2 then 1
					when c.VisibilityID = 3 then 1
					when coalesce(c.VisibilityID, 4) = 4  then 1
					else 0
				end = 1
		) t
		UNPIVOT
			(	[Count]
				for [CommentTypeName] in ([All], Discussions, Issues, Tasks, [Red Flags], [Data Events], [Challenges])
			) u
			inner join
			(
			select	* 
			from	(
					select	0 as [All],
							2 as Discussions,
							5 as Issues,
							6 as Tasks,
							7 as [Red Flags],
							8 as [Data Events],
							9 as [Challenges]
					)	t2
						unpivot
						(
						CommentType for CommentTypeName in ([All], Discussions, Issues, Tasks, [Red Flags], [Data Events], [Challenges])
						) u2
			) i on i.CommentTypeName = u.CommentTypeName
END
GO

ALTER PROCEDURE [dbo].[GetCommentDetailByID]
	@id int
AS
BEGIN
	with i (ResourceID) 
	as
	(
		select	r.ResourceID
		from	ResponsibilityDetails r
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
				O.Name as ObjectName,				
				O.Url as ObjectUrl,
				case
					WHEN C.ParentID IS NULL THEN C.OwnerObjectType
					ELSE 'Resource'
				end as ObjectType,
				O.Name as ResourceName,
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
						CRD.IconForeColor,
						CRD.IconBackColor,
						CRD.NgUrl
				from	CommentRelation CR
				inner join cache.ObjectDetails CRD on CR.CommentID = C.ID 
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
				left join cache.ObjectDetails O on O.[Object] = C.OwnerObjectType and O.ObjectID = C.OwnerObjectID
				INNER JOIN P ON C.ID = P.ID
	ORDER BY	C.ParentID, C.DateCreated DESC
END
GO

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
							inner join	ResponsibilityDetails o on o.ResourceID = @resourceID and o.Object = c2.OwnerObjectType and o.ObjectID = c2.OwnerObjectID
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
			D.Name as ObjectName,
			D.Url as ObjectUrl,
			(
			select	CRD.Object,
					CRD.ObjectID,
					CRD.TextPath,
					CRD.ObjectTypeName,
					CRD.Url,
					CRD.IconBackColor,
					CRD.IconForeColor,
					CRD.NgUrl
			from	CommentRelation CR
					inner join cache.ObjectDetails CRD on CR.CommentID = a.ID and a.ParentID is null and CR.ObjectType = CRD.[Object] and CR.ObjectID = CRD.ObjectID
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
			left join cache.ObjectDetails D on D.[Object] = a.OwnerObjectType and D.ObjectID = a.OwnerObjectID
	where	IsVisible = 1;
END
GO

ALTER PROCEDURE [dbo].[GetCommentDetailsByType]
--declare
	@type varchar(50), 
	@id int,
	@skip int,
	@take int,
	@dateStart datetime = null,
	@dateEnd datetime = null,
	@commentTypeID int = 0,
	@searchPhrase varchar(100) = ''
--set @type = 'Artifact'
--set @id = 733
--set @skip = 0
--set @take = 100
AS
BEGIN
	SET NOCOUNT ON;

	with i (ResourceID) 
	as
	(
		select	r.ResourceID
		from	ResponsibilityDetails r
				inner join Comment c on c.OwnerObjectType = r.Object and c.OwnerObjectID = r.ObjectID and c.ID = @id
	),
	 P
	AS
	(
		SELECT		C.*,
					CASE WHEN (select count(*) from i where ResourceID = C.CreatingResourceID) > 0  THEN
						1
					WHEN (select count(*) from i where ResourceID = C.CreatingResourceID) > 0  THEN
						1
					ELSE
						0
					END as CreatorIsOwner,
					coalesce(C.OwnerObjectType, CR.ObjectType) as ObjectType,
					coalesce(C.OwnerObjectID, CR.ObjectID) as ObjectID,
					(
					select	a.[Object],
							a.ObjectID,
							utility.getassetdisplayvalue(a.id) as TextPath,
							ast.Name as ObjectTypeName,							
							os.IconForeColor,
							os.IconBackColor,
							dbo.generatengobjecturl(a.[object],ast.[objectid],a.objectid) as Url
					from	CommentRelation CR
							inner join asset a on (CR.CommentID = C.ID and a.[object] = CR.[ObjectType] and a.objectid = CR.ObjectID)
							inner join assettype ast on ( a.assettypeid = ast.id)
							inner join objectstyle os on (ast.[object] = os.[objecttype] and ast.[objectid] = os.[objectid])							
					for xml path('tag'), root('tags'), type
					) as TagsXml
		FROM		Comment C
					INNER JOIN CommentRelation CR	ON C.ID = CR.CommentID
													AND (
														coalesce(@commentTypeID,0) = 0 OR (C.CommentTypeID = @commentTypeID)
														) --in (1,2,3,7)
													AND CR.ObjectType = @type 
													AND CR.ObjectID = @id
													AND (
														(C.DateCreated between @dateStart and @dateEnd and @dateStart is not null and @dateEnd is not null) or
														(@dateStart is null and @dateEnd is null)
														)
													AND C.ParentID IS NULL	
													and c.isdeleted = 0			
		WHERE
			coalesce(ltrim(rtrim(@searchPhrase)),'')='' or (lower(Body) like lower('%'+@searchPhrase+'%')) 
		ORDER BY	C.DateCreated DESC
		OFFSET  @skip ROWS 
		FETCH NEXT @take ROWS ONLY 

		UNION ALL

		SELECT	C.*,
				0 as CreatorIsOwner, 
				cast('Resource' as varchar(50)) as ObjectType,
				C.CreatingResourceID as ObjectID,
				NULL as TagsXml
		FROM	P
				INNER JOIN Comment C ON C.ParentID = P.ID
	)

	select	P.*,
			R.FirstName + ' ' + R.LastName as ResourceName,
			R.Email as ResourceEmail,
			utility.getassetdisplayvalue(a.id),
			dbo.generatengobjecturl(a.[object],ast.[objectid],a.objectid) as ObjectUrl,
			(
				select CommentID,
						ResourceID,
						vote as VoteValue
				from commentvote
				where commentid = p.ID
					for xml path('vote'), root('votes'), type
			) as VotesXML
	from	P
			left join reporting.Global_Resource R on R.ResourceID = P.CreatingResourceID			
			left join asset a on a.[object] = p.objecttype and a.objectid = p.objectid
			left join assettype ast on a.assettypeid = ast.id
	where
		isdeleted = 0;
END
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
					where	R.Adding = 1 
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
					I.*,
					SI.IntersectTypeID as SourceIntersectTypeID,
					utility.DeriveIntersectTypeName(SIT.ID) as SourceIntersectTypeName,
					TI.IntersectTypeID as TargetIntersectTypeID,
					utility.DeriveIntersectTypeName(TIT.ID) as TargetIntersectTypeName
				from @items I
				inner join [Intersect] SI on SI.ID = I.SourceIntersectID
				inner join IntersectType SIT on SIT.ID = SI.IntersectTypeID
				inner join [Intersect] TI on TI.ID = I.TargetIntersectID
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

ALTER PROCEDURE [dbo].[GetRenderedTemplateBodyNg]-- 'Tooltip', 'Resource', 2, 'Preview'
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
	select	@typeID = C_D.ObjectTypeID,
			@icon = '<div title=''' + C_D.Name + ''' class=''tooltip-icon'' style=''background-color: ' + C_D.IconBackColor + '; color: ' + C_D.IconForeColor + '''><i class=''fa fa-' + C_D.IconText + '''></i></div>',
			@n = C_D.Name,
			@t = C_D.ObjectTypeName,
			@d = f.formattedvalue,
			@link = C_D.Url
	from	cache.objectdetails C_D			
			left join fieldtype ft on (ft.[object] = C_D.[objecttype] and ft.objectid = C_D.objecttypeid and ft.name = 'Description')
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
				select FriendlyName from FieldType where [Object] = 'LookupType' and ObjectID = @MyLookupTypeID order by SortOrder asc

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
					select top 500 ri.ID from [ReferenceItem] ri
					inner join Asset ast on (ri.id = ast.objectid and ast.[object] = 'ReferenceItem')
					inner join [intersect] id on (id.objectid = ri.id and id.[object] = 'ReferenceItem')
					inner join [intersect] id_2 on (id_2.[object] = 'ReferenceItem' and id_2.[objectid] = @id and id_2.subjectid = id.subjectid)
					inner join [intersecttypedetail] it on (it.id = id.intersecttypeid and it.id = id_2.intersecttypeid and it.[object]='ReferenceItemType' and it.predicatetype = 3)
					left join AssetWithoutReadPermission RP on RP.ResourceID = @resourceId and RP.AssetID = ast.ID  
					where ri.ReferenceItemTypeID = @MyRefTypeID and ast.[State] = 1 and RP.AssetID is null
					order by DisplayValue asc
				end
			else
				begin
				insert into @refs 
					select top 500 ri.ID from [ReferenceItem] ri
					inner join Asset ast on (ri.id = ast.objectid and ast.[object] = 'ReferenceItem')
					left join AssetWithoutReadPermission RP on RP.ResourceID = @resourceId and RP.AssetID = ast.ID  
					where ri.ReferenceItemTypeID = @MyRefTypeID and ast.[State] = 1 and RP.AssetID is null
					order by DisplayValue asc
				end
		
			declare @refFieldTypes table (ID int identity, Name nvarchar(250))
			insert into @refFieldTypes values ('Code')
			insert into @refFieldTypes
				select FriendlyName from FieldType where [Object] = 'ReferenceItemType' and ObjectID = @MyRefTypeID order by SortOrder asc

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
						O.DisplayValue,
						L.Name as LevelName,
						dbo.GenerateNgObjectUrl('Artifact', L.ID, O.ID) as Url,
						1 as [Level]
				from	Artifact O
						inner join ArtifactType L on L.ID = O.ArtifactTypeID
				where	O.ID = @ID
				union all
				select	O.ID,
						O.ParentID,
						O.DisplayValue,
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
			--declare @e nvarchar(500), @fn nvarchar(250), @ln nvarchar(250)
			--select	@e = Email, @fn = FirstName, @ln = LastName
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

			insert into @stats 
				select		G.Name + ': ' + I.Name,
							MR.Value
				from		metrics.Score S
							inner join metrics.MapResult MR on MR.ScoreID = S.ID and S.EffectiveEndDate = '12/31/9999' and S.Object = @Type and S.ObjectID = @ID
							inner join metrics.Map M on M.ID = MR.MapID
							inner join metrics.[Group] G on G.ID = M.GroupID
							inner join metrics.Item I on I.ID = M.ItemID
				order by	G.Name + ': ' + I.Name

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

	if exists (select 1 from AssetWithoutReadPermission arp where arp.resourceid = @resourceId and arp.[object] = @Type and arp.objectid = @ID)
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
GO

ALTER PROCEDURE [dbo].[GetScoreHistoryByObject]-- 'Artifact', 733
--declare
	@type varchar(50),-- = 'Artifact',
	@id int-- = 4651
AS
begin
	--declare @DateStart date, 
	--		@DateEnd date

	--select	@DateEnd = max(Date),
	--		@DateStart = DATEADD(d, -30, max(Date))
	--from	Score
	--where	Object = @type 
	--		and ObjectID = @id
	--		and ScoreTypeID = 1
	
	select	EffectiveStartDate as [Date],
			cast(Value * 100 as int) as Score
	from	metrics.Score
	where	Object = @type 
			and ObjectID = @id
	union
	select	cast(getutcdate() as date) as [Date],
			cast(Value * 100 as int) as Score
	from	metrics.Score
	where	getutcdate() between EffectiveStartDate and EffectiveEndDate
			and Object = @type and ObjectID = @id
end
GO

ALTER PROCEDURE [dbo].[GetSiteNavigation]
(
	@ResourceID int = 0
)
AS
BEGIN
	SET NOCOUNT ON;

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		NULL AS Items	
FROM SiteNav n
WHERE n.Name = '#Monitor' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1
UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		NULL AS Items		
FROM SiteNav n
WHERE n.Name = '#Home' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1
UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(		
			select
				FAT.name,
				AUrl.[Url] as [url],
				0 as feature,		
				dbo.ArtifactNgSiteNavigation(fat.id) as items
					from	    ArtifactType FAT					
						inner join AssetType T on T.Object = 'ArtifactType' and T.ObjectID = FAT.ID					
						cross apply [dbo].[GetAssetUrl]('ArtifactType', FAT.ID, 0) AUrl
						left join SiteNav v on v.ObjectID = FAT.ID and v.Object = 'ArtifactType'
					where 
						not exists  (
							select	IT.SubjectID
							from	IntersectType IT 
									inner join [Predicate] P on IT.Object = T.Object and IT.ObjectID = FAT.ID and P.ID = IT.PredicateID and P.Type = 3
							) 	
							and v.ObjectID is null				
					FOR XML PATH('nav'), TYPE
		) AS Items
FROM SiteNav n
WHERE n.Name = '#Glossary' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1

UNION ALL


SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
			SELECT	name,
					url,
					0 as feature,
					null as items
			FROM	(
					SELECT		TOP 1000
								a.id,
								a.name,
								dbo.GenerateNgObjectUrl('TaxonomyType', a.ID, 0) As url
					FROM		TaxonomyType a
								left join SiteNav v on v.ObjectID = a.ID and v.Object = 'TaxonomyType'
					WHERE		v.ObjectID is null
					ORDER BY	a.name
					) BG
					FOR XML PATH('nav'), TYPE
		) AS Items
FROM SiteNav n
WHERE n.Name = '#Models' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1

UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
			SELECT	name,
					url,
					0 as feature,
					null as items
			FROM	(
					SELECT		TOP 1000
								a.id,
								a.name,
								dbo.GenerateNgObjectUrl('PolicyType', a.ID, 0) As url
					FROM		PolicyType a
					left join SiteNav v on v.ObjectID = a.ID and v.Object = 'PolicyType'
					WHERE		v.ObjectID is null
					ORDER BY	a.name
					) BG
					FOR XML PATH('nav'), TYPE
		) AS Items
FROM SiteNav n
WHERE n.Name = '#Policy' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1
		
UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		null AS Items
FROM SiteNav n
WHERE n.Name = '#Reference' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1

UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		2 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
		SELECT		name, 
					dbo.GenerateNgObjectUrl('FusionType', FT.ID, 0)  As url,
					2 as feature,
					(
					SELECT		name, 
								dbo.GenerateObjectUrl('Fusion', FT.ID, Fusion.ID)  As url,
								'F' + cast(Fusion.ID as varchar(15)) as menuID,
								2 as feature
					FROM		Fusion
					WHERE		Fusion.FusionTypeID = FT.ID
					ORDER BY	name
					FOR XML PATH('nav'), TYPE
					) AS items	
		FROM		FusionType FT
		ORDER BY	name
		FOR XML PATH('nav'), TYPE
		) AS Items	
	FROM SiteNav n
WHERE n.Name = '#Fusion' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1
		
UNION ALL

SELECT	n.Name as MenuID, 
		n.SortOrder,
		4 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
        SELECT	'People' AS name, --'#People' as MenuID,
                'community/groups' AS url, 		        
                0 as feature,
		        NULL AS Items
        FOR XML PATH('nav'), TYPE
        ) AS Items
FROM SiteNav n
WHERE n.Name = '#Community' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1
UNION ALL

SELECT	'#Admin' as MenuID,
		999 as SortOrder,
		0 as Feature,
		'fa-cogs' as Icon,
		'Administration' as Title,
		(
			select	*
			from	(
					SELECT	'Security' AS name, 
							'#/' AS url, 
							0 as feature,
							(
							select	*
							from	(
									SELECT	'Groups' AS name, 
											'#/groups/administration' AS url, 
											--'Menu_A_S_G' as menuID,
											0 as feature,
											NULL AS items
									union all
									SELECT	'Users' AS name, 
											'#/resources/administration' AS url, 
											--'Menu_A_S_R' as menuID,
											0 as feature,
											NULL AS items
									union all
									SELECT	'Responsibilities' AS name, 
											'#/governance/administration' AS url, 
											0 as feature,
											NULL AS items
                            ) bg
							FOR XML PATH('nav'), TYPE
							) AS items
						
					union all

					SELECT	'MetaModel' AS name, 
							'#/' AS url,
							0 as feature, 
							(
							select	*
							from	(
									SELECT	'Artifacts' AS name, 
											'#/artifacts/administration' AS url, 
											0 as feature,
											NULL AS items
									union all
									SELECT	'Attributes' AS name, 
											'#/attributes/administration' AS url, 
											0 as feature,
											NULL AS items
									union all
									SELECT	'Lookups' AS name, 
											'#/lookups/administration' AS url, 
											0 as feature,
											NULL AS items
									union all
									SELECT	'Models' AS name, 
											'#/catalogs/administration' AS url, 
											0 as feature,
											NULL AS items
                                    union all
									SELECT	'Policies' AS name, 
											'#/policies/administration' AS url, 
											1 as feature,
											NULL AS items
                                    union all
									SELECT	'Relationships' AS name, 
											'#/relations/administration' AS url, 
											0 as feature,
											NULL AS items
                                    union all
                                    SELECT	'Rules' AS name, 
											'#/rules/administration' AS url, 
											0 as feature,
											NULL AS items
									) bg
							FOR XML PATH('nav'), TYPE
							) AS items
						
					union all

					SELECT	'Metrics' AS name, 
							'#/' AS url,
							0 as feature, 
							(
							select	*
							from	(
									SELECT	'Scoring' AS name, 
											'#/analytics/administration' AS url, 
											5 as feature,
											NULL AS items
									union all
					                SELECT	'Dashboards' AS name, 
							                '#/reporting/administration' AS url, 
							                0 as feature,
							                NULL AS items
                                    union all
					                SELECT	'Surveys' AS name, 
							                '#/surveys/administration' AS url, 
							                7 as feature,
							                (
							                SELECT	'Response Types' AS name, 
									                '#/surveyresponsetypes/administration' AS url, 
									                7 as feature,
									                NULL AS items
							                FOR XML PATH('nav'), TYPE
							                ) AS items
									) bg
							FOR XML PATH('nav'), TYPE
							) AS items
						
					union all

					SELECT	'Reference' AS name, 
							'#/domains/administration' AS url, 
							0 as feature,
							NULL AS items

					union all

					SELECT	'Workflow' AS name, 
							'#/workflow/administration' AS url, 
							0 as feature,
							NULL AS items

                    union all

                    SELECT	'Templates' AS name, 
							'#/templates/administration' AS url, 
							0 as feature,
							NULL AS items

					union all

					SELECT	'Integration' AS name, 
							'#/' AS url, 
							0 as feature,
							(
							select	*
							from	(
									SELECT	'Bulk Loader' AS name, 
											'#/load' AS url, 
											0 as feature,
											NULL AS items
									union all
									SELECT	'Fusion' AS name, 
											'#/fusion/administration' AS url, 
											2 as feature,
											NULL AS items
									union all
									SELECT	'API' AS name, 
											'/swagger' AS url, 
											0 as feature,
											NULL AS items
									) bg
							FOR XML PATH('nav'), TYPE
							) AS items

                    union all

                    SELECT	'Settings' AS name, 
							'#/settings' AS url, 
							0 as feature,
							NULL AS items
            ) bg
			for xml path('nav'), type
		) as Items

	where 1 = 1

	UNION ALL

	SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
		SELECT	RT.name, 				
				dbo.GenerateNgObjectUrl('RuleType', RT.ID, RT.ID) As url,
				0 as feature,
				null AS items	
		FROM	RuleType RT
				LEFT JOIN SiteNav v on v.ObjectID = RT.ID and v.Object ='RuleType'
		WHERE	v.ObjectID IS NULL
		FOR XML PATH('nav'), TYPE
		) AS Items
	FROM SiteNav n
	WHERE n.Name = '#Data Quality' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1

	UNION ALL

	SELECT 
		'~' + Name AS MenuID,
		s.SortOrder,
		0 AS Feature,
		s.Icon as Icon,
		s.Title as Title,
		dbo.CustomSiteNavigation(ID) AS Items
	from SiteNav s
	where ParentID IS NULL and Name not like '#%' AND dbo.HasSiteNavPermission(s.ID, @ResourceID) = 1

	order by sortorder
END
GO

ALTER PROCEDURE [fusion].[ProcessFusionRelationships]
	@executionID int	
AS
BEGIN
	set NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	declare @objectType varchar(50) = 'FusionAttribute';

    -- delete any relations we already have that was already added from stagingrelation table so we dont duplicate
	delete	T
	from	fusion.StagingRelation T
			left join [Intersect] S on	S.Subject = @objectType and 
										S.Object = @objectType and
										(
											( S.SubjectID = T.StartFusionAttributeID and S.ObjectID = T.EndFusionAttributeID ) OR
											( S.SubjectID = T.EndFusionAttributeID and S.ObjectID = T.StartFusionAttributeID )
										)
	where	ExecutionID = @executionID and
			S.ID is null;
					
	Declare @IDList Table(IntersectID int, StageID Int);
			
	MERGE
		INTO    [Intersect] d
		USING   (
				SELECT	IntersectTypeID, 
						ID,
						StartFusionAttributeID,
						EndFusionAttributeID
				FROM	[fusion].stagingrelation
				where	ExecutionID = @executionID 
						and IntersectID is null
				) S
		ON      (1 = 0)
		WHEN NOT MATCHED THEN
		INSERT  (IntersectTypeID, Subject, SubjectID, Object, ObjectID)
		VALUES  (S.IntersectTypeID, @objectType, StartFusionAttributeID, @objectType, EndFusionAttributeID)
		OUTPUT  INSERTED.ID, S.ID into @IDList;
	
	--update StagingRelation to have the id's we used in intersect table.
	UPDATE	T
	SET		T.IntersectID = S.IntersectID
	from	[fusion].[StagingRelation] T
			inner join @IDList S on T.ExecutionID = @executionID and T.ID = S.StageID;
END
GO

ALTER PROCEDURE [fusion].[RelateAction]
	-- Add the parameters for the stored procedure here
	@R_Subject varchar(20), 
	@R_SubjectID int,
	@R_Object varchar(20),
	@R_ObjectID int,
	@R_IntersectTypeID int,	
	@R_IntersectID int = 0 output
AS
BEGIN
	SET NOCOUNT ON;

    -- Validate that intersect type exists.
	if exists(select 1 from IntersectType where ID = @R_IntersectTypeID)
	begin
		
		select	@R_IntersectID = ID
		from	[Intersect]
		where	Subject = @R_Subject 
			and SubjectID = @R_SubjectID 
			and Object = @R_Object 
			and ObjectID = @R_ObjectID
			and IntersectTypeID = @R_IntersectTypeID

		if @R_IntersectID is null
		begin
			if @R_IntersectTypeID is not null
			begin
				begin try
					insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
					values					(@R_IntersectTypeID, @R_Subject, @R_SubjectID, @R_Object, @R_ObjectID, 0, 0, getutcdate(), 0, getutcdate())  

					select @R_IntersectID = SCOPE_IDENTITY()
					
					exec utility.AddAuditEntry @R_Subject, @R_SubjectID, 0, getutcdate, 'Created', 'Intersect', @R_IntersectID
					exec utility.AddAuditEntry @R_Object, @R_ObjectID, 0, getutcdate, 'Created', 'Intersect', @R_IntersectID

				end try
				begin catch
					select ERROR_MESSAGE()
				end catch
			end
		end		
	end
end
GO

alter procedure [metrics].[LoadFromStaging]
as
begin
	-- 1. Remove all except the most recent staging values, grouped by date (not time).
/*
	insert into metrics.StagingResult values (21, '2/28/2018 3:11:00 PM', 100, 1, 0.95)
	insert into metrics.StagingResult values (21, '2/28/2018 4:11:00 PM', 100, 1, 0.90)
	insert into metrics.StagingResult values (21, '2/28/2018 5:11:00 PM', 100, 1, 0.91)
	insert into metrics.StagingResult values (21, '2/28/2018 6:11:00 PM', 100, 1, 0.80)
*/
	delete	T
	from	metrics.StagingResult T
			left join	(
						select	MapID,
								max(EffectiveDate) as EffectiveDate,
								AssetID
						from	metrics.StagingResult
						group by	MapID, AssetID
						) S  on S.MapID = T.MapID and S.EffectiveDate = T.EffectiveDate and S.AssetID = T.AssetID
	where	S.MapID is null;

	-- 2. Update pre-existing scores
	update	T
	set		T.Value = S.Score
	from	metrics.Score T
			inner join (
						select		cast(R.EffectiveDate as date) as EffectiveDate, A.Object, A.ObjectID, R.Score 
						from		metrics.StagingResult R
									inner join Asset A on A.ID = R.AssetID
						group by	cast(R.EffectiveDate as date), A.Object, A.ObjectID, R.Score 
						) S on S.EffectiveDate = T.EffectiveStartDate and S.Object = T.Object and S.ObjectID = T.ObjectID;


	-- 3. Insert new scores
	insert	metrics.Score
			select		A.Object, 
						A.ObjectID, 
						cast(R.EffectiveDate as date) as EffectiveDate, 
						case
							when M.EffectiveEndDate = cast('12/31/9999' as date) then M.EffectiveEndDate
							else DATEADD(d, -1, M.EffectiveEndDate)
						end as EffectiveEndDate, 
						R.Score 
			from		metrics.StagingResult R
						inner join Asset A on A.ID = R.AssetID
						outer apply	(
									select	coalesce(min(EffectiveStartDate), cast('12/31/9999' as date)) as EffectiveEndDate
									from	metrics.Score
									where	Object = A.Object and ObjectID = A.ObjectID and EffectiveStartDate > cast(R.EffectiveDate as date)
									) M
						left join metrics.Score T on T.EffectiveStartDate = cast(R.EffectiveDate as date) and T.Object = A.Object and T.ObjectID = A.ObjectID
			where		T.ID is null
			group by	R.EffectiveDate, M.EffectiveEndDate, A.Object, A.ObjectID, R.Score;

	-- 4. Merge the metric results, updating existing and adding new ones.
	merge   metrics.MapResult as T 
	using   ( 
			select  SR.MapID,
					S.ID as ScoreID,
					SR.Value
			from    metrics.StagingResult SR
					inner join Asset A on A.ID = SR.AssetID
					inner join metrics.Score S on S.Object = A.Object and S.ObjectID = A.ObjectID and S.EffectiveStartDate = cast(SR.EffectiveDate as date)
			) as S 
			on  (
				T.MapID = S.MapID and T.ScoreID = S.ScoreID
				)
	when    matched then 
			update
				set
				T.Value = S.Value
	when    not matched by target then 
			insert (MapID, ScoreID, [Value]) 
			values (S.MapID, S.ScoreID, S.Value);

	-- 5. End-date the older scores based on object and effective date comparisons.
	update	T
	set		T.EffectiveEndDate = DATEADD(d, -1, M.EffectiveStartDate)
	from	metrics.Score T
			inner join (
						select		MS.Object,
									MS.ObjectID,
									max(MS.EffectiveStartDate) as EffectiveStartDate 
						from		metrics.Score MS
									inner join (
												select		cast(R.EffectiveDate as date) as EffectiveDate, A.Object, A.ObjectID, Score 
												from		metrics.StagingResult R
															inner join Asset A on A.ID = R.AssetID
												group by	cast(R.EffectiveDate as date), A.Object, A.ObjectID, R.Score 
												) S on S.EffectiveDate = MS.EffectiveStartDate and S.Object = MS.Object and S.ObjectID = MS.ObjectID
						group by	MS.Object, 
									MS.ObjectID
						) M	on M.Object = T.Object and M.ObjectID = T.ObjectID and T.EffectiveStartDate < M.EffectiveStartDate and T.EffectiveEndDate = cast('12/31/9999' as date);

	-- 6. Clear the staging table.
	delete	SR
	from    metrics.StagingResult SR
			inner join Asset A on A.ID = SR.AssetID
			inner join metrics.Score S on S.Object = A.Object and S.ObjectID = A.ObjectID and S.EffectiveStartDate = cast(SR.EffectiveDate as date);
end
GO

alter procedure [tile].[GetObjectStatistics]
	@type varchar(50),
	@id int
AS
BEGIN
	declare @table table (Name nvarchar(250), Value varchar(250), [Group] varchar(25), Url varchar(250), MostRecent datetime, TypeID int)
	
	declare @ObjectScore varchar(250)

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
	from	metrics.Score
	where	getutcdate() between EffectiveStartDate and EffectiveEndDate
			and Object = @type and ObjectID = @id

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

alter procedure [utility].[CalculateScores]
--declare
	@Object varchar(50) = NULL,
	@ObjectID int = NULL,
	@Date date = null--'04/17/2017'
--set @Object = 'Artifact'
--set @ObjectID = 16437 --select * from Artifact where ID = 16437
as
begin
	SET NOCOUNT ON;

	if @Date is null
	begin
		set @Date = cast(getutcdate() as Date)
	end

	DROP TABLE IF EXISTS #MetricTypes

	create table #MetricTypes (
		ScoreTypeID int,
		ScoreTypeMetricID int,
		ScoreTypeMetricVersionID int,
		ObjectType varchar(50),
		ObjectTypeID int,
		CheckType int,
		Configuration xml,
		MaximumScore int,
		Object varchar(50),
		ObjectID int
	)
/*

	insert into #MetricTypes
		select	M.ScoreTypeID,
				M.ID as ScoreTypeMetricID,
				V.ID as ScoreTypeMetricVersionID,
				M.Object as ObjectType,
				M.ObjectID as ObjectTypeID,
				M.CheckType,
				M.Configuration,
				M.MaximumScore,
				A.Object,
				A.ObjectID
		from	ScoreType ST
				inner join ScoreTypeMetric M on M.ScoreTypeID = ST.ID  and M.Deleted = 0
				inner join	(
							select		ScoreTypeMetricID,
										max(IV.ID) as ID,
										max(IV.UpdatedOn) as UpdatedOn
							from		ScoreTypeMetricVersion IV
							group by	IV.ScoreTypeMetricID
							) V on V.ScoreTypeMetricID = M.ID
				inner join AssetType T on T.Object = M.Object and T.ObjectID = M.ObjectID 
				inner join Asset A on A.AssetTypeID = T.ID and ( (A.Object = @Object and A.ObjectID = @ObjectID) OR @ObjectID is null)

	DROP TABLE IF EXISTS #ScoreMetrics
	create table #ScoreMetrics (
		ScoreID bigint null,
		Object varchar(50),
		ObjectID int,
		ScoreTypeID int,
		[Date] date,
		ScoreTypeMetricVersionID int,
		MetricValue decimal(6,3),
	)

	insert into #ScoreMetrics
		select	NULL,
				T.Object,
				T.ObjectID,
				T.ScoreTypeID,
				@Date,
				T.ScoreTypeMetricVersionID,
				case 
					when T.CheckType = 1 and f.value('(ObjectType/text())[1]', 'varchar(50)') = 'AttributeType' and C1_A.ValueExists <> 0 then T.MaximumScore
					when T.CheckType = 1 and f.value('(ObjectType/text())[1]', 'varchar(50)') = 'ResponsibilityType' and C1_R.ValueExists <> 0 then T.MaximumScore
					when T.CheckType = 2 then C2.Multiplier * T.MaximumScore
					--when T.CheckType = 3 and T.ObjectType = 'ArtifactType' and C3_S.ValueExists <> 0 then T.MaximumScore
					when T.CheckType = 3 and f.value('(PropertyName/text())[1]', 'varchar(250)') <> 'Status' and C3_F.ValueExists <> 0 then T.MaximumScore
					when T.CheckType = 4 and f.value('(PropertyName/text())[1]', 'varchar(250)') = 'Description' and C4_D.ValueExists <> 0 then T.MaximumScore
					when T.CheckType = 4 and f.value('(PropertyName/text())[1]', 'varchar(250)') <> 'Description' and C4_F.ValueExists <> 0 then T.MaximumScore
					when T.CheckType = 5 and (C5_R.ValueExists <> 0 OR C5_R2.ValueExists <> 0) then T.MaximumScore
					when T.CheckType = 6 and C6_F.ValueExists <> 0 then T.MaximumScore
					when T.CheckType = 7 and C7_R.AverageScore is not null then (C7_R.AverageScore / 100) * T.MaximumScore
					when T.CheckType = 8 and C8_O.AverageScore is not null then (C8_O.AverageScore / 100) * T.MaximumScore
					when T.CheckType = 10 and C10_P.ValueExists <> 0 then T.MaximumScore
					else 0
				end as MetricValue
		from	#MetricTypes T
				cross apply Configuration.nodes('/fields') as F(f)
				outer apply (
							select		coalesce(M.Score, 0) as Multiplier
							from		TestExternalMetric M
							where		M.Object = T.[Object]
										and M.ObjectID = T.ObjectID 
										and M.MetricVersionID = T.ScoreTypeMetricVersionID
										and T.CheckType = 2
							) C2
				outer apply (
							select		ISNULL(AttributeTypeID, 0) as ValueExists
							from		Attribute 
							where		ObjectType = T.[Object] 
										and ObjectID = T.ObjectID 
										and AttributeTypeID = f.value('(ObjectID/text())[1]', 'int')
										and f.value('(ObjectType/text())[1]', 'varchar(50)') = 'AttributeType'
										and T.CheckType = 1
							group by	AttributeTypeID, ObjectType, ObjectID
							) C1_A
				outer apply (
							select		ISNULL(ResponsibilityTypeID, 0) as ValueExists
							from		[cache].[ResponsibilityItem]
							where		[Object] = T.[Object] 
										and ObjectID = T.ObjectID 
										and ResponsibilityTypeID = f.value('(ObjectID/text())[1]', 'int')
										and f.value('(ObjectType/text())[1]', 'varchar(50)') = 'ResponsibilityType'
										and T.CheckType = 1
							group by	ResponsibilityTypeID, [Object], ObjectID
							) C1_R
				outer apply (
							select		CASE 
											when F.FormattedValue = f.value('(PropertyValue/text())[1]', 'nvarchar(4000)') then 1
											else 0
										END as ValueExists									
							from		Field F
										inner join FieldType FT on FT.[Object] = T.ObjectType and FT.ObjectID = T.ObjectTypeID 
																and F.[ObjectType] = T.[Object] and F.ObjectID = T.ObjectID
																and FT.Name = f.value('(PropertyName/text())[1]', 'varchar(250)') 
																and T.CheckType = 3
							) C3_F
				outer apply (
							select		case 
											when Description is null then 0
											when LEN(Description) < 25 then 0
											else 1
										end as ValueExists
							from		cache.ObjectDetails
							where		[Object] = T.[Object] and ObjectID = T.ObjectID
										and f.value('(PropertyName/text())[1]', 'varchar(250)') = 'Description'
										and T.CheckType = 4
							) C4_D
				outer apply (
							select		CASE 
											when F.FormattedValue is not null then 1
											else 0
										END as ValueExists									
							from		Field F
										inner join FieldType FT on FT.[Object] = T.ObjectType and FT.ObjectID = T.ObjectTypeID 
																and F.[ObjectType] = T.[Object] and F.ObjectID = T.ObjectID
																and FT.Name = f.value('(PropertyName/text())[1]', 'varchar(250)') 
																and T.CheckType = 4
							) C4_F
				outer apply (
							select		case 
											when COUNT(1) > 0 then 1
											else 0
										end as ValueExists
							from		[Intersect] IR
										inner join IntersectType IRT on IRT.ID = IR.IntersectTypeID and (
																										(IR.Subject = T.Object and IR.SubjectID = T.ObjectID) OR 
																										(IR.Object = T.Object and IR.ObjectID = T.ObjectID)
																										)
										cross apply T.Configuration.nodes('/fields/CheckObjects') as R(r) 
							where		r.value('(Object/Type/text())[1]', 'varchar(50)') = case 
											when (IR.Subject = T.Object and IR.SubjectID = T.ObjectID) then IRT.Object 
											else IRT.Subject
										end
										and r.value('(Object/ID/text())[1]', 'int') = case 
											when (IR.Subject = T.Object and IR.SubjectID = T.ObjectID) then IRT.ObjectID
											else IRT.SubjectID
										end
										and T.CheckType = 5
							) C5_R
				outer apply (
							select		case 
											when COUNT(1) > 0 then 1
											else 0
										end as ValueExists
								from [Intersect] IR
								cross apply T.Configuration.nodes('/fields/CheckObjects') as R(r)
								where IR.IntersectTypeID = r.value('(IntersectType/text())[1]','varchar(50)')
								and ((IR.Subject = T.Object and IR.SubjectID = T.ObjectID) or (IR.Object = T.Object and IR.ObjectID = T.ObjectID))
								and T.CheckType = 5
							
							) C5_R2
				outer apply (
							select		ISNULL(ArtifactID, 0) as ValueExists
							from		FusionOwner
							where		ArtifactID = T.ObjectID
										and T.CheckType = 6
							group by	ArtifactID
							) C6_F
				outer apply (
							select	(sum(S.Value) / Count(1)) as [AverageScore],
									max(S.Date) as [Date] 
							from	[Intersect] I
									inner join IntersectType IT on	IT.ID = I.IntersectTypeID
																	and (
																		(I.Subject = T.[Object] and I.SubjectID = T.ObjectID) OR
																		(I.Object = T.[Object] and I.ObjectID = T.ObjectID)
																		)
																	and (
																		f.value('(ObjectType/text())[1]', 'varchar(25)') = case 
																			when (I.Subject = T.[Object] and I.SubjectID = T.ObjectID) then IT.Object
																			else IT.Subject
																		end 
																		and f.value('(ObjectID/text())[1]', 'int') = case 
																			when (I.Subject = T.[Object] and I.SubjectID = T.ObjectID) then IT.ObjectID
																			else IT.SubjectID
																		end
																		)
									left join Score S on	S.Object =	case 
																			when I.Subject = T.Object and I.SubjectID = T.ObjectID then I.Object
																			else I.Subject
																		end 
															and S.ObjectID =	case 
																					when I.Subject = T.Object and I.SubjectID = T.ObjectID then I.ObjectID
																					else I.SubjectID
																				end
															and S.ScoreTypeID = f.value('(ScoreTypeID/text())[1]', 'int')
							where	T.CheckType = 7
							) C7_R	-- ROLLUP VIA RELATIONSHIPS
				outer apply (
							select	(sum(S.Value) / Count(1)) as [AverageScore],
									max(S.Date) as [Date] 
							from	[cache].[Responsibilities] R
									left join Score S on S.Object = R.Object and S.ObjectID = R.ObjectID --and S.ScoreTypeID = f.value('(ScoreTypeID/text())[1]', 'int')
							where	T.CheckType = 8
									and R.ResponsibleObject = T.[Object] 
									and R.ResponsibleObjectID = T.ObjectID
									and R.ObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)') 
									and R.ObjectTypeID = f.value('(ObjectID/text())[1]', 'int')
							) C8_O	-- ROLLUP VIA OWNERSHIP
				outer apply (
							select	case 
										when COUNT(1) > 0 then 1
										else 0
									end as ValueExists
							from	[Intersect] I
									inner join IntersectType IT on IT.ID = I.IntersectTypeID and 
																IT.PredicateID = f.value('(Predicate/text())[1]', 'int') and 
																(
																(I.Subject = T.Object and I.SubjectID = T.ObjectID) OR
																(I.Object = T.Object and I.ObjectID = T.ObjectID)
																)
							where	T.CheckType = 10
							) C10_P	-- PREDICATE CHECK

	-- Gets results from merge statement below (OUTPUT)
	DROP TABLE IF EXISTS #Scores
	create table #Scores (ScoreID bigint, Object varchar(50), ObjectID int, ScoreTypeID int, Date date, [Action] varchar(15), CurrentScore int not null, NewScore int null)

	MERGE	Score AS T
	USING	(
			select		Object,
						ObjectID,
						ScoreTypeID,
						Date
			from		#ScoreMetrics
			group by	Object,
						ObjectID,
						ScoreTypeID,
						Date
			) AS S
	ON		(
			T.ScoreTypeID = S.ScoreTypeID
			and T.Object = S.Object
			and T.ObjectID = S.ObjectID
			and T.Date = S.Date
			)
	WHEN MATCHED THEN
		UPDATE	
		SET		T.Date = S.Date
	WHEN NOT MATCHED THEN	
		INSERT	
		VALUES	(S.Object, S.ObjectID, S.ScoreTypeID, S.Date, 0)
	OUTPUT inserted.ID, S.Object, S.ObjectID, S.ScoreTypeID, S.Date, $Action, inserted.Value, null into #Scores;

	--update the ScoreID column based on merge above.
	update	T
	set		T.ScoreID = S.ScoreID
	from	#ScoreMetrics T
			inner join #Scores S on S.Object = T.Object and S.ObjectID = T.ObjectID and S.ScoreTypeID = T.ScoreTypeID and S.Date = T.Date; 

	-- merge the results into the ScoreMetric table.
	MERGE	ScoreMetric AS T
	USING	(
			select	distinct
					ScoreID,
					ScoreTypeMetricVersionID,
					MetricValue
			from	#ScoreMetrics
			) AS S
	ON		(
			T.ScoreID = S.ScoreID
			and T.ScoreTypeMetricVersionID = S.ScoreTypeMetricVersionID
			)
	WHEN MATCHED THEN
		UPDATE	
		SET		T.Value = coalesce(S.MetricValue, 0)
	WHEN NOT MATCHED THEN	
		INSERT	
		VALUES	(S.ScoreID, S.ScoreTypeMetricVersionID, coalesce(S.MetricValue, 0));

	update	T
	set		T.Value = coalesce(S.Value, 0)
	from	Score T
	inner join	(
				select		CAST(ROUND( (SUM(MetricValue) / SUM(V.MaximumScore)) * 100, 0) as int) as Value,
							ScoreID
				from		#ScoreMetrics SM
							inner join ScoreTypeMetricVersion V on V.ID = SM.ScoreTypeMetricVersionID
				group by	ScoreID
				) S on S.ScoreID = T.ID;

	-- Now get which scores changed. 
	update	T
	set		T.NewScore = NS.Value
	from	#Scores T
			OUTER APPLY	(
						SELECT		TOP 1 
									*
						FROM		[Score]
						WHERE		Object = T.Object and ObjectID = T.ObjectID and ScoreTypeID = T.ScoreTypeID
						ORDER BY	[Date] DESC
						) NS;

	insert into [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select	'EventTopicNotification', 
				'<fields><ChangeType>ScoreUpdate</ChangeType><ObjectType>' + T.Object + '</ObjectType><ObjectTypeID>' + cast(T.ObjectID as varchar) + '</ObjectTypeID><Score>' + cast(S.NewScore as varchar) + '</Score></fields>',
				S.Object, 
				S.ObjectID
		from	#Scores S
				inner join Asset O on O.Object = S.Object and O.ObjectID = S.ObjectID
				inner join AssetType T on T.ID = O.AssetTypeID
		where	S.CurrentScore <> S.NewScore
				and S.[Action] = 'UPDATE';
*/
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

alter procedure [utility].[GetOwnersForWorkflow]
	@workflowID int,
	@workflowStepID int = 0,
	@workflowItemID int = 0
as
begin
	declare @objectId int,			
			@objectType varchar(50),
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
			select @objectType = object, @objectId = objectid from [workflow].[item] where id = @workflowItemID;
			
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
				from	ResponsibilityDetails RD
						inner join reporting.Global_Resource R on RD.Object = @objectType
								and RD.ObjectID = @objectId
								and RD.ResponsibilityTypeID = @responsibilityTypeID
								and RD.ResourceID = R.ResourceID
								and R.Email not like '%?subject=%' and R.Status = 'Active'
		end		
	end;

	-- if no one found email admins
	if not exists (select 1 from @tbl)
	begin
		insert into @tbl 
				select	R.ResourceID, 
						R.FirstName, 
						R.LastName, 
						R.Email, 
						R.Email, 
						R.DateLastLoggedIn, 
						1 as ResourceTypeID, 
						R.Status 
				from	reporting.Global_Resource R where isadministrator = 1 and status = 'Active'
	end		

	select * from @tbl;
end
GO

alter procedure [utility].[GetOwnersForWorkflowV2]
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
					R.DateLastLoggedIn, 
					1 as ResourceTypeID, 
					R.Status 
			from	ResponsibilityDetails RD
					inner join reporting.Global_Resource R on RD.Object = @objectType
							and RD.ObjectID = @objectId
							and RD.ResponsibilityTypeID = @responsibilityTypeID
							and RD.ResourceID = R.ResourceID
							and R.Email not like '%?subject=%' and R.Status = 'Active'
		end
	
	-- if noone found email admins
	if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select 
					R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from 
					reporting.Global_Resource R where isadministrator = 1 and status = 'Active'
		end
	

	select * from @tbl
end
GO

ALTER FUNCTION [utility].[ObjectDetail]
(
--declare
	@type varchar(50), 
	@id int
--set @type = 'Domain'
--set @id = 1
)
RETURNS @tbl TABLE 
(
	ID int,
	AssetID bigint,
	AssetTypeID int,
	Name nvarchar(max),
	TextPath nvarchar(2500),
	Description nvarchar(max),
	ParentID int null,
	ParentType nvarchar(250),
	Url nvarchar(2500),
	TypeID int,
	[Type] varchar(25),
	[TypeName] nvarchar(250),
	IconBackColor varchar(15),
	IconForeColor varchar(15),
	IconText varchar(15),
	Status nvarchar(25) null
) 
AS
BEGIN
	if @type = 'Artifact' or @type = 'Attribute' or @type = 'Fusion' or @type = 'FusionAttribute' or @type = 'Policy' or @type = 'ReferenceItem' or @type = 'Rule' or @type = 'Taxonomy'
	begin
		insert into @tbl (	ID,			AssetID,	AssetTypeID, Name,			TextPath,		[Description],	ParentID,	ParentType, Url,											TypeID,	[Type],	TypeName, Status)
			SELECT			ObjectID,	ID,			AssetTypeID, DisplayValue,	DisplayValue,	NULL,			null,		null,		dbo.GenerateObjectUrl(@type, TypeID, ObjectID),	TypeID,	Type,	TypeName, NULL
			FROM	AssetDetail
			where	Object = @type 
					and ObjectID = @id
	end

	if @type = 'ArtifactType' or @type = 'AttributeType' or @type = 'FusionType' or @type = 'FusionAttributeType' or @type = 'PolicyType' or @type = 'ReferenceItemType' or @type = 'RuleType' or @type = 'TaxonomyType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ObjectID,		Name,	Name,		Description,	NULL,		NULL,		turl.[url] as Url,	ObjectID,		@type,	'Asset Type'
			FROM	AssetType O
			cross apply [dbo].GetAssetUrl(@type,@id,0) turl
			WHERE	Object = @type
					and ObjectID = @id
	end

	if @type = 'Group'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	0,		@type,	'Group'
			FROM	[Group]
			WHERE	ID = @id
	end

	if @type = 'Intersect'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],				TypeName)
			SELECT			O.ID,	IName.Name,	IName.Name,		'',				NULL,		@type,		dbo.GenerateObjectUrl(@type, O.IntersectTypeID, O.ID),	O.IntersectTypeID,	'IntersectType',	T.Name
			FROM	[Intersect] O
					INNER JOIN IntersectType T ON O.IntersectTypeID = T.ID and O.ID = @id
					CROSS APPLY dbo.GetIntersectNames(O.ID) IName	
	end

	if @type = 'IntersectType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		T.Name,	T.Name,		'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Intersect Type'
			FROM	IntersectType 
			CROSS APPLY dbo.GetIntersectTypeNames(@id) T	
			WHERE	ID = @id
	end

	if @type = 'IssueType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],				TypeName)
			SELECT			O.ID,	O.Name,	O.Name,		O.Description,				NULL,		@type,		NULL,	O.ID,	'IssueType',	'Issue Type'
			FROM	IssueType O
			WHERE	ID = @id
	end

	if @type = 'Lookup'
	begin
		insert into @tbl (	ID,		Name,				TextPath,	[Description],	ParentID,	ParentType, Url,												TypeID,			[Type],			TypeName)
			SELECT			O.ID,	T.Name + ' Item',	T.Name,		'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, O.LookupTypeID, O.ID),	O.LookupTypeID,	'LookupType',	T.Name
			FROM	[Lookup] O
					INNER JOIN LookupType T ON O.LookupTypeID = T.ID AND O.ID = @id
	end

	if @type = 'LookupType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		'',				0,			@type,		dbo.GenerateObjectUrl(@type, ID, 0),	ID,		@type,	'Lookup Type'
			FROM	LookupType O
			WHERE	ID = @id
	end

	if @type = 'FusionQueryAttribute'
	begin
		insert into @tbl (	ID,		Name,		TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,						[Type],					TypeName)
			SELECT			O.ID,	O.DisplayValue,	O.DisplayValue,	'',				NULL,	@type,		dbo.GenerateObjectUrl(@type, 0, O.ID),
																											O.FusionQueryAttributeTypeID,	'FusionQueryAttributeType',	T.Name
			FROM	FusionQueryAttribute O
					INNER JOIN FusionQueryAttributeType T ON O.FusionQueryAttributeTypeID = T.ID and O.ID = @id					
	end
	
	if @type = 'FusionQueryAttributeType'
	begin
		insert into @tbl (	ID, Name,		TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,	O.Name,	O.Name,	'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Fusion Query Attribute Type'
			FROM	FusionQueryAttributeType O
			WHERE	ID = @id
	end

	if @type = 'Report'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,				[Type],			TypeName)
			SELECT			O.ID,	O.Name,	O.Name,	O.Description,	NULL,		@type,		'#',	0,	'Report',	'Report'
			FROM	Report O
			WHERE	O.ID = @id
	end

	if @type = 'Resource'
	begin
		insert into @tbl (ID, Name, Url, TypeID, [Type], TypeName)
			select	ResourceID, FirstName + ' ' + LastName, dbo.GenerateObjectUrl(@type, 1, @id), 1, 'ResourceType', 'Employee'
			from	reporting.Global_Resource 
			where	ResourceID = @id
	end

	if @type = 'ResponsibilityType'
	begin
		insert into @tbl (	ID, Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,	O.Name,	NULL,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Responsibility Type'
			FROM	ResponsibilityType O
			WHERE	ID = @id
	end

	if @type = 'ResourceType'
	begin
		insert into @tbl (ID, Name, Url, TypeID, [Type], TypeName)
		values			(@id, 'Resource Type', '#/resources/administration', @id, @type, 'Resource Type')
	end

	if @type = 'RuleImplementation'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,				[Type],			TypeName, Status)
			SELECT			O.ID,	coalesce(O.Name,'Implementation ' + cast(o.id as nvarchar)) ,	coalesce(O.Name,'Implementation ' + cast(o.id as nvarchar)),	null,	T.ID,		'Rule',		dbo.GenerateObjectUrl(@type, T.ID, O.ID),	T.RuleTypeID,	'RuleType',	T.DisplayValue, 'Active'
			FROM	[RuleImplementation] O
					inner join [Rule] T on T.ID = O.RuleID
			WHERE	O.ID = @id
	end

	if @type = 'ShoppingCart'
	begin
			insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			O.ID,		Name,	Name,		NULL,	NULL,		NULL,		dbo.GenerateObjectUrl('ShoppingCartType', O.ShoppingCartTypeID, O.ID),	O.ID,		@type,	T.Name
			FROM	ShoppingCart O
			inner join ShoppingCartType T on O.ShoppingCartTypeID = T.ID
			WHERE	O.ID = @id
	end

	update	T
	set		T.IconBackColor = coalesce(S.IconBackColor, '#000000'),
			T.IconForeColor = coalesce(S.IconForeColor, '#ffffff'),
			T.IconText =	--case @type
							--	when 'Taxonomy' then 'IM'
							--	when 'TaxonomyType' then 'IM'
								--else 
								COALESCE(S.IconText, 'leaf') 
							--end
	from	@tbl T
			left join ObjectStyle S ON S.ObjectType = T.[Type] and S.ObjectID = T.TypeID

	RETURN
END
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
			dbo.ArtifactNgSiteNavigation(id) as items
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
					where IT.SubjectID = @id
			--		) A
			) BG
			FOR XML PATH('nav'), TYPE
	)
END
GO

ALTER FUNCTION [dbo].[CustomSiteNavigation]
(
	@id int
)
RETURNS XML
WITH RETURNS NULL ON NULL INPUT
AS
BEGIN
	 RETURN 
    (
        SELECT  COALESCE(a.Name,v.Name) as name --v.name
                , v.[Route] AS url
				, 0 as feature,
				case when v.Object = 'ArtifactType' then
					dbo.ArtifactNgSiteNavigation(a.id)
				when v.Object = 'TaxonomyType' or v.Object = 'PolicyType' then
					null
				else
					[dbo].CustomSiteNavigation(v.id)
				end as items
        FROM    dbo.SiteNav v
		left join artifacttype a on a.id = v.objectID and v.Object = 'ArtifactType'
        WHERE   v.ParentID = @id order by sortorder
        FOR XML PATH('nav'),TYPE
    )
END
GO

ALTER FUNCTION [dbo].[GenerateNgObjectUrl] 
(
	@Type varchar(50),
	@TypeID int,
	@ObjectID int = 0
)
RETURNS varchar(500)
AS
BEGIN
	DECLARE @Prefix varchar(5) = ''--'a/'
	DECLARE @Url varchar(500)
	SET @Url = @Prefix

	SET @Url = CASE @Type
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
	END

	SET @Url = @Prefix + @Url

	RETURN @Url
END
GO

ALTER FUNCTION [dbo].[GenerateObjectUrl] 
(
	@Type varchar(50),
	@TypeID int,
	@ObjectID int = 0
)
RETURNS varchar(500)
AS
BEGIN
	DECLARE @Prefix varchar(5) = ''--'a/'
	DECLARE @Url varchar(500)
	SET @Url = @Prefix

	SET @Url = CASE @Type
		WHEN 'Artifact' THEN 'artifact/' +  + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)
		WHEN 'ArtifactType' THEN 'artifact/' + CAST(@TypeID as varchar)
		WHEN 'Domain' THEN 'domain/' +  + CAST(@TypeID as varchar) + '/' +  + CAST(@ObjectID as varchar)
		WHEN 'DomainType' THEN 'domain/' + CAST(@TypeID as varchar)
		WHEN 'ReferenceItem' THEN 'reference/' +  + CAST(@TypeID as varchar)-- + '/' +  + CAST(@ObjectID as varchar)
		WHEN 'ReferenceItemType' THEN 'reference/' + CAST(@TypeID as varchar)
		WHEN 'FusionAttribute' THEN 'fusion/fusionattribute/' + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)		
		WHEN 'Fusion' THEN 'fusion/' + CAST(@TypeID as varchar) + '/' + + CAST(@ObjectID as varchar)
		WHEN 'FusionType' THEN 'fusion/' + CAST(@TypeID as varchar)
		WHEN 'Group' THEN 'groups/' + CAST(@ObjectID as varchar)	
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
	END

	SET @Url = @Prefix + @Url

	RETURN @Url
END
GO

ALTER FUNCTION [dbo].[GetObjectStatisticScore]
(
--declare
	@type varchar(25) = 'Resource',
	@id int = 1
)
RETURNS int
AS
BEGIN
	declare @score int

	select	@score = cast(Value * 100 as int)
	from	metrics.Score
	where	getutcdate() between EffectiveStartDate and EffectiveEndDate
			and Object = @type and ObjectID = @id
	return @score
END
GO

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
		from	ResponsibilityDetails RD 					
				inner join reporting.Global_Resource R  on 
						RD.Type = @objectType and RD.TypeID = @objectId
						and RD.ResponsibilityTypeID = @responsibilityTypeID
						and	RD.ResourceID = R.ResourceID
						and R.Email not like '%?subject=%' 
						and R.Status = 'Active'
	
	-- if noone found email admins
	if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select 
					R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from 
					reporting.Global_Resource R where isadministrator = 1 and status = 'Active'
		end
	

	return (select string_agg(FirstName + ' ' + LastName,', ') as Resources from @tbl)

END
GO

ALTER FUNCTION [dbo].[GetWorkflowObjectsSummary]
(
	@versionId int,
	@filteredObject varchar(50) = null,
	@filteredObjectId int = null
)
RETURNS varchar(max)
AS
BEGIN

declare @itemCount int;

select @itemCount = count(*) from workflow.item i
inner join workflow.version v on v.id = @versionId and i.versionid = @versionid;

return (
	select string_agg(utility.GetAssetDisplayValue(x.id), ', ') + 
	case when @filteredObjectId is not null then
		case when @itemCount > 1 then
			' and ' + cast((@itemCount - 1) as varchar) + ' more...'
		else
			''
		end
	else
		case when @itemCount > 5 then
			' and ' + cast((@itemCount - 5) as varchar) + ' more...'
		else
			''
		end
	end from 
	(
		select distinct top 5 
		coalesce(a2.id, a.id) as id, coalesce(a.object,a2.object) as object, coalesce(a.objectid,a2.objectid) as objectid from workflow.item i
		left join Asset a on i.object != 'Issue' and a.object = i.object and a.objectid = i.objectid
		left join Issue s on i.object = 'Issue' and s.id = i.objectid
		left join Asset a2 on i.object = 'Issue' and a2.object = s.object and a2.objectid = s.objectid
		inner join workflow.version v on i.versionid = v.id and v.id = @versionId
		inner join workflow.type t on t.id = v.typeid
		where ((@filteredObjectId is not null and (coalesce(a2.object, a.object) = @filteredObject and coalesce(a2.objectId, a.objectId) = @filteredObjectId)) or (@filteredObjectId is null))
		order by 1
	) x 
)
END
GO

ALTER FUNCTION [utility].[DeriveIntersectName] 
(	
	@id int
)
RETURNS nvarchar(500)
AS
BEGIN
	DECLARE @result nvarchar(500)

	SET @result =	(
					SELECT	COALESCE(SA.DisplayValue, SI.Name, '') + ' / ' + COALESCE(OA.DisplayValue, '')
					FROM	[Intersect] I

							left join AssetDetail SA on SA.Object = I.Subject and SA.ObjectID = I.SubjectID
							left join AssetDetail OA on OA.Object = I.Object and OA.ObjectID = I.ObjectID

							left join [Intersect] SI on I.Subject = 'Intersect' and SI.ID = I.SubjectID

					WHERE	I.ID = @id										
					)

	RETURN @result
END
GO

ALTER FUNCTION [utility].[DeriveIntersectTypeName] 
(
--declare
	@id int
--set @id = 17
)
RETURNS nvarchar(500)
AS
BEGIN
	DECLARE @result nvarchar(500)

	SET @result =	(
					SELECT	COALESCE(SA.Name, SD.Name, SF.TextPath, SM.Name, SP.Name, SR.Name, ST.Name, SI.Name, SQF.Name, '') + 
							' [' + coalesce(P.Name,'/') + '] ' + 
							COALESCE(OA.Name, OD.Name, [OF].TextPath, OM.Name, OP.Name, [OR].Name, OT.Name, OQF.Name, '')
					FROM	[IntersectType] I
							left join ArtifactType SA on I.Subject = 'ArtifactType' and SA.ID = I.SubjectID
							left join ArtifactType OA on I.Object = 'ArtifactType' and OA.ID = I.ObjectID

							left join ReferenceItemType SD on I.Subject = 'ReferenceItemType' and SD.ID = I.SubjectID
							left join ReferenceItemType OD on I.Object = 'ReferenceItemType' and OD.ID = I.ObjectID

							left join [FusionAttributeType] SF on I.Subject = 'FusionAttributeType' and SF.ID = I.SubjectID
							left join [FusionAttributeType] [OF] on I.Object = 'FusionAttributeType' and [OF].ID = I.ObjectID

							left join [FusionQueryAttributeType] SQF on I.Subject = 'FusionQueryAttributeType' and SQF.ID = I.SubjectID
							left join [FusionQueryAttributeType] [OQF] on I.Object = 'FusionQueryAttributeType' and [OQF].ID = I.ObjectID

							left join [IntersectType] SI on I.Subject = 'IntersectType' and SI.ID = I.SubjectID

							left join [MapType] SM on I.Subject = 'MapType' and SM.ID = I.SubjectID
							left join [MapType] OM on I.Object = 'MapType' and OM.ID = I.ObjectID

							left join [PolicyType] SP on I.Subject = 'PolicyType' and SP.ID = I.SubjectID
							left join [PolicyType] OP on I.Object = 'PolicyType' and OP.ID = I.ObjectID

							left join [RuleType] SR on I.Subject = 'RuleType' and SR.ID = I.SubjectID
							left join [RuleType] [OR] on I.Object = 'RuleType' and [OR].ID = I.ObjectID

							left join [TaxonomyType] ST on I.Subject = 'TaxonomyType' and ST.ID = I.SubjectID
							left join [TaxonomyType] OT on I.Object = 'TaxonomyType' and OT.ID = I.ObjectID

							left join [Predicate] P on P.ID = I.PredicateID
					WHERE	I.ID = @id					
					)

	RETURN @result
END
GO

ALTER FUNCTION [utility].[GetBreadcrumbString]
(
	@Type varchar(50),
	@ID int,
	@Delimiter varchar(10)
)
RETURNS nvarchar(1000)
AS
BEGIN
	-- Declare the return variable here
	DECLARE @breadcrumb nvarchar(1000)

	IF (@Type = 'Artifact')
	BEGIN
		WITH H
		AS
		(
			SELECT	DisplayValue, 
					ParentID, 
					ID, 
					0 as [Level]
			FROM	Artifact
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.DisplayValue, 
					P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	Artifact	P
					INNER JOIN H AS C ON C.ParentID = P.ID and c.[level] < 6
		)

		SELECT @breadcrumb =	COALESCE(@breadcrumb + @Delimiter, '') + H.DisplayValue
								FROM	H
								ORDER BY H.level DESC

	END

	IF (@Type = 'FusionAttribute')
	BEGIN
		WITH H (Name, ParentID, ID, [level])
		AS
		(
			SELECT	Name, 
					ParentID, 
					ID, 
					0
			FROM	FusionAttribute
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.Name, 
					P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	FusionAttribute	P
					INNER JOIN H AS C ON C.ParentID = P.ID and c.[level] < 6
		)
	
		SELECT @breadcrumb =	COALESCE(@breadcrumb + @Delimiter, '') + H.Name
								FROM	H
								ORDER BY H.level DESC
	END

	IF (@Type = 'FusionAttributeType')
	BEGIN
		WITH H (Name, ParentID, ID, [level])
		AS
		(
			SELECT	Name, 
					ParentID, 
					ID, 
					0
			FROM	FusionAttributeType
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.Name, 
					P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	FusionAttributeType	P
					INNER JOIN H AS C ON C.ParentID = P.ID and c.[level] < 6
		)
	
		SELECT @breadcrumb =	COALESCE(@breadcrumb + @Delimiter, '') + H.Name
								FROM	H
								ORDER BY H.level DESC

		SELECT	@breadcrumb = FT.Name + @Delimiter + @breadcrumb
		FROM	FusionAttributeType FAT
				inner join FusionType FT on FAT.FusionTypeID = FT.ID and FAT.ID = @ID
	END

	IF (@Type = 'Policy')
	BEGIN
		WITH H
		AS
		(
			SELECT	DisplayValue, 
					ParentID, 
					ID, 
					0 as [Level]
			FROM	Policy
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.DisplayValue, 
					P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	Policy	P
					INNER JOIN H AS C ON C.ParentID = P.ID and c.[level] < 6
		)

		SELECT @breadcrumb =	COALESCE(@breadcrumb + @Delimiter, '') + H.DisplayValue
								FROM	H
								ORDER BY H.level DESC

	END

	IF (@Type = 'Taxonomy')
	BEGIN
		WITH H (DisplayValue, CatalogID, ParentID, ID, [level])
		AS
		(
			SELECT	DisplayValue, 
					TaxonomyTypeID, 
					ParentID, 
					ID, 
					0
			FROM	Taxonomy
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.DisplayValue, 
					P.TaxonomyTypeID, 
					P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	Taxonomy	P
					INNER JOIN H AS C ON C.ParentID = P.ID and c.[level] < 6
		)
	
		SELECT @breadcrumb =	COALESCE(@breadcrumb + @Delimiter, '') + H.DisplayValue
								FROM	H
								ORDER BY H.level DESC

		SELECT	@breadcrumb = T.Name + @Delimiter +  @breadcrumb
		FROM	TaxonomyType T 
				INNER JOIN Taxonomy O ON T.ID = O.TaxonomyTypeID WHERE O.ID = @ID 
	END

	RETURN @breadcrumb
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

ALTER FUNCTION [utility].[GetIntersectTypesByType]
(	
	@type varchar(50),
	@id int
)
RETURNS TABLE 
AS
RETURN 
(
	select	'I' as type,
			cast(I.ID as varchar) + '|' +
			case 
				when (Subject = @type and SubjectID = @id) then I.Object + '|' + cast(I.ObjectID as varchar)
				else I.Subject + '|' + cast(I.SubjectID as varchar)
			end as value,
			case 
				when (Subject = @type and SubjectID = @id) then I.SubjectName + ' [' + coalesce(P.Name, 'relates') + '] ' + I.ObjectName
				else I.ObjectName + ' [' + coalesce(P.Inverse, 'related') + '] ' + I.SubjectName
			end as title
	from	IntersectTypeDetail I
			left join [Predicate] P on P.ID = I.PredicateID
	where	(Subject = @type and SubjectID = @id) or 
			(Object = @type and ObjectID = @id)
)
GO


update	T
set		T.AssetTypeID = S.ID
from	FieldType T 
		inner join AssetType S on S.Object = T.Object and S.ObjectID = T.ObjectID


update	T
set		T.AssetID = S.ID
from	Field T 
		inner join Asset S on S.Object = T.ObjectType and S.ObjectID = T.ObjectID