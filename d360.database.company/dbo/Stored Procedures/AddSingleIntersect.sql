CREATE procedure [dbo].[AddSingleIntersect]
	@ResourceID int,
	@IntersectTypeID int,
	@Subject varchar(50),			-- The start object type.
	@SubjectID int,					-- The start object ID.
	@Object varchar(50),			-- The end object type.
	@ObjectID int,					-- The end object ID.	
	@Classification int,
	@Description nvarchar(4000)
as
begin
	set nocount on;

	declare @Date datetime = getutcdate(),
			@ErrorMessage nvarchar(2500),
			@IntersectID int,
			@Reversed bit = 0

	select	@IntersectID = ID,
			@Reversed = case
				when (Subject = @Subject and SubjectID = @SubjectID and Object = @Object and ObjectID = @ObjectID) then 0
				else 1
			end
	from	[Intersect]
	where	(
			(Subject = @Subject and SubjectID = @SubjectID and Object = @Object and ObjectID = @ObjectID) OR 
			(Subject = @Object and SubjectID = @ObjectID and Object = @Subject and ObjectID = @SubjectID)
			)

	if @IntersectID is not null and @IntersectID > 0
		begin
			-- Update
			update	[Intersect]
			set		Classification = @Classification,
					Description = @Description
			where	ID = @IntersectID
		end
	else
		begin
			-- Create
			declare @SubjectType varchar(50),
					@SubjectTypeID int,
					@ObjectType varchar(50),
					@ObjectTypeID int

			select	@SubjectType = ObjectType, @SubjectTypeID = ObjectTypeID	from cache.[Object] where [Object] = @Subject and ObjectID = @SubjectID 
			select	@ObjectType = ObjectType, @ObjectTypeID = ObjectTypeID		from cache.[Object] where [Object] = @Object and ObjectID = @ObjectID 

			select	distinct 
					@IntersectTypeID = ID,
					@Reversed = case
						when (Subject = @SubjectType and SubjectID = @SubjectTypeID and Object = @ObjectType and ObjectID = @ObjectTypeID) then 0
						else 1
					end
			from	IntersectType 
			where	(
						(Subject = @SubjectType and SubjectID = @SubjectTypeID and Object = @ObjectType and ObjectID = @ObjectTypeID) OR
						(Subject = @ObjectType and SubjectID = @ObjectTypeID and Object = @SubjectType and ObjectID = @SubjectTypeID)
					)

			if @IntersectTypeID is not null
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
						case @Reversed when 0 then @Subject else @Object end, 
						case @Reversed when 0 then @SubjectID else @ObjectID end,
						case @Reversed when 0 then @Object else @Subject end, 
						case @Reversed when 0 then @ObjectID else @SubjectID end,
						@ResourceID, @Date,
						@ResourceID, @Date
					)

					SELECT @IntersectID = SCOPE_IDENTITY()

					insert into cache.[Object] ( [Object], [ObjectID], [ObjectType], [ObjectTypeID] )
					values	( 'Intersect', @IntersectID, 'IntersectType', @IntersectTypeID );

					--Update the responsibilities of the object that should inherit form the other (Taxonomy can push relationships down to artifact)
					if ( (@Subject = 'Taxonomy' and @Object = 'Artifact') OR (@Subject = 'Artifact' and @Object = 'Taxonomy') )
						begin
							if @Subject = 'Artifact'
							begin
								exec [cache].[SynchronizeResponsibilitiesForObject] @Subject, @SubjectID
							end
							if @Object = 'Artifact'
							begin
								exec [cache].[SynchronizeResponsibilitiesForObject] @Object, @ObjectID
							end
						end
				end
		end

	select * from [Intersect] where ID = @IntersectID
end
GO

