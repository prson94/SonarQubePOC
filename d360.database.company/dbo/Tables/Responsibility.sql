CREATE TABLE [dbo].[Responsibility] (
    [ID]                     INT          IDENTITY (1, 1) NOT NULL,
    [ResponsibilityTypeID]   INT          NOT NULL,
    [ObjectType]             VARCHAR (50) NULL,
    [ObjectID]               INT          NULL,
    [ResponsibleObjectType]  VARCHAR (50) NULL,
    [ResponsibleObjectID]    INT          NULL,
    [UpdatedOn]              DATETIME     NULL,
    [UpdatedBy]              INT          NULL,
    [Visible]                BIT          CONSTRAINT [DF_Responsibility_Visible] DEFAULT ((1)) NOT NULL,
    [TargetResponsibilityID] INT          NULL,
    CONSTRAINT [PK_Responsibility] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Responsibility_ResponsibilityType] FOREIGN KEY ([ResponsibilityTypeID]) REFERENCES [dbo].[ResponsibilityType] ([ID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_Responsibility_ObjectType-ObjectID]
    ON [dbo].[Responsibility]([ObjectType] ASC, [ObjectID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Responsibility_ResponsibleObjectType-ResponsibleObjectID]
    ON [dbo].[Responsibility]([ResponsibleObjectType] ASC, [ResponsibleObjectID] ASC);


GO
CREATE TRIGGER [dbo].[Responsibility_AfterDelete]
   ON  [dbo].[Responsibility] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select ObjectType, ObjectID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Removed', 'Responsibility', ID from deleted

	--declare @tbl table (RowID int identity, ID int)
	--insert into @tbl 
	--	select ID from deleted

	--declare @current int = 1,
	--		@max int,
	--		@rID int
	--select @max = MAX(RowID) from @tbl
	--while @current<= @max
	--begin
	--	select @rID = ID from @tbl where RowID = @current
		exec [cache].[SynchronizeResponsibilities]
	--	set @current = @current + 1
	--end

GO
CREATE TRIGGER [dbo].[Responsibility_AfterInsert]
   ON  [dbo].[Responsibility] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select ObjectType, ObjectID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'Responsibility', ID from inserted

	--declare @tbl table (RowID int identity, ID int)
	--insert into @tbl 
	--	select ID from inserted

	--declare @current int = 1,
	--		@max int,
	--		@rID int
	--select @max = MAX(RowID) from @tbl
	--while @current<= @max
	--begin
	--	select @rID = ID from @tbl where RowID = @current
	exec [cache].[SynchronizeResponsibilities]
	--	set @current = @current + 1
	--end

GO
CREATE TRIGGER [dbo].[Responsibility_AfterUpdate]
   ON  [dbo].[Responsibility] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select ObjectType, ObjectID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'Responsibility', ID from inserted

	--declare @tbl table (RowID int identity, ID int)
	--insert into @tbl 
	--	select ID from inserted

	--declare @current int = 1,
	--		@max int,
	--		@rID int
	--select @max = MAX(RowID) from @tbl
	--while @current<= @max
	--begin
	--	select @rID = ID from @tbl where RowID = @current
	exec [cache].[SynchronizeResponsibilities]-- @rID, 0
	--	set @current = @current + 1
	--end
