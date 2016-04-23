
CREATE FUNCTION [utility].[GetHierarchyAssignedResponsibilityList]
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
							inner join ResponsibilityTypeRelation RTR on RTR.ResponsibilityTypeID = P.ResponsibilityTypeID and RTR.ObjectType = 'ArtifactType' and RTR.ObjectID = A.ArtifactTypeID
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