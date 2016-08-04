CREATE procedure [dbo].[AddRelationships]
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
--set @ID = 3
--set @Classification = 1
--set @IntersectRole = NULL
--set @Description = ''
--insert into @Objects VALUES ('Artifact', 2)
as
begin
	set nocount on;

	declare @current int,
			@max int,
			@ErrorMessage nvarchar(2500),
			@IntersectID int,
			
			@StartType varchar(50),	@StartTypeID int,
			@EndType varchar(50),	@EndTypeID int,	
			@IntersectTypeID int
	
	/*	Get the relationship types we need to check or create.	*/
	declare @RelationTypes table (
		ID int identity, 
		StartType varchar(50), StartTypeID int, 
		EndType varchar(50), EndTypeID int, 
		IntersectTypeID int
	)

	insert into @RelationTypes
		select	* 
		from	(
				select	distinct 
						S.ObjectType as StartType, S.ObjectTypeID as StartTypeID, 
						E.ObjectType as EndType, E.ObjectTypeID as EndTypeID, 
						RT.IntersectTypeID
				from	@Objects O
						inner join cache.[Object] S on S.[Object] = @Type and S.ObjectID = @ID
						inner join cache.[Object] E on E.[Object] = O.ObjectType and E.ObjectID = O.ObjectID
						left join utility.RelationshipTypes RT on RT.SourceObjectType = S.ObjectType and RT.SourceObjectID = S.ObjectTypeID and RT.TargetObjectType = E.ObjectType and RT.TargetObjectID = E.ObjectTypeID
				) O where IntersectTypeID is null

	set @current = 1
	select @max = MAX(ID) from @RelationTypes
	while @current <= @max
	begin
		select	@StartType = StartType,
				@StartTypeID = StartTypeID,	

				@EndType = EndType,
				@EndTypeID = EndTypeID,	

				@IntersectTypeID = IntersectTypeID
		from	@RelationTypes
		where	ID = @current

		-- Relationship does not yet exist, so CREATE.
		INSERT INTO [IntersectType] (UpdatedOn, UpdatedBy, Subject, SubjectID, Object, ObjectID, IsSystem) VALUES (getutcdate(), 0, @StartType, @StartTypeID, @EndType, @EndTypeID, 0)

		SELECT @IntersectTypeID = SCOPE_IDENTITY()

		INSERT INTO IntersectTypeNode	(IntersectTypeID, ObjectType, ObjectID, [Order]) 
		VALUES							(@IntersectTypeID, @StartType, @StartTypeID, 1)

		INSERT INTO IntersectTypeNode	(IntersectTypeID, ObjectType, ObjectID, [Order])
		VALUES							(@IntersectTypeID, @EndType, @EndTypeID, 2)

		set @current = @current + 1
	end


	-- Now deal with the objects themselves.
	declare @Relations table (
		ID int identity, 
			
		StartObject varchar(50), StartObjectID int, StartName nvarchar(500), StartType varchar(50), StartTypeID int, StartIntersectNodeID int, StartIntersectNodeTypeID int,
		EndObject varchar(50), EndObjectID int, EndName nvarchar(500), EndType varchar(50), EndTypeID int, EndIntersectNodeID int, EndIntersectNodeTypeID int,

		IntersectTypeID int, IntersectID int, [Action] varchar(1)
	)

	insert into @Relations
		select	distinct 
				O.ObjectType, O.ObjectID, OD.Name, OD.ObjectType, OD.ObjectTypeID, R.StartIntersectNodeID, RT.SourceIntersectTypeNodeID, 
				@Type, @ID, D.Name, D.ObjectType, D.ObjectTypeID, R.EndIntersectNodeID, RT.TargetIntersectTypeNodeID,
				RT.IntersectTypeID, R.IntersectID, CASE WHEN R.IntersectID IS NULL THEN 'C' ELSE 'U' END
		from	@Objects O
				left join cache.ObjectDetails OD on OD.[Object] = @Type and OD.ObjectID = @ID
				left join cache.ObjectDetails D on D.[Object] = O.ObjectType and D.ObjectID = O.ObjectID
				left join utility.RelationshipTypes RT on RT.SourceObjectType = OD.ObjectType and RT.SourceObjectID = OD.ObjectTypeID and RT.TargetObjectType = D.ObjectType and RT.TargetObjectID = D.ObjectTypeID
				outer apply (
							select	i.ID as IntersectID,
									N2.ID as StartIntersectNodeID,
									N1.ID as EndIntersectNodeID
							from	[Intersect] I
									inner join IntersectNode N1 on N1.IntersectID = I.ID and N1.ObjectType = @Type and N1.ObjectID = @ID
									inner join IntersectNode N2 on N2.IntersectID = I.ID and N2.ObjectType = O.ObjectType and N2.ObjectID = O.ObjectID
							where	i.IntersectTypeID = RT.IntersectTypeID
							) R

	set @current = 1
	select @max = MAX(ID) from @Relations
	while @current <= @max
	begin
		declare @StartObject varchar(50),	@StartObjectID int, @StartName nvarchar(500),	@StartIntersectNodeID int,	@StartIntersectNodeTypeID int, 
				@EndObject varchar(50),		@EndObjectID int,	@EndName nvarchar(500),		@EndIntersectNodeID int,	@EndIntersectNodeTypeID int,
				@Action varchar(1)

		set @IntersectID = null	--reset here

		select	@StartObject = StartObject,
				@StartObjectID = StartObjectID,
				@StartName = StartName,	
				@StartTypeID = StartTypeID,	
				@StartIntersectNodeID = StartIntersectNodeID,
				@StartIntersectNodeTypeID = StartIntersectNodeTypeID, 

				@EndObject = EndObject,
				@EndObjectID = EndObjectID,	
				@EndName = EndName,	
				@EndTypeID = EndTypeID,	
				@EndIntersectNodeID = EndIntersectNodeID,
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
					INSERT INTO [Intersect] (
						IntersectTypeID, 
						Classification, 
						[Description],
						[Subject], SubjectID,
						[Object], ObjectID,
						CreatedBy, CreatedOn,
						UpdatedBy, UpdatedOn		
					) 
					VALUES (
						@IntersectTypeID, 
						@Classification, 
						@Description,
						@StartObject, @StartObjectID,
						@EndObject, @EndObjectID,
						@ResourceID, @Date,
						@ResourceID, @Date
					)

					SELECT @IntersectID = SCOPE_IDENTITY()

					INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID) 
					VALUES						(@StartIntersectNodeTypeID, @IntersectID, @StartObject, @StartObjectID)

					SELECT @StartIntersectNodeID = SCOPE_IDENTITY()

					INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
					VALUES						(@EndIntersectNodeTypeID, @IntersectID, @EndObject, @EndObjectID)

					SELECT @EndIntersectNodeID = SCOPE_IDENTITY()

					update	@Relations
					set		IntersectID = @IntersectID,
							StartIntersectNodeID = @StartIntersectNodeID,
							EndIntersectNodeID = @EndIntersectNodeID
					where	(StartObject = @StartObject and StartObjectID = @StartObjectID and EndObject = @EndObject and EndObjectID = @EndObjectID) 
							or (StartObject = @EndObject and StartObjectID = @EndObjectID and EndObject = @StartObject and EndObjectID = @StartObjectID)
							--ID = @current

					insert into cache.[Object] ( [Object], [ObjectID], [ObjectType], [ObjectTypeID] )
					values	( 'Intersect', @IntersectID, 'IntersectType', @IntersectTypeID );

					insert into cache.Relationship ( IntersectID, SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID )
					values	( @IntersectID, @StartIntersectNodeTypeID, @StartIntersectNodeID, @StartObject, @StartObjectID, @EndIntersectNodeTypeID, @EndIntersectNodeID, @EndObject, @EndObjectID );
					insert into cache.Relationship ( IntersectID, SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID )
					values	( @IntersectID, @EndIntersectNodeTypeID, @EndIntersectNodeID, @EndObject, @EndObjectID, @StartIntersectNodeTypeID, @StartIntersectNodeID, @StartObject, @StartObjectID );

					--Update the responsibilities of the object that should inherit form the other (Taxonomy can push relationships down to artifact)
					if ( (@StartObject = 'Taxonomy' and @EndObject = 'Artifact') OR (@StartObject = 'Artifact' and @EndObject = 'Taxonomy') )
					begin
						if @StartObject = 'Artifact'
						begin
							exec [cache].[SynchronizeResponsibilitiesForObject] @StartObject, @StartObjectID
						end
						if @EndObject = 'Artifact'
						begin
							exec [cache].[SynchronizeResponsibilitiesForObject] @EndObject, @EndObjectID
						end
					end

					--exec utility.AddAuditEntry @StartObject, @StartObjectID, @ResourceID, @Date, 'Created', 'Intersect', @IntersectID
					--exec utility.AddAuditEntry @EndObject, @EndObjectID, @ResourceID, @Date, 'Created', 'Intersect', @IntersectID
				end
			else
				begin
					-- Update the Classification and Description only if the relationship already exists.
					if @IntersectID is not null
					begin
						update	[Intersect]
						set		Classification = @Classification,
								Description = @Description
						where	ID = @IntersectID

						--exec utility.AddAuditEntry 'Intersect', @IntersectID, @ResourceID, @Date, 'Updated', 'Intersect', @IntersectID
					end
				end
		end

		set @current = @current + 1
	end
end