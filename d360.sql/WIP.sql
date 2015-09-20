alter view SecurityDetail
as
	select	RD.CompanyID,
			case 
				when RG.ResourceID is not null then 'Resource'
				else RD.ResponsibleObjectType
			end as ResponsibleObjectType,
			COALESCE(RG.ResourceID, RD.ResponsibleObjectID) as ResponsibleObjectID,
			--COALESCE(CR.FirstName + ' ' + CR.LastName, RD.ResponsibleObjectName) as ResponsibleObjectName,
			RD.ObjectType,
			RD.ObjectID,
			RD.ObjectName,
			RTC.DependentObjectType,
			C.ID as ClaimID,
			C.Name as Claim
	from	ResponsibilityDetail RD
			inner join Responsibility R on R.CompanyID = Rd.CompanyID and R.ID = RD.ResponsibilityID
			inner join ResponsibilityType RT on RT.CompanyID = R.CompanyID and RT.ID = R.ResponsibilityTypeID
			inner join ResponsibilityTypeClaim RTC on RTC.CompanyID = RT.CompanyID and RTC.ResponsibilityTypeID = RT.ID
			left join [Group] G on G.CompanyID = RD.CompanyID and RD.ResponsibleObjectType = 'Group' and G.ID = RD.ResponsibleObjectID
			left join ResourceGroup RG on RG.CompanyID = G.CompanyID and RG.GroupID = G.ID
			--left join CompanyResource CR on CR.CompanyID = RG.CompanyID and CR.ResourceID = RG.ResourceID
			inner join Claim C on C.ID = RTC.ClaimID
--	where	RD.CompanyID = 4

select * from SecurityDetail where CompanyID = 9 and ObjectType = 'Artifact' and ObjectID = 16


ALTER view [ResponsibilityDetail]
as
	with 
		DLH as
		(
		select	T.CompanyID,
				R.ID as ResponsibilityID,
				R.ResponsibilityTypeID,
				cast('DomainListType' as varchar(25)) as AssigningItemType,
				T.ID as AssigningItemID,
				cast('DomainListType' as varchar(25)) as ObjectType,
				T.ID as ObjectID
		from	DomainListType T 
				inner join Responsibility R on R.CompanyID = T.CompanyID and R.ObjectType = 'DomainListType' and R.ObjectID = T.ID
		union all
		select	C.CompanyID,
				COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
				COALESCE(R.ResponsibilityTypeID, P.ResponsibilityTypeID) as ResponsibilityTypeID,
				cast(COALESCE(R.ObjectType, P.AssigningItemType) as varchar(25)) as AssigningItemType,
				COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
				cast('DomainList' as varchar(25)) as ObjectType,
				C.ID as ObjectID
		from	DomainList C
				inner join DLH P on P.CompanyID = C.CompanyID and P.ObjectType = 'DomainListType' and P.ObjectID = C.DomainListTypeID
				outer apply (
							select	ID,
									ResponsibilityTypeID,
									ObjectType,
									ObjectID
							from	Responsibility
							where	CompanyID = C.CompanyID 
									and ResponsibilityTypeID = P.ResponsibilityTypeID
									and ObjectType = 'DomainList' 
									and ObjectID = C.ID
							) R
		),
		IMTH as
		(
		select	CompanyID,
				'TaxonomyType' as AssigningItemType, 
				T.ID as AssigningItemID,
				cast('TaxonomyType' as varchar(25)) as ObjectType,
				ID,
				ResponsibilityID,
				ResponsibilityTypeID
		from	(
				select	T.CompanyID,
						T.ID,
						R.ID as ResponsibilityID,
						R.ResponsibilityTypeID
				from	TaxonomyType T 
						inner join Responsibility R on R.CompanyID = T.CompanyID and R.ObjectType = 'TaxonomyType' and R.ObjectID = T.ID
				) T
		union all
		select	
				C.CompanyID,
				P.AssigningItemType,
			    P.AssigningItemID,
				cast('Taxonomy' as varchar(25)) as ObjectType,
				C.ID,
				P.ResponsibilityID,
				P.ResponsibilityTypeID
		from	Taxonomy C
				inner join IMTH P on C.CompanyID = P.CompanyID and P.ID = C.TaxonomyTypeID
		),
		IMH as
		(
		select	CompanyID,
				'Taxonomy' as AssigningItemType, 
				T.ID as AssigningItemID,
				ID,
				ParentID,
				TaxonomyTypeID,
				ResponsibilityID,
				ResponsibilityTypeID
		from	(
				select	T.CompanyID,
						T.ID,
						T.ParentID,
						T.TaxonomyTypeID,
						R.ID as ResponsibilityID,
						R.ResponsibilityTypeID
				from	Taxonomy T 
						inner join Responsibility R on R.CompanyID = T.CompanyID and R.ObjectType = 'Taxonomy' and R.ObjectID = T.ID
				) T
		union all
		select	
				C.CompanyID,
				P.AssigningItemType,
			    COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
				C.ID,
				C.ParentID,
				C.TaxonomyTypeID,
				COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
				COALESCE(R.ResponsibilityTypeID, P.ResponsibilityTypeID) as ResponsibilityTypeID
		from	Taxonomy C
				inner join IMH P on C.CompanyID = P.CompanyID and P.TaxonomyTypeID = C.TaxonomyTypeID and C.ParentID = P.ID
				outer apply (
							select	*
							from	Responsibility 
							where	CompanyID = C.CompanyID 
									and ResponsibilityTypeID = P.ResponsibilityTypeID
									and ObjectType = 'Taxonomy' 
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
		select	CompanyID,
				'Taxonomy' as AssigningItemType, 
				T.ID as AssigningItemID,
				ID,
				ParentID,
				TaxonomyTypeID,
				ResponsibilityID,
				ResponsibilityTypeID
		from	(
				select	T.CompanyID,
						T.ID,
						T.ParentID,
						T.TaxonomyTypeID,
						R.ID as ResponsibilityID,
						R.ResponsibilityTypeID
				from	Taxonomy T 
						inner join Responsibility R on R.CompanyID = T.CompanyID and R.ObjectType = 'Taxonomy' and R.ObjectID = T.ID
				) T
		union all
		select	
				C.CompanyID,
				P.AssigningItemType,
				P.AssigningItemID,
				C.ID,
				C.ParentID,
				C.TaxonomyTypeID,
				P.ResponsibilityID,
				P.ResponsibilityTypeID
		from	Taxonomy C
				inner join RH P on C.CompanyID = P.CompanyID and P.TaxonomyTypeID = C.TaxonomyTypeID and C.ParentID = P.ID
		)


	select	P.CompanyID,
			P.ResponsibilityID,
			P.AssigningItemType,
			P.AssigningItemID,
			AID.Name as AssigningItemName,
			AID.Url as AssigningItemUrl,
			P.ObjectType,
			P.ObjectID,
			OD.Name as ObjectName,
			OD.Url as ObjectUrl,
			R.ResponsibleObjectType,
			R.ResponsibleObjectID,
			ROD.Name as ResponsibleObjectName,
			ROD.Url as ResponsibleObjectUrl,
			RT.Name as [Role]
	from	(
			select		P.CompanyID,
						P.AssigningItemType,
						P.AssigningItemID,
						P.ResponsibilityID,
						REL.ObjectType, 
						REL.ObjectID
			from		RH P
						cross apply (
									select	N1.ObjectID as TaxonomyID,
											N2.ObjectType,
											N2.ObjectID
									from	IntersectNode N1
											inner join [Intersect] I	on N1.CompanyID = P.CompanyID 
																		and I.CompanyID = N1.CompanyID 
																		and I.ID = N1.IntersectID 
																		and N1.ObjectType = 'Taxonomy' 
																		and N1.ObjectID = P.ID
											inner join IntersectNode N2 on N2.CompanyID = I.CompanyID 
																		and N2.IntersectID = I.ID 
																		and N2.ID <> N1.ID
																		and (
																				(
																				N2.ObjectType = 'Artifact'
																				and N2.ObjectID in (
																									select	ArtifactID 
																									from	ArtifactResponsibility 
																									where	CompanyID = I.CompanyID
																											and ResponsibilityTypeID = P.ResponsibilityTypeID
																											and TaxonomyTypeID = P.TaxonomyTypeID
																								   )
																				)
																				OR
																				N2.ObjectType <> 'Artifact'
																			)
									) REL
			union
			select	CompanyID,
					AssigningItemType,
					AssigningItemID,
					ResponsibilityID,
					ObjectType,
					ObjectID
			from	DLH 
			union
			select	CompanyID,
					AssigningItemType,
					AssigningItemID,
					ResponsibilityID,
					ObjectType,
					ID as ObjectID
			from	IMTH
			union 
			select	CompanyID,
					AssigningItemType,
					AssigningItemID,
					ResponsibilityID,
					'Taxonomy' as ObjectType,
					ID as ObjectID
			from	IMH
			union
			select	T.CompanyID,
					'Artifact' as AssigningItemType,
					T.ID as AssigningItemID,
					R.ID as ResponsibilityID,
					'Artifact' as ObjectType,
					T.ID as ObjectID
			from	Artifact T 
					inner join Responsibility R on R.CompanyID = T.CompanyID and R.ObjectType = 'Artifact' and R.ObjectID = T.ID
					inner join ArtifactResponsibility AR on AR.CompanyID = R.CompanyID and AR.ArtifactID = T.ID and AR.ResponsibilityTypeID = R.ResponsibilityTypeID and AR.TaxonomyTypeID is null
			--union 
			--select	P.CompanyID,
			--		AssigningItemType,
			--		AssigningItemID,
			--		ResponsibilityID,
			--		'Artifact' as ObjectType,
			--		P.ID as ObjectID
			--from	ArtifactHierarchy P
			union
			select	T.CompanyID,
					'ArtifactType' as AssigningItemType,
					T.ID as AssigningItemID,
					R.ID as ResponsibilityID,
					'ArtifactType' as ObjectType,
					T.ID as ObjectID
			from	ArtifactType T 
					inner join Responsibility R on R.CompanyID = T.CompanyID and R.ObjectType = 'ArtifactType' and R.ObjectID = T.ID
			union
			select	T.CompanyID,
					'ArtifactType' as AssigningItemType,
					T.ID as AssigningItemID,
					R.ID as ResponsibilityID,
					'Artifact' as ObjectType,
					A.ID as ObjectID
			from	ArtifactType T 
					inner join Responsibility R on R.CompanyID = T.CompanyID and R.ObjectType = 'ArtifactType' and R.ObjectID = T.ID
					inner join Artifact A on A.CompanyID = T.CompanyID and A.ArtifactTypeID = T.ID
			) P
			inner join Responsibility R on R.CompanyID = P.CompanyID and R.ID = P.ResponsibilityID
			inner join ResponsibilityType RT on RT.CompanyID = R.CompanyID and RT.ID = R.ResponsibilityTypeID
			cross apply utility.ObjectDetail(P.CompanyID, R.ResponsibleObjectType, R.ResponsibleObjectID) ROD
			cross apply utility.ObjectDetail(P.CompanyID, P.AssigningItemType, P.AssigningItemID) AID
			cross apply utility.ObjectDetail(P.CompanyID, P.ObjectType, P.ObjectID) OD

			--select * from [ResponsibilityDetail]