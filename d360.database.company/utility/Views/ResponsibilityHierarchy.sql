
CREATE view [utility].[ResponsibilityHierarchy]
as
	with 
		DLH as
		(
		select	R.ID as ResponsibilityID,
				R.ResponsibilityTypeID,
				cast('DomainType' as varchar(25)) as AssigningItemType,
				T.ID as AssigningItemID,
				cast('DomainType' as varchar(25)) as ObjectType,
				T.ID as ObjectID
		from	DomainType T 
				inner join Responsibility R on R.ObjectType = 'DomainListType' and R.ObjectID = T.ID
		union all
		select	COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
				COALESCE(R.ResponsibilityTypeID, P.ResponsibilityTypeID) as ResponsibilityTypeID,
				cast(COALESCE(R.ObjectType, P.AssigningItemType) as varchar(25)) as AssigningItemType,
				COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
				cast('DomainList' as varchar(25)) as ObjectType,
				C.ID as ObjectID
		from	Domain C
				inner join DLH P on P.ObjectType = 'DomainType' and P.ObjectID = C.DomainTypeID
				outer apply (
							select	ID,
									ResponsibilityTypeID,
									ObjectType,
									ObjectID
							from	Responsibility
							where	ResponsibilityTypeID = P.ResponsibilityTypeID
									and ObjectType = 'Domain' 
									and ObjectID = C.ID
							) R
		),
		IMTH as
		(
		select	'TaxonomyType' as AssigningItemType,
				T.ID as AssigningItemID,
				cast('TaxonomyType' as varchar(25)) as ObjectType,
				T.ID,
				R.ID as ResponsibilityID,
				R.ResponsibilityTypeID
		from	TaxonomyType T 
				inner join Responsibility R on R.ObjectType = 'TaxonomyType' and R.ObjectID = T.ID
		union all
		select	
				P.AssigningItemType,
			    P.AssigningItemID,
				cast('Taxonomy' as varchar(25)) as ObjectType,
				C.ID,
				P.ResponsibilityID,
				P.ResponsibilityTypeID
		from	Taxonomy C
				inner join IMTH P on P.ID = C.TaxonomyTypeID
		),
		IMH as
		(
		select	'Taxonomy' as AssigningItemType, 
				T.ID as AssigningItemID,
				T.ID,
				T.ParentID,
				T.TaxonomyTypeID,
				R.ID as ResponsibilityID,
				R.ResponsibilityTypeID
		from	Taxonomy T 
				inner join Responsibility R on R.ObjectType = 'Taxonomy' and R.ObjectID = T.ID
		union all
		select	
				P.AssigningItemType,
			    COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
				C.ID,
				C.ParentID,
				C.TaxonomyTypeID,
				COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
				COALESCE(R.ResponsibilityTypeID, P.ResponsibilityTypeID) as ResponsibilityTypeID
		from	Taxonomy C
				inner join IMH P on P.TaxonomyTypeID = C.TaxonomyTypeID and C.ParentID = P.ID
				outer apply (
							select	*
							from	Responsibility 
							where	ResponsibilityTypeID = P.ResponsibilityTypeID
									and ObjectType = 'Taxonomy' 
									and ObjectID = C.ID
							) R
		),
		PolicyHierarchy as
		(
		select	'Policy' as AssigningItemType, 
				P.ID as AssigningItemID,
				P.ID,
				P.ParentID,
				R.ID as ResponsibilityID,
				R.ResponsibilityTypeID
		from	Policy P 
				inner join Responsibility R on R.ObjectType = 'Policy' and R.ObjectID = P.ID --and P.ParentID is null
		union all
		select	
				P.AssigningItemType,
			    COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
				C.ID,
				C.ParentID,
				COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
				COALESCE(R.ResponsibilityTypeID, P.ResponsibilityTypeID) as ResponsibilityTypeID
		from	Policy C
				inner join PolicyHierarchy P on C.ParentID = P.ID
				outer apply (
							select	*
							from	Responsibility 
							where	ResponsibilityTypeID = P.ResponsibilityTypeID
									and ObjectType = 'Policy' 
									and ObjectID = C.ID
							) R
		),
		PolicyHierarchyForRule as
		(
		select	'Policy' as AssigningItemType, 
				P.ID as AssigningItemID,
				P.ID,
				P.ParentID,
				R.ID as ResponsibilityID,
				R.ResponsibilityTypeID
		from	Policy P 
				inner join Responsibility R on R.ObjectType = 'Policy' and R.ObjectID = P.ID
		union all
		select	
				P.AssigningItemType,
			    COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
				C.ID,
				C.ParentID,
				COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
				COALESCE(R.ResponsibilityTypeID, P.ResponsibilityTypeID) as ResponsibilityTypeID
		from	Policy C
				inner join PolicyHierarchyForRule P on C.ParentID = P.ID
				outer apply (
							select	*
							from	Responsibility 
							where	ResponsibilityTypeID = P.ResponsibilityTypeID
									and ObjectType = 'Policy' 
									and ObjectID = C.ID
							) R
		),
		--AH as	
		--(
		--select	CompanyID,
		--		'Artifact' as AssigningItemType, 
		--		T.ID as AssigningItemID,
		--		ID,
		--		ParentID,
		--		ResponsibilityID
		--from	(
		--		select	T.CompanyID,
		--				T.ID,
		--				T.ParentID,
		--				R.ID as ResponsibilityID
		--		from	Artifact T 
		--				inner join Responsibility R on R.CompanyID = T.CompanyID and R.ObjectType = 'Artifact' and R.ObjectID = T.ID
		--		) T
		--union all
		--select	
		--		C.CompanyID,
		--		P.AssigningItemType,
		--		P.AssigningItemID,
		--		C.ID,
		--		C.ParentID,
		--		P.ResponsibilityID
		--from	Artifact C
		--		inner join AH P on C.CompanyID = P.CompanyID and C.ParentID = P.ID
		--),
		RH as	
		(
		select	'Taxonomy' as AssigningItemType, 
				T.ID as AssigningItemID,
				T.ID,
				T.ParentID,
				T.TaxonomyTypeID,
				R.ID as ResponsibilityID,
				R.ResponsibilityTypeID
		from	Taxonomy T 
				inner join Responsibility R on R.ObjectType = 'Taxonomy' and R.ObjectID = T.ID
		union all
		select	
				P.AssigningItemType,
				P.AssigningItemID,
				C.ID,
				C.ParentID,
				C.TaxonomyTypeID,
				P.ResponsibilityID,
				P.ResponsibilityTypeID
		from	Taxonomy C
				inner join RH P on P.TaxonomyTypeID = C.TaxonomyTypeID and C.ParentID = P.ID
		)


	select	P.ResponsibilityID,
			R.ResponsibilityTypeID,
			P.AssigningItemType,
			P.AssigningItemID,
			P.ObjectType,
			P.ObjectID,
			R.ResponsibleObjectType,
			R.ResponsibleObjectID
	from	(
			select	AssigningItemType,
					AssigningItemID,
					ResponsibilityID,
					'Policy' as ObjectType,
					ID as ObjectID
			from	PolicyHierarchy
			union
			select	'Rule' as AssigningItemType,
					RU.ID as AssigningItemID,
					R.ID as ResponsibilityID,
					'Rule' as ObjectType,
					RU.ID as ObjectID
			from	[Rule] RU 
					inner join Responsibility R on R.ObjectType = 'Rule' and R.ObjectID = RU.ID
			--union
			--select	AssigningItemType,
			--		AssigningItemID,
			--		ResponsibilityID,
			--		'Rule' as ObjectType,
			--		R.ID as ObjectID
			--from	PolicyHierarchyForRule P
			--		inner join [Rule] R on R.PolicyID = P.ID
			union
			select	AssigningItemType,
					AssigningItemID,
					ResponsibilityID,
					ObjectType,
					ObjectID
			from	DLH 
			union
			select	AssigningItemType,
					AssigningItemID,
					ResponsibilityID,
					ObjectType,
					ID as ObjectID
			from	IMTH
			union 
			select	AssigningItemType,
					AssigningItemID,
					ResponsibilityID,
					'Taxonomy' as ObjectType,
					ID as ObjectID
			from	IMH
			union
			select	'ArtifactType' as AssigningItemType,
					T.ID as AssigningItemID,
					R.ID as ResponsibilityID,
					'ArtifactType' as ObjectType,
					T.ID as ObjectID
			from	ArtifactType T 
					inner join Responsibility R on R.ObjectType = 'ArtifactType' and R.ObjectID = T.ID
			union
			select	'ArtifactType' as AssigningItemType,
					T.ID as AssigningItemID,
					R.ID as ResponsibilityID,
					'Artifact' as ObjectType,
					A.ID as ObjectID
			from	ArtifactType T 
					inner join Responsibility R on R.ObjectType = 'ArtifactType' and R.ObjectID = T.ID
					inner join Artifact A on A.ArtifactTypeID = T.ID
			) P
			inner join Responsibility R on R.ID = P.ResponsibilityID
			inner join ResponsibilityType RT on RT.ID = R.ResponsibilityTypeID and RT.ResponsibilityTypeGroup = 1
