CREATE procedure [cache].[ReSynchronizeAllObjectDetails]
as
begin
	set nocount on;

	IF OBJECT_ID('tempdb..#Recache') IS NOT NULL
    DROP TABLE #Recache

	create table #Recache (
		[Object] varchar(50) not null,
		ObjectID int not null,
		Name nvarchar(250) null,
		TextPath nvarchar(2500) null,
		Description nvarchar(4000) null,
		Parent varchar(50) null,
		ParentID int null,
		ParentName nvarchar(250) null,
		Url nvarchar(2500) not null,
		ObjectType varchar(25) not null,
		ObjectTypeID int not null,
		ObjectTypeName nvarchar(250) null,
		StyleType varchar(25) not null,
		StyleTypeID int not null,
		IconBackColor varchar(15) null,
		IconForeColor varchar(15) null,
		IconText varchar(15) null
	);

	declare @type varchar(50);
	
	begin
		set @type = 'Artifact'
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, O.Name, O.TextPath, O.Description,
					case 
						when P.ID is not null then 'Artifact'
						else NULL
					end, O.ParentID, P.Name,
					dbo.GenerateObjectUrl(@type, O.ArtifactTypeID, O.ID),
					'ArtifactType', O.ArtifactTypeID, T.Name,
					'ArtifactType', O.ArtifactTypeID
			FROM	Artifact O
					INNER JOIN ArtifactType T ON O.ArtifactTypeID = T.ID
					left join Artifact P on P.ID = O.ParentID;

	end;

	begin
		set @type = 'ArtifactType'
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, O.Name, O.Name, O.Description,	
					case 
						when P.ID is not null then 'ArtifactType'
						else NULL
					end, O.ParentID, P.Name,
					dbo.GenerateObjectUrl(@type, O.ID, O.ID),	
					@type, 0, 'Artifact Type',
					@type, O.ID
			FROM	ArtifactType O
					left join ArtifactType P on P.ID = O.ParentID;
	end;

	begin
		set @type = 'AttributeType';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, O.Name, O.Name, O.Description,	
					case 
						when P.ID is not null then @type
						else NULL
					end, O.ParentID, P.Name,
					'#',--dbo.GenerateObjectUrl(@type, 0, O.ID),	
					'AttributeType', 0, 'Attribute Type',
					'AttributeType', O.ID
			FROM	AttributeType O
					left join AttributeType P on P.ID = O.ParentID;
	end;

	begin
		set @type = 'Domain';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, O.Name, O.Name, O.Description,	
					NULL, NULL, NULL,
					dbo.GenerateObjectUrl(@type, O.DomainTypeID, O.ID),	
					'DomainType', O.DomainTypeID, T.Name,
					'DomainType', O.DomainTypeID
			FROM	Domain O
					INNER JOIN DomainType T ON O.DomainTypeID = T.ID;
	end;

	begin
		set @type = 'DomainType';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, ID, Name, Name, Description,	
					NULL, NULL, NULL,
					dbo.GenerateObjectUrl(@type, ID, ID),
					@type, 0, 'Domain Type',
					@type, ID
			FROM	DomainType;
	end;

	begin
		set @type = 'Group';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, ID, Name, Name, Description,	
					NULL, NULL, NULL,
					dbo.GenerateObjectUrl(@type, 0, ID),
					'Group', 0, @type,
					'Group', 0
			FROM	[Group];
	end;

	begin
		set @type = 'Intersect';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, O.Name, O.Name, O.Description,
					NULL, NULL, NULL,
					'#',
					'IntersectType', O.IntersectTypeID,	T.Name,
					'IntersectType', O.IntersectTypeID
			FROM	[Intersect] O
					INNER JOIN IntersectType T ON O.IntersectTypeID = T.ID;
	end;

	begin
		set @type = 'IntersectType';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, ID, Name, Name, NULL,
					NULL, NULL, NULL,
					'#',--dbo.GenerateObjectUrl(@type, 0, ID),
					@type, 0, 'Intersect Type',
					@type, ID
			FROM	IntersectType;
	end;

	begin
		set @type = 'Event';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, G.Name + ' event',	R.Name + '.' + G.Name + ' event', NULL,
					'EventGroup', G.ID, G.Name,
					dbo.GenerateObjectUrl(@type, R.ID, O.ID),
					'Rule', R.ID, R.Name,
					'Rule', R.ID
			FROM	[Event] O
					INNER JOIN EventGroup G on G.ID = O.EventGroupID
					INNER JOIN [Rule] R on R.ID = G.RuleID;
	end;

	begin
		set @type = 'EventGroup';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, O.Name, R.Name + '.' + O.Name, NULL,
					NULL, NULL, NULL,
					dbo.GenerateObjectUrl(@type, 0, O.ID),
					'Rule', R.ID, R.Name,
					'Rule', R.ID
			FROM	EventGroup O
					inner join [Rule] R on R.ID = O.RuleID;
	end;

	begin
		set @type = 'Lookup';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, T.Name + ' Item', T.Name + ' Item', NULL,
					NULL, NULL, NULL,
					dbo.GenerateObjectUrl(@type, O.LookupTypeID, O.ID),
					'LookupType', O.LookupTypeID, T.Name,
					'LookupType', O.LookupTypeID
			FROM	[Lookup] O
					INNER JOIN LookupType T ON O.LookupTypeID = T.ID;
	end;

	begin
		set @type = 'LookupType';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, ID, Name, Name, NULL,
					NULL, NULL, NULL,
					dbo.GenerateObjectUrl(@type, ID, ID),
					@type, 0, 'Lookup Type',
					@type, ID
			FROM	LookupType;
	end;

	begin
		set @type = 'Fusion';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, O.Name, O.Name, NULL,
					NULL, NULL, NULL, 
					dbo.GenerateObjectUrl(@type, O.FusionTypeID, O.ID),	
					'FusionType', O.FusionTypeID, T.Name,
					'FusionType', O.FusionTypeID
			FROM	Fusion O
					INNER JOIN FusionType T ON O.FusionTypeID = T.ID;
	end;

	begin
		set @type = 'FusionType';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, ID, Name, Name, Description, 
					NULL, NULL, NULL,
					dbo.GenerateObjectUrl(@type, ID, ID),
					@type, 0, 'Fusion Type',
					@type, ID
			FROM	FusionType;
	end;

	begin
		set @type = 'FusionAttribute';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, O.Name, O.TextPath, NULL, 
					case 
						when P.ID is not null then 'FusionAttribute'
						else NULL
					end, O.ParentID,	P.Name,
					'#/fusion/' + CAST(FT.ID as varchar(15)) + '/' + + CAST(O.FusionID as varchar(15)) + '/' + CAST(O.ID as varchar(15)),
					'FusionAttributeType', O.FusionAttributeTypeID, T.Name,
					'FusionAttributeType', O.FusionAttributeTypeID
			FROM	FusionAttribute O
					LEFT JOIN FusionAttribute P on P.ID = O.ParentID
					INNER JOIN FusionAttributeType T ON O.FusionAttributeTypeID = T.ID
					INNER JOIN FusionType FT ON T.FusionTypeID = FT.ID;
	end;
 
	begin
		set @type = 'FusionAttributeType';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, O.Name, O.TextPath, NULL,
					case 
						when P.ID is not null then 'FusionAttributeType'
						else NULL
					end, O.ParentID, P.Name,
					'#',--dbo.GenerateObjectUrl(@type, 0, O.ID),
					'FusionType', T.ID, T.Name,
					'FusionType', T.ID
			FROM	FusionAttributeType O
					INNER JOIN FusionType T ON O.FusionTypeID = T.ID
					LEFT JOIN FusionAttributeType P on P.ID = O.ParentID;
	end;

	begin
		set @type = 'GroupType';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			select	@type, 0, 'Group', 'Group', NULL,
					NULL, NULL, NULL,
					'#', 
					@type, 0, 'Group',
					@type, 0
	end;

	begin
		set @type = 'Policy';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, O.Name, O.TextPath, O.Description,	
					case 
						when P.ID is not null then @type
						else NULL
					end, O.ParentID, P.Name,
					dbo.GenerateObjectUrl(@type, T.ID, O.ID),
					'PolicyType', T.ID, T.Name,
					'PolicyType', T.ID
			FROM	[Policy] O
					left join Policy P on P.ID = O.ParentID
					inner join PolicyType T on T.ID = O.PolicyTypeID;
	end;

	begin
		set @type = 'PolicyType';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, ID, Name, Name, Description,	
					NULL, NULL, NULL,
					dbo.GenerateObjectUrl(@type, 0, ID),
					'PolicyType', 0, 'Policy Type',
					'PolicyType', ID
			FROM	[PolicyType]
	end;

	begin
		set @type = 'Resource';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			select	@type, ResourceID, FirstName + ' ' + LastName, FirstName + ' ' + LastName, NULL,
					NULL, NULL, NULL,
					dbo.GenerateObjectUrl(@type, 1, ResourceID), 
					'ResourceType', 1, 'Employee',
					'ResourceType', 1
			from	reporting.Global_Resource;
	end;

	begin
		set @type = 'ResourceType';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			select	@type, 0, 'Resource', 'Resource', NULL,
					NULL, NULL, NULL,
					'#', 
					@type, 0, 'Resource',
					@type, 0
	end;

	begin
		set @type = 'ResponsibilityType';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, ID,	Name,	Name, Description,
					NULL, NULL, NULL,
					'#',--dbo.GenerateObjectUrl(@type, 0, ID),
					@type, 0, 'Responsibility Type',
					@type, 0
			FROM	ResponsibilityType;
	end;

	begin
		INSERT INTO [cache].[ObjectDetails] 
		VALUES ('RuleType', 1, 'Informational', 'Informational', 'An informational rule such as a rule defining a data event.  This rule delivers events that are purely informational, and there is no need to perform any other steps.', NULL, NULL, NULL, '#/rules', 'RuleType', 0, 'Rule Type', '#000000', '#ffffff', 'In')

		INSERT INTO [cache].[ObjectDetails] 
		VALUES ('RuleType', 2, 'Quality Check', 'Quality Check', 'A quality check rule.', NULL, NULL, NULL, '#/rules', 'RuleType', 0, 'Rule Type', '#000000', '#ffffff', 'In')

		INSERT INTO [cache].[ObjectDetails] 
		VALUES ('RuleType', 3, 'Metric', 'Metric', 'A metric rule.  These rules can be included as part of scoring for a related item.', NULL, NULL, NULL, '#/rules', 'RuleType', 0, 'Rule Type', '#000000', '#ffffff', 'In')

		INSERT INTO [cache].[ObjectDetails] 
		VALUES ('RuleType', 4, 'Profile', 'Profile', 'A profile rule.', NULL, NULL, NULL, '#/rules', 'RuleType', 0, 'Rule Type', '#000000', '#ffffff', 'In')

		set @type = 'Rule';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, O.Name, O.Name, O.Description,	
					NULL, NULL,	NULL,
					dbo.GenerateObjectUrl(@type, O.ID, O.ID),
					'RuleType', RuleType, 'Rule',
					'Rule', RuleType
			FROM	[Rule] O;
	end;

	begin
		set @type = 'Taxonomy';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, O.Name, O.TextPath, O.Description,
					case 
						when P.ID is not null then @type
						else NULL
					end, O.ParentID, P.Name,
					dbo.GenerateObjectUrl(@type, O.TaxonomyTypeID, O.ID),
					'TaxonomyType',	O.TaxonomyTypeID, C.Name + ' Model :' + T.Name,
					'TaxonomyType',	O.TaxonomyTypeID
			FROM	Taxonomy O
					INNER JOIN TaxonomyType T ON O.TaxonomyTypeID = T.ID
					inner join TaxonomyTypeClass C on C.ID = T.TaxonomyTypeClassID
					left join Taxonomy P on P.ID = O.ParentID;
	end;

	begin
		set @type = 'TaxonomyType';
		insert into #Recache ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, T.ID, T.Name, T.Name, T.Description,
					NULL, NULL, NULL,
					dbo.GenerateObjectUrl(@type, T.ID, T.ID),
					@type, C.ID, C.Name,
					@type, T.ID
			FROM	TaxonomyType T
					inner join TaxonomyTypeClass C on C.ID = T.TaxonomyTypeClassID;
	end;

	-- update style for object regardless of its type.
	update	T
	set		T.IconBackColor = coalesce(S.IconBackColor, '#000000'),
			T.IconForeColor = coalesce(S.IconForeColor, '#ffffff'),
			T.IconText		= coalesce(S.IconText, 'leaf') 
	from	#Recache T
			left join ObjectStyle S ON S.ObjectType = T.StyleType and S.ObjectID = T.StyleTypeID;

	-- upsert the individual object into the cache table.
	merge	cache.ObjectDetails as T
	using	(
			SELECT	*
			FROM	#Recache
			) as S
	on		(
			T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
			)
	when matched then
			update	
			set		T.Name = S.Name,
					T.TextPath = S.TextPath,
					T.Description = S.Description,
					T.Parent = S.Parent,
					T.ParentID = S.ParentID,
					T.ParentName = S.ParentName,
					T.Url = S.Url,
					T.ObjectType = S.ObjectType,
					T.ObjectTypeID = S.ObjectTypeID,
					T.ObjectTypeName = S.ObjectTypeName,
					T.IconBackColor = S.IconBackColor,
					T.IconForeColor = S.IconForeColor,
					T.IconText = S.IconText
	when not matched then
			insert (
					[Object], ObjectID, Name, TextPath, Description, 
					Parent, ParentID, ParentName, 
					Url, 
					ObjectType, ObjectTypeID, ObjectTypeName, 
					IconBackColor, IconForeColor, IconText)
			values (
					S.[Object], S.ObjectID, S.Name, S.TextPath, S.Description, 
					S.Parent, S.ParentID, S.ParentName, 
					S.Url, 
					S.ObjectType, S.ObjectTypeID, S.ObjectTypeName, 
					S.IconBackColor, S.IconForeColor, S.IconText
					);
end