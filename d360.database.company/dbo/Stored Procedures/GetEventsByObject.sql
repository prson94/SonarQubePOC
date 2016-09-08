CREATE PROCEDURE [dbo].[GetEventsByObject] --'Policy', 1-- 'Rule', 7
	@Type varchar(250),
	@ID int,
	@Status varchar(25) = NULL
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
									inner join cache.Relationships CR on CR.SourceObject = 'Policy' and CR.SourceObjectID = PH.ID and CR.TargetObject = 'Rule'
							)
		end

	if @Type = 'Rule'
		begin
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
					inner join [Intersect] CR on	(
													(CR.Subject = @Type and CR.SubjectID = @ID and CR.Object = 'Rule' and CR.ObjectID = R.ID) OR 
													(CR.Object = @Type and CR.ObjectID = @ID and CR.Subject = 'Rule' and CR.SubjectID = R.ID)
													)
		end
END
GO

