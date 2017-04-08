CREATE view [cache].[Relationships]
as
	SELECT	IntersectTypeID,
			ID as IntersectID,
			0 as SourceIntersectTypeNodeID,
			0 as SourceIntersectNodeID,
			Subject as SourceObject,
			SubjectID as SourceObjectID,
			SubjectName as SourceObjectName,
			SubjectType as SourceType,
			SubjectTypeID as SourceTypeID,
			SubjectTypeName as SourceTypeName,
			0 as TargetIntersectTypeNodeID,
			0 as TargetIntersectNodeID,
			Object as TargetObject,
			ObjectID as TargetObjectID,
			ObjectName as TargetObjectName,
			ObjectType as TargetType,
			ObjectTypeID as TargetTypeID,
			ObjectTypeName as TargetTypeName,
			'' as [Role]
	FROM	[IntersectDetail]
union
	SELECT	IntersectTypeID,
			ID as IntersectID,
			0 as SourceIntersectTypeNodeID,
			0 as SourceIntersectNodeID,
			Object as SourceObject,
			ObjectID as SourceObjectID,
			ObjectName as SourceObjectName,
			ObjectType as SourceType,
			ObjectTypeID as SourceTypeID,
			ObjectTypeName as SourceTypeName,
			0 as TargetIntersectTypeNodeID,
			0 as TargetIntersectNodeID,
			Subject as TargetObject,
			SubjectID as TargetObjectID,
			SubjectName as TargetObjectName,
			SubjectType as TargetType,
			SubjectTypeID as TargetTypeID,
			SubjectTypeName as TargetTypeName,
			'' as [Role]
	FROM	[IntersectDetail]
GO

