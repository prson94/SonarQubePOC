CREATE PROCEDURE validate.IntersectType
	@ID int,
	@Nodes IntersectionNodeType READONLY
AS 
BEGIN
	SET NOCOUNT ON;
	IF  EXISTS	(
				SELECT	COUNT(1) as Cnt
				FROM	dbo.IntersectType T
						CROSS APPLY (
									SELECT	IntersectTypeID
									FROM	IntersectTypeNode N
											INNER JOIN @Nodes TN ON TN.ObjectType = N.ObjectType AND TN.ObjectID = N.ObjectID AND N.IntersectTypeID = T.ID
									) N
				WHERE	(@ID = 0) OR (@ID <> 0 AND T.ID = @ID)
				GROUP BY	T.ID HAVING COUNT(1) > 1
				)
	BEGIN
		RAISERROR('An intersect type with this configuration already exists.', 10, 1) 
	END
END