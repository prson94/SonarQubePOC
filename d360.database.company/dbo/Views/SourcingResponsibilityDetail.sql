
CREATE view [dbo].[SourcingResponsibilityDetail]
as
	with 
		T as	-- Gets taxonomy heirarchy to show responsibilies on each node properly.
		(
		select	'Taxonomy' as AssigningItemType, 
				T.ID as AssigningItemID,
				ID,
				ParentID,
				TaxonomyTypeID,
				ResponsibilityID,
				ResponsibilityTypeID
		from	(
				select	T.ID,
						T.ParentID,
						T.TaxonomyTypeID,
						R.ID as ResponsibilityID,
						R.ResponsibilityTypeID
				from	Taxonomy T 
						inner join Responsibility R on R.ObjectType = 'Taxonomy' and R.ObjectID = T.ID
				) T
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
				inner join T P on P.TaxonomyTypeID = C.TaxonomyTypeID and C.ParentID = P.ID
				outer apply (
							select	*
							from	Responsibility 
							where	ResponsibilityTypeID = P.ResponsibilityTypeID
									and ObjectType = 'Taxonomy' 
									and ObjectID = C.ID
							) R
		),
		T_A as	-- Gets taxonomy heirarchy to show responsibilies on each related artifact properly
		(
		select	'Taxonomy' as AssigningItemType, 
				T.ID as AssigningItemID,
				ID,
				ParentID,
				TaxonomyTypeID,
				ResponsibilityID,
				ResponsibilityTypeID
		from	(
				select	T.ID,
						T.ParentID,
						T.TaxonomyTypeID,
						R.ID as ResponsibilityID,
						R.ResponsibilityTypeID
				from	Taxonomy T 
						inner join Responsibility R on R.ObjectType = 'Taxonomy' and R.ObjectID = T.ID
				) T
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
				inner join T_A P on P.TaxonomyTypeID = C.TaxonomyTypeID and C.ParentID = P.ID
		),
		T_I as	-- Gets taxonomy heirarchy to show responsibilies on each related intersect (via related artifacts) properly
		(
		select	'Taxonomy' as AssigningItemType, 
				T.ID as AssigningItemID,
				ID,
				ParentID,
				TaxonomyTypeID,
				ResponsibilityID,
				ResponsibilityTypeID
		from	(
				select	T.ID,
						T.ParentID,
						T.TaxonomyTypeID,
						R.ID as ResponsibilityID,
						R.ResponsibilityTypeID
				from	Taxonomy T 
						inner join Responsibility R on R.ObjectType = 'Taxonomy' and R.ObjectID = T.ID
				) T
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
				inner join T_A P on P.TaxonomyTypeID = C.TaxonomyTypeID and C.ParentID = P.ID
		)

	select	P.ResponsibilityID,
			R.ResponsibilityTypeID,
			P.AssigningItemType,
			P.AssigningItemID,
			AID.Name as AssigningItemName,
			AID.Url as AssigningItemUrl,
			AID.ObjectTypeName as AssigningTypeName,
			AID.IconBackColor as AssigningIconBackColor,
			AID.IconForeColor as AssigningIconForeColor,
			AID.IconText as AssigningIconText,
			P.ObjectType,
			P.ObjectID,
			OD.Name as ObjectName,
			OD.ObjectTypeName,
			OD.Url as ObjectUrl,
			R.ResponsibleObjectType,
			R.ResponsibleObjectID,
			ROD.Name as ResponsibleObjectName,
			ROD.Url as ResponsibleObjectUrl,
			ROD.IconBackColor as ResponsibleObjectIconBackColor,
			ROD.IconForeColor as ResponsibleObjectIconForeColor,
			ROD.IconText as ResponsibleObjectIconText,
			RT.Name as [Role],
			dbo.GetObjectStatisticScore(P.ObjectType, P.ObjectID) as CurrentScore,
			CI.ContextItems,
			P.Actual
	from	(

			-- Responsibilities on the artifact.
			--select		P.AssigningItemType,
			--			P.AssigningItemID,
			--			P.ResponsibilityID,
			--			REL.ObjectType, 
			--			REL.ObjectID,
			--			cast(0 as bit) as Actual
			--from		T_A P
			--			cross apply (
			--						select	SourceObjectID as TaxonomyID,
			--								TargetObject as ObjectType,
			--								TargetObjectID as ObjectID
			--						from	cache.Relationships
			--						where	SourceObject = 'Taxonomy'
			--								and SourceObjectID = P.ID
			--								and (
			--										(
			--										TargetObject = 'Artifact'
			--										and TargetObjectID in	(
			--																select	ArtifactID 
			--																from	ArtifactResponsibility 
			--																where	ResponsibilityTypeID = P.ResponsibilityTypeID
			--																		and TaxonomyTypeID = P.TaxonomyTypeID
			--																)
			--										)
			--										OR
			--										TargetObject <> 'Artifact'
			--									)
			--						) REL

			-- Responsibilities on the intersect (via artifact).
			--union 
			--select		P.AssigningItemType,
			--			P.AssigningItemID,
			--			P.ResponsibilityID,
			--			RELI.ObjectType, 
			--			RELI.ObjectID,
			--			cast(0 as bit) as Actual
			--from		T_I P
			--			cross apply (
			--						select	SourceObjectID as TaxonomyID,
			--								TargetObject as ObjectType,
			--								TargetObjectID as ObjectID
			--						from	cache.Relationships
			--						where	SourceObject = 'Taxonomy'
			--								and SourceObjectID = P.ID
			--								and (
			--										(
			--										TargetObject = 'Artifact'
			--										and TargetObjectID in	(
			--																select	ArtifactID 
			--																from	ArtifactResponsibility 
			--																where	ResponsibilityTypeID = P.ResponsibilityTypeID
			--																		and TaxonomyTypeID = P.TaxonomyTypeID
			--																)
			--										)
			--										OR
			--										TargetObject <> 'Artifact'
			--									)
			--						) RELA
			--			cross apply (
			--						select	'Intersect' as ObjectType,
			--								IntersectID as ObjectID
			--						from	cache.Relationships
			--						where	SourceObject = RELA.ObjectType
			--								and SourceObjectID = RELA.ObjectID
			--								and TargetObject <> 'Taxonomy'
			--						) RELI
												
			-- Responsibilities directly assigned to taxonomy
			--union 
			select	AssigningItemType,
					AssigningItemID,
					ResponsibilityID,
					'Taxonomy' as ObjectType,
					ID as ObjectID,
					cast(1 as bit) as Actual
			from	T

			-- Responsibilities directly assigned to artifact
			union
			select	'Artifact' as AssigningItemType,
					T.ID as AssigningItemID,
					R.ID as ResponsibilityID,
					'Artifact' as ObjectType,
					T.ID as ObjectID,
					cast(0 as bit) as Actual
			from	Artifact T 
					inner join Responsibility R on R.ObjectType = 'Artifact' and R.ObjectID = T.ID

			-- Responsibilities directly assigned to intersect
			union
			select	'Intersect' as AssigningItemType,
					T.ID as AssigningItemID,
					R.ID as ResponsibilityID,
					'Intersect' as ObjectType,
					T.ID as ObjectID,
					cast(1 as bit) as Actual
			from	[Intersect] T 
					inner join Responsibility R on R.ObjectType = 'Intersect' and R.ObjectID = T.ID
			) P
			inner join Responsibility R on R.ID = P.ResponsibilityID
			inner join ResponsibilityType RT on RT.ID = R.ResponsibilityTypeID and RT.ResponsibilityTypeGroup <> 1
			left join cache.ObjectDetails ROD on ROD.[Object] = R.ResponsibleObjectType and ROD.ObjectID = R.ResponsibleObjectID
			inner join cache.ObjectDetails AID on AID.[Object] = P.AssigningItemType and AID.ObjectID = P.AssigningItemID
			inner join cache.ObjectDetails OD on OD.[Object] = P.ObjectType and OD.ObjectID = P.ObjectID
			outer apply (
						select (
								select	D.Name + ': ' + I.Code + ' - ' + I.Name + '; '
								from	ResponsibilityContextItem C
										inner join DomainItem I on C.ObjectType = 'DomainItem' and C.ObjectID = I.ID
										inner join Domain D on D.ID = I.DomainID
								where	ResponsibilityID = P.ResponsibilityID
								for xml path ('')--, root('items')
								) as ContextItems
						) CI
