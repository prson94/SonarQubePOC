CREATE procedure [cache].[SynchronizeRelationships]
	@Intersects IDTable READONLY
as
begin
	declare @count int
	select @count = count(1) from @Intersects

	if @count = 0
	begin
		--REFRESH ENTIRE TABLE
		merge cache.Relationship as T
		using (
				select	distinct
						S.IntersectID,
						S.IntersectTypeNodeID as SourceIntersectTypeNodeID, 
						S.ID as SourceIntersectNodeID,
						S.ObjectType as SourceObject,
						S.ObjectID as SourceObjectID,
						T.IntersectTypeNodeID as TargetIntersectTypeNodeID,
						T.ID as TargetIntersectNodeID,
						T.ObjectType as TargetObject,
						T.ObjectID as TargetObjectID
				from	dbo.IntersectNode S
						inner join dbo.IntersectNode T on T.IntersectID = S.IntersectID and T.ID <> S.ID
			  ) as S (
					IntersectID, 
					SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, 
					TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID
					)
		on    (T.IntersectID = S.IntersectID and T.SourceObject = S.SourceObject and T.SourceObjectID = S.SourceObjectID)
		when not matched then
			insert (
					IntersectID, SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID
					)
			values (
					S.IntersectID, S.SourceIntersectTypeNodeID, S.SourceIntersectNodeID, S.SourceObject, S.SourceObjectID, S.TargetIntersectTypeNodeID, S.TargetIntersectNodeID, S.TargetObject, S.TargetObjectID
					);
	end
	else
	begin
		--REFRESH SINGLE INTERSECT ENTRIES (2)
		merge cache.Relationship as T
		using (
				select	distinct
						S.IntersectID,
						S.IntersectTypeNodeID as SourceIntersectTypeNodeID, 
						S.ID as SourceIntersectNodeID,
						S.ObjectType as SourceObject,
						S.ObjectID as SourceObjectID,
						T.IntersectTypeNodeID as TargetIntersectTypeNodeID,
						T.ID as TargetIntersectNodeID,
						T.ObjectType as TargetObject,
						T.ObjectID as TargetObjectID
				from	dbo.IntersectNode S
						inner join dbo.IntersectNode T on T.IntersectID = S.IntersectID and T.ID <> S.ID
						inner join @Intersects C on C.ObjectID = S.IntersectID
			  ) as S (
					IntersectID, 
					SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, 
					TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID
					)
		on    (T.IntersectID = S.IntersectID and T.SourceObject = S.SourceObject and T.SourceObjectID = S.SourceObjectID)
		when not matched then
			insert (
					IntersectID, 
					SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, 
					TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID
					)
			values (
					S.IntersectID, 
					S.SourceIntersectTypeNodeID, S.SourceIntersectNodeID, S.SourceObject, S.SourceObjectID, 
					S.TargetIntersectTypeNodeID, S.TargetIntersectNodeID, S.TargetObject, S.TargetObjectID
					);
	end
end
