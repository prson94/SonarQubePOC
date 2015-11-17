CREATE procedure [dbo].[GetEnvironmentDetailsDiagramData]
--declare
	@ObjectType varchar(50),
	@ObjectID int
--set @ObjectType = 'Artifact'
--set @ObjectID = 11808
as
begin
	declare @tbl table (ID int, ParentID int, RtID int, ParentRtID int, TargetResponsibilityID int, ResponsibleObjectType varchar(50), ResponsibleObjectID int, AssigningItemType varchar(50), AssigningItemID int, [Role] nvarchar(250))

	insert into @tbl
		select	--distinct
				ResponsibilityID,
				coalesce(TargetResponsibilityID, 0),
				ResponsibilityTypeID,
				NULL,
				TargetResponsibilityID,
				ResponsibleObject as ResponsibleObjectType,
				ResponsibleObjectID,
				AssigningItem as AssigningItemType, 
				AssigningItemID,
				ResponsibilityType as [Role]
		from	cache.Responsibilities S--SourcingResponsibilityDetail S
		where	S.[Object] = @ObjectType and S.ObjectID = @ObjectID and S.[ResponsibilityTypeGroup] = 2

	update	T
	set		T.ParentRtID = h.ParentID
	from	@tbl T
			INNER JOIN ResponsibilityTypeHierarchy h on h.ID = T.RtID

	update	T
	set		ParentID = P.ID
	from	@tbl T
			inner join @tbl P on T.ParentRtID = P.RtID and T.ParentID = 0

	select	0 as ID,
			NULL as ParentID,
			null as AssigningItemType, 
			null as AssigningItemID,
			@ObjectType as ObjectType,
			@ObjectID as ObjectID,
			Name,
			ObjectTypeName as [Type],
			IconBackColor as BackColor,
			IconForeColor as ForeColor,
			Url,
			NULL TechnicalRelationships,
			NULL as Contexts,
			NULL as Transformations,
			'' as [Role]
	from	cache.ObjectDetails 
	where	[Object] = @ObjectType and ObjectID = @ObjectID --utility.ObjectDetail(@ObjectType, @ObjectID)
	union
	select	R.ID, 
			R.ParentID, 
			R.AssigningItemType, 
			R.AssigningItemID,
			R.ResponsibleObjectType as ObjectType,
			R.ResponsibleObjectID as ObjectID,
			D.Name,
			D.ObjectTypeName as [Type],
			D.IconBackColor as BackColor,
			D.IconForeColor as ForeColor,
			D.Url,
			T.TechnicalRelationships,
			C.Contexts,
			X.Transformations,
			R.[Role]
	from	@tbl R
			inner join cache.ObjectDetails D on D.[Object] = R.ResponsibleObjectType and D.ObjectID = R.ResponsibleObjectID--cross apply utility.ObjectDetail(R.ResponsibleObjectType, R.ResponsibleObjectID) D
			outer apply (
						select (
								select	TN.ObjectType as "@type",
										TN.ObjectID as "@id",
										FT.Name as "@attribute",
										coalesce(F.Name, '') "@fusion",
										FA.TextPath as "@name",
										'#/fusion/' + CAST(FT.FusionTypeID as varchar(15)) + '/' + + CAST(FA.FusionID as varchar(15)) as "@url"
								from	IntersectNode SN
										inner join IntersectNode TN on 
																	TN.IntersectID = SN.IntersectID and TN.ID <> SN.ID
																	and SN.ObjectType = R.ResponsibleObjectType and SN.ObjectID = R.ResponsibleObjectID 
																	and TN.ObjectType = @ObjectType and TN.ObjectID = @ObjectID
										inner join IntersectNode SFN on 
																	SFN.ObjectType = 'Intersect' and SFN.ObjectID = TN.IntersectID
										inner join IntersectNode TFN on 
																	TFN.IntersectID = SFN.IntersectID and TFN.ID <> SFN.ID
																	and SFN.ObjectType = 'Intersect' and SFN.ObjectID = TN.IntersectID
																	and TFN.ObjectType = 'FusionAttribute'
										inner join FusionAttribute FA on FA.ID = TFN.ObjectID
										inner join Fusion F on F.ID = FA.FusionID
										inner join FusionAttributeType FT on FT.ID = FA.FusionAttributeTypeID
								for xml path('relationship'), root('relationships')
							) as TechnicalRelationships
						) T
			outer apply (
						select (
								select	case ResponsibilityTransformationType
											when 1 then 'Business Transformation'
											else 'Technical Transformation'
										end as "@type",
										ID as "@id",
										Description as "description"
								from	ResponsibilityTransformation
								where	ResponsibilityID = R.ID
								for xml path('transformation'), root('transformations')
							) as Transformations
						) X
			outer apply (
						select (
								select	LT.Name as "@lookup",
										L.Name as "@name",
										L.Code as "@code"
								from	ResponsibilityContextItem RCI
										inner join DomainItem L on RCI.ObjectType = 'DomainItem' and L.ID = RCI.ObjectID and RCI.ResponsibilityID = R.ID
										inner join Domain LT on LT.ID = L.DomainID
								for xml path('context'), root('contexts')
							) as Contexts
						) C
end
