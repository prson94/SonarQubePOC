CREATE view [dbo].[Relationship]
as
	select	I.IntersectTypeID,
			R.IntersectID,
			case I.Classification
				when 0 then 2
				else I.Classification
			end as Classification,
			I.Description,
			substring((
						select	', ' + P.Name as [text()]
						from	IntersectMap IM
								inner join [Predicate] P on	P.ID = IM.PredicateID	
															and (
																(IM.SubjectIntersectNodeID = R.[SourceIntersectNodeID] and IM.ObjectIntersectNodeID = R.[TargetIntersectNodeID]) or
																(IM.SubjectIntersectNodeID = R.[TargetIntersectNodeID] and IM.ObjectIntersectNodeID = R.[SourceIntersectNodeID])
																)
						for xml path('')
						), 3, 1000) as [Role],
			--R.[Role],
			R.SourceIntersectTypeNodeID,
			R.SourceObject as SourceObjectType,
			R.SourceObjectID,
			coalesce(S.TextPath, S.Name) as SourceName, 
			S.Parent as SourceParent,
			S.ParentID as SourceParentID,
			S.ParentName as SourceParentName,
			S.ObjectTypeID as SourceTypeID,
			S.ObjectType as SourceType,
			S.ObjectTypeName as SourceTypeName,
			S.[Url] as SourceUrl,
			R.TargetIntersectTypeNodeID,
			T.Object as TargetObjectType,
			T.ObjectID as TargetObjectID,
			coalesce(T.TextPath, T.Name) as TargetName,
			T.Parent as TargetParent,
			T.ParentID as TargetParentID,
			T.ParentName as TargetParentName,
			T.ObjectTypeID as TargetTypeID,
			T.ObjectType as TargetType,
			T.ObjectTypeName as TargetTypeName,
			T.[Url] as TargetUrl,
			TR.[Exists] as HasTechnicalRelationships
	from	cache.Relationship R
			inner join [Intersect] I on I.ID = R.IntersectID
			left join [cache].[ObjectDetails] S on S.[Object] = R.SourceObject and S.ObjectID = R.SourceObjectID
			left join [cache].[ObjectDetails] T on T.[Object] = R.TargetObject and T.ObjectID = R.TargetObjectID
			cross apply (
						select	case 
									when count(1) > 0 then cast(1 as bit) 
									else cast(0 as bit) 
								end as [Exists]
						from	cache.Relationships
						where	SourceObject = 'Intersect' and SourceObjectID = R.IntersectID
						) TR

