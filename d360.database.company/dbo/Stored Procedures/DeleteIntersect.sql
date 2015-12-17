

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

		IF EXISTS(select 1 from IntersectNode where ObjectType = 'Intersect' and ObjectID = @ID)
		BEGIN
			RAISERROR('Item is used in other relationships.', 16, 1);
		END

		IF EXISTS(
			select	TIN.ID
			from	IntersectNode s
					inner join IntersectNode t on t.IntersectID = s.IntersectID and t.ID <> s.ID and s.IntersectID = @ID
					inner join IntersectTypeNode ST on ST.ID = S.IntersectTypeNodeID and ST.[Order] = 1
					inner join Responsibility R on R.ResponsibleObjectType = s.ObjectType and R.ResponsibleObjectID = s.ObjectID and R.ObjectType = 'Intersect'
					inner join IntersectNode TIN on TIN.IntersectID = R.ObjectID and TIN.ObjectType = t.ObjectType and TIN.ObjectID = t.ObjectID
					inner join IntersectTypeNode TITN on TITN.ID = TIN.IntersectTypeNodeID and TITN.[Order] <> 1
		)
		BEGIN
			RAISERROR('Relationship is a source for other relationships.  You must first remove those consuming relationships before deleting this one.', 16, 1);
		END

		if exists(select 1 from [Attribute] where ObjectType = 'Intersect' and ObjectID = @ID)
		begin
			DELETE	[Attribute]
			WHERE	ObjectType = 'Intersect' and ObjectID = @ID
		end

		if exists(select 1 from Responsibility where ObjectType = 'Intersect' and ObjectID = @ID)
		begin
			delete	ResponsibilityContextItem
			where	ResponsibilityID in (
										select	ID
										from	Responsibility
										where	ObjectType = 'Intersect' 
												and ObjectID = @ID
										)

			delete	Responsibility
			where	ObjectType = 'Intersect' 
					and ObjectID = @ID
		end

		declare @nodes table ([Type] varchar(25), ID int, IntersectNodeID int)
	
		insert into @nodes
			select	n.ObjectType,
					n.ObjectID,
					n.ID
			from	IntersectNode n
			where	n.IntersectID = @ID


		declare @oType varchar(25), 
				@oID int, 
				@oNodeID int,
				@date datetime

		set @date = getutcdate()

		select	top 1
				@oType = [Type],
				@oID = ID,
				@oNodeID = IntersectNodeID
		from	@nodes

		exec utility.AddAuditEntry @oType, @oID, @ResourceID, @date, 'Removed', 'Intersect', @ID
		delete @nodes where IntersectNodeID = @oNodeID

		select	top 1
				@oType = [Type],
				@oID = ID,
				@oNodeID = IntersectNodeID
		from	@nodes

		exec utility.AddAuditEntry @oType, @oID, @ResourceID, @date, 'Removed', 'Intersect', @ID
		delete @nodes where IntersectNodeID = @oNodeID


		-- Now delete the actual records.
		delete	IntersectNode
		where	IntersectID = @ID

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
