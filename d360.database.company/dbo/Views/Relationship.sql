
CREATE view [dbo].[Relationship]
as
	select	R.IntersectTypeID,
			R.IntersectID,
			case R.Classification
				when 0 then 2
				else R.Classification
			end as Classification,
			R.Description,
			R.[Role],
			R.SourceIntersectTypeNodeID,
			R.SourceObject as SourceObjectType,
			R.SourceObjectID,
			coalesce(S.TextPath, R.SourceObjectName) as SourceName, 
			S.Parent as SourceParent,
			S.ParentID as SourceParentID,
			S.ParentName as SourceParentName,
			R.SourceTypeID,
			R.SourceType,
			R.SourceTypeName,
			dbo.GenerateObjectUrl(R.SourceObject, R.SourceTypeID, R.SourceObjectID) as SourceUrl,
			R.TargetIntersectTypeNodeID,
			R.TargetObject as TargetObjectType,
			R.TargetObjectID,
			coalesce(T.TextPath, R.TargetObjectName) as TargetName,
			T.Parent as TargetParent,
			T.ParentID as TargetParentID,
			T.ParentName as TargetParentName,
			R.TargetTypeID,
			R.TargetType,
			R.TargetTypeName,
			dbo.GenerateObjectUrl(R.TargetObject, R.TargetTypeID, R.TargetObjectID) as TargetUrl,
			TR.[Exists] as HasTechnicalRelationships
	from	cache.Relationships R
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

