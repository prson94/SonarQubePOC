merge into  AssetType T
using       (
			select	'User' as Name,
					'A resource account.' as Description,
					11 as [Class],
					'{FirstName} {LastName}' as DisplayFormat,
					1 as [State],
					0 as Hierarchical,
					0 as HierarchyMaximumDepth,
					'ResourceType' as Object,
					1 as ObjectID,
					'00000001-0000-0000-0000-a00000000011' as [uid]
			union
			select	'Group' as Name,
					'A security group.' as Description,
					12 as [Class],
					'{Name}' as DisplayFormat,
					1 as [State],
					0 as Hierarchical,
					0 as HierarchyMaximumDepth,
					'GroupType' as Object,
					1 as ObjectID,
					'00000001-0000-0000-0000-b00000000012' as [uid]
            ) S
on          (
                S.Object = T.Object and 
                S.ObjectID = T.ObjectID
            )
when matched then
    update set
            T.[uid] = S.[uid]
when not matched by target then
    insert  (Name, Description, [Class], DisplayFormat, [State], Hierarchical, HierarchyMaximumDepth, [Object], [ObjectID], [CreatedOn], [CreatedBy],[UpdatedOn],[UpdatedBy])
    values  (S.Name, S.Description, S.[Class], S.DisplayFormat, S.[State], S.Hierarchical, S.HierarchyMaximumDepth, S.Object, S.ObjectID, '1/1/2018', 0, '1/1/2018', 0);

merge into  Asset T
using       (
			SELECT	T.ID as AssetTypeID, 
					1 as State, 
					'Resource' as Object, 
					O.ResourceID as ObjectID
			FROM	reporting.global_resource O 
					inner join AssetType T on T.Object = 'ResourceType' and T.ObjectID = 1
			union
			SELECT	T.ID as AssetTypeID, 
					1 as State, 
					'Group' as Object, 
					O.ID as ObjectID
			FROM	[Group] O 
					inner join AssetType T on T.Object = 'GroupType' and T.ObjectID = 1
            ) S
on          (
                T.AssetTypeID = S.AssetTypeID and
                S.Object = T.Object and 
                S.ObjectID = T.ObjectID
            )
when matched then
    update set
            T.CreatedBy = 0,
			T.[CreatedOn] = getutcdate(),
			T.UpdatedBy = 0,
            T.UpdatedOn = getutcdate()
when not matched by target then
    insert  (AssetTypeID,[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
    values  (S.AssetTypeID, S.[State], S.Object, S.ObjectID, getutcdate(), 0, getutcdate(), 0);


begin
	--delete relation override items on assets that no longer exist
	delete from ResponsibilityTypeRelationOverrideItem where assetid > 0 and assetid not in (select id from asset)
	--delete rule result overrides on items that no longer exist
	delete from ResponsibilityTypeRelationRuleResult where assetid > 0 and assetid not in (select id from asset)
end
go

-- Columns

CREATE TABLE [dbo].[AssetCrossReference]
(
[uid] [varchar] (250) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[DataSource] [varchar] (250) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[Type] [varchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[ExternalID] [varchar] (250) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL
)
GO
-- Constraints and Indexes

ALTER TABLE [dbo].[AssetCrossReference] ADD CONSTRAINT [PK_AssetCrossReference] PRIMARY KEY CLUSTERED  ([DataSource], [Type], [ExternalID], [uid])
GO
CREATE NONCLUSTERED INDEX [IX_AssetCrossReference_uid] ON [dbo].[AssetCrossReference] ([uid])
GO
CREATE NONCLUSTERED INDEX [IX_AssetCrossReference_uid_DataSource] ON [dbo].[AssetCrossReference] ([uid], [DataSource])
GO

CREATE TABLE [dbo].[MapGraph]
(
[Subject] [varchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[Predicate] [varchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[Object] [varchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[Sequence] [smallint] NOT NULL
)
GO

CREATE TABLE [metrics].[StagingResultArchive]
(
[MapID] [bigint] NOT NULL,
[EffectiveDate] [datetime] NOT NULL,
[AssetID] [bigint] NOT NULL,
[Value] [bit] NOT NULL
)
GO
-- Constraints and Indexes

ALTER TABLE [metrics].[StagingResultArchive] ADD CONSTRAINT [PK_MetricStagingResultArchive] PRIMARY KEY NONCLUSTERED  ([MapID], [EffectiveDate] DESC, [AssetID])
GO

CREATE VIEW [dbo].[GlossaryGraph] AS
select *, 'artifact/' + cast (artifactTypeID as varchar(50)) + '/' + cast(id as varchar(50)) as GlossaryURL
 from [reporting].[Glossary_All]
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

drop  PROCEDURE [dbo].[GetLineageV2]
GO

drop  procedure [fusion].[GenerateCognosLineageData]
GO

ALTER view [dbo].[AssetWithType]
as
	select	A.ID,
			A.AssetTypeID,
			A.State,
			A.Object,
			A.ObjectID,
			A.SourceID,
			A.CreatedOn,
			A.CreatedBy,
			A.UpdatedOn,
			A.UpdatedBy,
			T.Class as AssetTypeClass,
			T.Description as AssetTypeDescription,
			T.Name as TypeName,
			T.Object as Type,
			T.ObjectID as TypeID,
			coalesce(S.IconBackColor, '#000') as BackColor,
			coalesce(S.IconForeColor, '#fff') as ForeColor,
			coalesce(S.IconText, 'leaf') as Icon,
			A.UID
	from	Asset A
			inner join AssetType T on T.ID = A.AssetTypeID
			left join ObjectStyle S on S.ObjectType = T.Object and S.ObjectID = T.ObjectID
GO

ALTER view [dbo].[AssetDetail]
as
	select	A.ID,
			A.AssetTypeID,
			A.State,
			A.Object,
			A.ObjectID,
			A.SourceID,
			D.DisplayValue,
			K.KeyHash,
			F.FieldHash,
			A.CreatedOn,
			A.CreatedBy,
			A.UpdatedOn,
			A.UpdatedBy,
			A.AssetTypeClass,
			A.AssetTypeDescription,
			A.TypeName,
			A.Type,
			A.TypeID,
			A.BackColor,
			A.ForeColor,
			A.Icon,
			A.UID
	from	AssetWithType A
			cross apply dbo.GetAssetDisplayValueById(A.ID) D	--left join GetAssetDisplayValue() D on D.ID = A.ID
			left join GetAssetKeyHash() K on K.ID = A.ID
			left join GetAssetFieldHash() F on F.ID = A.ID
GO

ALTER FUNCTION [dbo].[GetAssetDisplayValue]()
RETURNS TABLE 
AS
RETURN 
(
	select		A.AssetTypeID,
				A.ID,
				A.Object,
				A.ObjectID,
				string_agg(coalesce(D.FormattedValue, D.value), '') as DisplayValue
	from		Asset A
				inner join AssetType T on T.ID = A.AssetTypeID 
				outer apply (
							select	TL.value,
									coalesce(case when TF.Value = 'FirstName' then R.FirstName + ' ' else R.LastName end, F.FormattedValue, RI.Code, FA.TextPath, FA.Name, FU.Name) as FormattedValue
							from	string_split(replace(T.DisplayFormat, '{', '|'), '|') TF
									cross apply string_split(replace(TF.[value], '}', '|'), '|') TL
									left join FieldType FT on FT.AssetTypeID = T.ID and FT.Name like TL.Value
									left join Field F on F.FieldTypeID = FT.ID and F.AssetID = A.ID
									left join ReferenceItem RI on TL.Value = 'Code' and A.Object = 'ReferenceItem' and RI.ID = A.ObjectID
									left join FusionAttribute FA on TL.Value = 'Name' and A.Object = 'FusionAttribute' and FA.ID = A.ObjectID
									left join Fusion FU on TL.Value = 'Name' and A.Object = 'Fusion' and FU.ID = A.ObjectID
									left join reporting.Global_resource R on TF.Value in ('FirstName', 'LastName') and A.Object = 'Resource' and R.ResourceID = A.ObjectID
							where	RTRIM(TF.value) <> ''
									and RTRIM(TL.value) <> ''
							) D
	group by	A.AssetTypeID,
				A.ID,
				A.Object,
				A.ObjectID
)
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
	UID uniqueidentifier,
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
		insert into @tbl (	ID,		UID,	AssetID,	AssetTypeID, Name,			TextPath,		[Description],	ParentID,	ParentType, Url,											TypeID,	[Type],	TypeName, Status)
			SELECT			ObjectID,	UID, ID, 		AssetTypeID, DisplayValue,	DisplayValue,	NULL,			null,		null,		dbo.GenerateObjectUrl(@type, TypeID, ObjectID),	TypeID,	Type,	TypeName, NULL
			FROM	AssetDetail
			where	Object = @type 
					and ObjectID = @id
	end

	if @type = 'ArtifactType' or @type = 'AttributeType' or @type = 'FusionType' or @type = 'FusionAttributeType' or @type = 'PolicyType' or @type = 'ReferenceItemType' or @type = 'RuleType' or @type = 'TaxonomyType'
	begin
		insert into @tbl (	ID,		UID, Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ObjectID, UID,		Name,	Name,		Description,	NULL,		NULL,		turl.[url] as Url,	ObjectID,		@type,	'Asset Type'
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

	if @type = 'Issue'
	begin
		insert into @tbl (	ID,		Name,				TextPath,	[Description],	ParentID,	ParentType, Url,												TypeID,			[Type],			TypeName)
			SELECT			O.ID,	'',	'',		'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, O.IssueTypeID, O.ID),	O.IssueTypeID,	'IssueType',	T.Name
			FROM	Issue O
					INNER JOIN IssueType T ON O.IssueTypeID = T.ID AND O.ID = @id
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
			INNER JOIN AssetDetail A on T.LookupObjectType = 'Artifact' AND T.LookupObjectID = A.TypeID	and A.[Object] = 'Artifact'		
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

ALTER procedure [bulkload].[Promotions]
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

	-- resolve lookups first as we need the id to generate the hash correctly

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
GO

ALTER procedure [dbo].[DeleteObject]
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

ALTER FUNCTION [utility].[GetAssetDisplayValue]
(
	@ID bigint
)
RETURNS nvarchar(max)
AS
BEGIN
	declare @formattedValue nvarchar(max)

	select	@formattedValue = DisplayValue
	from	dbo.GetAssetDisplayValue()
	where	ID = @ID
	

	return @formattedValue
END
GO

ALTER procedure [metrics].[LoadFromStaging]
as
begin
	-- 1. Remove all except the most recent staging values, grouped by date (not time).
	/*
		insert into metrics.StagingResult values (52, '5/29/2018 3:11:00 PM', 2, 1, 0)
		insert into metrics.StagingResult values (53, '5/29/2018 3:11:00 PM', 2, 0, 0)
		insert into metrics.StagingResult values (54, '5/29/2018 3:11:00 PM', 2, 0, 0)
		insert into metrics.StagingResult values (55, '5/29/2018 3:11:00 PM', 2, 1, 0)

		insert into metrics.StagingResult values (52, '5/31/2018 3:11:00 PM', 2, 1, 0)
		insert into metrics.StagingResult values (53, '5/31/2018 3:11:00 PM', 2, 0, 0)
		insert into metrics.StagingResult values (54, '5/31/2018 3:11:00 PM', 2, 1, 0)
		insert into metrics.StagingResult values (55, '5/31/2018 3:11:00 PM', 2, 1, 0)
	*/
	set nocount on;

	DECLARE @TranName VARCHAR(20);  
	SELECT @TranName = 'UpdateScores';  
	begin transaction @TranName;

	begin try
		drop table if exists #gh

		create table #gh (GroupingID int, ID int, ParentID int null, Name nvarchar(250), Weight decimal(5,3), EffectiveStartDate datetime, EffectiveEndDate datetime, Level int, Type char(1));

		with g as (
			select	ID as GroupingID,
					ID,
					ParentID,
					Name,
					Weight,
					cast(null as datetime) as EffectiveStartDate,
					cast(null as datetime) as EffectiveEndDate,
					1 as Level,
					'G' as Type
			from	[metrics].[Group]
			where	ParentID is null
					and State = 1
			union all
			select	g.GroupingID,
					C.ID,
					C.ParentID,
					C.Name,
					C.Weight,
					cast(null as datetime) as EffectiveStartDate,
					cast(null as datetime) as EffectiveEndDate,
					g.Level+1 as Level,
					'G' as Type
			from	[metrics].[Group] C
					inner join g on g.ID = C.ParentID and C.State = 1
		)

		insert into #gh
			select * from g;

		--select * from #gh

		insert into #gh
			select	G.GroupingID,
					M.ID,
					G.ID,
					I.Name,
					M.Weight,
					M.EffectiveStartDate,
					M.EffectiveEndDate,
					G.Level + 1 as Level,
					'M' as Type
			from	#gh G 
					inner join [metrics].[Map] M on M.GroupID = G.ID
					inner join [metrics].[Item] I on I.ID = M.ItemID;


		update	#gh
		set		EffectiveEndDate = '12/31/9999'
		where	EffectiveStartDate is not null and EffectiveEndDate is null;

		--select * from #gh

		drop table if exists #a

		create table #a (AssetID bigint, Value bit, ID int, ParentID int null, Name nvarchar(250), Weight decimal(5,3), EffectiveDate datetime, Level int, Type char(1), New_ID varchar(250), New_ParentID varchar(250), Score decimal(5,3));

		insert into #a (AssetID, ID, EffectiveDate, Level, Type)
			select	distinct
					R.AssetID,
					0 as ID,
					R.EffectiveDate,
					0 as Level,
					'A' as Type
			from	[metrics].[StagingResult] R
					inner join #gh H on H.ID = R.MapID and H.Type = 'M' and R.EffectiveDate between H.EffectiveStartDate and H.EffectiveEndDate;

		insert into #a (AssetID, Value, ID, ParentID, Name, Weight, EffectiveDate, Level, Type)
			select	G.AssetID,
					R.Value,
					H.ID,
					H.ParentID,
					H.Name,
					H.Weight,
					G.EffectiveDate,
					H.Level,
					H.Type
			from	#gh H
					inner join	(
								select	distinct
										R.AssetID,
										R.EffectiveDate,
										H.GroupingID
								from	[metrics].[StagingResult] R
										inner join #gh H on H.ID = R.MapID and H.Type = 'M' and R.EffectiveDate between H.EffectiveStartDate and H.EffectiveEndDate
								) G on G.GroupingID = H.GroupingID
					left join [metrics].[StagingResult] R on R.AssetID = G.AssetID and R.EffectiveDate = G.EffectiveDate and R.MapID = H.ID and H.Type = 'M' ;

		--Calculate parent/child concatenated IDs.
		update	#a
		set		New_ID = cast(format(EffectiveDate, 'yyyyMMddHHmmss', 'en-US') as varchar) + '.' + cast(AssetID as varchar) + '.' + cast(Type as varchar) + '.' + cast(ID as varchar);

		update	#a
		set		New_ParentID = cast(format(EffectiveDate, 'yyyyMMddHHmmss', 'en-US') as varchar) + '.' + cast(AssetID as varchar) + '.G.' + cast(ParentID as varchar)
		where	Type <> 'A'
				and ParentID is not null;

		update	#a
		set		New_ParentID = cast(format(EffectiveDate, 'yyyyMMddHHmmss', 'en-US') as varchar) + '.' + cast(AssetID as varchar) + '.A.0'
		where	Type <> 'A'
				and ParentID is null;

		--Now start calculating scores.
		update	#a
		set		Score = IIF(Value = 1, Weight, 0)
		where	Type = 'M';

		declare @level int
		select	@level = max(Level) 
		from	#a;

		while @level >= 0
		begin
			if @level > 0
				begin
					update	T
					set		T.Score = S.Score * T.Weight
					from	#a T
							cross apply (
								select	sum(Score) as Score
								from	#a
								where	New_ParentID = T.New_ID
							) S
					where	T.Type = 'G'	
							and T.Level = @level
				end
			else
				begin
					update	T
					set		T.Score = S.Score / C.[Count]
					from	#a T
							cross apply (
								select	sum(Score) as Score
								from	#a
								where	New_ParentID = T.New_ID
							) S
							cross apply (
								select	count(1) as [Count]
								from	#a
								where	New_ParentID = T.New_ID
							) C
					where	T.Type = 'A'
							and T.Level = @level
				end

			set @level = @level-1
		end

		--select * from #a

		/*
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
		*/

		-- 2. Update pre-existing scores
		update	T
		set		T.Value = S.Score
		from	metrics.Score T
				inner join (
							select		cast(R.EffectiveDate as date) as EffectiveDate, A.Object, A.ObjectID, R.Score 
							from		#a R
										inner join Asset A on A.ID = R.AssetID and R.Type = 'A'
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
				from		#a R
							inner join Asset A on A.ID = R.AssetID and R.Type = 'A'
							outer apply	(
										select	coalesce(min(EffectiveStartDate), cast('12/31/9999' as date)) as EffectiveEndDate
										from	metrics.Score
										where	Object = A.Object and ObjectID = A.ObjectID and EffectiveStartDate > cast(R.EffectiveDate as date)
										) M
							left join metrics.Score T on T.EffectiveStartDate = cast(R.EffectiveDate as date) and T.Object = A.Object and T.ObjectID = A.ObjectID
				where		T.ID is null
				group by	R.EffectiveDate, M.EffectiveEndDate, A.Object, A.ObjectID, R.Score;

		-- 4. Merge the metric results, updating existing and adding new ones.
		update	T
		set		T.Value = S.Value
		from	metrics.MapResult T
				inner join (
					select  distinct
							SR.ID,
							S.ID as ScoreID,
							coalesce(SR.Value, cast(0 as bit)) as Value
					from	#a SR
							inner join Asset A on A.ID = SR.AssetID and SR.Type = 'M'
							inner join metrics.Score S on S.Object = A.Object and S.ObjectID = A.ObjectID and S.EffectiveStartDate = cast(SR.EffectiveDate as date)			
				) S on S.ID = T.MapID and S.ScoreID = T.ScoreID;

		insert into metrics.MapResult (MapID, ScoreID, [Value])
			select  SR.ID,
					S.ID as ScoreID,
					cast(max(coalesce(cast(SR.Value as int), cast(0 as int))) as bit) as Value
			from	#a SR
					inner join Asset A on A.ID = SR.AssetID and SR.Type = 'M'
					inner join metrics.Score S on S.Object = A.Object and S.ObjectID = A.ObjectID and S.EffectiveStartDate = cast(SR.EffectiveDate as date)
					left join metrics.MapResult E on E.MapID = SR.ID and E.ScoreID = S.ID
			where	E.MapID is null
			group by	SR.ID,
						S.ID;

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

		-- 6. Backup staging table items that we are about to remove.
		insert into [metrics].[StagingResultArchive]
			select	T.[MapID],
					T.[EffectiveDate],
					T.[AssetID],
					T.[Value]
			from    metrics.StagingResult T
					inner join #a S on S.AssetID = T.AssetID and S.EffectiveDate = T.EffectiveDate and S.ID = T.MapID and S.Type = 'M';

		-- 7. Clear the staging table.
		delete	T
		from    metrics.StagingResult T
				inner join #a S on S.AssetID = T.AssetID and S.EffectiveDate = T.EffectiveDate and S.ID = T.MapID and S.Type = 'M';

		-- 8. Delete any possible dupes from score tables.
		delete	metrics.MapResult 
		where	ScoreID in	(
							select		T.ID
							from		metrics.Score T
										inner join	(
													select		max(ID) as ID,
																Object,
																ObjectID,
																EffectiveStartDate,
																EffectiveEndDate
													from		metrics.Score 
													group by	Object,
																ObjectID,
																EffectiveStartDate,
																EffectiveEndDate
													having		count(1) > 1
													) S on S.ID > T.ID and S.Object = T.Object and S.ObjectID = T.ObjectID and S.EffectiveStartDate = T.EffectiveStartDate and S.EffectiveEndDate = T.EffectiveEndDate
							);

		delete		T
		from		metrics.Score T
					inner join	(
								select		max(ID) as ID,
											Object,
											ObjectID,
											EffectiveStartDate,
											EffectiveEndDate
								from		metrics.Score 
								group by	Object,
											ObjectID,
											EffectiveStartDate,
											EffectiveEndDate
								having		count(1) > 1
								) S on S.ID > T.ID and S.Object = T.Object and S.ObjectID = T.ObjectID and S.EffectiveStartDate = T.EffectiveStartDate and S.EffectiveEndDate = T.EffectiveEndDate;

		commit transaction @TranName;
	end try
	begin catch
		rollback transaction @TranName;
	end catch
end
GO

ALTER view [dbo].[AssetApiModel]
as
select	ID,
		AssetTypeID
		,SourceID
from	Asset
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
GO

ALTER proc [dbo].[GetPageInformation]
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
GO

ALTER procedure [dbo].[GenerateAssetTypeSql]
--declare	
		@type varchar(50),-- = 'ArtifactType',--'TaxonomyType',
		@id int,-- = 1,
		@pt int,-- = 3,--4,
		@showPassword bit = 0
as
begin
	set nocount on;
	declare @avoids table (Type varchar(250));
	insert into @avoids values ('File'), ('FusionLookup'), ('Attribute'), ('FilteredLookup'), ('ComplexRelationLookup'), ('DataTableSelect'), ('OwnershipLookup'), ('RefListRelationship');

	/*
	select	A.ID as AssetID,
			A.ObjectID as ID,
			P.ParentID,
			A.AssetTypeID,
			T.ObjectID as TypeID
	from	Asset A
			inner join AssetType T on T.ID = A.AssetTypeID and T.Object = @type and T.ObjectID = @id
			outer apply (
						select	I.SubjectID as ParentID
						from	[Intersect] I
								inner join IntersectType IT on IT.ID = I.IntersectTypeID 
								inner join [Predicate] P on P.ID = IT.PredicateID and P.Type = @pt
						where	I.Object = A.Object and I.ObjectID = A.ObjectID
						) P
	*/

	select	'left join Field F'+cast(ID as nvarchar)+' on F'+cast(ID as nvarchar)+'.FieldTypeID = ' + cast(ID as nvarchar) + '  and A.Object = F'+cast(ID as nvarchar)+'.ObjectType and A.ObjectID = F'+cast(ID as nvarchar)+'.ObjectID' + 
			case [Type]
				when 'Relationship' 
				then ' left join [Intersect] I'+cast(ID as nvarchar)+' on I'+cast(ID as nvarchar)+'.IntersectTypeID = F'+cast(ID as nvarchar)+'.LookupObjectID' +
										 ' A.Object = case I'+cast(ID as nvarchar)+'.Subject and A.ObjectID = I'+cast(ID as nvarchar)+'.SubjectID then I'+cast(ID as nvarchar)+'.Object = F'+cast(ID as nvarchar)+'.ObjectType else I'+cast(ID as nvarchar)+'.Subject = F'+cast(ID as nvarchar)+'.ObjectType end and' + 
										 ' A.Object = case I'+cast(ID as nvarchar)+'.Subject and A.ObjectID = I'+cast(ID as nvarchar)+'.SubjectID then I'+cast(ID as nvarchar)+'.ObjectID = F'+cast(ID as nvarchar)+'.ObjectID else I'+cast(ID as nvarchar)+'.SubjectID = F'+cast(ID as nvarchar)+'.ObjectID end and' + 
										 ' left join dbo.GetAssetDisplayValue() R'+cast(ID as nvarchar)+' on' + 
										 ' R'+cast(ID as nvarchar)+'.Object = case F'+cast(ID as nvarchar)+'.ObjectType = I'+cast(ID as nvarchar)+'.Subject and F'+cast(ID as nvarchar)+'.ObjectID = I'+cast(ID as nvarchar)+'.SubjectID then I'+cast(ID as nvarchar)+'.Object else I'+cast(ID as nvarchar)+'.Subject end and' + 
										 ' R'+cast(ID as nvarchar)+'.ObjectID = case F'+cast(ID as nvarchar)+'.ObjectType = I'+cast(ID as nvarchar)+'.Subject and F'+cast(ID as nvarchar)+'.ObjectID = I'+cast(ID as nvarchar)+'.SubjectID then I'+cast(ID as nvarchar)+'.ObjectID else I'+cast(ID as nvarchar)+'.SubjectID end'
				when 'FieldFromRelationship' 
				then ' left join [Intersect] I'+cast(ID as nvarchar)+' on I'+cast(ID as nvarchar)+'.IntersectTypeID = F'+cast(ID as nvarchar)+'.LookupObjectID' +
										 ' A.Object = case I'+cast(ID as nvarchar)+'.Subject and A.ObjectID = I'+cast(ID as nvarchar)+'.SubjectID then I'+cast(ID as nvarchar)+'.Object = F'+cast(ID as nvarchar)+'.ObjectType else I'+cast(ID as nvarchar)+'.Subject = F'+cast(ID as nvarchar)+'.ObjectType end and' + 
										 ' A.Object = case I'+cast(ID as nvarchar)+'.Subject and A.ObjectID = I'+cast(ID as nvarchar)+'.SubjectID then I'+cast(ID as nvarchar)+'.ObjectID = F'+cast(ID as nvarchar)+'.ObjectID else I'+cast(ID as nvarchar)+'.SubjectID = F'+cast(ID as nvarchar)+'.ObjectID end and' + 
										 ' left join [Field] RF'+cast(ID as nvarchar)+' on' + 
										 ' RF'+cast(ID as nvarchar)+'.FieldTypeID = ' + cast(LookupObjectFieldTypeID as nvarchar) + ' and' +
										 ' RF'+cast(ID as nvarchar)+'.ObjectType = case F'+cast(ID as nvarchar)+'.ObjectType = I'+cast(ID as nvarchar)+'.Subject and F'+cast(ID as nvarchar)+'.ObjectID = I'+cast(ID as nvarchar)+'.SubjectID then I'+cast(ID as nvarchar)+'.Object else I'+cast(ID as nvarchar)+'.Subject end and' + 
										 ' RF'+cast(ID as nvarchar)+'.ObjectID = case F'+cast(ID as nvarchar)+'.ObjectType = I'+cast(ID as nvarchar)+'.Subject and F'+cast(ID as nvarchar)+'.ObjectID = I'+cast(ID as nvarchar)+'.SubjectID then I'+cast(ID as nvarchar)+'.ObjectID else I'+cast(ID as nvarchar)+'.SubjectID end'
				else ''
			end as JoinStatement,
			case [Type]
				when 'Date' then 'cast(F'+cast(ID as nvarchar)+'.Value as Date)'
				when 'DateTime' then 'cast(F'+cast(ID as nvarchar)+'.Value as DateTime)'
				when 'Decimal' then 'cast(F'+cast(ID as nvarchar)+'.Value as decimal(18,10))'
				when 'Html' then 'dbo.StripHTML(F'+cast(ID as nvarchar)+'.Value)'
				when 'Lookup' then 'F'+cast(ID as nvarchar)+'.FormattedValue'
				when 'Number' then 'cast(F'+cast(ID as nvarchar)+'.Value as int)'
				when 'Relationship' then 'R'+cast(ID as nvarchar)+'.DisplayValue'
				when 'FieldFromRelationship' then 'RF'+cast(ID as nvarchar)+'.FormattedValue'
				when 'Password' then case when @showPassword = 1 then 'F'+cast(ID as nvarchar)+'.Value' else '''*****''' end
				else 'F'+cast(ID as nvarchar)+'.Value'
			end + ' as [' + Name + ']' as ColumnStatement,
			case [Type]
				when 'Date' then 'cast(F'+cast(ID as nvarchar)+'.Value as Date)'
				when 'DateTime' then 'cast(F'+cast(ID as nvarchar)+'.Value as DateTime)'
				when 'Decimal' then 'cast(F'+cast(ID as nvarchar)+'.Value as decimal(18,10))'
				when 'Html' then 'dbo.StripHTML(F'+cast(ID as nvarchar)+'.Value)'
				when 'Lookup' then 'F'+cast(ID as nvarchar)+'.FormattedValue'
				when 'Number' then 'cast(F'+cast(ID as nvarchar)+'.Value as int)'
				when 'Relationship' then 'R'+cast(ID as nvarchar)+'.DisplayValue'
				when 'FieldFromRelationship' then 'RF'+cast(ID as nvarchar)+'.FormattedValue'
				when 'Password' then case when @showPassword = 1 then 'F'+cast(ID as nvarchar)+'.Value' else '''*****''' end
				else 'F'+cast(ID as nvarchar)+'.Value'
			end as SortStatement,
			Name,
			ColumnOrder,
			IsListable,
			SortOrder
	from	FieldType
	where	Type not in (select Type from @avoids)
			and Object = @type
			and ObjectID = @id

	--select	string_agg(CN, ', ') as [Columns]
	--from	(
	--		select		top 100 percent	
	--					case [Type]
	--						when 'Date' then 'cast(F'+cast(ID as nvarchar)+'.Value as Date)'
	--						when 'DateTime' then 'cast(F'+cast(ID as nvarchar)+'.Value as DateTime)'
	--						when 'Decimal' then 'cast(F'+cast(ID as nvarchar)+'.Value as decimal(18,10))'
	--						when 'Html' then 'dbo.StripHTML(F'+cast(ID as nvarchar)+'.Value)'
	--						when 'Lookup' then 'F'+cast(ID as nvarchar)+'.FormattedValue'
	--						when 'Number' then 'cast(F'+cast(ID as nvarchar)+'.Value as int)'
	--						when 'Relationship' then 'R'+cast(ID as nvarchar)+'.DisplayValue'
	--						when 'FieldFromRelationship' then 'RF'+cast(ID as nvarchar)+'.FormattedValue'
	--						when 'Password' then '''*****'''
	--						else 'F'+cast(ID as nvarchar)+'.Value'
	--					end + ' as [' + Name + ']' as CN
	--		from		FieldType
	--		where		Type not in (select Type from @avoids)
	--					and Object = @type
	--					and ObjectID = @id
	--		order by	ColumnOrder
	--		) O

	--select	string_agg(CN, ',') as [Sorts]
	--from	(
	--		select		top 100 percent	
	--					case [Type]
	--						when 'Date' then 'cast(F'+cast(ID as nvarchar)+'.Value as Date)'
	--						when 'DateTime' then 'cast(F'+cast(ID as nvarchar)+'.Value as DateTime)'
	--						when 'Decimal' then 'cast(F'+cast(ID as nvarchar)+'.Value as decimal(18,10))'
	--						when 'Html' then 'dbo.StripHTML(F'+cast(ID as nvarchar)+'.Value)'
	--						when 'Lookup' then 'F'+cast(ID as nvarchar)+'.FormattedValue'
	--						when 'Number' then 'cast(F'+cast(ID as nvarchar)+'.Value as int)'
	--						when 'Relationship' then 'R'+cast(ID as nvarchar)+'.DisplayValue'
	--						when 'FieldFromRelationship' then 'RF'+cast(ID as nvarchar)+'.FormattedValue'
	--						when 'Password' then '''*****'''
	--						else 'F'+cast(ID as nvarchar)+'.Value'
	--					end as CN
	--		from		FieldType
	--		where		Type not in (select Type from @avoids)
	--					and Object = @type
	--					and ObjectID = @id
	--		order by	SortOrder
	--		) O
end
GO

ALTER PROCEDURE [dbo].[GetFieldNamesByObjectType]
	@type varchar(50),
	@id int
AS
BEGIN
	declare @t table (Name nvarchar(250))

	if (@type = 'ArtifactType')
	begin
		insert into @t values ('Name')
		insert into @t values ('Description')
		--insert into @t values ('Status')
		insert into @t values ('ParentID')
		insert into @t values ('TaxonomyTypeID')
	end
	if (@type = 'AttributeType')
	begin
		insert into @t values ('ObjectID')
	end
	if (@type = 'Domain')
	begin
		insert into @t values ('Name')
		insert into @t values ('Description')
	end
	if (@type = 'DomainItem')
	begin
		insert into @t values ('Code')
		insert into @t values ('Name')
		insert into @t values ('Description')
	end
	--if (@type = 'LookupType')
	--begin
	--end
	if (@type = 'TaxonomyType')
	begin
		insert into @t values ('Name')
		insert into @t values ('Description')
	end

	select Name, cast(0 as bit) as IsCustomField from @t
	union
	select Name, cast(1 as bit) as IsCustomField from FieldTypeWithRelation where [Object] = @type and ObjectID = @id
END
GO

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

ALTER TRIGGER [dbo].[ResponsibilityTypeRelationOverrideItem_AfterUpsert]
   ON  [dbo].[ResponsibilityTypeRelationOverrideItem]
   AFTER INSERT, UPDATE
AS 
BEGIN
	SET NOCOUNT ON;

	-- 1. Override rule assignments
	update	T
	set		T.Overridden = 1,
			T.OverrideID = S.ID
	from	ResponsibilityTypeRelationRuleResult T
			inner join inserted S on S.AssetID = T.AssetID and S.ResponsibilityTypeID = T.ResponsibilityTypeID and T.RuleID <> 0;

	-- 2. Load Override assignments
	merge	ResponsibilityTypeRelationRuleResult as T
	using	(
			select	0 as RuleID,
					I.ID,
					I.ResponsibilityTypeID,
					A.ID as AssetID,
					A.Object,
					A.ObjectID,
					A.AssetTypeID,
					T.Object as Type,
					T.ObjectID as TypeID,
					I.SecurityAsset,
					I.SecurityAssetID,
					R.PermissionsBitMask,
					I.Context
			from	Asset A
					inner join AssetType T on T.ID = A.AssetTypeID
					inner join ResponsibilityTypeRelation R on R.ObjectType = T.Object and R.ObjectID = T.ObjectID
					inner join inserted I on I.AssetID = A.ID and I.ResponsibilityTypeID = R.ResponsibilityTypeID
			) as S 
	on		(
			S.RuleID = T.RuleID
			and S.ID = T.OverrideID
			)
	when	matched then
	update	set
			T.SecurityAsset = S.SecurityAsset,
			T.SecurityAssetID = S.SecurityAssetID,
			T.PermissionsBitMask = S.PermissionsBitMask,
			T.Context = S.Context,
			T.ResponsibilityTypeID = S.ResponsibilityTypeID
	when	not matched by target then
			insert (RuleID, ResponsibilityTypeID, AssetID, AssetTypeID, SecurityAsset, SecurityAssetID, PermissionsBitMask, Context, ApplyToType, IsVisible, Overridden, OverrideID)
			values (S.RuleID, S.ResponsibilityTypeID, S.AssetID, S.AssetTypeID, S.SecurityAsset, S.SecurityAssetID, S.PermissionsBitMask, S.Context, 0, 1, 0, S.ID);
END
GO

ALTER TABLE [api].[EntityFieldType] ADD [ItemNameOverride] [varchar] (250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
GO
ALTER TABLE [dbo].[Map] DROP CONSTRAINT [DF_Map_State]
GO
ALTER TABLE [dbo].[Map] DROP COLUMN [State]
GO

CREATE NONCLUSTERED INDEX [IX_IntegrationExecutionAsset_Execution] ON [integration].[ExecutionAsset] ([ExecutionID] DESC)
GO
CREATE NONCLUSTERED INDEX [IX_IntegrationExecutionUnresolvedRelationItem_ExecutionAssetInclude] ON [integration].[ExecutionUnresolvedRelationItem] ([ExecutionID] DESC) INCLUDE ([ObjectAssetID], [SubjectAssetID])
GO
CREATE NONCLUSTERED INDEX [IX_IntegrationExecutionUnresolvedRelationItem_ExecutionIntersectInclude] ON [integration].[ExecutionUnresolvedRelationItem] ([ExecutionID] DESC, [IntersectID])
GO
CREATE NONCLUSTERED INDEX [IX_IntegrationExecutionUnresolvedRelationItem_ObjectInfo] ON [integration].[ExecutionUnresolvedRelationItem] ([ExecutionID] DESC, [IntersectTypeID], [ObjectAssetTypeID], [ObjectSourceID])
GO
CREATE NONCLUSTERED INDEX [IX_IntegrationExecutionUnresolvedRelationItem_SubjectInfo] ON [integration].[ExecutionUnresolvedRelationItem] ([ExecutionID] DESC, [IntersectTypeID], [SubjectAssetTypeID], [SubjectSourceID])
GO

ALTER TRIGGER [dbo].[ResponsibilityTypeRelationOverrideItem_AfterUpsert]
   ON  [dbo].[ResponsibilityTypeRelationOverrideItem]
   AFTER INSERT, UPDATE
AS 
BEGIN
	SET NOCOUNT ON;

	-- 1. Override rule assignments
	update	T
	set		T.Overridden = 1,
			T.OverrideID = S.ID
	from	ResponsibilityTypeRelationRuleResult T
			inner join inserted S on S.AssetID = T.AssetID and S.ResponsibilityTypeID = T.ResponsibilityTypeID and T.RuleID <> 0;

	-- 2. Load Override assignments
	merge	ResponsibilityTypeRelationRuleResult as T
	using	(
			select	0 as RuleID,
					I.ID,
					I.ResponsibilityTypeID,
					A.ID as AssetID,
					A.Object,
					A.ObjectID,
					A.AssetTypeID,
					T.Object as Type,
					T.ObjectID as TypeID,
					I.SecurityAsset,
					I.SecurityAssetID,
					R.PermissionsBitMask,
					I.Context
			from	Asset A
					inner join AssetType T on T.ID = A.AssetTypeID
					inner join ResponsibilityTypeRelation R on R.ObjectType = T.Object and R.ObjectID = T.ObjectID
					inner join inserted I on I.AssetID = A.ID and I.ResponsibilityTypeID = R.ResponsibilityTypeID
			) as S 
	on		(
			S.RuleID = T.RuleID
			and S.ID = T.OverrideID
			)
	when	matched then
	update	set
			T.SecurityAsset = S.SecurityAsset,
			T.SecurityAssetID = S.SecurityAssetID,
			T.PermissionsBitMask = S.PermissionsBitMask,
			T.Context = S.Context,
			T.ResponsibilityTypeID = S.ResponsibilityTypeID
	when	not matched by target then
			insert (RuleID, ResponsibilityTypeID, AssetID, AssetTypeID, SecurityAsset, SecurityAssetID, PermissionsBitMask, Context, ApplyToType, IsVisible, Overridden, OverrideID)
			values (S.RuleID, S.ResponsibilityTypeID, S.AssetID, S.AssetTypeID, S.SecurityAsset, S.SecurityAssetID, S.PermissionsBitMask, S.Context, 0, 1, 0, S.ID);
END
GO

ALTER TABLE [dbo].[AssetDataQualityImplementation] ADD CONSTRAINT [FK_AssetDataQualityImplementation_Asset] FOREIGN KEY ([AssetID]) REFERENCES [dbo].[Asset] ([ID]) ON DELETE CASCADE
GO

-- primary key constraints
declare @schema_name nvarchar(256) = N'fusion'
declare @table_name nvarchar(256) = N'AgentError'
declare @Command  nvarchar(1000)

select	@Command = @schema_name + '.' + d.name
from	sys.tables t join sys.key_constraints d on d.parent_object_id = t.object_id and t.name = @table_name and t.schema_id = schema_id(@schema_name)

if @Command is not null
begin
	exec sp_rename @Command, 'PK_FusionAgentError'
end
GO

declare @schema_name nvarchar(256) = N'fusion'
declare @table_name nvarchar(256) = N'AgentErrorItem'
declare @Command  nvarchar(1000)

select	@Command = @schema_name + '.' + d.name
from	sys.tables t join sys.key_constraints d on d.parent_object_id = t.object_id and t.name = @table_name and t.schema_id = schema_id(@schema_name)

if @Command is not null
begin
	exec sp_rename @Command, 'PK_FusionAgentErrorItem'
end
GO

declare @schema_name nvarchar(256) = N'dbo'
declare @table_name nvarchar(256) = N'ReportResponsibility'
declare @Command  nvarchar(1000)

select	@Command = @schema_name + '.' + d.name
from	sys.tables t join sys.key_constraints d on d.parent_object_id = t.object_id and t.name = @table_name and t.schema_id = schema_id(@schema_name)

if @Command is not null
begin
	exec sp_rename @Command, 'PK_ReportResponsibility'
end
GO

declare @schema_name nvarchar(256) = N'dbo'
declare @table_name nvarchar(256) = N'ResourcePasswordReset'
declare @Command  nvarchar(1000)

select	@Command = @schema_name + '.' + d.name
from	sys.tables t join sys.key_constraints d on d.parent_object_id = t.object_id and t.name = @table_name and t.schema_id = schema_id(@schema_name)

if @Command is not null
begin
	exec sp_rename @Command, 'PK_ResourcePasswordReset'
end
GO

declare @schema_name nvarchar(256) = N'fusion'
declare @table_name nvarchar(256) = N'Rule'
declare @Command  nvarchar(1000)

select	@Command = @schema_name + '.' + d.name
from	sys.tables t join sys.key_constraints d on d.parent_object_id = t.object_id and t.name = @table_name and t.schema_id = schema_id(@schema_name)

if @Command is not null
begin
	exec sp_rename @Command, 'PK_FusionRule'
end
GO

declare @schema_name nvarchar(256) = N'fusion'
declare @table_name nvarchar(256) = N'RuleStep'
declare @Command  nvarchar(1000)

select	@Command = @schema_name + '.' + d.name
from	sys.tables t join sys.key_constraints d on d.parent_object_id = t.object_id and t.name = @table_name and t.schema_id = schema_id(@schema_name)

if @Command is not null
begin
	exec sp_rename @Command, 'PK_FusionRuleStep'
end
GO

declare @schema_name nvarchar(256) = N'fusion'
declare @table_name nvarchar(256) = N'StagingFileItem'
declare @Command  nvarchar(1000)

select	@Command = @schema_name + '.' + d.name
from	sys.tables t join sys.key_constraints d on d.parent_object_id = t.object_id and t.name = @table_name and t.schema_id = schema_id(@schema_name)

if @Command is not null
begin
	exec sp_rename @Command, 'PK_FusionStagingFileItem'
end
GO

------------------------------------------------------------------------------------------------------------


-- default constraints
declare @schema_name nvarchar(256) = N'dbo'
declare @table_name nvarchar(256) = N'AssetDisplayFormatFieldTypes'
declare @col_name nvarchar(256) = N'FieldTypeID'
declare @Command  nvarchar(1000)

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

ALTER TABLE [dbo].AssetDisplayFormatFieldTypes ADD  CONSTRAINT DF_AssetDisplayFormatFieldTypes_FieldTypeID  DEFAULT ((0)) FOR FieldTypeID
GO


declare @schema_name nvarchar(256) = N'dbo'
declare @table_name nvarchar(256) = N'Comment'
declare @col_name nvarchar(256) = N'IsDeleted'
declare @Command  nvarchar(1000)

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

ALTER TABLE [dbo].Comment ADD  CONSTRAINT DF_Comment_IsDeleted  DEFAULT ((0)) FOR IsDeleted
GO

declare @schema_name nvarchar(256) = N'fusion'
declare @table_name nvarchar(256) = N'Execution'
declare @col_name nvarchar(256) = N'Version'
declare @Command  nvarchar(1000)

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

ALTER TABLE fusion.Execution ADD  CONSTRAINT  [DF_FusionExecution_Version] DEFAULT ('unknown') FOR [Version]
GO

declare @schema_name nvarchar(256) = N'dbo'
declare @table_name nvarchar(256) = N'Issue'
declare @col_name nvarchar(256) = N'UpdatedOn'
declare @Command  nvarchar(1000)

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

ALTER TABLE [dbo].Issue ADD  CONSTRAINT [DF_Issue_UpdatedOn] DEFAULT (getutcdate()) FOR UpdatedOn
GO

declare @schema_name nvarchar(256) = N'dbo'
declare @table_name nvarchar(256) = N'Issue'
declare @col_name nvarchar(256) = N'Criticality'
declare @Command  nvarchar(1000)

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

ALTER TABLE [dbo].Issue ADD  CONSTRAINT [DF_Issue_Criticality] DEFAULT (0) FOR Criticality
GO


declare @schema_name nvarchar(256) = N'dbo'
declare @table_name nvarchar(256) = N'Policy'
declare @col_name nvarchar(256) = N'DisplayValue'
declare @Command  nvarchar(1000)

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

ALTER TABLE [dbo].[Policy] ADD  CONSTRAINT [DF_Policy_DisplayValue] DEFAULT ('<INVALID VALUE>') FOR DisplayValue
GO




declare @schema_name nvarchar(256) = N'dbo'
declare @table_name nvarchar(256) = N'ResourcePasswordReset'
declare @col_name nvarchar(256) = N'ID'
declare @Command  nvarchar(1000)

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

ALTER TABLE [dbo].ResourcePasswordReset ADD  CONSTRAINT [DF_ResourcePasswordReset_ID] DEFAULT (newid()) FOR ID
GO




declare @schema_name nvarchar(256) = N'dbo'
declare @table_name nvarchar(256) = N'RuleDimension'
declare @col_name nvarchar(256) = N'IsSystemDefined'
declare @Command  nvarchar(1000)

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

ALTER TABLE [dbo].RuleDimension ADD  CONSTRAINT [DF_RuleDimension_IsSystemDefined] DEFAULT (0) FOR IsSystemDefined
GO




declare @schema_name nvarchar(256) = N'dbo'
declare @table_name nvarchar(256) = N'RuleDimension'
declare @col_name nvarchar(256) = N'UpdatedOn'
declare @Command  nvarchar(1000)

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

ALTER TABLE [dbo].RuleDimension ADD  CONSTRAINT [DF_RuleDimension_UpdatedOn] DEFAULT (getutcdate()) FOR UpdatedOn
GO



declare @schema_name nvarchar(256) = N'fusion'
declare @table_name nvarchar(256) = N'RulePromotion'
declare @col_name nvarchar(256) = N'ObjectTypeID'
declare @Command  nvarchar(1000)

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

ALTER TABLE fusion.RulePromotion ADD  CONSTRAINT [DF_FusionRulePromotion_ObjectTypeID] DEFAULT (-1) FOR ObjectTypeID
GO



declare @schema_name nvarchar(256) = N'fusion'
declare @table_name nvarchar(256) = N'RulePromotion'
declare @col_name nvarchar(256) = N'CreatedOn'
declare @Command  nvarchar(1000)

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

ALTER TABLE fusion.RulePromotion ADD  CONSTRAINT [DF_FusionRulePromotion_CreatedOn] DEFAULT (getutcdate()) FOR CreatedOn
GO



declare @schema_name nvarchar(256) = N'fusion'
declare @table_name nvarchar(256) = N'RulePromotion'
declare @col_name nvarchar(256) = N'UpdatedOn'
declare @Command  nvarchar(1000)

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

ALTER TABLE fusion.RulePromotion ADD  CONSTRAINT [DF_FusionRulePromotion_UpdatedOn] DEFAULT (getutcdate()) FOR UpdatedOn
GO



declare @schema_name nvarchar(256) = N'fusion'
declare @table_name nvarchar(256) = N'RuleStepMapping'
declare @col_name nvarchar(256) = N'IsConstantValue'
declare @Command  nvarchar(1000)

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

ALTER TABLE fusion.RuleStepMapping ADD  CONSTRAINT DF_FusionRuleStepMapping_IsConstantValue DEFAULT (0) FOR IsConstantValue
GO



declare @schema_name nvarchar(256) = N'dbo'
declare @table_name nvarchar(256) = N'Shortcut'
declare @col_name nvarchar(256) = N'DisplayOrder'
declare @Command  nvarchar(1000)

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

ALTER TABLE dbo.Shortcut ADD  CONSTRAINT DF_Shortcut_DisplayOrder DEFAULT (100) FOR DisplayOrder
GO


declare @schema_name nvarchar(256) = N'dbo'
declare @table_name nvarchar(256) = N'Shortcut'
declare @col_name nvarchar(256) = N'LinkTarget'
declare @Command  nvarchar(1000)

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

ALTER TABLE dbo.Shortcut ADD  CONSTRAINT DF_Shortcut_LinkTarget DEFAULT (0) FOR LinkTarget
GO



declare @schema_name nvarchar(256) = N'fusion'
declare @table_name nvarchar(256) = N'StagingRelationUnresolved'
declare @col_name nvarchar(256) = N'CreatedOn'
declare @Command  nvarchar(1000)

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

ALTER TABLE fusion.StagingRelationUnresolved ADD  CONSTRAINT DF_FusionStagingRelationUnresolved_CreatedOn DEFAULT (getutcdate()) FOR CreatedOn
GO




declare @schema_name nvarchar(256) = N'queue'
declare @table_name nvarchar(256) = N'Task'
declare @col_name nvarchar(256) = N'AssetID'
declare @Command  nvarchar(1000)

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

ALTER TABLE [queue].Task ADD  CONSTRAINT DF_QueueTask_AssetID DEFAULT (0) FOR AssetID
GO