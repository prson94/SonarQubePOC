CREATE PROCEDURE [dbo].[GetRelatedObjectsByEventTypeWrapper]
	@ID int
AS
BEGIN
	--declare @CheckedIDs PreviouslyCheckedIDTable,
	--		@CheckedTypeIDs PreviouslyCheckedIDTable

	select	T.ObjectType,
			T.ObjectID
	from	IntersectNode S
			inner join IntersectNode T on T.IntersectID = S.IntersectID and T.ID <> S.ID and S.ObjectType = 'EventType' and S.ObjectID = @ID

	--SELECT ObjectType, ObjectID from GetRelatedObjectsByEventType('EventType', @ID, @CheckedIDs, @CheckedTypeIDs)
END
