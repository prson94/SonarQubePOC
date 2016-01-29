CREATE FUNCTION utility.GetVerticalResponsibilityList
(
	@Object varchar(50),
	@ObjectID int,
	@Priority int
)
RETURNS 
@tbl TABLE 
(
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
)
AS
BEGIN

	if @Object = 'ArtifactType' OR @Object = 'Artifact'
		begin
			insert into @tbl
				select	'Artifact Vertical' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						'ArtifactType' as AssigningItemType,
						T.ID as AssigningItemID,
						'Artifact' as ObjectType,
						A.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	ArtifactType T 
						inner join Responsibility R on R.ObjectType = 'ArtifactType' and R.ObjectID = T.ID
						inner join Artifact A on A.ArtifactTypeID = T.ID 
													and (
															(
																(
																(@Object = 'ArtifactType' and A.ArtifactTypeID = @ObjectID) OR 
																(@Object = 'Artifact' and A.ID = @ObjectID)
																)
																and @ObjectID is not null 
															)
															OR @ObjectID is null 
														);

			insert into @tbl
				select	'Taxonomy Vertical' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						'TaxonomyType' as AssigningItemType,
						T.ID as AssigningItemID,
						'Taxonomy' as ObjectType,
						A.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority+1 as [Priority]
				from	TaxonomyType T 
						inner join Responsibility R on R.ObjectType = 'TaxonomyType' and R.ObjectID = T.ID
						inner join Taxonomy A on A.TaxonomyTypeID = T.ID
						inner join Artifact AR on AR.TaxonomyTypeID = T.ID
												  and	(
															(
																(
																(@Object = 'ArtifactType' and AR.ArtifactTypeID = @ObjectID) OR 
																(@Object = 'Artifact' and AR.ID = @ObjectID)
																)
																and @ObjectID is not null 
															)
															OR @ObjectID is null 
														)
						inner join cache.Relationship RE on RE.SourceObject = 'Taxonomy' and RE.SourceObjectID = A.ID and RE.TargetObject = 'Artifact' and RE.TargetObjectID = AR.ID;
		end
	if @Object = 'DomainType' OR @Object = 'Domain'
		begin
			insert into @tbl
				select	'Domain Vertical' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						'DomainType' as AssigningItemType,
						T.ID as AssigningItemID,
						'Domain' as ObjectType,
						A.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	DomainType T 
						inner join Responsibility R on R.ObjectType = 'DomainType' and R.ObjectID = T.ID
						inner join Domain A on A.DomainTypeID = T.ID 
												and (
														(
															(
															(@Object = 'DomainType' and T.ID = @ObjectID) 
															OR (@Object = 'Domain' and A.ID = @ObjectID) 
															)
															and @ObjectID is not null
														)
														or (@ObjectID is null)
													);
		end
	if @Object = 'FusionType' OR @Object = 'Fusion'
		begin
			insert into @tbl
				select	'Fusion Vertical' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						'FusionType' as AssigningItemType,
						T.ID as AssigningItemID,
						'Fusion' as ObjectType,
						A.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	FusionType T 
						inner join Responsibility R on R.ObjectType = 'FusionType' and R.ObjectID = T.ID
						inner join Fusion A on A.FusionTypeID = T.ID 
												and (
														(
															(
															(@Object = 'FusionType' and T.ID = @ObjectID) 
															OR (@Object = 'Fusion' and A.ID = @ObjectID) 
															)
															and @ObjectID is not null
														)
														or (@ObjectID is null)
													);																		 
		end
	if @Object = 'TaxonomyType' OR @Object = 'Taxonomy'
		begin
			insert into @tbl
				select	'Taxonomy Vertical' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						'TaxonomyType' as AssigningItemType,
						T.ID as AssigningItemID,
						'Taxonomy' as ObjectType,
						A.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	TaxonomyType T 
						inner join Responsibility R on R.ObjectType = 'TaxonomyType' and R.ObjectID = T.ID
						inner join Taxonomy A on A.TaxonomyTypeID = T.ID 
												and (
														(
															(
															(@Object = 'TaxonomyType' and T.ID = @ObjectID) 
															OR (@Object = 'Taxonomy' and A.ID = @ObjectID)
															)
															and @ObjectID is not null
														)
														or (@ObjectID is null)
													);
		end
	RETURN 
END
GO



CREATE FUNCTION utility.GetDirectlyAssignedResponsibilityList
(
	@Object varchar(50),
	@ObjectID int,
	@Priority int
)
RETURNS 
@tbl TABLE 
(
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
)
AS
BEGIN

	if @Object = 'Artifact'
		begin
			insert into @tbl
				select	'Artifact Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	Artifact T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID 
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
	if @Object = 'ArtifactType'
		begin
			insert into @tbl
				select	'Artifact Type Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	ArtifactType T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID 
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
	if @Object = 'Domain'
		begin
			insert into @tbl
				select	'Domain Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	Domain T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
	if @Object = 'DomainType'
		begin
			insert into @tbl
				select	'Domain Type Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	DomainType T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
	if @Object = 'Fusion'
		begin
			insert into @tbl
				select	'Fusion Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	Fusion T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
	if @Object = 'FusionType'
		begin
			insert into @tbl
				select	'Fusion Type Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	FusionType T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
	if @Object = 'Rule'
		begin
			insert into @tbl
				select	'Rule Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						RU.ID as AssigningItemID,
						@Object as ObjectType,
						RU.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	[Rule] RU 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = RU.ID
							and (
								(RU.ID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
								);
		end
	if @Object = 'Taxonomy'
		begin
			insert into @tbl
				select	'Taxonomy Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	Taxonomy T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
								(T.ID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
								)
		end
	if @Object = 'TaxonomyType'
		begin
			insert into @tbl
				select	'Taxonomy Type Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	TaxonomyType T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
								(T.ID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
								)
		end
	RETURN 
END
GO

CREATE FUNCTION utility.GetHierarchyAssignedResponsibilityList
(
	@Object varchar(50),
	@ObjectID int,
	@Priority int
)
RETURNS 
@tbl TABLE 
(
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
)
AS
BEGIN
	declare @tblModelHierarchy table (
		Visible bit,
		ResponsibilityID int,
		ResponsibilityTypeID int,
		AssigningItem varchar(50),
		AssigningItemID int,
		[Object] varchar(50),
		ObjectID int,
		ContextHash varchar(50),
		[Level] int
	);

	if @Object = 'Artifact'
		begin
			with ModelRelationHierarchy as
			(
			select	R.Visible,
					'Taxonomy' as AssigningItemType, 
					T.ID as AssigningItemID,
					T.ID,
					T.ParentID,
					T.TaxonomyTypeID,
					R.ID as ResponsibilityID,
					R.ResponsibilityTypeID,
					utility.GetResponsibilityContextHash(R.ID) as ContextHash,
					1 as [Level]
			from	Taxonomy T 
					left join Responsibility R on R.ObjectType = 'Taxonomy' and R.ObjectID = T.ID 
			union all
			select	
					COALESCE(R.Visible, P.Visible) as Visible,
					P.AssigningItemType,
					COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
					C.ID,
					C.ParentID,
					C.TaxonomyTypeID,
					COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
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
							P.ResponsibilityTypeID,
							P.AssigningItemType,
							P.AssigningItemID,
							R.TargetObject, 
							R.TargetObjectID,
							P.ContextHash,
							P.[Level]
				from		ModelRelationHierarchy P
							inner join cache.Relationship R on 
								R.SourceObject = 'Taxonomy' and R.SourceObjectID = P.ID 
								and R.TargetObject = 'Artifact'
							inner join Artifact A on A.ID = R.TargetObjectID and A.TaxonomyTypeID = P.TaxonomyTypeID
								and (
									(A.ID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
									)
				where		P.ResponsibilityID is not null;


			insert into @tbl
				select	'Hierarchy Assigned' as [Source],
						O.Visible,
						O.ResponsibilityID,
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
		end

	if @Object = 'Policy'
		begin
			with PolicyHierarchy as
			(
			select	R.Visible,
					P.ID as AssigningItemID,
					P.ID,
					P.ParentID,
					R.ID as ResponsibilityID,
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

			insert into @tblModelHierarchy
				select	Visible,
						ResponsibilityID,
						ResponsibilityTypeID,
						'Policy' as AssigningItemType,
						AssigningItemID,
						'Policy' as TargetObject, 
						ID,
						ContextHash,
						[Level]
				from	PolicyHierarchy
				where	ResponsibilityID is not null;

			insert into @tbl
				select	'Hierarchy Assigned' as [Source],
						O.Visible,
						O.ResponsibilityID,
						O.ResponsibilityTypeID,
						O.AssigningItem,
						O.AssigningItemID,
						O.[Object],
						O.ObjectID,
						O.ContextHash,
						1 as [Priority]
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
									) M on M.ResponsibilityTypeID = O.ResponsibilityTypeID and M.[Object] = O.[Object] and M.ObjectID = O.ObjectID and M.ContextHash = O.ContextHash and M.[Level] = O.[Level]
											and (
												(O.ObjectID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
												)
		end
	if @Object = 'Taxonomy'
		begin
			with ModelHierarchy as
			(
			select	R.Visible,
					T.ID as AssigningItemID,
					T.ID,
					T.ParentID,
					T.TaxonomyTypeID,
					R.ID as ResponsibilityID,
					R.ResponsibilityTypeID,
					utility.GetResponsibilityContextHash(R.ID) as ContextHash,
					1 as [Level]
			from	Taxonomy T 
					left join Responsibility R on R.ObjectType = 'Taxonomy' and R.ObjectID = T.ID
			union all
			select	COALESCE(R.Visible, P.Visible) as Visible,
					COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
					C.ID,
					C.ParentID,
					C.TaxonomyTypeID,
					COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
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
						ResponsibilityTypeID,
						'Taxonomy' as AssigningItemType,
						AssigningItemID,
						'Taxonomy' as TargetObject, 
						ID,
						ContextHash,
						[Level]
				from	ModelHierarchy
				where	ResponsibilityID is not null;


			insert into @tbl
				select	'Hierarchy Assigned' as [Source],
						O.Visible,
						O.ResponsibilityID,
						O.ResponsibilityTypeID,
						O.AssigningItem,
						O.AssigningItemID,
						O.[Object],
						O.ObjectID,
						O.ContextHash,
						@Priority as [Priority]
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
									) M on M.ResponsibilityTypeID = O.ResponsibilityTypeID and M.[Object] = O.[Object] and M.ObjectID = O.ObjectID and M.ContextHash = O.ContextHash and M.[Level] = O.[Level]
											and (
												(O.ObjectID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
												)
		end
	RETURN 
END
GO

--[cache].[SynchronizeResponsibilitiesForObject] 'Artifact', 973904
ALTER procedure [cache].[SynchronizeResponsibilitiesForObject]
--declare
	@Object varchar(50),
	@ObjectID int
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

	delete #Responsibilities where [Object] + cast(ObjectID as varchar) <> @Object + cast(@ObjectID as varchar)
	delete cache.ResponsibilityItem where [Object] = @Object and ObjectID = @ObjectID

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
go

ALTER procedure [cache].[SynchronizeResponsibilities]
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
go

ALTER TRIGGER [dbo].[Responsibility_AfterInsert]
   ON  [dbo].[Responsibility] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Add', [queue].WriteIndexXml('', ObjectType, ObjectID, coalesce(UpdatedBy, 0)), 'Responsibility', ID from inserted

	declare @tbl table (RowID int identity, [ObjectType] varchar(50), ObjectID int)
	insert into @tbl 
		select [ObjectType], ObjectID from inserted
	declare @c int = 1,
			@m int,
			@o varchar(50),
			@oid int
	select @m = max(RowID) from @tbl

	while @c <= @m
	begin
		select	@o = ObjectType,
				@oid = ObjectID
		from	@tbl
		where	RowID = @c
		
		exec [cache].[SynchronizeResponsibilitiesForObject] @o, @oid

		set @c = @c + 1
	end
GO

ALTER TRIGGER [dbo].[Responsibility_AfterUpdate]
   ON  [dbo].[Responsibility] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Update', [queue].WriteIndexXml('', ObjectType, ObjectID, coalesce(UpdatedBy, 0)), 'Responsibility', ID from inserted

	declare @tbl table (RowID int identity, [ObjectType] varchar(50), ObjectID int)
	insert into @tbl 
		select [ObjectType], ObjectID from inserted
	declare @c int = 1,
			@m int,
			@o varchar(50),
			@oid int
	select @m = max(RowID) from @tbl

	while @c <= @m
	begin
		select	@o = ObjectType,
				@oid = ObjectID
		from	@tbl
		where	RowID = @c
		
		exec [cache].[SynchronizeResponsibilitiesForObject] @o, @oid

		set @c = @c + 1
	end
GO

ALTER TRIGGER [dbo].[Responsibility_AfterDelete]
   ON  [dbo].[Responsibility] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', ObjectType, ObjectID, coalesce(UpdatedBy, 0)), 'Responsibility', ID from deleted

	declare @tbl table (RowID int identity, [ObjectType] varchar(50), ObjectID int)
	insert into @tbl 
		select [ObjectType], ObjectID from deleted
	declare @c int = 1,
			@m int,
			@o varchar(50),
			@oid int
	select @m = max(RowID) from @tbl

	while @c <= @m
	begin
		select	@o = ObjectType,
				@oid = ObjectID
		from	@tbl
		where	RowID = @c
		
		exec [cache].[SynchronizeResponsibilitiesForObject] @o, @oid

		set @c = @c + 1
	end
GO


ALTER PROCEDURE [dbo].[DeleteIntersect]
	@ID int,
	@ResourceID int
AS
BEGIN
	SET NOCOUNT ON;
	declare @trancount int;
    set @trancount = @@trancount;	
	
	BEGIN TRY
		if @trancount = 0
            begin transaction
        else
			save transaction DeleteIntersect

		IF NOT EXISTS(select 1 from [Intersect] where ID = @ID)
		BEGIN
			RAISERROR('Item does not exist.', 16, 1);
		END

		IF EXISTS(select 1 from IntersectNode where ObjectType = 'Intersect' and ObjectID = @ID)
		BEGIN
			RAISERROR('Item is used in other relationships.', 16, 1);
		END

		IF EXISTS(
			select	TIN.ID
			from	IntersectNode s
					inner join IntersectNode t on t.IntersectID = s.IntersectID and t.ID <> s.ID and s.IntersectID = @ID
					inner join IntersectTypeNode ST on ST.ID = S.IntersectTypeNodeID and ST.[Order] = 1
					inner join Responsibility R on R.ResponsibleObjectType = s.ObjectType and R.ResponsibleObjectID = s.ObjectID and R.ObjectType = 'Intersect'
					inner join IntersectNode TIN on TIN.IntersectID = R.ObjectID and TIN.ObjectType = t.ObjectType and TIN.ObjectID = t.ObjectID
					inner join IntersectTypeNode TITN on TITN.ID = TIN.IntersectTypeNodeID and TITN.[Order] <> 1
		)
		BEGIN
			RAISERROR('Relationship is a source for other relationships.  You must first remove those consuming relationships before deleting this one.', 16, 1);
		END

		if exists(select 1 from [Attribute] where ObjectType = 'Intersect' and ObjectID = @ID)
		begin
			DELETE	[Attribute]
			WHERE	ObjectType = 'Intersect' and ObjectID = @ID
		end

		if exists(select 1 from Responsibility where ObjectType = 'Intersect' and ObjectID = @ID)
		begin
			delete	ResponsibilityContextItem
			where	ResponsibilityID in (
										select	ID
										from	Responsibility
										where	ObjectType = 'Intersect' 
												and ObjectID = @ID
										)

			delete	Responsibility
			where	ObjectType = 'Intersect' 
					and ObjectID = @ID
		end

		declare @nodes table ([Type] varchar(25), ID int, IntersectNodeID int)
	
		insert into @nodes
			select	n.ObjectType,
					n.ObjectID,
					n.ID
			from	IntersectNode n
			where	n.IntersectID = @ID


		declare @oType varchar(25), 
				@oID int, 
				@oNodeID int,
				@date datetime,
				@firstObject varchar(50),
				@firstObjectID int,
				@secondObject varchar(50),
				@secondObjectID int,
				@resolveResponsibilities bit = 0

		set @date = getutcdate()

		select	top 1
				@oType = [Type],
				@oID = ID,
				@oNodeID = IntersectNodeID
		from	@nodes

		set @firstObject = @oType
		set @firstObjectID = @oID

		exec utility.AddAuditEntry @oType, @oID, @ResourceID, @date, 'Removed', 'Intersect', @ID
		delete @nodes where IntersectNodeID = @oNodeID

		select	top 1
				@oType = [Type],
				@oID = ID,
				@oNodeID = IntersectNodeID
		from	@nodes

		set @secondObject  = @oType
		set @secondObjectID = @oID

		exec utility.AddAuditEntry @oType, @oID, @ResourceID, @date, 'Removed', 'Intersect', @ID
		delete @nodes where IntersectNodeID = @oNodeID

		-- Now delete the actual records.
		delete	IntersectNode
		where	IntersectID = @ID

		delete	[Intersect]
		where	ID = @ID


		delete cache.Relationship where IntersectID = @ID

		--Update the responsibilities of the object that should inherit form the other (Taxonomy can push relationships down to artifact)
		if ( (@firstObject = 'Taxonomy' and @secondObject = 'Artifact') OR (@firstObject = 'Artifact' and @secondObject = 'Taxonomy') )
		begin
			if @firstObject = 'Artifact'
			begin
				exec [cache].[SynchronizeResponsibilitiesForObject] @firstObject, @firstObjectID
			end
			if @secondObject = 'Artifact'
			begin
				exec [cache].[SynchronizeResponsibilitiesForObject] @secondObject, @secondObjectID
			end
		end

		if @trancount = 0
			commit;
	END TRY
	BEGIN CATCH
		declare @message varchar(4000), @xstate int;
        select @message = ERROR_MESSAGE(), @xstate = XACT_STATE();
        if @xstate = -1
            rollback;
        if @xstate = 1 and @trancount = 0
            rollback
        if @xstate = 1 and @trancount > 0
            rollback transaction DeleteIntersect;

        raiserror ('Unable to remove relationship: %s', 16, 1, @message);
	END CATCH
END
GO


alter procedure [dbo].[AddRelationships]
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
					INSERT INTO [Intersect] (IntersectTypeID, Classification, [Description]) VALUES (@IntersectTypeID, @Classification, @Description)

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
end
GO

alter procedure [dbo].[AsyncDeleteObject]
	@Object varchar(50),
	@ObjectID int,
	@ParentObject varchar(50),
	@ParentObjectID int,
	@ResourceID int
as
begin
	set nocount on;
	
	declare @trans varchar(25) = 'Trans',
			@current int = 1,
			@max int,
			@date datetime = getutcdate()

	begin try
		begin transaction @trans

		exec [utility].[AddAuditEntry] @ParentObject, @ParentObjectID, @ResourceID, @date, 'Removed', @Object, @ObjectID

		--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID], [Priority])
		--values ('ObjectIndex', 'D', @Object, @ObjectID, 4)

		--COMMON
		delete CommentRelation					where ObjectType = @Object and ObjectID = @ObjectID
		delete Field							where ObjectType = @Object and ObjectID = @ObjectID
		delete Follow							where ObjectType = @Object and ObjectID = @ObjectID
		delete Responsibility					where ObjectType = @Object and ObjectID = @ObjectID
		delete SurveyObjectCache				where ObjectType = @Object and ObjectID = @ObjectID
		delete cache.[Object]					where [Object] = @Object and ObjectID = @ObjectID--delete cache.ObjectDetails				where [Object] = @Object and ObjectID = @ObjectID

		if charindex('Type', @Object) > 0
		begin
			delete AttributeTypeRelation			where ObjectType = @Object AND ObjectID = @ObjectID
			delete FieldType						where [Object] = @Object AND ObjectID = @ObjectID
			delete ResponsibilityTypeRelation		where ObjectType = @Object and ObjectID = @ObjectID
			delete ResponsibilityTypeObjectClaim	where ObjectType = @Object and ObjectID = @ObjectID
			delete StatisticTypeRelation			where ObjectType = @Object and ObjectID = @ObjectID
			delete WorkflowTypeRelation				where [Object] = @Object and ObjectID = @ObjectID

			if @Object in ('AttributeTypeRelation', 'AttributeTypeRelation', 'ResponsibilityTypeRelation', 'ResponsibilityType')
			begin
				exec utility.CalculateStatistics
			end

			if @Object = 'ArtifactType'
			begin
				declare @ah table (ID int);
				with ah as	(
							select	ID, 
									ParentID
							from	Artifact
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID
							from	Artifact C
									inner join ah P on P.ID = C.ParentID
							)
				insert into @ah 
					select ID from ah
			
				delete Artifact where ID in (select ID from @ah)
			end

			if @Object = 'AttributeType'
			begin
				delete AttributeTypeRelation		where AttributeTypeID = @ObjectID
			
				declare @ath table (RowID int identity, ID int, ParentID int null, [Level] int);
				with ath as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	AttributeType
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	AttributeType C
									inner join ath P on P.ID = C.ParentID
							)
				insert into @ath 
					select ID, ParentID, [Level] from ath order by [Level] desc

				select @max = max(RowID) from @ath

				while @current <= @max
				begin
					declare @attributeTypeID int
					select @attributeTypeID = ID from @ath where RowID = @current
					delete Attribute where AttributeTypeID = @attributeTypeID
					delete AttributeType where ID = @attributeTypeID
					set @current = @current + 1
				end
			end

			if @Object = 'DomainType'
			begin
				delete DomainItem where DomainID in (select ID from Domain where DomainTypeID = @ObjectID)
				delete Domain where DomainTypeID = @ObjectID
				delete DomainGroup where DomainTypeID = @ObjectID
			end

			if @Object = 'FieldType'
			begin
				delete Field where FieldTypeID = @ObjectID
				delete FieldType where ID = @ObjectID
			end

			if @Object = 'FusionAttributeType'
			begin
				declare @fath table (RowID int identity, ID int, ParentID int null, [Level] int);
				with fath as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	FusionAttributeType
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	FusionAttributeType C
									inner join fath P on P.ID = C.ParentID
							)
				insert into @fath 
					select ID, ParentID, [Level] from fath order by [Level] desc

				select @max = max(RowID) from @fath

				while @current <= @max
				begin
					declare @fusionAttributeTypeID int
					select @fusionAttributeTypeID = ID from @fath where RowID = @current
					delete FusionAttribute where FusionAttributeTypeID = @fusionAttributeTypeID
					delete FusionAttributeType where ID = @fusionAttributeTypeID
					set @current = @current + 1
				end
			end

			if @Object = 'FusionType'
			begin
				declare @fth table (RowID int identity, ID int, ParentID int null, [Level] int);
				with fth as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	FusionAttributeType
							where	FusionTypeID = @ObjectID and ParentID is null
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	FusionAttributeType C
									inner join fth P on P.ID = C.ParentID
							)
				insert into @fth 
					select ID, ParentID, [Level] from fth order by [Level] desc

				select @max = max(RowID) from @fth

				while @current <= @max
				begin
					declare @fattributeTypeID int
					select @fattributeTypeID = ID from @fth where RowID = @current
					delete FusionAttribute where FusionAttributeTypeID = @fattributeTypeID
					delete FusionAttributeType where ID = @fattributeTypeID
					set @current = @current + 1
				end
				delete FusionType where ID = @ObjectID
			end

			if @Object = 'IntersectType'
			begin
				-- Stores the sources we have identified through the loop below.
				declare @tblRelationshipIDs table (ID int)

				--Seed initial tables values
				insert into @tblRelationshipIDs
					select	R.ID 
					from	Responsibility R
							inner join [Intersect] I on I.IntersectTypeID = 2 and R.ObjectType = 'Intersect' and R.ObjectID = I.ID 

				-- follow trail all the way back.
				while exists(
						select	1 
						from	Responsibility
						where	TargetResponsibilityID in (select ID from @tblRelationshipIDs)
								and ID not in (select ID from @tblRelationshipIDs)
				)
				begin
					insert into @tblRelationshipIDs
						select	ID
						from	Responsibility
						where	TargetResponsibilityID in (select ID from @tblRelationshipIDs)
								and ID not in (select ID from @tblRelationshipIDs)
				end

				delete Responsibility where ID in (select ID from @tblRelationshipIDs)

				delete [Intersect] where IntersectTypeID = @ObjectID
				delete IntersectType where ID = @ObjectID
			end

			if @Object = 'LookupType'
			begin
				delete [Lookup] where LookupTypeID = @ObjectID
			end

			if @Object = 'PolicyType'
			begin
				delete Policy where PolicyTypeID = @ObjectID
				delete PolicyTypeLevel where PolicyTypeID = @ObjectID
			end

			if @Object = 'ResponsibilityType'
			begin
				delete Responsibility where ResponsibilityTypeID = @ObjectID
				delete ResponsibilityType where ID = @ObjectID
			end

			if @Object = 'StatisticType'
			begin
				delete [Statistic] where StatisticTypeID = @ObjectID
				delete [StatisticTypeRelation] where StatisticTypeID = @ObjectID
			end

			if @Object = 'SurveyType'
			begin
				delete SurveyObjectCache where SurveyTypeID = @ObjectID
				delete Survey where SurveyTypeID = @ObjectID
				delete SurveyType where ID = @ObjectID
			end

			if @Object = 'TaxonomyType'
			begin
				delete Taxonomy where TaxonomyTypeID = @ObjectID
				delete TaxonomyTypeLevel where TaxonomyTypeID = @ObjectID
				delete TaxonomyType where ID = @ObjectID
			end

		end
		else
		begin
			delete Attribute							where ObjectType = @Object and ObjectID = @ObjectID
			delete cache.Relationship					where [SourceObject] = @Object and SourceObjectID = @ObjectID
			delete cache.Relationship					where [TargetObject] = @Object and TargetObjectID = @ObjectID

			BEGIN TRY
				DECLARE @tblIntersectIDs table (ID int)

				INSERT INTO @tblIntersectIDs
					SELECT	IntersectID
					FROM	IntersectNode
					WHERE	ObjectType = @Object and ObjectID = @ObjectID

				delete [Intersect] where ID in (select ID from @tblIntersectIDs)
			END TRY
			BEGIN CATCH

			END CATCH

			if @Object = 'Artifact'
			begin
				delete	RelatedArtifact where ArtifactID = @ObjectID
			end

			if @Object = 'Domain'
			begin
				delete DomainItem where DomainID = @ObjectID
			end

			if @Object = 'Responsibility'
			begin
				exec cache.SynchronizeResponsibilitiesForObject @ParentObject, @ParentObjectID 
			end

			if @Object = 'Taxonomy'
			begin
				declare @th table (ID int);
				with th as	(
							select	ID, 
									ParentID
							from	Taxonomy
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID
							from	Taxonomy C
									inner join th P on P.ID = C.ParentID
							)
				insert into @th 
					select ID from th
			
				delete Taxonomy where ID in (select ID from @th)

				exec cache.SynchronizeResponsibilities
			end
		end
		
		commit transaction @trans
	end try
	begin catch
		DECLARE @ErrorMessage NVARCHAR(4000);
		DECLARE @ErrorSeverity INT;
		DECLARE @ErrorState INT;

		SELECT 
			@ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE();

		-- Use RAISERROR inside the CATCH block to return error
		-- information about the original error that caused
		-- execution to jump to the CATCH block.
		RAISERROR (@ErrorMessage, -- Message text.
				   @ErrorSeverity, -- Severity.
				   @ErrorState -- State.
				   );

		rollback transaction @trans
	end catch
end
GO

alter procedure [dbo].[DeleteObject]
	@Obj varchar(50),
	@ObjectID int,
	@ResourceID int
as
begin
	set nocount on;
	
	declare @Object varchar(50) = @Obj,
			@trans varchar(25) = 'Trans',
			@current int = 1,
			@max int

	begin try
		begin transaction @trans

		INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        VALUES (
				'ObjectVersion', 
				'<fields>
				 <Action>Removed</Action>
				 <ActionObject>' + @Obj + '</ActionObject>
				 <ActionObjectID>' + cast(@ObjectID as varchar) + '</ActionObjectID>
				 <ResourceID>' + cast(@ResourceID as varchar) + '</ResourceID>
				</fields>', 
				@Obj, 
				@ObjectID)

		INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		values ('ObjectIndex', 'D', @Obj, @ObjectID)

		--COMMON
		delete CommentRelation					where ObjectType = @Object and ObjectID = @ObjectID
		delete Field							where ObjectType = @Object and ObjectID = @ObjectID
		delete Follow							where ObjectType = @Object and ObjectID = @ObjectID
		delete Responsibility					where ObjectType = @Object and ObjectID = @ObjectID
		delete SurveyObjectCache				where ObjectType = @Object and ObjectID = @ObjectID
		delete cache.[Object]					where [Object] = @Object and ObjectID = @ObjectID

		if charindex('Type', @Object) > 0
		begin
			delete AttributeTypeRelation			where ObjectType = @Object AND ObjectID = @ObjectID
			delete FieldType						where [Object] = @Object AND ObjectID = @ObjectID
			delete ResponsibilityTypeRelation		where ObjectType = @Object and ObjectID = @ObjectID
			delete ResponsibilityTypeObjectClaim	where ObjectType = @Object and ObjectID = @ObjectID
			delete StatisticTypeRelation			where ObjectType = @Object and ObjectID = @ObjectID
			delete WorkflowTypeRelation				where [Object] = @Object and ObjectID = @ObjectID

			if @Object = 'ArtifactType'
			begin
				declare @ah table (ID int);
				with ah as	(
							select	ID, 
									ParentID
							from	Artifact
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID
							from	Artifact C
									inner join ah P on P.ID = C.ParentID
							)
				insert into @ah 
					select ID from ah
			
				delete Artifact where ID in (select ID from @ah)
			end

			if @Object = 'AttributeType'
			begin
				delete AttributeTypeRelation		where AttributeTypeID = @ObjectID
			
				declare @ath table (RowID int identity, ID int, ParentID int null, [Level] int);
				with ath as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	AttributeType
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	AttributeType C
									inner join ath P on P.ID = C.ParentID
							)
				insert into @ath 
					select ID, ParentID, [Level] from ath order by [Level] desc

				select @max = max(RowID) from @ath

				while @current <= @max
				begin
					declare @attributeTypeID int
					select @attributeTypeID = ID from @ath where RowID = @current
					delete Attribute where AttributeTypeID = @attributeTypeID
					delete AttributeType where ID = @attributeTypeID
					set @current = @current + 1
				end
			end

			if @Object = 'DomainType'
			begin
				delete DomainItem where DomainID in (select ID from Domain where DomainTypeID = @ObjectID)
				delete Domain where DomainTypeID = @ObjectID
				delete DomainGroup where DomainTypeID = @ObjectID
			end

			if @Object = 'FieldType'
			begin
				delete Field where FieldTypeID = @ObjectID
				delete FieldType where ID = @ObjectID
			end

			if @Object = 'FusionAttributeType'
			begin
				declare @fath table (RowID int identity, ID int, ParentID int null, [Level] int);
				with fath as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	FusionAttributeType
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	FusionAttributeType C
									inner join fath P on P.ID = C.ParentID
							)
				insert into @fath 
					select ID, ParentID, [Level] from fath order by [Level] desc

				select @max = max(RowID) from @fath

				while @current <= @max
				begin
					declare @fusionAttributeTypeID int
					select @fusionAttributeTypeID = ID from @fath where RowID = @current
					delete FusionAttribute where FusionAttributeTypeID = @fusionAttributeTypeID
					delete FusionAttributeType where ID = @fusionAttributeTypeID
					set @current = @current + 1
				end
			end

			if @Object = 'FusionType'
			begin
				declare @fth table (RowID int identity, ID int, ParentID int null, [Level] int);
				with fth as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	FusionAttributeType
							where	FusionTypeID = @ObjectID and ParentID is null
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	FusionAttributeType C
									inner join fth P on P.ID = C.ParentID
							)
				insert into @fth 
					select ID, ParentID, [Level] from fth order by [Level] desc

				select @max = max(RowID) from @fth

				while @current <= @max
				begin
					declare @fattributeTypeID int
					select @fattributeTypeID = ID from @fth where RowID = @current
					delete FusionAttribute where FusionAttributeTypeID = @fattributeTypeID
					delete FusionAttributeType where ID = @fattributeTypeID
					set @current = @current + 1
				end
				delete FusionType where ID = @ObjectID
			end

			if @Object = 'IntersectType'
			begin
				-- Stores the sources we have identified through the loop below.
				declare @tblRelationshipIDs table (ID int)

				--Seed initial tables values
				insert into @tblRelationshipIDs
					select	R.ID 
					from	Responsibility R
							inner join [Intersect] I on I.IntersectTypeID = 2 and R.ObjectType = 'Intersect' and R.ObjectID = I.ID 

				-- follow trail all the way back.
				while exists(
						select	1 
						from	Responsibility
						where	TargetResponsibilityID in (select ID from @tblRelationshipIDs)
								and ID not in (select ID from @tblRelationshipIDs)
				)
				begin
					insert into @tblRelationshipIDs
						select	ID
						from	Responsibility
						where	TargetResponsibilityID in (select ID from @tblRelationshipIDs)
								and ID not in (select ID from @tblRelationshipIDs)
				end

				delete Responsibility where ID in (select ID from @tblRelationshipIDs)

				delete [Intersect] where IntersectTypeID = @ObjectID
				delete IntersectType where ID = @ObjectID
			end

			if @Object = 'LookupType'
			begin
				delete [Lookup] where LookupTypeID = @ObjectID
			end

			if @Object = 'PolicyType'
			begin
				delete Policy where PolicyTypeID = @ObjectID
				delete PolicyTypeLevel where PolicyTypeID = @ObjectID
			end

			if @Object = 'ResponsibilityType'
			begin
				delete Responsibility where ResponsibilityTypeID = @ObjectID
				delete ResponsibilityType where ID = @ObjectID
			end

			if @Object = 'StatisticType'
			begin
				delete [Statistic] where StatisticTypeID = @ObjectID
				delete [StatisticTypeRelation] where StatisticTypeID = @ObjectID
			end

			if @Object = 'SurveyType'
			begin
				delete SurveyObjectCache where SurveyTypeID = @ObjectID
				delete Survey where SurveyTypeID = @ObjectID
				delete SurveyType where ID = @ObjectID
			end

			if @Object = 'TaxonomyType'
			begin
				delete Taxonomy where TaxonomyTypeID = @ObjectID
				delete TaxonomyTypeLevel where TaxonomyTypeID = @ObjectID
				delete TaxonomyType where ID = @ObjectID
			end

		end
		else
		begin
			delete Attribute							where ObjectType = @Object and ObjectID = @ObjectID
			delete cache.Relationship					where [SourceObject] = @Object and SourceObjectID = @ObjectID
			delete cache.Relationship					where [TargetObject] = @Object and TargetObjectID = @ObjectID

			BEGIN TRY
				DECLARE @tblIntersectIDs table (ID int)

				INSERT INTO @tblIntersectIDs
					SELECT	IntersectID
					FROM	IntersectNode
					WHERE	ObjectType = @Object and ObjectID = @ObjectID

				delete [Intersect] where ID in (select ID from @tblIntersectIDs)
			END TRY
			BEGIN CATCH

			END CATCH

			if @Object = 'Artifact'
			begin
				delete	RelatedArtifact where ArtifactID = @ObjectID
			end

			if @Object = 'Domain'
			begin
				delete DomainItem where DomainID = @ObjectID
			end

			if @Object = 'Taxonomy'
			begin
				declare @th table (ID int);
				with th as	(
							select	ID, 
									ParentID
							from	Taxonomy
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID
							from	Taxonomy C
									inner join th P on P.ID = C.ParentID
							)
				insert into @th 
					select ID from th
			
				delete Taxonomy where ID in (select ID from @th)
			end
		end
		
		commit transaction @trans
	end try
	begin catch
		 DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;

    SELECT 
        @ErrorMessage = ERROR_MESSAGE(),
        @ErrorSeverity = ERROR_SEVERITY(),
        @ErrorState = ERROR_STATE();

    -- Use RAISERROR inside the CATCH block to return error
    -- information about the original error that caused
    -- execution to jump to the CATCH block.
    RAISERROR (@ErrorMessage, -- Message text.
               @ErrorSeverity, -- Severity.
               @ErrorState -- State.
               );

		rollback transaction @trans
	end catch
end
GO

alter procedure [dbo].[GetLineageDiagram]
--declare
	@type varchar(50),
	@id int
--set @type = 'Artifact'
--set @id = 4651
as
begin
	
	declare @rows table (ID int, [Subject] varchar(50), [SubjectID] int, [Object] varchar(50), ObjectID int, PredicateID int, [Level] int);
	
	-- Process upstream
	declare @level int = 1

	insert into @rows
		select	M.ID,
				SR.SourceObject,
				SR.SourceObjectID,
				@type as TargetObject,
				@id as TargetObjectID,
				M.[PredicateID],
				@level as [Level]
		from	IntersectMap M
				inner join cache.Relationship SR on SR.TargetIntersectNodeID = M.ObjectIntersectNodeID and M.[Type] = 1 and SR.TargetObject = @type and SR.TargetObjectID = @id
				--inner join cache.Relationship TR on TR.TargetIntersectNodeID = M.ObjectIntersectNodeID and M.[Type] = 1 and TR.TargetObject = @type and TR.TargetObjectID = @id;

	while exists(
			select	1
			from	@rows as d
					inner join cache.Relationship R on R.TargetObject = d.[Subject] and R.TargetObjectID = d.[SubjectID]
					inner join IntersectMap M on M.ID not in (select ID from @rows) and M.[Type] = 1 and R.TargetIntersectNodeID = M.ObjectIntersectNodeID
			where	exists(select 1 from cache.Relationship where SourceObject = @type and SourceObjectID = @id and TargetObject = R.SourceObject and TargetObjectID = R.SourceObjectID)
					--and d.[Level] between 1 and (@level-1)
		)
	begin
		set @level = @level + 1
		insert into @rows
			select	M.ID,
					R.SourceObject,
					R.SourceObjectID,
					d.[Subject] as TargetObject,
					d.SubjectID as TargetObjectID,
					M.[PredicateID],
					@level
			from	@rows as d
					inner join cache.Relationship R on R.TargetObject = d.[Subject] and R.TargetObjectID = d.[SubjectID]
					inner join IntersectMap M on M.ID not in (select ID from @rows) and M.[Type] = 1 and R.TargetIntersectNodeID = M.ObjectIntersectNodeID
			where	exists(select 1 from cache.Relationship where SourceObject = @type and SourceObjectID = @id and TargetObject = R.SourceObject and TargetObjectID = R.SourceObjectID)
					--and d.[Level] between 1 and (@level-1)
	end

	-- Process downstream
	set @level = -1
	
	insert into @rows
		select	M.ID,
				@type as SourceObject,
				@id as SourceObjectID,
				R.TargetObject,
				R.TargetObjectID,
				M.[PredicateID],
				@level as [Level]
		from	IntersectMap M
				inner join cache.Relationship R on R.SourceIntersectNodeID = M.SubjectIntersectNodeID and M.[Type] = 1 and R.SourceObject = @type and R.SourceObjectID = @id;


	while exists(
			select	1
			from	@rows as d
					inner join cache.Relationship R on R.SourceObject = d.[Object] and R.SourceObjectID = d.[ObjectID]
					inner join IntersectMap M on M.ID not in (select ID from @rows) and M.[Type] = 1 and R.SourceIntersectNodeID = M.SubjectIntersectNodeID
			where	exists(select 1 from cache.Relationship where SourceObject = @type and SourceObjectID = @id and TargetObject = R.TargetObject and TargetObjectID = R.TargetObjectID)
					and d.[Level] between -1 and (@level+1)
		)
	begin
		set @level = @level - 1
		insert into @rows
			select	M.ID,
					d.[Object] as SourceObject,
					d.[ObjectID] as SourceObjectID,
					R.TargetObject as TargetObject,
					R.TargetObjectID as TargetObjectID,
					M.[PredicateID],
					@level
			from	@rows as d
					inner join cache.Relationship R on R.SourceObject = d.[Object] and R.SourceObjectID = d.[ObjectID]
					inner join IntersectMap M on M.ID not in (select ID from @rows) and M.[Type] = 1 and R.SourceIntersectNodeID = M.SubjectIntersectNodeID
			where	exists(select 1 from cache.Relationship where SourceObject = @type and SourceObjectID = @id and TargetObject = R.TargetObject and TargetObjectID = R.TargetObjectID)
					and d.[Level] between -1 and (@level+1)
	end

	select	R.ID as IntersectMapID,
			R.[Level],
			S.[Object] as Sub,
			S.ObjectID as SubID,
			case R.[Level] when -1 then '0' else cast(R.[Level] as varchar) end + S.[Object] + cast(S.ObjectID as varchar) as SubjectID,
			S.TextPath as [Subject],
			S.ObjectTypeName as SubjectType,
			S.IconBackColor as SubjectBackColor,
			S.IconForeColor as SubjectForeColor,
			O.[Object] as Obj,
			O.ObjectID as ObjID,
			cast(R.[Level]-1 as varchar) + O.[Object] + cast(O.ObjectID as varchar) as ObjectID,
			O.TextPath as [Object],
			O.ObjectTypeName as ObjectType,
			O.IconBackColor as ObjectBackColor,
			O.IconForeColor as ObjectForeColor,
			P.Name as Predicate,
			0 as Exclude
	from	@rows R
			inner join cache.ObjectDetails S on S.[Object] = R.[Subject] and S.ObjectID = R.SubjectID
			inner join cache.ObjectDetails O on O.[Object] = R.[Object] and O.ObjectID = R.ObjectID
			inner join Predicate P on P.ID = R.PredicateID
end
GO

DROP TABLE [dbo].[ResponsibilityTypeSourceType]
GO

DROP PROCEDURE GetEnvironmentDetailsDiagramData
GO

DROP PROCEDURE [dbo].[GetLineageDiagramData]
GO

DROP PROCEDURE [dbo].[GetMapDiagram]
GO

DROP PROCEDURE [dbo].[ExcludeMapIntersect]
GO

DROP PROCEDURE [dbo].[FindExcludeMapIntersect]
GO

DROP TABLE [dbo].[IntersectMapExclusion]
GO

DROP TABLE [dbo].[PredicatePhrase]
GO

DROP TABLE [dbo].[IntersectMapContextItem]
GO

DROP TABLE [dbo].[ResponsibilityTransformation]
GO

DROP TABLE [dbo].[ResponsibilityTypeHierarchy]
GO

DROP TABLE [dbo].[Map]
GO


alter procedure [utility].[CalculateStatistics]
--declare
	@Type varchar(50) = NULL,
	@ID int = NULL,
	@TargetStatisticTypeID int = NULL
as
begin
	SET NOCOUNT ON;

	declare @current int, @max int
	declare @relations table (ID int identity, ObjectType varchar(50), ObjectID int, PossibleScore int)

	IF OBJECT_ID('tempdb..#StatisticTypes') IS NOT NULL
	BEGIN
		DROP TABLE #StatisticTypes
	END
	create table #StatisticTypes (ID int identity, StatisticTypeID int)

	insert into #StatisticTypes
		select ID from StatisticType where (@TargetStatisticTypeID is not null and ID = @TargetStatisticTypeID) OR @TargetStatisticTypeID is null order by ID

	set		@current	= 1
	select	@max		= MAX(ID) from #StatisticTypes

	IF OBJECT_ID('tempdb..#Statistics') IS NOT NULL
	BEGIN
		DROP TABLE #Statistics
	END
	create table #Statistics (StatisticTypeID int, ObjectType varchar(50), ObjectID int, Score int)
--select * from StatisticType
	while @current <= @max
	begin
		declare @StatisticTypeID int,
				@CheckType int,
				@CheckObjectType varchar(25),
				@CheckObjectID int,
				@ObjectType varchar(25),
				@ObjectID int,
				@PropertyName varchar(250),
				@Value nvarchar(4000),
				@Configuration xml

		select	@StatisticTypeID = S.ID,
				@CheckType = S.CheckType,
				@Configuration = S.Configuration 
		from	#StatisticTypes T
				inner join StatisticType S on S.ID = T.StatisticTypeID
		where	T.ID = @current

		delete @relations

		insert into @relations
			select	O.ObjectType, 
					O.ObjectID,
					R.Score
			from	StatisticTypeRelation R
					cross apply	(
								select	ID as ObjectID,
										'Artifact' as ObjectType,
										ArtifactTypeID as TypeID
								from	Artifact 
								where	R.ObjectType = 'ArtifactType' and ArtifactTypeID = R.ObjectID
										and (
											(@Type = 'Artifact' and ID = @ID and @Type is not null) OR (@Type is null) 
											)
								union
								select	ID as ObjectID,
										'Domain' as ObjectType,
										DomainTypeID as TypeID
								from	Domain 
								where	R.ObjectType = 'DomainType' and DomainTypeID = R.ObjectID
										and (
											(@Type = 'Domain' and ID = @ID and @Type is not null) OR (@Type is null) 
											)
								union
								select	ID as ObjectID,
										'Fusion' as ObjectType,
										FusionTypeID as TypeID
								from	Fusion 
								where	R.ObjectType = 'FusionType' and FusionTypeID = R.ObjectID
										and (
											(@Type = 'Fusion' and ID = @ID and @Type is not null) OR (@Type is null) 
											)
								union	
								select	ID as ObjectID,
										'Taxonomy' as ObjectType,
										TaxonomyTypeID as TypeID
								from	Taxonomy 
								where	R.ObjectType = 'TaxonomyType' and TaxonomyTypeID = R.ObjectID
										and (
											(@Type = 'Taxonomy' and ID = @ID and @Type is not null) OR (@Type is null) 
											)
								union	
								select	ID as ObjectID,
										'Group' as ObjectType,
										0 as TypeID
								from	[Group]
								where	R.ObjectType = 'Group' and R.ObjectID = 0
										and (
											(@Type = 'Group' and ID = @ID and @Type is not null) OR (@Type is null) 
											)
								union	
								select	ResourceID as ObjectID,
										'Resource' as ObjectType,
										1 as TypeID
								from	reporting.Global_Resource 
								where	R.ObjectType = 'Resource' and R.ObjectID = 1
										and (
											(@Type = 'Resource' and ResourceID = @ID and @Type is not null) OR (@Type is null) 
											)
								) O
			where	StatisticTypeID = @StatisticTypeID

		-- EXISTENCE
		if (@CheckType = 1)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			if @CheckObjectType = 'AttributeType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.ObjectType,
							R.ObjectID,
							case 
								when O.ValueExists <> 0 then R.PossibleScore
								else 0
							end as Score
					from	@relations R
							outer apply (
										select		ISNULL(AttributeTypeID, 0) as ValueExists
										from		Attribute 
										where		ObjectType = R.ObjectType and ObjectID = R.ObjectID and AttributeTypeID = @CheckObjectID
										group by	AttributeTypeID, ObjectType, ObjectID
										) O
			end

			if @CheckObjectType = 'ResponsibilityType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.ObjectType,
							R.ObjectID,
							case 
								when P.ValueExists <> 0 then R.PossibleScore
								else 0
							end as Score
					from	@relations R
							outer apply (
										select		ISNULL(ResponsibilityTypeID, 0) as ValueExists
										from		ResponsibilityDetail
										where		ObjectType = R.ObjectType and ObjectID = R.ObjectID and ResponsibilityTypeID = @CheckObjectID
										group by	ResponsibilityTypeID, ObjectType, ObjectID
										) P
			end
		end

		-- COUNT (instead of score)
		if (@CheckType = 2)	--COUNT
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			if @CheckObjectType = 'AttributeType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.ObjectType,
							R.ObjectID,
							COALESCE(O.Score, 0) as Score
					from	@relations R
							outer apply (
										select		COUNT(AttributeTypeID) as Score
										from		Attribute 
										where		ObjectType = R.ObjectType and ObjectID = R.ObjectID and AttributeTypeID = @CheckObjectID
										group by	AttributeTypeID, ObjectType, ObjectID
										) O
			end

			--if @CheckObjectType = 'EventType'
			--begin
			--	insert into #Statistics
			--		select	@StatisticTypeID as StatisticTypeID,
			--				R.ObjectType,
			--				R.ObjectID,
			--				O.Score
			--		from	@relations R
			--				outer apply (
			--							select		COUNT(RoleID) as Score
			--							from		[Event]
			--							where		EventTypeID = R.ObjectType and ObjectID = R.ObjectID and RoleID = @CheckObjectID
			--							group by	EventTypeID, ObjectID, RoleID
			--							) O
			--end

			if @CheckObjectType = 'ResponsibilityType' --this is different in that it stores the role that you need to check instead of the OwnershipTypeID.
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.ObjectType,
							R.ObjectID,
							COALESCE(O.Score, 0) as Score
					from	@relations R
							outer apply (
										select		COUNT(1) as Score
										from		ResponsibilityDetail
										where		ObjectType = R.ObjectType and ObjectID = R.ObjectID and ResponsibilityTypeID = @CheckObjectID
										group by	ResponsibilityTypeID, ObjectType, ObjectID
										) O
			end
		end

		-- PROPERTY VALUE CHECK
		if (@CheckType = 3)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int'),
					@PropertyName = f.value('(PropertyName/text())[1]', 'varchar(250)'),
					@Value = f.value('(Value/text())[1]', 'nvarchar(4000)')
			from	@Configuration.nodes('/fields') as F(f)

			if @PropertyName = 'Status'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.ObjectType,
							R.ObjectID,
							case 
								when O.ValueExists <> 0 then R.PossibleScore
								else 0
							end as Score
					from	@relations R
							outer apply (
										select		CASE 
														when [Status] = @Value then 1
														else 0
													END as ValueExists
										from		Artifact
										where		R.ObjectType = 'Artifact' and ID = R.ObjectID
										) O
			end
		end

		-- PROPERTY POPULATED
		if (@CheckType = 4)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int'),
					@PropertyName = f.value('(PropertyName/text())[1]', 'varchar(250)')
			from	@Configuration.nodes('/fields') as F(f)

			if @PropertyName = 'Description'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.ObjectType,
							R.ObjectID,
							case 
								when D.Description is null then 0
								when LEN(D.Description) < 25 then 0
								else R.PossibleScore
							end as Score
					from	@relations R
							left join cache.ObjectDetails D on D.[Object] = R.ObjectType and D.ObjectID = R.ObjectID
							--outer apply utility.ObjectDetail(R.ObjectType, R.ObjectID) D
			end
		end

		-- RELATIONSHIP
		if (@CheckType = 5)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.ObjectType,
						R.ObjectID,
						case 
							when O.ValueExists <> 0 then R.PossibleScore
							else 0
						end as Score
				from	@relations R
						outer apply (
									select	count(1) as ValueExists
									from	IntersectNode SN 
											inner join IntersectNode TN on TN.IntersectID = SN.IntersectID
											inner join IntersectTypeNode TNT on TNT.ID = TN.IntersectTypeNodeID and TNT.ObjectType = @CheckObjectType and TNT.ObjectID = @CheckObjectID
									where	SN.ObjectType = R.ObjectType 
											and SN.ObjectID = R.ObjectID
									) O

		end

		-- FUSION OWNERSHIP
		if (@CheckType = 6)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.ObjectType,
						R.ObjectID,
						case 
							when O.ValueExists <> 0 then R.PossibleScore
							else 0
						end as Score
				from	@relations R
						outer apply (
									select		ISNULL(RelationshipOwnerObjectID, 0) as ValueExists
									from		FusionAttributeOwnerRule
									where		RelationshipOwnerObjectType = R.ObjectType and RelationshipOwnerObjectID = R.ObjectID
									group by	RelationshipOwnerObjectType, RelationshipOwnerObjectID
									) O
		end

		-- ROLLUP VIA RELATIONSHIPS
		if (@CheckType = 7)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.ObjectType,
						R.ObjectID,
						round((T.Total/C.[Count]) * R.PossibleScore, 0) Score
				from	@relations R
						cross apply (
									select	count(1) as [Count] 
									from	cache.Relationships 
									where	SourceObject = R.ObjectType and SourceObjectID = R.ObjectID
											and TargetType = @CheckObjectType and TargetTypeID = @CheckObjectID
									) C
						outer apply (
									select	sum(dbo.GetObjectStatisticScore(TargetObject, TargetObjectID)) as Total
									from	cache.Relationships 
									where	SourceObject = R.ObjectType and SourceObjectID = R.ObjectID 
											and TargetType = @CheckObjectType and TargetTypeID = @CheckObjectID
									) T
				where C.[Count] > 0
		end

		-- ROLLUP VIA OWNERSHIP
		if (@CheckType = 8)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.ObjectType,
						R.ObjectID,
						round((T.Total/C.[Count]) * R.PossibleScore, 0) Score
				from	@relations R
						cross apply (
									select	count(1) as [Count] 
									from	cache.Responsibilities
									where	ResponsibleObject = R.ObjectType and ResponsibleObjectID = R.ObjectID
											and ObjectType = @CheckObjectType and ObjectTypeID = @CheckObjectID
									) C
						outer apply (
									select	sum(dbo.GetObjectStatisticScore([Object], ObjectID)) as Total
									from	cache.Responsibilities 
									where	ResponsibleObject = R.ObjectType and ResponsibleObjectID = R.ObjectID 
											and ObjectType = @CheckObjectType and ObjectTypeID = @CheckObjectID
									) T
				where C.[Count] > 0
		end
		

		-- EVENT METRIC CHECK
		if (@CheckType = 9)
		begin
			declare @ValidField nvarchar(250),-- = 'ValidCount',
					@InvalidField nvarchar(250),-- = 'InvalidCount',
					@Threshold decimal(9,2),-- = 0.10,
					@TotalValid float,
					@TotalInvalid float

			select	@ValidField = f.value('(ValidField/text())[1]', 'nvarchar(250)'),
					@InvalidField = f.value('(InvalidField/text())[1]', 'nvarchar(250)'),
					@Threshold = f.value('(Threshold/text())[1]', 'decimal(9,2)')
			from	@Configuration.nodes('/fields') as F(f)


			select	@TotalValid = sum(cast(V.ValidCount as int)),
					@TotalInvalid = sum(cast(I.InvalidCount as int))
			from	cache.Relationships REL
					inner join [Rule] R on R.ID = REL.TargetObjectID and REL.TargetObject = 'Rule' and R.RuleType in (3,4)
					inner join EventGroup EG on EG.RuleID = R.ID
					inner join [Event] E on E.EventGroupID = EG.ID 
					inner join (
								select	R.ID,
										max(E.Date) as [Date]
								from	cache.Relationships REL
										inner join [Rule] R on R.ID = REL.TargetObjectID and REL.TargetObject = 'Rule' and R.RuleType in (3,4)
										inner join EventGroup EG on EG.RuleID = R.ID
										inner join [Event] E on E.EventGroupID = EG.ID
								group by R.ID					
								) F on F.ID = R.ID and F.[Date] = E.[Date]
					cross apply (
								select	Value as ValidCount
								from	FieldWithRelation
								where	ObjectType = 'Event' and ObjectID = E.ID and Name = @ValidField
								) V
					cross apply (
								select	Value as InvalidCount
								from	FieldWithRelation
								where	ObjectType = 'Event' and ObjectID = E.ID and Name = @InvalidField
								) I

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.ObjectType,
						R.ObjectID,
						case 
							when cast(@TotalInvalid / @TotalValid as decimal(9,2)) < @Threshold then R.PossibleScore
							else 0
						end as Score
				from	@relations R
		end

		set @current = @current + 1
	end

	-- now merge the Statistics table
	MERGE	Statistic AS T
	USING	(
			select	S.*,
					MS.DateStart
			from	#Statistics S
					outer apply (
								select		StatisticTypeID,
											ObjectType,
											ObjectID,
											MAX(DateStart) as DateStart
								from		Statistic
								where		StatisticTypeID = S.StatisticTypeID
											and ObjectType = S.ObjectType
											and ObjectID = S.ObjectID
								group by	StatisticTypeID,
											ObjectType,
											ObjectID
								) MS
			) AS S
	ON		(
			T.StatisticTypeID = S.StatisticTypeID
			and T.ObjectType = S.ObjectType
			and T.ObjectID = S.ObjectID
			and T.DateStart = S.DateStart
			and T.Score = S.Score
			)
		WHEN MATCHED THEN 
			UPDATE SET T.DateEnd = getutcdate()
		WHEN NOT MATCHED THEN	
			INSERT	
			VALUES	(
					S.StatisticTypeID, 
					S.ObjectType, 
					S.ObjectID,
					getutcdate(), 
					getutcdate(), 
					S.Score
					);
end
GO

drop procedure [dbo].[GetRelationships]
go

--create procedure [dbo].[GetRelationships]
----declare
--	@ObjectType varchar(50),
--	@ObjectID int
----set @ObjectType = 'Artifact'
----set @ObjectID = 4651
--as
--begin
--	IF OBJECT_ID('tempdb..#Relates') IS NOT NULL
--		DROP TABLE #Relates;

--	create table #Relates (
--		IntersectID int, 
--		ObjectType varchar(50), 
--		ObjectID int, 
--		ObjectName nvarchar(1000),
--		TypeName nvarchar(250),
--		Url nvarchar(2000),
--		ConcatValue varchar(65)
--	);

--	CREATE NONCLUSTERED INDEX IX_TempRelates ON #Relates (ConcatValue ASC);

--	--Intersect loading
--	insert into #Relates
--		select	R.IntersectID,
--				R.TargetObject as ObjectType,
--				R.TargetObjectID as ObjectID,
--				coalesce(D.TextPath, R.TargetObjectName) as Name,
--				R.TargetTypeName as TypeName,
--				dbo.GenerateObjectUrl(R.TargetObject, R.TargetTypeID, R.TargetObjectID) Url,
--				R.TargetObject + cast(R.TargetObjectID as varchar(15))
--		from	cache.Relationships R
--				left join cache.ObjectDetails D on D.[Object] = R.TargetObject and D.ObjectID = R.TargetObjectID
--		where	R.SourceObject = @ObjectType
--				and R.SourceObjectID = @ObjectID
	
--	if (@ObjectType <> 'Intersect')
--	begin
--		--Source loading
--		insert into #Relates
--			select	NULL as IntersectID,
--					R.ResponsibleObjectType,
--					R.ResponsibleObjectID,
--					R.ResponsibleObjectName,
--					ROD.ObjectTypeName as TypeName,
--					ROD.Url,
--					NULL
--			from	SourcingResponsibilityDetail R
--					inner join cache.ObjectDetails ROD on ROD.[Object] = R.ResponsibleObjectType and ROD.ObjectID = R.ResponsibleObjectID --cross apply utility.ObjectDetail(R.ResponsibleObjectType, R.ResponsibleObjectID) ROD
--			where	R.ObjectType = @ObjectType 
--					and R.ObjectID = @ObjectID
--					and R.ResponsibleObjectType + cast(R.ResponsibleObjectID as varchar(15)) not in (select ObjectType + cast(ObjectID as varchar(15)) from #Relates)
--	end

--	-- Return the results to client.
--	select		IntersectID, 
--				ObjectType, 
--				ObjectID, 
--				ObjectName,
--				TypeName,
--				Url
--	from		#Relates
--	order by	TypeName,
--				ObjectName
--end

--GO



DROP VIEW [dbo].[SourcingResponsibilityDetail]
GO

alter view [cache].[Relationships]
as
	SELECT	I.[IntersectTypeID]
			,R.[IntersectID]
			,I.[Classification]
			,I.[Description]
			,R.[SourceIntersectTypeNodeID]
			,R.[SourceIntersectNodeID]
			,R.[SourceObject]
			,R.[SourceObjectID]
			,SD.[TextPath] as [SourceObjectName]
			,SD.[ObjectType] as [SourceType]
			,SD.[ObjectTypeID] as [SourceTypeID]
			,SD.ObjectTypeName as [SourceTypeName]
			,R.[TargetIntersectTypeNodeID]
			,R.[TargetIntersectNodeID]
			,R.[TargetObject]
			,R.[TargetObjectID]
			,TD.TextPath as [TargetObjectName]
			,TD.ObjectType as [TargetType]
			,TD.ObjectTypeID as [TargetTypeID]
			,TD.ObjectTypeName as [TargetTypeName]
			,substring((
						select	', ' + P.Name as [text()]
						from	IntersectMap IM
								inner join Predicate P on	P.ID = IM.PredicateID	
															and (
																(IM.SubjectIntersectNodeID = R.[SourceIntersectNodeID] and IM.ObjectIntersectNodeID = R.[TargetIntersectNodeID]) or
																(IM.SubjectIntersectNodeID = R.[TargetIntersectNodeID] and IM.ObjectIntersectNodeID = R.[SourceIntersectNodeID])
																)
						for xml path('')
						), 3, 1000) as [Role]
	FROM	cache.Relationship R
			inner join cache.ObjectDetails SD on SD.[Object] = R.[SourceObject] and SD.[ObjectID] = R.[SourceObjectID]
			inner join cache.ObjectDetails TD on TD.[Object] = R.[TargetObject] and TD.[ObjectID] = R.[TargetObjectID]
			inner join [Intersect] I on I.ID = R.IntersectID
GO

DROP TABLE [dbo].[IntersectTypeRoleRelation]
GO

DROP TABLE [dbo].[IntersectTypeRole]
GO

CREATE TABLE [dbo].[SourceRule](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](1000) NOT NULL,
	[Object] [varchar](50) NOT NULL,
	[ObjectID] [int] NOT NULL,
	[AppliesToObject] [varchar](50) NULL,
	[AppliesToObjectID] [int] NULL,
	[AppliesToObjectList] [xml] NULL,
	CONSTRAINT [PK_SourceRule] PRIMARY KEY CLUSTERED ( [ID] ASC )
)
GO

CREATE TABLE [dbo].[IntersectMapSourceRule](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[IntersectMapID] [int] NOT NULL,
	[SourceRuleID] [int] NOT NULL,
	[Description] [nvarchar](4000) NOT NULL,
	[SortOrder] [int] NOT NULL,
	CONSTRAINT [PK_IntersectMapSourceRule] PRIMARY KEY CLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [dbo].[IntersectMapSourceRule] ADD  DEFAULT ((1)) FOR [SortOrder]
GO

ALTER TABLE [dbo].[IntersectMapSourceRule]  WITH CHECK ADD  CONSTRAINT [FK_IntersectMapSourceRule_IntersectMap] FOREIGN KEY([IntersectMapID]) REFERENCES [dbo].[IntersectMap] ([ID])
GO
ALTER TABLE [dbo].[IntersectMapSourceRule] CHECK CONSTRAINT [FK_IntersectMapSourceRule_IntersectMap]
GO

ALTER TABLE [dbo].[IntersectMapSourceRule]  WITH CHECK ADD  CONSTRAINT [FK_IntersectMapSourceRule_SourceRule] FOREIGN KEY([SourceRuleID]) REFERENCES [dbo].[SourceRule] ([ID])
GO
ALTER TABLE [dbo].[IntersectMapSourceRule] CHECK CONSTRAINT [FK_IntersectMapSourceRule_SourceRule]
GO

CREATE TABLE [dbo].[IntersectMapSourceRuleContext](
	[IntersectMapSourceRuleID] [int] NOT NULL,
	[Object] [varchar](50) NOT NULL,
	[ObjectID] [int] NOT NULL,
	CONSTRAINT [PK_IntersectMapSourceRuleContext] PRIMARY KEY CLUSTERED ( [IntersectMapSourceRuleID] ASC, [Object] ASC, [ObjectID] ASC )
)
GO
ALTER TABLE [dbo].[IntersectMapSourceRuleContext]  WITH CHECK ADD  CONSTRAINT [FK_IntersectMapSourceRuleContext_IntersectMapSourceRule] FOREIGN KEY([IntersectMapSourceRuleID]) REFERENCES [dbo].[IntersectMapSourceRule] ([ID])
GO
ALTER TABLE [dbo].[IntersectMapSourceRuleContext] CHECK CONSTRAINT [FK_IntersectMapSourceRuleContext_IntersectMapSourceRule]
GO

CREATE TABLE [dbo].[SourceRuleContext](
	[SourceRuleID] [int] NOT NULL,
	[Object] [varchar](50) NOT NULL,
	[ObjectID] [int] NOT NULL,
	CONSTRAINT [PK_SourceRuleContext] PRIMARY KEY CLUSTERED ( [SourceRuleID] ASC, [Object] ASC, [ObjectID] ASC )
)
GO
ALTER TABLE [dbo].[SourceRuleContext]  WITH CHECK ADD  CONSTRAINT [FK_SourceRuleContext_SourceRule] FOREIGN KEY([SourceRuleID]) REFERENCES [dbo].[SourceRule] ([ID])
GO
ALTER TABLE [dbo].[SourceRuleContext] CHECK CONSTRAINT [FK_SourceRuleContext_SourceRule]
GO

