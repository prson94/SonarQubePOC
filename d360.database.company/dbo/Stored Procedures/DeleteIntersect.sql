
CREATE PROCEDURE [dbo].[DeleteIntersect]
	@ID int,
	@ResourceID int
AS
BEGIN
	SET NOCOUNT ON;
	declare @trancount int;
    set @trancount = @@trancount;	
	
	BEGIN TRY
		if @trancount = 0
            begin transaction
        else
			save transaction DeleteIntersect

		IF NOT EXISTS(select 1 from [Intersect] where ID = @ID)
		BEGIN
			RAISERROR('Item does not exist.', 16, 1);
		END

		IF EXISTS(select 1 from [Intersect] where (Subject = 'Intersect' and SubjectID = @ID) OR (Object = 'Intersect' and ObjectID = @ID) )
		BEGIN
			RAISERROR('Item is used in other relationships.', 16, 1);
		END

		if exists(select 1 from [Attribute] where ObjectType = 'Intersect' and ObjectID = @ID)
		begin
			DELETE	[Attribute]
			WHERE	ObjectType = 'Intersect' and ObjectID = @ID
		end

		declare @oNodeID int,
				@date datetime,
				@Subject varchar(50),
				@SubjectID int,
				@Object varchar(50),
				@ObjectID int

		set @date = getutcdate()

		select	@Subject = Subject,
				@SubjectID = SubjectID,
				@Object = Object,
				@ObjectID = ObjectID
		from	[Intersect]
		where	ID = @ID

		exec utility.AddAuditEntry @Subject, @SubjectID, @ResourceID, @date, 'Removed', 'Intersect', @ID
		exec utility.AddAuditEntry @Object, @ObjectID, @ResourceID, @date, 'Removed', 'Intersect', @ID

		-- Now delete the actual record.
		delete	[Intersect]
		where	ID = @ID

		if @trancount = 0
			commit;
	END TRY
	BEGIN CATCH
		declare @message varchar(4000), @xstate int;
        select @message = ERROR_MESSAGE(), @xstate = XACT_STATE();
        if @xstate = -1
            rollback;
        if @xstate = 1 and @trancount = 0
            rollback
        if @xstate = 1 and @trancount > 0
            rollback transaction DeleteIntersect;

        raiserror ('Unable to remove relationship: %s', 16, 1, @message);
	END CATCH
END
