CREATE FUNCTION [utility].[GetObjectLevel]
(
	@Type varchar(50),
	@ID int
)
RETURNS int
AS
BEGIN
	DECLARE @level int

	IF (@Type = 'Artifact')
	BEGIN
		WITH H (ParentID, ID, [level])
		AS
		(
			SELECT	ParentID, 
					ID, 
					1
			FROM	Artifact
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	Artifact	P
					INNER JOIN H AS C ON C.ParentID = P.ID	
		)
		SELECT @level =	MAX([level]) FROM H
	END

	IF (@Type = 'Domain')
	BEGIN
		WITH H (ParentID, ID, [level])
		AS
		(
			SELECT	ParentID, 
					ID, 
					1
			FROM	Domain
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	Domain	P
					INNER JOIN H AS C ON C.ParentID = P.ID	
		)
		SELECT @level =	MAX([level]) FROM H
	END

	IF (@Type = 'FusionAttribute')
	BEGIN
		WITH H (ParentID, ID, [level])
		AS
		(
			SELECT	ParentID, 
					ID, 
					1
			FROM	FusionAttribute
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	FusionAttribute	P
					INNER JOIN H AS C ON C.ParentID = P.ID	
		)
		SELECT @level =	MAX([level]) FROM H
	END

	IF (@Type = 'EventType')
	BEGIN
		WITH H (ParentID, ID, [level])
		AS
		(
			SELECT	ParentID, 
					ID, 
					1
			FROM	EventType
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	EventType	P
					INNER JOIN H AS C ON C.ParentID = P.ID	
		)
		SELECT @level =	MAX([level]) FROM H
	END

	IF (@Type = 'FusionAttributeType')
	BEGIN
		WITH H (ParentID, ID, [level])
		AS
		(
			SELECT	ParentID, 
					ID, 
					1
			FROM	FusionAttributeType
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	FusionAttributeType	P
					INNER JOIN H AS C ON C.ParentID = P.ID	
		)
		SELECT @level =	MAX([level]) FROM H
	END

	IF (@Type = 'Policy')
	BEGIN
		WITH H (ParentID, ID, [level])
		AS
		(
			SELECT	ParentID, 
					ID, 
					1
			FROM	Policy
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	Policy	P
					INNER JOIN H AS C ON C.ParentID = P.ID
		)
		SELECT @level =	MAX([level]) FROM H
	END

	IF (@Type = 'Taxonomy')
	BEGIN
		WITH H (ParentID, ID, [level])
		AS
		(
			SELECT	ParentID, 
					ID, 
					1
			FROM	Taxonomy
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	Taxonomy	P
					INNER JOIN H AS C ON C.ParentID = P.ID
		)
		SELECT @level =	MAX([level]) FROM H
	END

	RETURN @level
END