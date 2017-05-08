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
	if @type = 'Artifact'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],			TypeName, Status)
			SELECT			O.ID,	O.Name,	O.TextPath,	O.Description,	O.ParentID,	@type,		dbo.GenerateObjectUrl(@type, O.ArtifactTypeID, O.ID),	O.ArtifactTypeID,	'ArtifactType',	T.Name, O.Status
			FROM	Artifact O
					INNER JOIN ArtifactType T ON O.ArtifactTypeID = T.ID and O.ID = @id
	end

	if @type = 'ArtifactType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Artifact Type'
			FROM	ArtifactType O
			WHERE	ID = @id
	end

	if @type = 'Attribute'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],				TypeName)
			SELECT			O.ID,	'',		'',			'',				O.ParentID,	@type,		D.Url,	O.AttributeTypeID,	'AttributeType',	T.Name
			FROM	[Attribute] O
					INNER JOIN AttributeType T ON O.AttributeTypeID = T.ID and O.ID = @id
					cross apply  utility.ObjectDetail(O.ObjectType, O.ObjectID) D
	end

	if @type = 'AttributeType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		Description,	ParentID,	@type,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Attribute Type'
			FROM	AttributeType
			WHERE	ID = @id
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
			SELECT			ID,		Name,	Name,		'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Intersect Type'
			FROM	IntersectType
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

	if @type = 'Fusion'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,												TypeID,			[Type],			TypeName)
			SELECT			O.ID,	O.Name,	O.Name,		'',				NULL,		@type,		dbo.GenerateObjectUrl(@type, O.FusionTypeID, O.ID),	O.FusionTypeID,	'FusionType',	T.Name
			FROM	Fusion O
					INNER JOIN FusionType T ON O.FusionTypeID = T.ID and O.ID = @id
	end

	if @type = 'FusionType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Fusion Type'
			FROM	FusionType O
			WHERE	ID = @id
	end

	if @type = 'FusionAttribute'
	begin
		insert into @tbl (	ID,		Name,		TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,						[Type],					TypeName)
			SELECT			O.ID,	coalesce(O.TextPath, O.Name),	O.TextPath,	'',				O.ParentID,	@type,		dbo.GenerateObjectUrl(@type, FT.ID, O.ID),
																											O.FusionAttributeTypeID,	'FusionAttributeType',	T.Name
			FROM	FusionAttribute O
					INNER JOIN FusionAttributeType T ON O.FusionAttributeTypeID = T.ID and O.ID = @id
					INNER JOIN FusionType FT ON T.FusionTypeID = FT.ID
	end

	if @type = 'FusionAttributeType'
	begin
		insert into @tbl (	ID, Name,		TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,	O.Name,	O.TextPath,	'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Fusion Attribute Type'
			FROM	FusionAttributeType O
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

	if @type = 'Map'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],			TypeName, Status)
			SELECT			O.ID,	O.Name,	O.Name,	NULL,	NULL,	NULL,		dbo.GenerateObjectUrl(@type, O.MapTypeID, O.ID),	O.MapTypeID,	'MapType',	T.Name, NULL
			FROM	Map O
					INNER JOIN MapType T ON O.MapTypeID = T.ID and O.ID = @id
	end

	if @type = 'MapType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],			TypeName, Status)
			SELECT			O.ID,	O.Name,	O.Name,	O.Description,	NULL,	NULL,		dbo.GenerateObjectUrl(@type, O.ID, O.ID),	O.ID,	'MapType',	Name, NULL
			FROM	MapType O
	end

	if @type = 'Policy'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,				[Type],			TypeName)
			SELECT			O.ID,	O.Name,	O.TextPath,	O.Description,	NULL,		@type,		dbo.GenerateObjectUrl(@type, T.ID, O.ID),	T.ID,	'PolicyType',	T.Name
			FROM	[Policy] O
					INNER JOIN PolicyType T ON O.PolicyTypeID = T.ID AND O.ID = @id
	end

	if @type = 'PolicyType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID,	[Type],	TypeName)
			SELECT			O.ID,	O.Name,	O.Name,		O.Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, O.ID, O.ID),	C.ID,	@type,	C.Name
			FROM	PolicyType O
					inner join PolicyTypeClass C on C.ID = O.PolicyTypeClassID
			WHERE	O.ID = @id
	end

	if @type = 'ReferenceItem'
	begin
		insert into @tbl (	ID,	
							Name, TextPath, [Description],	
							ParentID, ParentType, 
							Url, 
							TypeID, [Type], TypeName)
			SELECT			O.ID,		
							O.DisplayValue, O.DisplayValue, NULL,
							NULL, NULL, 
							dbo.GenerateObjectUrl(@type, T.ID, O.ID),
							T.ID, 'ReferenceItemType', T.Name
			FROM	ReferenceItem O
					inner join ReferenceItemType T on T.ID = O.ReferenceItemTypeID and O.ID = @id
	end

	if @type = 'ReferenceItemType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	0,		@type,	'Reference Item Type'
			FROM	ReferenceItemType
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

	if @type = 'Rule'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,				[Type],			TypeName, Status)
			SELECT			O.ID,	O.Name,	O.Name,	O.Description,	NULL,		@type,		dbo.GenerateObjectUrl(@type, 0, O.ID),	O.RuleTypeID,	'RuleType',	T.Name, case O.Status when 1 then 'Draft' when 2 then 'Active' else 'Inactive' end
			FROM	[Rule] O
					inner join RuleType T on T.ID = O.RuleTypeID
			WHERE	O.ID = @id
	end

	if @type = 'RuleType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID,	[Type],	TypeName)
			SELECT			O.ID,	O.Name,	O.Name,		O.Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, O.ID, O.ID),	O.ID,	@type,	O.Name
			FROM	RuleType O
			WHERE	O.ID = @id
	end

	if @type = 'StatisticType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Analytic Type'
			FROM	StatisticType O
			WHERE	ID = @id
	end

	if @type = 'Synonym'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,			[Type],		TypeName)
			SELECT			O.ID,	O.Name,	D.TextPath,	D.TypeName,		O.ObjectID,	O.Object,	D.Url,	O.PredicateID,	'Synonym',	P.Name
			FROM	[Synonym] O
					INNER JOIN [Predicate] P ON O.PredicateID = P.ID and O.ID = @id
					cross apply  utility.ObjectDetail(O.[Object], O.ObjectID) D
	end

	if @type = 'Taxonomy'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],			TypeName)
			SELECT			O.ID,	O.Name,	O.TextPath,	O.Description,	O.ParentID,	@type,		dbo.GenerateObjectUrl(@type, O.TaxonomyTypeID, O.ID),	O.TaxonomyTypeID,	'TaxonomyType',	C.Name + ' Model'
			FROM	Taxonomy O
					INNER JOIN TaxonomyType T ON O.TaxonomyTypeID = T.ID AND O.ID = @id
					inner join TaxonomyTypeClass C on C.ID = T.TaxonomyTypeClassID
	end

	if @type = 'TaxonomyType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID,	[Type],	TypeName)
			SELECT			O.ID,	O.Name,	O.Name,		O.Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, O.ID),	C.ID,	@type,	C.Name
			FROM	TaxonomyType O
					inner join TaxonomyTypeClass C on C.ID = O.TaxonomyTypeClassID
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
