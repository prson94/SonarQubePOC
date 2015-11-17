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
		TargetResponsibilityID int,
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


	-- DOMAIN RESPONSIBILITY ASSIGNMENT LOGIC : START ---------------------------------------------------------------------------------------

	-- Vertical Assignments from Domain Type-------
	insert into #Responsibilities
		select	'Domain Vertical' as [Source],
				R.Visible,
				R.ID,
				R.TargetResponsibilityID,
				R.ResponsibilityTypeID,
				'DomainType' as AssigningItemType,
				T.ID as AssigningItemID,
				'Domain' as ObjectType,
				A.ID as ObjectID,
				utility.GetResponsibilityContextHash(R.ID),
				1 as [Priority]
		from	DomainType T 
				inner join Responsibility R on R.ObjectType = 'DomainType' and R.ObjectID = T.ID
				inner join Domain A on A.DomainTypeID = T.ID;
	-------------------------------------------------

	-- Domain Overrides -----------------------------
	insert into #Responsibilities
		select	'Domain Override' as [Source],
				R.Visible,
				R.ID,
				R.TargetResponsibilityID,
				R.ResponsibilityTypeID,
				'Domain' as AssigningItemType,
				T.ID as AssigningItemID,
				'Domain' as ObjectType,
				T.ID as ObjectID,
				utility.GetResponsibilityContextHash(R.ID),
				2 as [Priority]
		from	Domain T 
				inner join Responsibility R on R.ObjectType = 'Domain' and R.ObjectID = T.ID;
	---------------------------------------------------

	-- DOMAIN RESPONSIBILITY ASSIGNMENT LOGIC : END -----------------------------------------------------------------------------------------


	-- FUSION RESPONSIBILITY ASSIGNMENT LOGIC : START ---------------------------------------------------------------------------------------

	-- Vertical Assignments from Fusion Type-------
	insert into #Responsibilities
		select	'Fusion Vertical' as [Source],
				R.Visible,
				R.ID,
				R.TargetResponsibilityID,
				R.ResponsibilityTypeID,
				'FusionType' as AssigningItemType,
				T.ID as AssigningItemID,
				'Fusion' as ObjectType,
				A.ID as ObjectID,
				utility.GetResponsibilityContextHash(R.ID),
				1 as [Priority]
		from	FusionType T 
				inner join Responsibility R on R.ObjectType = 'FusionType' and R.ObjectID = T.ID
				inner join Fusion A on A.FusionTypeID = T.ID;
	-------------------------------------------------

	-- Fusion Overrides -----------------------------
	insert into #Responsibilities
		select	'Fusion Override' as [Source],
				R.Visible,
				R.ID,
				R.TargetResponsibilityID,
				R.ResponsibilityTypeID,
				'Fusion' as AssigningItemType,
				T.ID as AssigningItemID,
				'Fusion' as ObjectType,
				T.ID as ObjectID,
				utility.GetResponsibilityContextHash(R.ID),
				2 as [Priority]
		from	Fusion T 
				inner join Responsibility R on R.ObjectType = 'Fusion' and R.ObjectID = T.ID;
	---------------------------------------------------

	-- FUSION RESPONSIBILITY ASSIGNMENT LOGIC : END -----------------------------------------------------------------------------------------



	-- EVENT RESPONSIBILITY ASSIGNMENT LOGIC : START ----------------------------------------------------------------------------------------

	---------------------------------------------------
	declare @tblEventHierarchy table (
		Visible bit,
		ResponsibilityID int,
		TargetResponsibilityID int,
		ResponsibilityTypeID int,
		AssigningItem varchar(50),
		AssigningItemID int,
		[Object] varchar(50),
		ObjectID int,
		ContextHash varchar(50),
		[Level] int
	);

	---------------------------------------------------
	with PolicyHierarchy as
	(
	select	R.Visible,
			P.ID as AssigningItemID,
			P.ID,
			P.ParentID,
			R.ID as ResponsibilityID,
			R.TargetResponsibilityID,
			R.ResponsibilityTypeID,
			utility.GetResponsibilityContextHash(R.ID) as ContextHash,
			1 as [Level]
	from	Policy P 
			left join Responsibility R on R.ObjectType = 'Policy' and R.ObjectID = P.ID
	where	P.ParentID is null
	union all
	select	
			COALESCE(R.Visible, P.Visible) as Visible,
			COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
			C.ID,
			C.ParentID,
			COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
			COALESCE(R.TargetResponsibilityID, P.TargetResponsibilityID) as TargetResponsibilityID,
			COALESCE(R.ResponsibilityTypeID, P.ResponsibilityTypeID) as ResponsibilityTypeID,
			coalesce(R.ContextHash, P.ContextHash) as ContextHash,
			P.[Level] + 1 as [Level] 
	from	Policy C
			inner join PolicyHierarchy P on C.ParentID = P.ID
			outer apply (
						select	*,
								utility.GetResponsibilityContextHash(ID) as ContextHash
						from	Responsibility 
						where	ResponsibilityTypeID = P.ResponsibilityTypeID
								and ObjectType = 'Policy' 
								and ObjectID = C.ID
						) R
	)

	insert into @tblEventHierarchy
		select	Visible,
				ResponsibilityID,
				TargetResponsibilityID,
				ResponsibilityTypeID,
				'Policy' as AssigningItemType,
				AssigningItemID,
				'Policy' as TargetObject, 
				ID,
				ContextHash,
				[Level]
		from	PolicyHierarchy
		where	ResponsibilityID is not null;


	insert into #Responsibilities
		select	'PolicyHierarchy' as [Source],
				O.Visible,
				O.ResponsibilityID,
				O.TargetResponsibilityID,
				O.ResponsibilityTypeID,
				O.AssigningItem,
				O.AssigningItemID,
				O.[Object],
				O.ObjectID,
				O.ContextHash,
				1 as [Priority]
		from	@tblEventHierarchy O
				inner join	(
							select		ResponsibilityTypeID,
										[Object],
										ObjectID,
										ContextHash,
										Max([Level]) as [Level]
							from		@tblEventHierarchy
							group by	ResponsibilityTypeID,
										[Object],
										ObjectID,
										ContextHash
							) M on M.ResponsibilityTypeID = O.ResponsibilityTypeID and M.[Object] = O.[Object] and M.ObjectID = O.ObjectID and M.ContextHash = O.ContextHash and M.[Level] = O.[Level];
	---------------------------------------------------


	---------------------------------------------------
	--insert into #Responsibilities
	--	select	'PolicyHierarchyForRule' as [Source],
	--			P.Visible,
	--			P.ResponsibilityID,
	--			P.TargetResponsibilityID,
	--			P.ResponsibilityTypeID,
	--			P.AssigningItem,
	--			P.AssigningItemID,
	--			'Rule' as [Object],
	--			R.ID as ObjectID,
	--			P.ContextHash,
	--			1 as [Priority]
	--	from	[Rule] R
	--			inner join #Responsibilities P on P.AssigningItem = 'Policy' and P.AssigningItemID = R.PolicyID;
	---------------------------------------------------


	---- Rule Direct Assignments ----------------------
	insert into #Responsibilities
		select	'RuleDirect' as [Source],
				R.Visible,
				R.ID,
				R.TargetResponsibilityID,
				R.ResponsibilityTypeID,
				'Rule' as AssigningItemType,
				RU.ID as AssigningItemID,
				'Rule' as ObjectType,
				RU.ID as ObjectID,
				utility.GetResponsibilityContextHash(R.ID),
				2 as [Priority]
		from	[Rule] RU 
				inner join Responsibility R on R.ObjectType = 'Rule' and R.ObjectID = RU.ID;
	---------------------------------------------------


	-- EVENT RESPONSIBILITY ASSIGNMENT LOGIC : END ------------------------------------------------------------------------------------------




	-- GLOSSARY (ARTIFACT) RESPONSIBILITY ASSIGNMENT LOGIC : START --------------------------------------------------------------------------

	declare @tblModelHierarchy table (
		Visible bit,
		ResponsibilityID int,
		TargetResponsibilityID int,
		ResponsibilityTypeID int,
		AssigningItem varchar(50),
		AssigningItemID int,
		[Object] varchar(50),
		ObjectID int,
		ContextHash varchar(50),
		[Level] int
	);

	---------------------------------------------------
	insert into #Responsibilities
		select	'Artifact Type Direct' as [Source],
				R.Visible,
				R.ID,
				R.TargetResponsibilityID,
				R.ResponsibilityTypeID,
				'ArtifactType' as AssigningItemType,
				T.ID as AssigningItemID,
				'ArtifactType' as ObjectType,
				T.ID as ObjectID,
				utility.GetResponsibilityContextHash(R.ID),
				1 as [Priority]
		from	ArtifactType T 
				inner join Responsibility R on R.ObjectType = 'ArtifactType' and R.ObjectID = T.ID;
	---------------------------------------------------

	---------------------------------------------------
	insert into #Responsibilities
		select	'Taxonomy Type Direct' as [Source],
				R.Visible,
				R.ID,
				R.TargetResponsibilityID,
				R.ResponsibilityTypeID,
				'TaxonomyType' as AssigningItemType,
				T.ID as AssigningItemID,
				'TaxonomyType' as ObjectType,
				T.ID as ObjectID,
				utility.GetResponsibilityContextHash(R.ID),
				1 as [Priority]
		from	TaxonomyType T 
				inner join Responsibility R on R.ObjectType = 'TaxonomyType' and R.ObjectID = T.ID;
	---------------------------------------------------

	-- Vertical Assignments from Taxonomy Type-------
	insert into #Responsibilities
		select	'Taxonomy Vertical' as [Source],
				R.Visible,
				R.ID,
				R.TargetResponsibilityID,
				R.ResponsibilityTypeID,
				'TaxonomyType' as AssigningItemType,
				T.ID as AssigningItemID,
				'Taxonomy' as ObjectType,
				A.ID as ObjectID,
				utility.GetResponsibilityContextHash(R.ID),
				1 as [Priority]
		from	TaxonomyType T 
				inner join Responsibility R on R.ObjectType = 'TaxonomyType' and R.ObjectID = T.ID
				inner join Taxonomy A on A.TaxonomyTypeID = T.ID;
	-------------------------------------------------

	---------------------------------------------------
	with ModelHierarchy as
	(
	select	R.Visible,
			T.ID as AssigningItemID,
			T.ID,
			T.ParentID,
			T.TaxonomyTypeID,
			R.ID as ResponsibilityID,
			R.TargetResponsibilityID,
			R.ResponsibilityTypeID,
			utility.GetResponsibilityContextHash(R.ID) as ContextHash,
			1 as [Level]
	from	Taxonomy T 
			left join Responsibility R on R.ObjectType = 'Taxonomy' and R.ObjectID = T.ID 
	--where	T.ParentID is null
	union all
	select	COALESCE(R.Visible, P.Visible) as Visible,
			COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
			C.ID,
			C.ParentID,
			C.TaxonomyTypeID,
			COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
			COALESCE(R.TargetResponsibilityID, P.TargetResponsibilityID) as TargetResponsibilityID,
			COALESCE(R.ResponsibilityTypeID, P.ResponsibilityTypeID) as ResponsibilityTypeID,
			coalesce(R.ContextHash, P.ContextHash) as ContextHash,
			P.[Level] + 1 as [Level]
	from	Taxonomy C
			inner join ModelHierarchy P on P.TaxonomyTypeID = C.TaxonomyTypeID and C.ParentID = P.ID
			outer apply (
						select	*,
								utility.GetResponsibilityContextHash(ID) as ContextHash
						from	Responsibility 
						where	ResponsibilityTypeID = P.ResponsibilityTypeID
								and ObjectType = 'Taxonomy' 
								and ObjectID = C.ID
						) R
	)

	insert into @tblModelHierarchy
		select	Visible,
				ResponsibilityID,
				TargetResponsibilityID,
				ResponsibilityTypeID,
				'Taxonomy' as AssigningItemType,
				AssigningItemID,
				'Taxonomy' as TargetObject, 
				ID,
				ContextHash,
				[Level]
		from	ModelHierarchy
		where	ResponsibilityID is not null;


	insert into #Responsibilities
		select	'ModelHierarchy' as [Source],
				O.Visible,
				O.ResponsibilityID,
				O.TargetResponsibilityID,
				O.ResponsibilityTypeID,
				O.AssigningItem,
				O.AssigningItemID,
				O.[Object],
				O.ObjectID,
				O.ContextHash,
				2 as [Priority]
		from	@tblModelHierarchy O
				inner join	(
							select		ResponsibilityTypeID,
										[Object],
										ObjectID,
										ContextHash,
										Max([Level]) as [Level]
							from		@tblModelHierarchy
							group by	ResponsibilityTypeID,
										[Object],
										ObjectID,
										ContextHash
							) M on M.ResponsibilityTypeID = O.ResponsibilityTypeID and M.[Object] = O.[Object] and M.ObjectID = O.ObjectID and M.ContextHash = O.ContextHash and M.[Level] = O.[Level];
	---------------------------------------------------


	-- Vertical Assignments from Artifact Type-------
	insert into #Responsibilities
		select	'Artifact Vertical' as [Source],
				R.Visible,
				R.ID,
				R.TargetResponsibilityID,
				R.ResponsibilityTypeID,
				'ArtifactType' as AssigningItemType,
				T.ID as AssigningItemID,
				'Artifact' as ObjectType,
				A.ID as ObjectID,
				utility.GetResponsibilityContextHash(R.ID),
				1 as [Priority]
		from	ArtifactType T 
				inner join Responsibility R on R.ObjectType = 'ArtifactType' and R.ObjectID = T.ID
				inner join Artifact A on A.ArtifactTypeID = T.ID;
	-------------------------------------------------


	-- Get Model Settings Propogated to Relations VIA OWNING MODEL ---
	delete @tblModelHierarchy;

	with ModelRelationHierarchy as
	(
	select	R.Visible,
			'Taxonomy' as AssigningItemType, 
			T.ID as AssigningItemID,
			T.ID,
			T.ParentID,
			T.TaxonomyTypeID,
			R.ID as ResponsibilityID,
			R.TargetResponsibilityID,
			R.ResponsibilityTypeID,
			utility.GetResponsibilityContextHash(R.ID) as ContextHash,
			1 as [Level]
	from	Taxonomy T 
			left join Responsibility R on R.ObjectType = 'Taxonomy' and R.ObjectID = T.ID 
	--where	T.ParentID is null
	union all
	select	
			COALESCE(R.Visible, P.Visible) as Visible,
			P.AssigningItemType,
			COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
			C.ID,
			C.ParentID,
			C.TaxonomyTypeID,
			COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
			COALESCE(R.TargetResponsibilityID, P.TargetResponsibilityID) as TargetResponsibilityID,
			COALESCE(R.ResponsibilityTypeID, P.ResponsibilityTypeID) as ResponsibilityTypeID,
			coalesce(R.ContextHash, P.ContextHash) as ContextHash,
			P.[Level] + 1 as [Level]
	from	Taxonomy C
			inner join ModelRelationHierarchy P on P.TaxonomyTypeID = C.TaxonomyTypeID and C.ParentID = P.ID
			outer apply (
						select	*,
								utility.GetResponsibilityContextHash(ID) as ContextHash
						from	Responsibility 
						where	ResponsibilityTypeID = P.ResponsibilityTypeID
								and ObjectType = 'Taxonomy' 
								and ObjectID = C.ID
						) R
	)

	insert into @tblModelHierarchy
		select		P.Visible,
					P.ResponsibilityID,
					P.TargetResponsibilityID,
					P.ResponsibilityTypeID,
					P.AssigningItemType,
					P.AssigningItemID,
					R.TargetObject, 
					R.TargetObjectID,
					P.ContextHash,
					P.[Level]
		from		ModelRelationHierarchy P
					inner join cache.Relationships R on 
						R.SourceObject = 'Taxonomy' and R.SourceObjectID = P.ID 
						and R.TargetObject = 'Artifact'
					inner join Artifact A on A.ID = R.TargetObjectID and A.TaxonomyTypeID = P.TaxonomyTypeID
		where		P.ResponsibilityID is not null;


	insert into #Responsibilities
		select	'ModelRelationHierarchy' as [Source],
				O.Visible,
				O.ResponsibilityID,
				O.TargetResponsibilityID,
				O.ResponsibilityTypeID,
				O.AssigningItem,
				O.AssigningItemID,
				O.[Object],
				O.ObjectID,
				O.ContextHash,
				2 as [Priority]
		from	@tblModelHierarchy O
				inner join	(
							select		ResponsibilityTypeID,
										[Object],
										ObjectID,
										ContextHash,
										Max([Level]) as [Level]
							from		@tblModelHierarchy
							group by	ResponsibilityTypeID,
										[Object],
										ObjectID,
										ContextHash
							) M on M.ResponsibilityTypeID = O.ResponsibilityTypeID and M.[Object] = O.[Object] and M.ObjectID = O.ObjectID and M.ContextHash = O.ContextHash and M.[Level] = O.[Level];
	-------------------------------------------------

	-- Artifact Overrides ---------------------------
	insert into #Responsibilities
		select	'Artifact Override' as [Source],
				R.Visible,
				R.ID,
				R.TargetResponsibilityID,
				R.ResponsibilityTypeID,
				'Artifact' as AssigningItemType,
				T.ID as AssigningItemID,
				'Artifact' as ObjectType,
				T.ID as ObjectID,
				utility.GetResponsibilityContextHash(R.ID),
				3 as [Priority]
		from	Artifact T 
				inner join Responsibility R on R.ObjectType = 'Artifact' and R.ObjectID = T.ID;
	-------------------------------------------------
	

	-- GLOSSARY (ARTIFACT) RESPONSIBILITY ASSIGNMENT LOGIC : END ----------------------------------------------------------------------------


	declare @current int = 1,
			@max int,
			@ResponsibilityID int,
			@TargetResponsibilityID int,
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
					@TargetResponsibilityID = TargetResponsibilityID,
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


	merge	cache.Responsibilities as T
	using	(
			select	distinct
					TR.ResponsibilityID,
					TR.ResponsibilityTypeID,
					RT.Name as ResponsibilityType,
					TR.AssigningItem,
					TR.AssigningItemID,
					AI.Name as AssigningItemName,
					AI.Url as AssigningItemUrl,
					AI.ObjectType as AssigningItemType,
					AI.ObjectTypeID as AssigningItemTypeID,
					AI.ObjectTypeName as AssigningTypeName,
					TR.[Object],
					TR.ObjectID,
					OI.Name as ObjectName,
					OI.ObjectType,
					OI.ObjectTypeID,
					OI.ObjectTypeName,
					OI.Url as ObjectUrl,
					R.ResponsibleObjectType as ResponsibleObject,
					R.ResponsibleObjectID,
					RI.Name as ResponsibleObjectName,
					RI.Url as ResponsibleObjectUrl,
					TR.ContextHash,
					TR.Visible,
					RT.ResponsibilityTypeGroup,
					TR.TargetResponsibilityID
			from	#Responsibilities TR
					inner join Responsibility R on R.ID = TR.ResponsibilityID
					inner join ResponsibilityType RT on RT.ID = R.ResponsibilityTypeID
					inner join cache.ObjectDetails AI on AI.[Object] = TR.[AssigningItem] and AI.ObjectID = TR.AssigningItemID
					inner join cache.ObjectDetails OI on OI.[Object] = TR.[Object] and OI.ObjectID = TR.ObjectID
					inner join cache.ObjectDetails RI on RI.[Object] = R.ResponsibleObjectType and RI.ObjectID = R.ResponsibleObjectID
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
			set		T.AssigningItemName = S.AssigningItemName,
					T.AssigningItemUrl = S.AssigningItemUrl,
					T.AssigningItemType = S.AssigningItemType,
					T.AssigningItemTypeID = S.AssigningItemTypeID,
					T.AssigningTypeName = S.AssigningTypeName,
					T.ObjectName = S.ObjectName,
					T.ObjectType = S.ObjectType,
					T.ObjectTypeID = S.ObjectTypeID,
					T.ObjectTypeName = S.ObjectTypeName,
					T.ObjectUrl = S.ObjectUrl,
					T.ResponsibleObject = S.ResponsibleObject,
					T.ResponsibleObjectID = S.ResponsibleObjectID,
					T.ResponsibleObjectName = S.ResponsibleObjectName,
					T.ResponsibleObjectUrl = S.ResponsibleObjectUrl,
					T.TargetResponsibilityID = S.TargetResponsibilityID,
					T.Visible = S.Visible
	when	not matched by target then
			INSERT	(
					[ResponsibilityID], [ResponsibilityTypeID], [ResponsibilityType], 
					[AssigningItem], [AssigningItemID], [AssigningItemName], [AssigningItemUrl], [AssigningItemType], [AssigningItemTypeID], [AssigningTypeName], 
					[Object], [ObjectID], [ObjectName], [ObjectType], [ObjectTypeID], [ObjectTypeName], [ObjectUrl], 
					[ResponsibleObject], [ResponsibleObjectID], [ResponsibleObjectName], [ResponsibleObjectUrl], 
					[ContextHash], [ResponsibilityTypeGroup], Visible,
					TargetResponsibilityID
					)
			VALUES	(
					S.[ResponsibilityID], S.[ResponsibilityTypeID], S.[ResponsibilityType], 
					S.[AssigningItem], S.[AssigningItemID], S.[AssigningItemName], S.[AssigningItemUrl], S.[AssigningItemType], S.[AssigningItemTypeID], S.[AssigningTypeName], 
					S.[Object], S.[ObjectID], S.[ObjectName], S.[ObjectType], S.[ObjectTypeID], S.[ObjectTypeName], S.[ObjectUrl], 
					S.[ResponsibleObject], S.[ResponsibleObjectID], S.[ResponsibleObjectName], S.[ResponsibleObjectUrl], 
					S.[ContextHash], S.[ResponsibilityTypeGroup], S.Visible,
					S.TargetResponsibilityID
					)
	when	not matched by source then 
			DELETE;
end
