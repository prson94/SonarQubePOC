
CREATE PROCEDURE [fusion].[RelateAction]
	-- Add the parameters for the stored procedure here
	@R_Subject varchar(20), 
	@R_SubjectID int,
	@R_Object varchar(20),
	@R_ObjectID int,
	@R_IntersectTypeID int,	
	@R_IntersectID int = 0 output
AS
BEGIN
	SET NOCOUNT ON;

    -- Validate that intersect type exists.
	if exists(select 1 from IntersectType where ID = @R_IntersectTypeID)
	begin
		
		select	@R_IntersectID = ID
		from	[Intersect]
		where	Subject = @R_Subject 
			and SubjectID = @R_SubjectID 
			and Object = @R_Object 
			and ObjectID = @R_ObjectID
			and IntersectTypeID = @R_IntersectTypeID

		if @R_IntersectID is null
		begin
			if @R_IntersectTypeID is not null
			begin
				begin try
					insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
					values					(@R_IntersectTypeID, @R_Subject, @R_SubjectID, @R_Object, @R_ObjectID, 0, 0, getutcdate(), 0, getutcdate())  

					select @R_IntersectID = SCOPE_IDENTITY()
					
					exec utility.AddAuditEntry @R_Subject, @R_SubjectID, 0, getutcdate, 'Created', 'Intersect', @R_IntersectID
					exec utility.AddAuditEntry @R_Object, @R_ObjectID, 0, getutcdate, 'Created', 'Intersect', @R_IntersectID

				end try
				begin catch
					select ERROR_MESSAGE()
				end catch
			end
		end		
	end
end