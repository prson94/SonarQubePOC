CREATE procedure [cache].[SynchronizeResponsibilitiesForObject]
--declare
	@Object varchar(50),
	@ObjectID int
--set @Object = 'ArtifactType'
--set @ObjectID = 11
as
begin
	set nocount on;

	IF OBJECT_ID('tempdb.dbo.#Responsibilities', 'U') IS NOT NULL
		drop table #Responsibilities;

	create table #Responsibilities
	(
		ID int identity,
		[Source] varchar(50), 
		Visible bit,
		ResponsibilityID int,
		ResponsibilityTypeID int,
		AssigningItem varchar(50),
		AssigningItemID int,
		[Object] varchar(50),
		ObjectID int,
		ContextHash varchar(50),
		[Priority] int
	);

	CREATE CLUSTERED INDEX [IX_TempResponsibilities] ON #Responsibilities ([ID] ASC);
	CREATE NONCLUSTERED INDEX [IX_TempResponsibilities_Combined] ON #Responsibilities ([Object] ASC, [ObjectID] ASC, ContextHash ASC);

	insert into #Responsibilities
		select * from utility.GetVerticalResponsibilityList(@Object, @ObjectID, 1);

	insert into #Responsibilities
		select * from utility.GetHierarchyAssignedResponsibilityList(@Object, @ObjectID, 4);

	insert into #Responsibilities
		select * from utility.GetDirectlyAssignedResponsibilityList(@Object, @ObjectID, 7);


--select * from #Responsibilities
	--delete #Responsibilities where [Object] + cast(ObjectID as varchar) <> @Object + cast(@ObjectID as varchar)
	--delete cache.ResponsibilityItem where [Object] = @Object and ObjectID = @ObjectID
	DELETE	T
	FROM	cache.ResponsibilityItem T
			INNER JOIN #Responsibilities S ON S.[Object] = T.[Object] 
											and S.[ObjectID] = T.[ObjectID] 
											and S.ResponsibilityTypeID = T.ResponsibilityTypeID 
											and S.ContextHash = T.ContextHash;

	declare @current int = 1,
			@max int,
			@ResponsibilityID int,
			@ResponsibilityTypeID int,
			@AssigningItem varchar(50),
			@AssigningItemID int,
			@Obj varchar(50),
			@ObjID int,
			@ContextHash varchar(50),
			@Priority int;

	select @max = max(ID) from #Responsibilities;

	while @current <= @max
	begin
		if exists(select 1 from #Responsibilities where ID = @current)
		begin
			select	@ResponsibilityID = ResponsibilityID,
					@ResponsibilityTypeID = ResponsibilityTypeID,
					@AssigningItem = AssigningItem,
					@AssigningItemID = AssigningItemID,
					@Obj = [Object],
					@ObjID = ObjectID,
					@ContextHash = ContextHash,
					@Priority = [Priority]
			from	#Responsibilities
			where	ID = @current;

			delete	#Responsibilities
			where	ResponsibilityTypeID = @ResponsibilityTypeID
					and [Object] = @Obj
					and ObjectID = @ObjID
					and ContextHash = @ContextHash
					and [Priority] < @Priority
					and ResponsibilityTypeID <> 0;
		end
		set @current = @current + 1
	end;

--select * from #Responsibilities

	insert into cache.ResponsibilityItem
	(
		[ResponsibilityID], [ResponsibilityTypeID], [ResponsibilityType], 
		[AssigningItem], [AssigningItemID], 
		[Object], [ObjectID], 
		[ResponsibleObject], [ResponsibleObjectID], 
		[ContextHash], [ResponsibilityTypeGroup], Visible
	)
		select	distinct
				TR.ResponsibilityID,
				TR.ResponsibilityTypeID,
				RT.Name as ResponsibilityType,
				TR.AssigningItem,
				TR.AssigningItemID,
				TR.[Object],
				TR.ObjectID,
				R.ResponsibleObjectType as ResponsibleObject,
				R.ResponsibleObjectID,
				TR.ContextHash,
				RT.ResponsibilityTypeGroup,
				TR.Visible
		from	#Responsibilities TR
				inner join Responsibility R on R.ID = TR.ResponsibilityID
				inner join ResponsibilityType RT on RT.ID = R.ResponsibilityTypeID
end