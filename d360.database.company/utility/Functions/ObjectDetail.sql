CREATE FUNCTION [utility].[ObjectDetail]
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
	Name nvarchar(250),
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
			SELECT			ObjectID,		Name,	Name,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ObjectID),	ObjectID,		@type,	'Asset Type'
			FROM	AssetType O
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
			SELECT			O.ID,	O.Name,	O.Name,		'',				NULL,		@type,		dbo.GenerateObjectUrl(@type, O.IntersectTypeID, O.ID),	O.IntersectTypeID,	'IntersectType',	T.Name
			FROM	[Intersect] O
					INNER JOIN IntersectType T ON O.IntersectTypeID = T.ID and O.ID = @id
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