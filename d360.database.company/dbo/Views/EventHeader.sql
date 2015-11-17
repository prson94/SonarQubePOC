
--CREATE FUNCTION [dbo].[GetEventTypesByObject]
--(
--	@ObjectType varchar(25),
--	@ObjectID int,
--	@CheckedIDs PreviouslyCheckedIntersectTypeIDTable readonly--,
--	--@CollectedEventTypeIDs PreviouslyCheckedIntersectTypeIDTable readonly
--)
--RETURNS 
--@EventTypes TABLE 
--(
--	ID int
--)
--AS
--BEGIN
--	declare @checked PreviouslyCheckedIntersectTypeIDTable
--	declare @search table (ID int identity, IntersectTypeID int, ObjectType varchar(25), ObjectID int)

--	INSERT INTO @search
--		SELECT	S.IntersectTypeID,
--				E.ObjectType,
--				E.ObjectID 
--		FROM	IntersectTypeNode S
--				INNER JOIN IntersectTypeNode E ON	S.IntersectTypeID = E.IntersectTypeID 
--													AND S.ObjectType = @ObjectType
--													AND S.ObjectID = @ObjectID
--													AND S.IntersectTypeID NOT IN (SELECT ID FROM @CheckedIDs)

--	INSERT INTO @checked 
--		SELECT ID FROM @CheckedIDs
--	INSERT INTO @checked
--		SELECT IntersectTypeID FROM @search

--	--INSERT INTO @EventTypes
--	--	SELECT ID FROM @CollectedEventTypeIDs

--	INSERT INTO @EventTypes
--		SELECT	ObjectID 
--		FROM	@search
--		WHERE	ObjectType = 'EventType'

--	declare @i int,
--			@max int,
--			@oType varchar(25),
--			@oID int

--	set @i = 1
--	SELECT @max = MAX(ID) FROM @search

--	WHILE (@i <= @max)
--	BEGIN
--		SELECT	@oType = ObjectType,
--				@oID = ObjectID
--		FROM	@search
--		WHERE	ID = @i

--		INSERT INTO @EventTypes
--			SELECT ID FROM dbo.GetEventTypesByObject(@oType, @oID, @checked)--, @EventTypes)

--		SET @i = @i + 1
--	END

--	--INSERT INTO @EventTypes
--	--	SELECT	E.ObjectID 
--	--	FROM	IntersectTypeNode E
--	--			INNER JOIN IntersectTypeNode O	ON E.IntersectTypeID = O.IntersectTypeID 
--	--											AND O.ObjectType = @ObjectType
--	--											AND O.ObjectID = @ObjectID
--	--											AND E.ObjectType = 'EventType'

--	RETURN
--END
--GO

CREATE VIEW	[dbo].[EventHeader]
AS
SELECT		G.ID as EventGroupID,
			G.RuleID as EventTypeID,
			G.Name,
			COALESCE(E.EventCount, 0) as EventCount,
			COALESCE(E.Status, 'Closed') as Status
FROM		EventGroup G
			LEFT JOIN	(
						SELECT		EventGroupID,
									Status,
									COUNT(1) As EventCount
						FROM		[Event]
						GROUP BY	EventGroupID, Status
						) E ON E.EventGroupID = G.ID
