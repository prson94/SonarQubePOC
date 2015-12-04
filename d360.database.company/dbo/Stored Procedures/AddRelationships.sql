create procedure [dbo].[AddRelationships]
--declare
	@ResourceID int,
	@Date datetime,
	@Type varchar(50),				-- The start object type.
	@ID int,						-- The start object ID.
	@Classification int,
	@IntersectRole int,
	@Description nvarchar(4000),
	@Objects ObjectsTable READONLY
	
--set @ResourceID = 1
--set @Date = getutcdate()
--set @Type = 'Artifact'
--set @ID = 4651
--set @Classification = 1
--set @IntersectRole = NULL
--set @Description = ''
--insert into @Objects VALUES ('Artifact', 11808)
as
begin
	set nocount on;

	if @IntersectRole = 0 
	begin
		set @IntersectRole = null
	end

	declare @current int,
			@max int,
			@ErrorMessage nvarchar(2500),
			@IntersectID int
	
	declare @Intersects IDTable
	
	/*	Get all relationships that we need to calculate.	*/
	declare @Relations table (
		ID int identity, 
			
		StartObject varchar(50), StartObjectID int, 
		StartName nvarchar(500), StartTypeID int, StartIntersectNodeTypeID int, 

		EndObject varchar(50), EndObjectID int,
		EndName nvarchar(500), EndTypeID int, EndIntersectNodeTypeID int,
		IntersectTypeID int, IntersectID int, [Action] varchar(1)
	)
	insert into @Relations
		select	distinct 
				O.ObjectType, O.ObjectID,
				OD.Name, OD.ObjectTypeID, RT.SourceIntersectTypeNodeID, 
				@Type, @ID,
				D.Name, D.ObjectTypeID, RT.TargetIntersectTypeNodeID,
				RT.IntersectTypeID, R.ID, CASE WHEN R.ID IS NULL THEN 'C' ELSE 'U' END
		from	@Objects O
				left join cache.ObjectDetails OD on OD.[Object] = @Type and OD.ObjectID = @ID
				left join cache.ObjectDetails D on D.[Object] = O.ObjectType and D.ObjectID = O.ObjectID
				left join utility.RelationshipTypes RT on RT.SourceObjectType = OD.ObjectType and RT.SourceObjectID = OD.ObjectTypeID and RT.TargetObjectType = D.ObjectType and RT.TargetObjectID = D.ObjectTypeID
				outer apply (
							select	i.ID
							from	[Intersect] I
									inner join IntersectNode N1 on N1.IntersectID = I.ID and N1.ObjectType = @Type and N1.ObjectID = @ID
									inner join IntersectNode N2 on N2.IntersectID = I.ID and N2.ObjectType = O.ObjectType and N2.ObjectID = O.ObjectID
							where	i.IntersectTypeID = RT.IntersectTypeID
							) R

	set @current = 1
	select @max = MAX(ID) from @Relations
	while @current <= @max
	begin
		declare @StartObject varchar(50),	@StartObjectID int, @StartName nvarchar(500),	@StartTypeID int,	@StartIntersectNodeTypeID int, 
				@EndObject varchar(50),		@EndObjectID int,	@EndName nvarchar(500),		@EndTypeID int,		@EndIntersectNodeTypeID int,
				@IntersectTypeID int,		@Action varchar(1)

		set @IntersectID = null	--reset here

		select	@StartObject = StartObject,
				@StartObjectID = StartObjectID,
				@StartName = StartName,	
				@StartTypeID = StartTypeID,	
				@StartIntersectNodeTypeID = StartIntersectNodeTypeID, 

				@EndObject = EndObject,
				@EndObjectID = EndObjectID,	
				@EndName = EndName,	
				@EndTypeID = EndTypeID,	
				@EndIntersectNodeTypeID = EndIntersectNodeTypeID,

				@IntersectTypeID = IntersectTypeID, 
				@IntersectID = IntersectID, 
				@Action = [Action]
		from	@Relations
		where	ID = @current

		if @ID > 0
		begin
			-- Relationship does not yet exist, so CREATE.
			if @IntersectID is null and @StartIntersectNodeTypeID is not null and @EndIntersectNodeTypeID is not null
				begin
					INSERT INTO [Intersect] (IntersectTypeID, Classification, [Description], [IntersectTypeRoleID]) VALUES (@IntersectTypeID, @Classification, @Description, @IntersectRole)

					SELECT @IntersectID = SCOPE_IDENTITY()

					INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID) 
					VALUES						(@StartIntersectNodeTypeID, @IntersectID, @StartObject, @StartObjectID)

					INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
					VALUES						(@EndIntersectNodeTypeID, @IntersectID, @EndObject, @EndObjectID)

					update	@Relations
					set		IntersectID = @IntersectID
					where	(StartObject = @StartObject and StartObjectID = @StartObjectID and EndObject = @EndObject and EndObjectID = @EndObjectID) 
							or (StartObject = @EndObject and StartObjectID = @EndObjectID and EndObject = @StartObject and EndObjectID = @StartObjectID)
							--ID = @current

					exec utility.AddAuditEntry @StartObject, @StartObjectID, @ResourceID, @Date, 'Created', 'Intersect', @IntersectID
					exec utility.AddAuditEntry @EndObject, @EndObjectID, @ResourceID, @Date, 'Created', 'Intersect', @IntersectID
				end
			else
				begin
					-- Update the Classification and Description only if the relationship already exists.
					if @IntersectID is not null
					begin
						update	[Intersect]
						set		Classification = @Classification,
								Description = @Description,
								IntersectTypeRoleID = @IntersectRole
						where	ID = @IntersectID

						exec utility.AddAuditEntry 'Intersect', @IntersectID, @ResourceID, @Date, 'Updated', 'Intersect', @IntersectID
					end
				end
			
			if (@IntersectID is not null) and (not exists(select 1 from @Intersects where ObjectID = @IntersectID))
			begin
				insert into @Intersects VALUES (@IntersectID)
				exec [cache].[SynchronizeObjectDetails] 'Intersect', @IntersectID
			end
		end

		set @current = @current + 1
	end

	exec cache.SynchronizeRelationships @Intersects
end