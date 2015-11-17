CREATE FUNCTION dbo.GetRelatedObjectsByEventType
(
	@ObjectType varchar(25),
	@ObjectID int,
	@CheckedIDs PreviouslyCheckedIDTable readonly,
	@CheckedIntersectTypeIDs PreviouslyCheckedIDTable readonly
)
RETURNS 
@Objects TABLE 
(
	ObjectType varchar(25),
	ObjectID int
)
AS
BEGIN
	declare @checkedTypes PreviouslyCheckedIDTable
	declare @checked PreviouslyCheckedIDTable
	declare @search table (ID int identity, IntersectTypeID int, IntersectID int, ObjectType varchar(25), ObjectID int)

	INSERT INTO @search
		SELECT	I.IntersectTypeID,
				S.IntersectID,
				E.ObjectType,
				E.ObjectID 
		FROM	IntersectNode S
				INNER JOIN [Intersect] I on S.IntersectID = I.ID and I.IntersectTypeID NOT IN (SELECT ID FROM @CheckedIntersectTypeIDs)
				INNER JOIN IntersectNode E ON	S.IntersectID = E.IntersectID 
												AND S.ObjectType = @ObjectType
												AND S.ObjectID = @ObjectID
												AND S.IntersectID NOT IN (SELECT ID FROM @CheckedIDs)

	INSERT INTO @checkedTypes
		SELECT ID FROM @CheckedIntersectTypeIDs
	INSERT INTO @checkedTypes
		SELECT DISTINCT IntersectTypeID FROM @search

	INSERT INTO @checked 
		SELECT ID FROM @CheckedIDs
	INSERT INTO @checked
		SELECT IntersectID FROM @search

	INSERT INTO @Objects
		SELECT	ObjectType,
				ObjectID 
		FROM	@search
		WHERE	ObjectType + CAST(ObjectID as varchar(15)) <> @ObjectType + CAST(@ObjectID as varchar(15))

	declare @i int,
			@max int,
			@oType varchar(25),
			@oID int

	set @i = 1
	SELECT @max = MAX(ID) FROM @search

	WHILE (@i <= @max)
	BEGIN
		SELECT	@oType = ObjectType,
				@oID = ObjectID
		FROM	@search
		WHERE	ID = @i

		INSERT INTO @Objects
			SELECT ObjectType, ObjectID FROM dbo.GetRelatedObjectsByEventType(@oType, @oID, @checked, @checkedTypes)

		SET @i = @i + 1
	END

	RETURN
END