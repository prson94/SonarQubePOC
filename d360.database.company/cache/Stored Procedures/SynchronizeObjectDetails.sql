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
		ObjectType varchar(25) not null,
		ObjectTypeID int not null
	);

	if @type = 'Artifact'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'ArtifactType', O.ArtifactTypeID
			FROM	Artifact O
			WHERE	O.ID = @id
	end;

	if @type = 'ArtifactType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, @type, 0
			FROM	ArtifactType O
			WHERE	O.ID = @id;
	end;

	if @type = 'AttributeType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 
					'AttributeType', 0
			FROM	AttributeType O
			WHERE	O.ID = @id;
	end;

	if @type = 'Domain'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'DomainType', O.DomainTypeID
			FROM	Domain O
			WHERE	O.ID = @id;
	end;

	if @type = 'DomainType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, ID, @type, 0
			FROM	DomainType
			WHERE	ID = @id;
	end;

	if @type = 'Group'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, ID, 'GroupType', 0
			FROM	[Group]
			WHERE	ID = @id;
	end;

	if @type = 'GroupType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			select	@type, 0, @type, 0
	end;


	if @type = 'Intersect'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'IntersectType', O.IntersectTypeID
			FROM	[Intersect] O
			WHERE	O.ID = @id;
	end;

	if @type = 'IntersectType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, ID, @type, 0
			FROM	IntersectType
			WHERE	ID = @id;
	end;

	if @type = 'Event'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'Rule', R.ID
			FROM	[Event] O
					INNER JOIN EventGroup G on G.ID = O.EventGroupID AND O.ID = @id
					INNER JOIN [Rule] R on R.ID = G.RuleID;
	end;

	if @type = 'EventGroup'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'Rule', R.ID
			FROM	EventGroup O
					inner join [Rule] R on R.ID = O.RuleID and O.ID = @id;
	end;

	if @type = 'Lookup'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'LookupType', O.LookupTypeID
			FROM	[Lookup] O
					INNER JOIN LookupType T ON O.LookupTypeID = T.ID AND O.ID = @id;
	end;

	if @type = 'LookupType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, ID, @type, 0
			FROM	LookupType
			WHERE	ID = @id;
	end;

	if @type = 'Fusion'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'FusionType', O.FusionTypeID
			FROM	Fusion O
			WHERE	O.ID = @id;
	end;

	if @type = 'FusionType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, ID, @type, 0
			FROM	FusionType
			WHERE	ID = @id;
	end;

	if @type = 'FusionAttribute'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'FusionAttributeType', O.FusionAttributeTypeID
			FROM	FusionAttribute O
			WHERE	O.ID = @id;
	end;

	if @type = 'FusionAttributeType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'FusionType', O.FusionTypeID
			FROM	FusionAttributeType O
			WHERE	O.ID = @id;
	end;

	if @type = 'Policy'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'PolicyType', O.PolicyTypeID
			FROM	[Policy] O
			WHERE	O.ID = @id;
	end;

	if @type = 'PolicyType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, T.ID, @type, T.PolicyTypeClassID
			FROM	PolicyType T
			WHERE	T.ID = @id;
	end;

	if @type = 'Resource'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			select	@type, ResourceID, 'ResourceType', 1
			from	reporting.Global_Resource 
			where	ResourceID = @id;
	end;

	if @type = 'ResourceType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			select	@type, 1, @type, 0
	end;

	if @type = 'ResponsibilityType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, ID, @type, 0
			FROM	ResponsibilityType
			WHERE	ID = @id;

		--UPDATE	T
		--SET		T.ResponsibilityType = S.Name
		--FROM	cache.ResponsibilityItem T INNER JOIN @item S ON S.[Object] = @type and S.ObjectID = T.ResponsibilityTypeID
	end;

	if @type = 'Rule'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'RuleType', O.RuleType
			FROM	[Rule] O
			WHERE	O.ID = @id;
	end;

	if @type = 'Taxonomy'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'TaxonomyType', O.TaxonomyTypeID
			FROM	Taxonomy O
			WHERE	O.ID = @id;
	end;

	if @type = 'TaxonomyType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, T.ID, @type, T.TaxonomyTypeClassID
			FROM	TaxonomyType T
			WHERE	T.ID = @id;
	end;

	-- upsert the individual object into the cache table.
	merge	cache.[Object] as T
	using	(
			SELECT	*
			FROM	@item
			) as S
	on		(
			T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
			)
	when matched then
			update	
			set		T.ObjectType = S.ObjectType, 
					T.ObjectTypeID = S.ObjectTypeID
	when not matched then
			insert ( [Object], ObjectID, ObjectType, ObjectTypeID )
			values ( S.[Object], S.ObjectID, S.ObjectType, S.ObjectTypeID );
end
GO