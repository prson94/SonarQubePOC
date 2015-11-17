CREATE procedure AddMappingDependencies
--declare
	@ResourceID int,
	@MappingID int,
	@SourceSystem varchar(50),
	@SourceSystemID int,
	@SourceObject varchar(50),
	@SourceObjectID int,
	@SourceFusionAttributeID int,

	@TargetSystem varchar(50),
	@TargetSystemID int,
	@TargetObject varchar(50),
	@TargetObjectID int,
	@TargetFusionAttributeID int,

	@Contexts varchar(2500) = null

	--set @ResourceID = 1
	--set @MappingID = 1
	--set @SourceSystem = 'Artifact'
	--set @SourceSystemID = 733
	--set @SourceObject = 'Artifact'
	--set @SourceObjectID = 4651
	--set @SourceFusionAttributeID = 3613

	--set @TargetSystem = 'Artifact'
	--set @TargetSystemID = 772
	--set @TargetObject = 'Artifact'
	--set @TargetObjectID = 4651
	--set @TargetFusionAttributeID = 105572
as
begin
	set nocount on;
	declare @SourceIntersectID int,
			@SourceFusionIntersectID int,
			@TargetIntersectID int,
			@TargetFusionIntersectID int,
			@ResponsibilityID int,
			@Date datetime = getutcdate()

	-- create and get source intersect
	EXEC AddRelationship @ResourceID, @Date, @SourceSystem, @SourceSystemID, 1, NULL, @SourceObject, @SourceObjectID
	select	@SourceIntersectID = IntersectID
	from	[cache].[Relationships]
	where	SourceObject = @SourceSystem and SourceObjectID = @SourceSystemID and TargetObject = @SourceObject and TargetObjectID = @SourceObjectID

	-- create and get source fusion intersect
	EXEC AddRelationship @ResourceID, @Date, 'Intersect', @SourceIntersectID, 1, NULL, 'FusionAttribute', @SourceFusionAttributeID
	select	@SourceFusionIntersectID = IntersectID
	from	[cache].[Relationships]
	where	SourceObject = 'Intersect' and SourceObjectID = @SourceIntersectID and TargetObject = 'FusionAttribute' and TargetObjectID = @SourceFusionAttributeID

	-- create and get target intersect
	EXEC AddRelationship @ResourceID, @Date, @TargetSystem, @TargetSystemID, 1, NULL, @TargetObject, @TargetObjectID
	select	@TargetIntersectID = IntersectID
	from	[cache].[Relationships]
	where	SourceObject = @TargetSystem and SourceObjectID = @TargetSystemID and TargetObject = @TargetObject and TargetObjectID = @TargetObjectID

	-- create and get target fusion intersect
	EXEC AddRelationship @ResourceID, @Date, 'Intersect', @TargetIntersectID, 1, NULL, 'FusionAttribute', @TargetFusionAttributeID
	select	@TargetFusionIntersectID = IntersectID
	from	[cache].[Relationships]
	where	SourceObject = 'Intersect' and SourceObjectID = @TargetIntersectID and TargetObject = 'FusionAttribute' and TargetObjectID = @TargetFusionAttributeID


	select	@ResponsibilityID = ID
	from	Responsibility 
	where	ResponsibilityTypeID = 0 and ObjectType = 'Intersect' and ObjectID = @TargetIntersectID and ResponsibleObjectType = @SourceSystem and ResponsibleObjectID = @SourceSystemID
	if @ResponsibilityID is null
	begin
		insert into Responsibility	(ResponsibilityTypeID, ObjectType, ObjectID, ResponsibleObjectType, ResponsibleObjectID, UpdatedOn, UpdatedBy, Visible)
		values						(0, 'Intersect', @TargetIntersectID, @SourceSystem, @SourceSystemID, @Date, @ResourceID, 1)
		set @ResponsibilityID = SCOPE_IDENTITY()
	end

	if not exists(select 1 from MappingItem where MappingID = @MappingID and SourceIntersectID = @SourceFusionIntersectID and TargetIntersectID = @TargetFusionIntersectID and ResponsibilityID = @ResponsibilityID)
	begin
		insert into MappingItem (MappingID, SourceIntersectID, TargetIntersectID, ResponsibilityID, UpdatedOn, UpdatedBy) 
		values					(@MappingID, @SourceFusionIntersectID, @TargetFusionIntersectID, @ResponsibilityID, @date, @ResourceID) 
	end
end
