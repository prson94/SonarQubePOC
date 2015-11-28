CREATE procedure [cache].[SynchronizeObjectDetails]
--declare 
	@type varchar(50),
	@id int
--set @type = 'IntersectType'
--set @id = 27
as
begin
	set nocount on;

	declare @item table (
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
		IconBackColor varchar(15) null,
		IconForeColor varchar(15) null,
		IconText varchar(15) null,
		StyleType varchar(25) not null,
		StyleTypeID int not null
	);

	declare @ShouldRecacheIntersectTypes bit = 0,
			@ShouldRecacheIntersects bit = 0,
			@ShouldRecacheResponsibility bit = 0,
			@ShouldRecacheResponsibilityTypeName bit = 0,
			@ShouldRecacheResponsibilityResourceInfo bit = 0,
			@ShouldRecacheRelationships bit = 0

	if @type = 'Artifact'
	begin
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, O.Name, O.TextPath, O.Description,
					case 
						when P.ID is not null then 'Artifact'
						else NULL
					end, O.ParentID, P.Name,
					dbo.GenerateObjectUrl(@type, O.ArtifactTypeID, O.ID),
					'ArtifactType', O.ArtifactTypeID, T.Name,
					'ArtifactType', O.ArtifactTypeID
			FROM	Artifact O
					INNER JOIN ArtifactType T ON O.ArtifactTypeID = T.ID and O.ID = @id
					left join Artifact P on P.ID = O.ParentID;

		with artifactHierarchy as (
			select	A.ID,
					A.Name,
					A.ParentID,
					A.TextPath,
					P.Name as ParentName
			from	Artifact A
					left join Artifact P on P.ID = A.ParentID
			where	A.ParentID = @id
			union all
			select	C.ID,
					C.Name,
					C.ParentID,
					C.TextPath,
					P.Name
			from	Artifact C
					inner join artifactHierarchy P on P.ID = C.ParentID
		)
		update	T
		set		T.TextPath = S.TextPath,
				T.ParentName = S.ParentName
		from	cache.ObjectDetails T
				inner join artifactHierarchy S on T.[Object] = 'Artifact' and T.ObjectID = S.ID;

		set @ShouldRecacheIntersects = 1;
		set @ShouldRecacheResponsibility = 1;
		set @ShouldRecacheRelationships = 1;
	end;

	if @type = 'ArtifactType'
	begin
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, O.Name, O.Name, O.Description,	
					case 
						when P.ID is not null then 'ArtifactType'
						else NULL
					end, O.ParentID, P.Name,
					dbo.GenerateObjectUrl(@type, O.ID, O.ID),	
					@type, 0, 'Artifact Type',
					@type, O.ID
			FROM	ArtifactType O
					left join ArtifactType P on P.ID = O.ParentID
			WHERE	O.ID = @id;

		update	T
		set		T.ObjectTypeName = S.Name
		from	cache.ObjectDetails T
				inner join @item S on T.[Object] = 'Artifact' and T.ObjectTypeID = S.ObjectID;

		set @ShouldRecacheIntersectTypes = 1;
		set @ShouldRecacheResponsibility = 1;
		set @ShouldRecacheRelationships = 1;
	end;

	--if @type = 'Attribute'
	--begin
		--insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
		--	SELECT	@type, O.ID, '', '', '',
		--			O.ParentID,	@type,		D.Url,	O.AttributeTypeID,	'AttributeType',	T.Name,
		--			'AttributeType', T.ID
		--	FROM	[Attribute] O
		--			INNER JOIN AttributeType T ON O.AttributeTypeID = T.ID and O.ID = @id
		--			cross apply  utility.ObjectDetail(O.ObjectType, O.ObjectID) D
	--end;

	if @type = 'AttributeType'
	begin
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, O.Name, O.Name, O.Description,	
					case 
						when P.ID is not null then @type
						else NULL
					end, O.ParentID, P.Name,
					'#',--dbo.GenerateObjectUrl(@type, 0, O.ID),	
					'AttributeType', 0, 'Attribute Type',
					'AttributeType', O.ID
			FROM	AttributeType O
					left join AttributeType P on P.ID = O.ParentID
			WHERE	O.ID = @id;

		update	T
		set		T.ObjectTypeName = S.Name
		from	cache.ObjectDetails T
				inner join @item S on T.[Object] = 'Attribute' and T.ObjectTypeID = S.ObjectID
	end;

	if @type = 'Domain'
	begin
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, O.Name, O.Name, O.Description,	
					NULL, NULL, NULL,
					dbo.GenerateObjectUrl(@type, O.DomainTypeID, O.ID),	
					'DomainType', O.DomainTypeID, T.Name,
					'DomainType', O.DomainTypeID
			FROM	Domain O
					INNER JOIN DomainType T ON O.DomainTypeID = T.ID and O.ID = @id;

		set @ShouldRecacheIntersects = 1;
		set @ShouldRecacheResponsibility = 1;
		set @ShouldRecacheRelationships = 1;
	end;

	if @type = 'DomainType'
	begin
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, ID, Name, Name, Description,	
					NULL, NULL, NULL,
					dbo.GenerateObjectUrl(@type, ID, ID),
					@type, 0, 'Domain Type',
					@type, ID
			FROM	DomainType
			WHERE	ID = @id;

		update	T
		set		T.ObjectTypeName = S.Name
		from	cache.ObjectDetails T
				inner join @item S on T.[Object] = 'Domain' and T.ObjectTypeID = S.ObjectID;

		set @ShouldRecacheIntersectTypes = 1;
		set @ShouldRecacheResponsibility = 1;
		set @ShouldRecacheRelationships = 1;
	end;

	if @type = 'Group'
	begin
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, ID, Name, Name, Description,	
					NULL, NULL, NULL,
					dbo.GenerateObjectUrl(@type, 0, ID),
					'Group', 0, @type,
					'GroupType', 0
			FROM	[Group]
			WHERE	ID = @id;
		
		set @ShouldRecacheResponsibilityResourceInfo = 1;
		set @ShouldRecacheRelationships = 1;
	end;

	if @type = 'GroupType'
	begin
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			select	@type, 0, 'Group', 'Group', NULL,
					NULL, NULL, NULL,
					'#', 
					@type, 0, 'Group',
					@type, 0
	end;


	if @type = 'Intersect'
	begin
		declare @IntersectName nvarchar(500) = utility.DeriveIntersectName(@id)
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, @IntersectName, @IntersectName, O.Description,
					NULL, NULL, NULL,
					'#',--dbo.GenerateObjectUrl(@type, O.IntersectTypeID, O.ID),
					'IntersectType', O.IntersectTypeID,	T.Name,
					'IntersectType', O.IntersectTypeID
			FROM	[Intersect] O
					INNER JOIN IntersectType T ON O.IntersectTypeID = T.ID and O.ID = @id;

		set @ShouldRecacheIntersects = 1;
		set @ShouldRecacheRelationships = 1;
	end;

	if @type = 'IntersectType'
	begin
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, ID, Name, Name, NULL,
					NULL, NULL, NULL,
					'#',--dbo.GenerateObjectUrl(@type, 0, ID),
					@type, 0, 'Intersect Type',
					@type, ID
			FROM	IntersectType
			WHERE	ID = @id;

		with intersectTypeHierarchy as (
			select	I.ID,
					I.Name,
					T.Name as ObjectTypeName
			from	[Intersect] I
					inner join @item T on T.ObjectID = I.IntersectTypeID
			where	IntersectTypeID = @id
		)

		update	T
		set		T.ObjectTypeName = S.ObjectTypeName,
				T.Name = S.Name
		from	cache.ObjectDetails T
				inner join intersectTypeHierarchy S on T.[Object] = 'Intersect' and T.ObjectID = S.ID;

		set @ShouldRecacheIntersectTypes = 1;
		set @ShouldRecacheRelationships = 1;
	end;

	if @type = 'Event'
	begin
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, G.Name + ' event',	R.Name + '.' + G.Name + ' event', NULL,
					'EventGroup', G.ID, G.Name,
					dbo.GenerateObjectUrl(@type, R.ID, O.ID),
					'Rule', R.ID, R.Name,
					'Rule', R.ID
			FROM	[Event] O
					INNER JOIN EventGroup G on G.ID = O.EventGroupID AND O.ID = @id
					INNER JOIN [Rule] R on R.ID = G.RuleID;
	end;

	if @type = 'EventGroup'
	begin
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, O.Name, R.Name + '.' + O.Name, NULL,
					NULL, NULL, NULL,
					dbo.GenerateObjectUrl(@type, 0, O.ID),
					'Rule', R.ID, R.Name,
					'Rule', R.ID
			FROM	EventGroup O
					inner join [Rule] R on R.ID = O.RuleID
			WHERE	O.ID = @id;

		with eventGroupHierarchy as (
			select	I.ID,
					T.Name as ParentName,
					T.TextPath + ' event' as TextPath
			from	[Event] I
					inner join @item T on T.ObjectID = I.EventGroupID
		)

		update	T
		set		T.ParentName = S.ParentName,
				T.TextPath = S.TextPath
		from	cache.ObjectDetails T
				inner join eventGroupHierarchy S on T.[Object] = 'Event' and T.ObjectID = S.ID
	end;

	if @type = 'Lookup'
	begin
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, T.Name + ' Item', T.Name + ' Item', NULL,
					NULL, NULL, NULL,
					dbo.GenerateObjectUrl(@type, O.LookupTypeID, O.ID),
					'LookupType', O.LookupTypeID, T.Name,
					'LookupType', O.LookupTypeID
			FROM	[Lookup] O
					INNER JOIN LookupType T ON O.LookupTypeID = T.ID AND O.ID = @id;
	end;

	if @type = 'LookupType'
	begin
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, ID, Name, Name, NULL,
					NULL, NULL, NULL,
					dbo.GenerateObjectUrl(@type, ID, ID),
					@type,	0, 'Lookup Type',
					@type, ID
			FROM	LookupType
			WHERE	ID = @id;

		update	T
		set		T.ObjectTypeName = S.Name
		from	cache.ObjectDetails T
				inner join @item S on T.[Object] = 'Lookup' and T.ObjectTypeID = @id
	end;

	if @type = 'Fusion'
	begin
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, O.Name, O.Name, NULL,
					NULL, NULL, NULL, 
					dbo.GenerateObjectUrl(@type, O.FusionTypeID, O.ID),	
					'FusionType', O.FusionTypeID, T.Name,
					'FusionType', O.FusionTypeID
			FROM	Fusion O
					INNER JOIN FusionType T ON O.FusionTypeID = T.ID and O.ID = @id;
		
		set @ShouldRecacheResponsibility = 1;
	end;

	if @type = 'FusionType'
	begin
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, ID, Name, Name, Description, 
					NULL, NULL, NULL,
					dbo.GenerateObjectUrl(@type, ID, ID),
					@type, 0, 'Fusion Type',
					@type, ID
			FROM	FusionType
			WHERE	ID = @id;

		update	T
		set		T.ObjectTypeName = S.Name
		from	cache.ObjectDetails T
				inner join @item S on T.[Object] = 'Fusion' and T.ObjectTypeID = S.ObjectID;
		update	T
		set		T.ObjectTypeName = S.Name
		from	cache.ObjectDetails T
				inner join @item S on T.[Object] = 'FusionAttributeType' and T.ObjectTypeID = S.ObjectID;

		set @ShouldRecacheResponsibility = 1;
	end;

	if @type = 'FusionAttribute'
	begin
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, O.Name, O.TextPath, NULL, 
					case 
						when P.ID is not null then 'Artifact'
						else NULL
					end, O.ParentID,	P.Name,
					'#/fusion/' + CAST(FT.ID as varchar(15)) + '/' + + CAST(O.FusionID as varchar(15)) + '/' + CAST(O.ID as varchar(15)),
					'FusionAttributeType', O.FusionAttributeTypeID, T.Name,
					'FusionAttributeType', O.FusionAttributeTypeID
			FROM	FusionAttribute O
					LEFT JOIN FusionAttribute P on P.ID = O.ParentID
					INNER JOIN FusionAttributeType T ON O.FusionAttributeTypeID = T.ID and O.ID = @id
					INNER JOIN FusionType FT ON T.FusionTypeID = FT.ID;

		with fusionAttributeHierarchy as (
			select	A.ID,
					A.Name,
					A.ParentID,
					A.TextPath,
					P.Name as ParentName
			from	FusionAttribute A
					left join FusionAttribute P on P.ID = A.ParentID
			where	A.ParentID = @id
			union all
			select	C.ID,
					C.Name,
					C.ParentID,
					C.TextPath,
					P.Name
			from	FusionAttribute C
					inner join fusionAttributeHierarchy P on P.ID = C.ParentID
		)
		update	T
		set		T.TextPath = S.TextPath,
				T.ParentName = S.ParentName
		from	cache.ObjectDetails T
				inner join fusionAttributeHierarchy S on T.[Object] = 'FusionAttribute' and T.ObjectID = S.ID;

		set @ShouldRecacheIntersects = 1;
		set @ShouldRecacheRelationships = 1;
	end;

	if @type = 'FusionAttributeType'
	begin
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, O.Name, O.TextPath, NULL,
					case 
						when P.ID is not null then 'Artifact'
						else NULL
					end, O.ParentID, P.Name,
					'#',--dbo.GenerateObjectUrl(@type, 0, O.ID),
					'FusionType', T.ID, T.Name,
					'FusionType', T.ID
			FROM	FusionAttributeType O
					INNER JOIN FusionType T ON O.FusionTypeID = T.ID and O.ID = @id
					LEFT JOIN FusionAttributeType P on P.ID = O.ParentID;

		update	T
		set		T.ObjectTypeName = S.Name
		from	cache.ObjectDetails T
				inner join @item S on T.[Object] = 'FusionAttribute' and T.ObjectTypeID = S.ObjectID;

		with fusionAttributeTypeHierarchy as (
			select	A.ID,
					A.Name,
					A.ParentID,
					A.TextPath,
					P.Name as ParentName
			from	FusionAttributeType A
					left join FusionAttributeType P on P.ID = A.ParentID
			where	A.ParentID = @id
			union all
			select	C.ID,
					C.Name,
					C.ParentID,
					C.TextPath,
					P.Name
			from	FusionAttributeType C
					inner join fusionAttributeTypeHierarchy P on P.ID = C.ParentID
		)
		update	T
		set		T.TextPath = S.TextPath,
				T.ParentName = S.ParentName
		from	cache.ObjectDetails T
				inner join fusionAttributeTypeHierarchy S on T.[Object] = 'FusionAttributeType' and T.ObjectID = S.ID;

		set @ShouldRecacheIntersectTypes = 1;
		set @ShouldRecacheRelationships = 1;
	end;

	if @type = 'Policy'
	begin
		if @id = 0
			begin
				insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
					select	@type, 0, 'Policy', 'Policy', NULL,
							NULL, NULL, NULL,
							'#', 
							@type, 0, 'Policy',
							@type, 0
			end
		else
			begin
				insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
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
							inner join PolicyType T on T.ID = O.PolicyTypeID
					WHERE	O.ID = @id;

				with policyHierarchy as (
					select	A.ID,
							A.Name,
							A.ParentID,
							A.TextPath,
							P.Name as ParentName
					from	Policy A
							left join Policy P on P.ID = A.ParentID
					where	A.ParentID = @id
					union all
					select	C.ID,
							C.Name,
							C.ParentID,
							C.TextPath,
							P.Name
					from	Policy C
							inner join policyHierarchy P on P.ID = C.ParentID
				)
				update	T
				set		T.TextPath = S.TextPath,
						T.ParentName = S.ParentName
				from	cache.ObjectDetails T
						inner join policyHierarchy S on T.[Object] = 'Policy' and T.ObjectID = S.ID;
			end

		set @ShouldRecacheResponsibility = 1;
		set @ShouldRecacheRelationships = 1;
	end;

	if @type = 'PolicyType'
	begin
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, T.ID, T.Name, T.Name, T.Description,
					NULL, NULL, NULL,
					dbo.GenerateObjectUrl(@type, T.ID, T.ID),
					@type, 0, C.Name,
					@type, T.ID
			FROM	PolicyType T
					inner join PolicyTypeClass C on C.ID = T.PolicyTypeClassID
			WHERE	T.ID = @id;

		update	T
		set		T.ObjectTypeName = S.Name
		from	cache.ObjectDetails T
				inner join @item S on T.[Object] = 'Policy' and T.ObjectTypeID = S.ObjectID;

		set @ShouldRecacheIntersectTypes = 1;
		set @ShouldRecacheResponsibility = 1;
		set @ShouldRecacheRelationships = 1;
	end;

	if @type = 'Resource'
	begin
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			select	@type, ResourceID, FirstName + ' ' + LastName, FirstName + ' ' + LastName, NULL,
					NULL, NULL, NULL,
					dbo.GenerateObjectUrl(@type, 1, @id), 
					'ResourceType', 1, 'Employee',
					'ResourceType', 1
			from	reporting.Global_Resource 
			where	ResourceID = @id;

		set @ShouldRecacheResponsibilityResourceInfo = 1;
		set @ShouldRecacheRelationships = 1;
	end;

	if @type = 'ResourceType'
	begin
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			select	@type, 0, 'Resource', 'Resource', NULL,
					NULL, NULL, NULL,
					'#', 
					@type, 0, 'Resource',
					@type, 0
	end;

	if @type = 'ResponsibilityType'
	begin
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, ID,	Name,	Name, Description,
					NULL, NULL, NULL,
					'#',--dbo.GenerateObjectUrl(@type, 0, ID),
					@type, 0, 'Responsibility Type',
					@type, ID
			FROM	ResponsibilityType
			WHERE	ID = @id;

		set @ShouldRecacheResponsibilityTypeName = 1;
	end;

	if @type = 'Rule'
	begin
		if @id = 0
			begin
				insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
					select	@type, 0, 'Rule', 'Rule', NULL,
							NULL, NULL, NULL,
							'#', 
							@type, 0, 'Rule',
							@type, 0
			end
		else
			begin
				insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
					SELECT	@type, O.ID, O.Name, O.Name, O.Description,	
							NULL, NULL,	NULL,
							dbo.GenerateObjectUrl(@type, O.RuleType, O.ID),
							'RuleType', O.RuleType, 'Rule',
							'RuleType', O.RuleType
					FROM	[Rule] O
					WHERE	O.ID = @id;

				update	T
				set		T.ObjectTypeName = S.Name
				from	cache.ObjectDetails T
						inner join @item S on T.[Object] = 'EventGroup' and T.ObjectTypeID = S.ObjectID;
				update	T
				set		T.ObjectTypeName = S.Name
				from	cache.ObjectDetails T
						inner join @item S on T.[Object] = 'Event' and T.ObjectTypeID = S.ObjectID;
			end

		set @ShouldRecacheResponsibility = 1;
		set @ShouldRecacheRelationships = 1;
	end;

	if @type = 'Taxonomy'
	begin
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, O.ID, O.Name, O.TextPath, O.Description,
					case 
						when P.ID is not null then @type
						else NULL
					end, O.ParentID, P.Name,
					dbo.GenerateObjectUrl(@type, O.TaxonomyTypeID, O.ID),
					'TaxonomyType',	O.TaxonomyTypeID, C.Name + ' Model :' + T.Name,
					'TaxonomyType',	O.TaxonomyTypeID
			FROM	Taxonomy O
					INNER JOIN TaxonomyType T ON O.TaxonomyTypeID = T.ID AND O.ID = @id
					inner join TaxonomyTypeClass C on C.ID = T.TaxonomyTypeClassID
					left join Taxonomy P on P.ID = O.ParentID;

		with taxonomyHierarchy as (
			select	A.ID,
					A.Name,
					A.ParentID,
					A.TextPath,
					P.Name as ParentName
			from	Taxonomy A
					left join Taxonomy P on P.ID = A.ParentID
			where	A.ParentID = @id
			union all
			select	C.ID,
					C.Name,
					C.ParentID,
					C.TextPath,
					P.Name
			from	Taxonomy C
					inner join taxonomyHierarchy P on P.ID = C.ParentID
		)
		update	T
		set		T.TextPath = S.TextPath,
				T.ParentName = S.ParentName
		from	cache.ObjectDetails T
				inner join taxonomyHierarchy S on T.[Object] = 'Taxonomy' and T.ObjectID = S.ID;

		set @ShouldRecacheIntersects = 1;
		set @ShouldRecacheResponsibility = 1;
		set @ShouldRecacheRelationships = 1;
	end;

	if @type = 'TaxonomyType'
	begin
		insert into @item ([Object], ObjectID, Name, TextPath, Description, Parent, ParentID, ParentName, Url, ObjectType, ObjectTypeID, ObjectTypeName, StyleType, StyleTypeID)
			SELECT	@type, T.ID, T.Name, T.Name, T.Description,
					NULL, NULL, NULL,
					dbo.GenerateObjectUrl(@type, T.ID, T.ID),
					@type, 0, C.Name,
					@type, T.ID
			FROM	TaxonomyType T
					inner join TaxonomyTypeClass C on C.ID = T.TaxonomyTypeClassID
			WHERE	T.ID = @id;

		update	T
		set		T.ObjectTypeName = S.Name
		from	cache.ObjectDetails T
				inner join @item S on T.[Object] = 'Taxonomy' and T.ObjectTypeID = S.ObjectID;

		set @ShouldRecacheIntersectTypes = 1;
		set @ShouldRecacheResponsibility = 1;
		set @ShouldRecacheRelationships = 1;
	end;

	-- update style for object regardless of its type.
	update	T
	set		T.IconBackColor = coalesce(S.IconBackColor, '#000000'),
			T.IconForeColor = coalesce(S.IconForeColor, '#ffffff'),
			T.IconText		= coalesce(S.IconText, 'leaf') 
	from	@item T
			left join ObjectStyle S ON S.ObjectType = T.StyleType and S.ObjectID = T.StyleTypeID;

	-- upsert the individual object into the cache table.
	merge	cache.ObjectDetails as T
	using	(
			SELECT	*
			FROM	@item
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

	if @ShouldRecacheIntersectTypes = 1
	begin
		with intersectTypeRecacheHierarchy as (
			select	O.ID,
					O.Name
			from	IntersectTypeNode N
					inner join [IntersectType] O on O.ID = N.IntersectTypeID and N.ObjectType = @type and N.ObjectID = @id
		)
		update	T
		set		T.Name = S.Name
		from	cache.ObjectDetails T
				inner join intersectTypeRecacheHierarchy S on T.[Object] = 'IntersectType' and T.ObjectID = S.ID
	end

	if @ShouldRecacheIntersects = 1
	begin
		with intersectRecacheHierarchy as (
			select	O.ID,
					O.Name
			from	IntersectNode N
					inner join [Intersect] O on O.ID = N.IntersectID and N.ObjectType = @type and N.ObjectID = @id
		)
		update	T
		set		T.Name = S.Name
		from	cache.ObjectDetails T
				inner join intersectRecacheHierarchy S on T.[Object] = 'Intersect' and T.ObjectID = S.ID
	end

	if @ShouldRecacheResponsibility = 1
	begin
		UPDATE	T
		SET		T.AssigningItemName = S.Name
		FROM	cache.Responsibilities T INNER JOIN @item S ON	T.AssigningItem = S.[Object]		and T.AssigningItemID = S.ObjectID

		UPDATE	T
		SET		T.AssigningTypeName = S.Name
		FROM	cache.Responsibilities T INNER JOIN @item S ON	T.AssigningItemType = S.[Object]	and T.AssigningItemTypeID = S.ObjectID

		UPDATE	T
		SET		T.ObjectName = S.Name
		FROM	cache.Responsibilities T INNER JOIN @item S ON	T.[Object] = S.[Object]				and T.ObjectID = S.ObjectID

		UPDATE	T
		SET		T.ObjectTypeName = S.Name
		FROM	cache.Responsibilities T INNER JOIN @item S ON	T.ObjectType = S.[Object]			and T.ObjectTypeID = S.ObjectID
	end

	if @ShouldRecacheResponsibilityTypeName = 1
	begin
		UPDATE	T
		SET		T.ResponsibilityType = S.Name
		FROM	cache.Responsibilities T INNER JOIN @item S ON S.ObjectID = T.ResponsibilityTypeID
	end

	if @ShouldRecacheResponsibilityResourceInfo = 1
	begin
		UPDATE	T
		SET		T.ResponsibleObjectName = S.Name
		FROM	cache.Responsibilities T INNER JOIN @item S ON	T.ResponsibleObject = S.[Object]	and T.ResponsibleObjectID = S.ObjectID
	end

	if @ShouldRecacheRelationships = 1
	begin
		UPDATE	R
		SET		R.SourceObjectName = S.Name
		FROM	cache.Relationships R INNER JOIN @item S ON R.SourceObject = S.[Object] and R.SourceObjectID = S.ObjectID

		UPDATE	R
		SET		R.SourceTypeName = S.Name
		FROM	cache.Relationships R INNER JOIN @item S ON R.SourceType = S.[Object] and R.SourceTypeID = S.ObjectID

		UPDATE	R
		SET		R.TargetObjectName = S.Name
		FROM	cache.Relationships R INNER JOIN @item S ON R.TargetObject = S.[Object] and R.TargetObjectID = S.ObjectID

		UPDATE	R
		SET		R.TargetTypeName = S.Name
		FROM	cache.Relationships R INNER JOIN @item S ON R.TargetType = S.[Object] and R.TargetTypeID = S.ObjectID
	end
end
GO