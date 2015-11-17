CREATE FUNCTION utility.GetRootEventTypeID
(
	@ID int
)
RETURNS int
AS
BEGIN
	-- Declare the return variable here
	DECLARE @rootID int;

	WITH E (ID, ParentID)
	AS
	(
		SELECT	ID, 
				ParentID
		FROM	EventType
		WHERE	ID = @ID		
		UNION ALL
		SELECT	P.ID, 
				P.ParentID
		FROM	EventType	P
				INNER JOIN E AS C ON C.ParentID = P.ID	
	)

	select @rootID = ID FROM E WHERE ParentID is null

	RETURN @rootID
END