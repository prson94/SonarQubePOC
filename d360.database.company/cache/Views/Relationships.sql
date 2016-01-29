CREATE view [cache].[Relationships]
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