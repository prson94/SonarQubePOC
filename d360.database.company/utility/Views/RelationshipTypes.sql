CREATE VIEW [utility].[RelationshipTypes]
AS
	select	S.IntersectTypeID,
			S.ID as SourceIntersectTypeNodeID,
			S.ObjectType as SourceObjectType,
			S.ObjectID as SourceObjectID,
			S.MenuDisplayText as SourceMenuDisplayText,
			T.ID as TargetIntersectTypeNodeID,
			T.ObjectType as TargetObjectType,
			T.ObjectID as TargetObjectID,
			T.MenuDisplayText as TargetMenuDisplayText
	from	IntersectTypeNode S
			inner join IntersectTypeNode T on T.IntersectTypeID = S.IntersectTypeID and T.ID <> S.ID
			inner join IntersectType IT on IT.ID = S.IntersectTypeID
