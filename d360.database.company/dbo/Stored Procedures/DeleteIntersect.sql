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

		IF EXISTS(
			select	I.ID
			from	[Intersect] I
					inner join MapItem MI on MI.SourceIntersectID = I.ID and I.ID = @ID
		)
		BEGIN
			RAISERROR('Relationship is a source for other relationships.  You must first remove those consuming relationships before deleting this one.', 16, 1);
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

		-- Delete anywhere that the intersect is a target or consumer.
		delete MapRuleItemMapItem where MapItemID in (select ID from MapItem where TargetIntersectID = @ID)
		delete MapItemMap where MapItemID in (select ID from MapItem where TargetIntersectID = @ID)
		delete MapItem where TargetIntersectID = @ID

		-- Now delete the actual record.
		delete	[Intersect]
		where	ID = @ID

		--Update the responsibilities of the object that should inherit form the other (Taxonomy can push relationships down to artifact)
		if ( (@Subject = 'Taxonomy' and @Object = 'Artifact') OR (@Subject = 'Artifact' and @Object = 'Taxonomy') )
		begin
			if @Subject = 'Artifact'
			begin
				exec [cache].[SynchronizeResponsibilitiesForObject] @Subject, @SubjectID
			end
			if @Object = 'Artifact'
			begin
				exec [cache].[SynchronizeResponsibilitiesForObject] @Object, @ObjectID
			end
		end

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
