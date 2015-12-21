CREATE procedure [dbo].[AddMapRelationship]
--declare
	@MapID int,
	@ResourceID int,
	@Date datetime,
	@ObjectType varchar(50),			-- The start object type.
	@ObjectID int,						-- The start object ID.
	@Classification int,
	@IntersectRole int,
	@Description nvarchar(4000),
	@SubjectType varchar(50),
	@SubjectID int,
	@PredicateName nvarchar(100),
	@PredicatePhrase nvarchar(250)
	
--set @ResourceID = 1
--set @Date = getutcdate()
--set @ObjectType = 'Artifact'
--set @ObjectID = 4651
--set @Classification = 1
--set @IntersectRole = NULL
--set @Description = ''
--insert into @Objects VALUES ('Artifact', 11808)


as
begin
	set nocount on;

	declare @Objects ObjectsTable;

	insert into @Objects values (@SubjectType, @SubjectID);

	if @IntersectRole = 0 
	begin
		set @IntersectRole = null
	end

	if @MapID = 0
	begin
		set @MapID = null
	end

	declare @current int,
			@max int,
			@ErrorMessage nvarchar(2500),
			@IntersectID int,
			
			@StartType varchar(50),	@StartTypeID int,
			@EndType varchar(50),	@EndTypeID int,	
			@IntersectTypeID int,
			@SubjectNodeID int, @ObjectNodeID int
	
	declare @Intersects IDTable
	
	/*	Get the relationship types we need to check or create.	*/
	declare @RelationTypes table (
		ID int identity, 
		StartType varchar(50), StartTypeID int, 
		EndType varchar(50), EndTypeID int, 
		IntersectTypeID int
	)
	
	insert into @RelationTypes
		select	distinct 
				S.ObjectType, S.ObjectTypeID, 
				E.ObjectType, E.ObjectTypeID, 
				RT.IntersectTypeID
		from	@Objects O
				inner join cache.ObjectDetails S on S.[Object] = @ObjectType and S.ObjectID = @ObjectID
				inner join cache.ObjectDetails E on E.[Object] = O.ObjectType and E.ObjectID = O.ObjectID
				left join utility.RelationshipTypes RT on RT.SourceObjectType = S.ObjectType and RT.SourceObjectID = S.ObjectTypeID and RT.TargetObjectType = E.ObjectType and RT.TargetObjectID = E.ObjectTypeID and RT.IntersectTypeID is null

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
		INSERT INTO [IntersectType] (UpdatedOn, UpdatedBy) VALUES (getutcdate(), 0)

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
			
		StartObject varchar(50), StartObjectID int, StartName nvarchar(500), StartType varchar(50), StartTypeID int, StartIntersectNodeTypeID int,
		EndObject varchar(50), EndObjectID int, EndName nvarchar(500), EndType varchar(50), EndTypeID int, EndIntersectNodeTypeID int,

		IntersectTypeID int, IntersectID int, [Action] varchar(1)
	)

	insert into @Relations
		select	distinct 
				O.ObjectType, O.ObjectID, OD.Name, OD.ObjectType, OD.ObjectTypeID, RT.SourceIntersectTypeNodeID, 
				@ObjectType, @ObjectID, D.Name, D.ObjectType, D.ObjectTypeID, RT.TargetIntersectTypeNodeID,
				RT.IntersectTypeID, R.ID, CASE WHEN R.ID IS NULL THEN 'C' ELSE 'U' END
		from	@Objects O
				left join cache.ObjectDetails OD on OD.[Object] = @ObjectType and OD.ObjectID = @ObjectID
				left join cache.ObjectDetails D on D.[Object] = O.ObjectType and D.ObjectID = O.ObjectID
				left join utility.RelationshipTypes RT on RT.SourceObjectType = OD.ObjectType and RT.SourceObjectID = OD.ObjectTypeID and RT.TargetObjectType = D.ObjectType and RT.TargetObjectID = D.ObjectTypeID
				outer apply (
							select	i.ID
							from	[Intersect] I
									inner join IntersectNode N1 on N1.IntersectID = I.ID and N1.ObjectType = @ObjectType and N1.ObjectID = @ObjectID
									inner join IntersectNode N2 on N2.IntersectID = I.ID and N2.ObjectType = O.ObjectType and N2.ObjectID = O.ObjectID
							where	i.IntersectTypeID = RT.IntersectTypeID
							) R

	set @current = 1
	select @max = MAX(ID) from @Relations
	while @current <= @max
	begin
		declare @StartObject varchar(50),	@StartObjectID int, @StartName nvarchar(500),	@StartIntersectNodeTypeID int, 
				@EndObject varchar(50),		@EndObjectID int,	@EndName nvarchar(500),		@EndIntersectNodeTypeID int,
				@Action varchar(1)

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

		if @ObjectID > 0
		begin
			-- Relationship does not yet exist, so CREATE.
			if @IntersectID is null and @StartIntersectNodeTypeID is not null and @EndIntersectNodeTypeID is not null
				begin
					INSERT INTO [Intersect] (IntersectTypeID, Classification, [Description], [IntersectTypeRoleID]) VALUES (@IntersectTypeID, @Classification, @Description, @IntersectRole)

					SELECT @IntersectID = SCOPE_IDENTITY()

					INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID) 
					VALUES						(@StartIntersectNodeTypeID, @IntersectID, @StartObject, @StartObjectID)

					SELECT @ObjectNodeID = SCOPE_IDENTITY();

					INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
					VALUES						(@EndIntersectNodeTypeID, @IntersectID, @EndObject, @EndObjectID)
					
					SELECT @SubjectNodeID = SCOPE_IDENTITY();

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

			--insert new map record if applicable
			if @MapID is null or @MapID = 0
			begin
				insert into Map (Name, [Type]) values ('',1);
				SELECT @MapID = SCOPE_IDENTITY();
			end
			
			if (@IntersectID is not null and @SubjectNodeID is null and @ObjectNodeID is null)
			begin 

				select
					@ObjectNodeID = N.ObjectID
				from
					IntersectNode N
				join
					@Relations R on R.IntersectID = N.IntersectID
				where
					N.IntersectID = @IntersectID and N.IntersectTypeNodeID = R.StartIntersectNodeTypeID;
				
				select
					@SubjectNodeID = N.ObjectID
				from
					IntersectNode N
				join
					@Relations R on R.IntersectID = N.IntersectID
				where
					N.IntersectID = @IntersectID and N.IntersectTypeNodeID = R.EndIntersectNodeTypeID;
					
			end

			declare @PredicateID int, @PredicatePhraseID int;
			if (@PredicateName is not null and @PredicatePhrase is not null)
			begin
			
				if (select count(*) from Predicate where Name = @PredicateName) = 0
				begin
				select * from predicate
					insert into Predicate (Name, Phrase) values (@PredicateName,@PredicatePhrase)
					set @PredicateID = SCOPE_IDENTITY();
				end
				else
				begin
					select @PredicateID=ID from Predicate where Name = @PredicateName;
				end
				if (select count(*) from PredicatePhrase where Phrase = @PredicatePhrase AND PredicateID = @PredicateID) = 0
				begin
					insert into PredicatePhrase(PredicateID,Phrase) values (@PredicateID,@PredicatePhrase);
					set @PredicatePhraseID = SCOPE_IDENTITY();
				end
				else
				begin
					select @PredicatePhraseID = ID from PredicatePhrase where PredicateID = @PredicateID and Phrase = @PredicatePhrase; 
				end
			end

			insert into intersectmap (MapID, SubjectIntersectNodeID, ObjectIntersectNodeID, PredicatePhraseID, Type)
			select top 1
				@MapID as MapID,
				@SubjectNodeID as SubjectIntersectNodeID,
				@ObjectNodeID as ObjectIntersectNodeID,
				@PredicatePhraseID as PredicatePhraseID,
				1 as Type
			from intersectmap m
			where not exists (select * from intersectmap where mapid = @MapID and subjectintersectnodeid = @SubjectNodeID 
				and objectintersectnodeid = @ObjectNodeID);

			

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