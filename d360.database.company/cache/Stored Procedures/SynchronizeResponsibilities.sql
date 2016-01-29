
CREATE procedure [cache].[SynchronizeResponsibilities]
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


	-- DOMAIN RESPONSIBILITY ASSIGNMENT LOGIC ---------------------------------------------------------------------------------------

	insert into #Responsibilities
		select * from utility.GetVerticalResponsibilityList('DomainType', null, 1);

	insert into #Responsibilities
		select * from utility.GetDirectlyAssignedResponsibilityList('DomainType', null, 2);

	insert into #Responsibilities
		select * from utility.GetHierarchyAssignedResponsibilityList('Domain', null, 2);



	-- FUSION RESPONSIBILITY ASSIGNMENT LOGIC ---------------------------------------------------------------------------------------

	insert into #Responsibilities
		select * from utility.GetVerticalResponsibilityList('FusionType', null, 1);

	insert into #Responsibilities
		select * from utility.GetDirectlyAssignedResponsibilityList('FusionType', null, 2);

	insert into #Responsibilities
		select * from utility.GetDirectlyAssignedResponsibilityList('Fusion', null, 2);


	-- EVENT RESPONSIBILITY ASSIGNMENT LOGIC ----------------------------------------------------------------------------------------

	insert into #Responsibilities
		select * from utility.GetVerticalResponsibilityList('PolicyType', null, 1);

	insert into #Responsibilities
		select * from utility.GetHierarchyAssignedResponsibilityList('Policy', null, 4);

	insert into #Responsibilities
		select * from utility.GetDirectlyAssignedResponsibilityList('PolicyType', null, 7);

	insert into #Responsibilities
		select * from utility.GetDirectlyAssignedResponsibilityList('Policy', null, 7);

	insert into #Responsibilities
		select * from utility.GetDirectlyAssignedResponsibilityList('Rule', null, 7);


	-- ARTIFACT RESPONSIBILITY ASSIGNMENT LOGIC --------------------------------------------------------------------------

	insert into #Responsibilities
		select * from utility.GetVerticalResponsibilityList('ArtifactType', null, 1);

	insert into #Responsibilities
		select * from utility.GetHierarchyAssignedResponsibilityList('Artifact', null, 4);

	insert into #Responsibilities
		select * from utility.GetDirectlyAssignedResponsibilityList('ArtifactType', null, 7);

	insert into #Responsibilities
		select * from utility.GetDirectlyAssignedResponsibilityList('Artifact', null, 7);


	-- MODEL RESPONSIBILITY ASSIGNMENT LOGIC ----------------------------------------------------------------------------

	insert into #Responsibilities
		select * from utility.GetVerticalResponsibilityList('TaxonomyType', null, 1);

	insert into #Responsibilities
		select * from utility.GetHierarchyAssignedResponsibilityList('Taxonomy', null, 4);

	insert into #Responsibilities
		select * from utility.GetDirectlyAssignedResponsibilityList('TaxonomyType', null, 7);

	insert into #Responsibilities
		select * from utility.GetDirectlyAssignedResponsibilityList('Taxonomy', null, 7);

	---------------------------------------------------------------------------------------------------------------------

	declare @current int = 1,
			@max int,
			@ResponsibilityID int,
			@ResponsibilityTypeID int,
			@AssigningItem varchar(50),
			@AssigningItemID int,
			@Object varchar(50),
			@ObjectID int,
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
					@Object = [Object],
					@ObjectID = ObjectID,
					@ContextHash = ContextHash,
					@Priority = [Priority]
			from	#Responsibilities
			where	ID = @current;

			delete	#Responsibilities
			where	ResponsibilityTypeID = @ResponsibilityTypeID
					and [Object] = @Object 
					and ObjectID = @ObjectID
					and ContextHash = @ContextHash
					and [Priority] < @Priority
					and ResponsibilityTypeID <> 0;
		end
		set @current = @current + 1
	end;


	merge	cache.ResponsibilityItem as T
	using	(
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
					TR.Visible,
					RT.ResponsibilityTypeGroup
			from	#Responsibilities TR
					inner join Responsibility R on R.ID = TR.ResponsibilityID
					inner join ResponsibilityType RT on RT.ID = R.ResponsibilityTypeID
			) as S
	on		(
			T.ResponsibilityID  = S.ResponsibilityID 
			and T.AssigningItem = S.AssigningItem 
			and T.AssigningItemID = S.AssigningItemID 
			and T.[Object] = S.[Object]
			and T.ObjectID = S.ObjectID
			and T.ContextHash = S.ContextHash
			)
	when	matched then
			update	
			set		T.ResponsibleObject = S.ResponsibleObject,
					T.ResponsibleObjectID = S.ResponsibleObjectID,
					T.Visible = S.Visible
	when	not matched by target then
			INSERT	(
					[ResponsibilityID], [ResponsibilityTypeID], [ResponsibilityType], 
					[AssigningItem], [AssigningItemID], 
					[Object], [ObjectID], 
					[ResponsibleObject], [ResponsibleObjectID], 
					[ContextHash], [ResponsibilityTypeGroup], Visible
					)
			VALUES	(
					S.[ResponsibilityID], S.[ResponsibilityTypeID], S.[ResponsibilityType], 
					S.[AssigningItem], S.[AssigningItemID], 
					S.[Object], S.[ObjectID], 
					S.[ResponsibleObject], S.[ResponsibleObjectID], 
					S.[ContextHash], S.[ResponsibilityTypeGroup], S.Visible
					)
	when	not matched by source then 
			DELETE;
end
