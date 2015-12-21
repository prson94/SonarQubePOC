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
--set @ID = 972874
--set @Classification = 1
--set @IntersectRole = NULL
--set @Description = ''
--insert into @Objects VALUES ('Artifact', 733)
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
				@Type, @ID, D.Name, D.ObjectType, D.ObjectTypeID, RT.TargetIntersectTypeNodeID,
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

		if @ID > 0
		begin
			-- Relationship does not yet exist, so CREATE.
			if @IntersectID is null and @StartIntersectNodeTypeID is not null and @EndIntersectNodeTypeID is not null
				begin
					INSERT INTO [Intersect] (IntersectTypeID, Classification, [Description]) VALUES (@IntersectTypeID, @Classification, @Description)

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

					insert into cache.[Object] ( [Object], [ObjectID], [ObjectType], [ObjectTypeID] )
					values	( 'Intersect', @IntersectID, 'IntersectType', @IntersectTypeID );

					insert into cache.Relationship ( IntersectID, SourceIntersectTypeNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetObject, TargetObjectID )
					values	( @IntersectID, @StartIntersectNodeTypeID, @StartObject, @StartObjectID, @EndIntersectNodeTypeID, @EndObject, @EndObjectID );

					insert into cache.Relationship ( IntersectID, TargetIntersectTypeNodeID, TargetObject, TargetObjectID, SourceIntersectTypeNodeID, SourceObject, SourceObjectID )
					values	( @IntersectID, @StartIntersectNodeTypeID, @StartObject, @StartObjectID, @EndIntersectNodeTypeID, @EndObject, @EndObjectID );

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
								Description = @Description
						where	ID = @IntersectID

						exec utility.AddAuditEntry 'Intersect', @IntersectID, @ResourceID, @Date, 'Updated', 'Intersect', @IntersectID
					end
				end
		end

		set @current = @current + 1
	end

	--merge	[cache].[Object] as T
	--using	(
	--		select	distinct
	--				'Intersect' as [Object],
	--				IntersectID as ObjectID,
	--				'IntersectType' as ObjectType,
	--				IntersectTypeID as ObjectTypeID
	--		from	@Relations
	--		where	IntersectID is not null and IntersectTypeID is not null
	--		) as S
	--on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	--when	not matched then
	--		insert	( [Object], [ObjectID], [ObjectType], [ObjectTypeID] )
	--		values	( S.[Object], S.[ObjectID], S.[ObjectType], S.[ObjectTypeID] );

	--merge	[cache].[Relationship] as T
	--using	(
	--		select	IntersectID,
	--				StartIntersectNodeTypeID as SourceIntersectTypeNodeID,					
	--				StartObject as SourceObject,
	--				StartObjectID as SourceObjectID,
	--				EndIntersectNodeTypeID as TargetIntersectTypeNodeID,
	--				EndObject as TargetObject,
	--				EndObjectID as TargetObjectID
	--		from	@Relations
	--		where	IntersectID is not null and IntersectTypeID is not null
	--		union
	--		select	IntersectID,
	--				EndIntersectNodeTypeID as SourceIntersectTypeNodeID,
	--				EndObject as SourceObject,
	--				EndObjectID as SourceObjectID,
	--				StartIntersectNodeTypeID as TargetIntersectTypeNodeID,					
	--				StartObject as TargetObject,
	--				StartObjectID as TargetObjectID
	--		from	@Relations
	--		where	IntersectID is not null and IntersectTypeID is not null
	--		) as S
	--on		T.IntersectID = S.IntersectID and T.[SourceObject] = S.[SourceObject] and T.[SourceObjectID] = S.[SourceObjectID]
	--when	not matched then
	--		insert	( IntersectID, SourceIntersectTypeNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetObject, TargetObjectID )
	--		values	( S.IntersectID, S.SourceIntersectTypeNodeID, S.SourceObject, S.SourceObjectID, S.TargetIntersectTypeNodeID, S.TargetObject, S.TargetObjectID );
end