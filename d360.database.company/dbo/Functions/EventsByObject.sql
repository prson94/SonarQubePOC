CREATE FUNCTION EventsByObject
(
	@Type varchar(250),
	@ID int,
	@Status varchar(50) = NULL
)
RETURNS 
@tbl TABLE 
(
	EventID int,
	RuleID int,
	[Rule] nvarchar(250),
	EventName nvarchar(250),
	EventGroupID int,
	SourceID nvarchar(250),
	[Status] varchar(50),
	[Date] datetime
)
AS
BEGIN
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
			insert into @tbl
				SELECT	E.ID AS EventID,
						G.RuleID,
						R.Name as [Rule],
						G.Name as EventName,
						E.EventGroupID,
						E.SourceID,
						E.Status,
						E.Date
				FROM	[Event] E
						INNER JOIN EventGroup G ON E.EventGroupID = G.ID 
												AND (E.Status = @Status OR 1=1)
						INNER JOIN [Rule] R on R.ID = G.RuleID
				where	R.ID in (
								select	distinct
										CR.TargetObjectID
								from	PH
										inner join cache.Relationship CR on CR.SourceObject = 'Policy' and CR.SourceObjectID = PH.ID and CR.TargetObject = 'Rule'
								)
		end

	if @Type = 'Rule'
		begin
			insert into @tbl
				SELECT	E.ID AS EventID,
						G.RuleID,
						R.Name as [Rule],
						G.Name as EventName,
						E.EventGroupID,
						E.SourceID,
						E.Status,
						E.Date
				FROM	[Event] E
						INNER JOIN EventGroup G ON E.EventGroupID = G.ID 
												AND (E.Status = @Status OR 1=1)
						INNER JOIN [Rule] R on R.ID = G.RuleID and R.ID = @ID
		end

	if @Type = 'EventGroup'
		begin
			insert into @tbl
				SELECT	E.ID AS EventID,
						G.RuleID,
						R.Name as [Rule],
						G.Name as EventName,
						E.EventGroupID,
						E.SourceID,
						E.Status,
						E.Date
				FROM	[Event] E
						INNER JOIN EventGroup G ON E.EventGroupID = G.ID 
												AND E.EventGroupID = @ID
												AND (E.Status = @Status OR 1=1)
						INNER JOIN [Rule] R on R.ID = G.RuleID
		end

	if @Type <> 'EventGroup' and @Type <> 'Policy' 
		begin
			insert into @tbl
				SELECT	E.ID AS EventID,
						G.RuleID,
						R.Name as [Rule],
						G.Name as EventName,
						E.EventGroupID,
						E.SourceID,
						E.Status,
						E.Date
				FROM	[Event] E
						INNER JOIN EventGroup G ON E.EventGroupID = G.ID 
												and (E.Status = @Status OR 1=1)
						INNER JOIN [Rule] R on R.ID = G.RuleID
						inner join cache.Relationship CR on CR.SourceObject = @Type and CR.SourceObjectID = @ID and CR.TargetObject = 'Rule' and CR.TargetObjectID = R.ID
		end

	RETURN 
END