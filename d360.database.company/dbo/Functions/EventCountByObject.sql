CREATE FUNCTION [dbo].[EventCountByObject]
(
	@Type varchar(250),
	@ID int,
	@Status varchar(50) = NULL
)
RETURNS int
AS
BEGIN
	DECLARE @count int

	if @Type = 'Policy'
	begin
		with PH as	(
					select	ID,
							ParentID
					from	Policy
					where	ID = @ID
					union all
					select	C.ID,
							C.ParentID
					from	Policy C 
							inner join PH on C.ParentID = PH.ID
					)

			SELECT	@count = count(1)
			FROM	[Event] E
					INNER JOIN EventGroup G ON E.EventGroupID = G.ID 
											AND (E.Status = @Status OR 1=1)
					INNER JOIN [Rule] R on R.ID = G.RuleID
			where	R.ID in (
							select	distinct
									case when CR.Subject = 'Policy' and CR.Object = 'Rule' then CR.ObjectID else CR.SubjectID end
							from	PH
									inner join [Intersect] CR on (CR.Subject = 'Policy' and CR.SubjectID = PH.ID and CR.Object = 'Rule') OR (CR.Object = 'Policy' and CR.ObjectID = PH.ID and CR.Subject = 'Rule')
							)
	end

	if @Type = 'Rule'
	begin
		SELECT	@count = count(1)
		FROM	[Event] E
				INNER JOIN EventGroup G ON E.EventGroupID = G.ID 
										AND (E.Status = @Status OR 1=1)
				INNER JOIN [Rule] R on R.ID = G.RuleID and R.ID = @ID
	end

	if @Type = 'EventGroup'
	begin
		SELECT	@count = count(1)
		FROM	[Event] E
				INNER JOIN EventGroup G ON E.EventGroupID = G.ID 
										AND E.EventGroupID = @ID
										AND (E.Status = @Status OR 1=1)
				INNER JOIN [Rule] R on R.ID = G.RuleID
	end

	if @Type <> 'EventGroup' and @Type <> 'Policy' 
	begin
		SELECT	@count = count(1)
		FROM	[Event] E
				INNER JOIN EventGroup G ON E.EventGroupID = G.ID 
										and (E.Status = @Status OR 1=1)
				INNER JOIN [Rule] R on R.ID = G.RuleID
				inner join [Intersect] CR on (CR.Subject = @Type and CR.SubjectID = @ID and CR.Object = 'Rule' and CR.ObjectID = R.ID) OR (CR.Object = @Type and CR.ObjectID = @ID and CR.Subject = 'Rule' and CR.SubjectID = R.ID)
	end

	RETURN @count
END