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
						ID as IntersectID,
						Subject,
						SubjectID,
						Object,
						ObjectID
				from	[Intersect]
				union
				select	distinct
						ID as IntersectID,
						Object as Subject,
						ObjectID as SubjectID,
						Subject as Object,
						SubjectID as ObjectID
				from	[Intersect]
			  ) as S
		on    (T.IntersectID = S.IntersectID and T.SourceObject = S.Subject and T.SourceObjectID = S.SubjectID)
		when not matched then
			insert (IntersectID, SourceObject, SourceObjectID, TargetObject, TargetObjectID)
			values (S.IntersectID, S.Subject, S.SubjectID, S.Object, S.ObjectID);
	end
	else
	begin
		--REFRESH SINGLE INTERSECT ENTRIES (2)
		merge cache.Relationship as T
		using (
				select	distinct
						I.ID as IntersectID,
						I.Subject,
						I.SubjectID,
						I.Object,
						I.ObjectID
				from	[Intersect] I
						inner join @Intersects C on C.ObjectID = I.ID
				union
				select	distinct
						I.ID as IntersectID,
						I.Object as Subject,
						I.ObjectID as SubjectID,
						I.Subject as Object,
						I.SubjectID as ObjectID
				from	[Intersect] I
						inner join @Intersects C on C.ObjectID = I.ID
			  ) as S
		on    (T.IntersectID = S.IntersectID and T.SourceObject = S.Subject and T.SourceObjectID = S.SubjectID)
		when not matched then
			insert (IntersectID, SourceObject, SourceObjectID, TargetObject, TargetObjectID)
			values (S.IntersectID, S.Subject, S.SubjectID, S.Object, S.ObjectID);
	end
end
GO

