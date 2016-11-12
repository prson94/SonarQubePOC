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
			@max int
	
	declare @Relations table (
		ID int identity, 
			
		Subject varchar(50), SubjectID int, SubjectType varchar(50), SubjectTypeID int, 
		Object varchar(50), ObjectID int, ObjectType varchar(50), ObjectTypeID int, 

		IntersectTypeID int, IntersectID int, [Action] varchar(1),
		
		IsReversed bit
	)

	insert into @Relations
		select	distinct 
				SD.Object, SD.ObjectID, SD.ObjectType, SD.ObjectTypeID, 
				OD.Object, OD.ObjectID, OD.ObjectType, OD.ObjectTypeID, 
				RT.ID, R.ID, CASE WHEN R.ID IS NULL THEN 'C' ELSE 'U' END,
				case
					when (RT.Subject = SD.ObjectType and RT.SubjectID = SD.ObjectTypeID and RT.Object = OD.ObjectType and RT.ObjectID = OD.ObjectTypeID) then cast(1 as bit)
					else cast(0 as bit)
				end
		from	@Objects O
				inner join cache.Object SD on SD.[Object] = @Type and SD.ObjectID = @ID
				inner join cache.Object OD on OD.[Object] = O.ObjectType and OD.ObjectID = O.ObjectID
				inner join [IntersectType] RT on	(
													(RT.Subject = SD.ObjectType and RT.SubjectID = SD.ObjectTypeID and RT.Object = OD.ObjectType and RT.ObjectID = OD.ObjectTypeID) OR
													(RT.Object = SD.ObjectType and RT.ObjectID = SD.ObjectTypeID and RT.Subject = OD.ObjectType and RT.SubjectID = OD.ObjectTypeID)
													)
				left join [Intersect] R on	R.IntersectTypeID = RT.ID and 
											(
												(R.Subject = SD.Object and R.SubjectID = SD.ObjectID and R.Object = OD.Object and R.ObjectID = OD.ObjectID) OR
												(R.Object = SD.Object and R.ObjectID = SD.ObjectID and R.Subject = OD.Object and R.SubjectID = OD.ObjectID)
											)

	set @current = 1
	select @max = MAX(ID) from @Relations
	while @current <= @max
	begin
		declare @Subject varchar(50),		@SubjectID int, 
				@Object varchar(50),		@ObjectID int,	
				@Action varchar(1),			@IsReversed bit,
				@IntersectTypeID int,		@IntersectID int,
				@s varchar(50),				@o varchar(50),
				@sid int,					@oid int
		
		set		@IntersectID = null	--reset here

		select	@Subject = Subject,
				@SubjectID = SubjectID,

				@Object = Object,
				@ObjectID = ObjectID,	

				@IntersectTypeID = IntersectTypeID, 
				@IntersectID = IntersectID, 
				@Action = [Action],
				@IsReversed = IsReversed
		from	@Relations
		where	ID = @current

		if @IntersectID is null
		begin
			-- Relationship does not yet exist, so CREATE.
			if @IntersectID is null
				begin
					if @IsReversed = 1
					begin
						set @s = @Object
						set @sid = @ObjectID
						set @o = @Subject
						set @oid = @SubjectID 
					end
					else
					begin
						set @o = @Object
						set @oid = @ObjectID
						set @s = @Subject
						set @sid = @SubjectID
					end

					INSERT INTO [Intersect] (
						IntersectTypeID, Classification, [Description],
						[Subject], SubjectID, [Object], ObjectID,
						CreatedBy, CreatedOn, UpdatedBy, UpdatedOn		
					) 
					VALUES (
						@IntersectTypeID,  @Classification,  @Description,
						@s, @sid, @o, @oid,
						@ResourceID, @Date, @ResourceID, @Date
					)

					SELECT @IntersectID = SCOPE_IDENTITY()

					insert into cache.[Object] ( [Object], [ObjectID], [ObjectType], [ObjectTypeID] )
					values	( 'Intersect', @IntersectID, 'IntersectType', @IntersectTypeID );

					--insert into cache.Relationship ( IntersectID, SourceObject, SourceObjectID, TargetObject, TargetObjectID )
					--values	( @IntersectID, @s, @sid, @o, @oid );
					--insert into cache.Relationship ( IntersectID, SourceObject, SourceObjectID, TargetObject, TargetObjectID )
					--values	( @IntersectID, @o, @oid, @s, @sid );

					--Update the responsibilities of the object that should inherit form the other (Taxonomy can push relationships down to artifact)
					if ( (@s = 'Taxonomy' and @o = 'Artifact') OR (@s = 'Artifact' and @o = 'Taxonomy') )
					begin
						if @s = 'Artifact'
						begin
							exec [cache].[SynchronizeResponsibilitiesForObject] @s, @sid
						end
						if @o = 'Artifact'
						begin
							exec [cache].[SynchronizeResponsibilitiesForObject] @o, @oid
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
GO

