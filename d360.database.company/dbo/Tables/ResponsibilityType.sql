CREATE TABLE [dbo].[ResponsibilityType] (
    [ID]                      INT             IDENTITY (1, 1) NOT NULL,
    [Name]                    NVARCHAR (250)  NOT NULL,
    [ResponsibilityTypeGroup] INT             NOT NULL,
    [Description]             NVARCHAR (4000) NULL,
    [UpdatedOn]               DATETIME        NULL,
    [UpdatedBy]               INT             NULL,
    CONSTRAINT [PK_ResponsibilityType] PRIMARY KEY CLUSTERED ([ID] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_ResponsibilityType_ResponsibilityTypeGroup]
    ON [dbo].[ResponsibilityType]([ResponsibilityTypeGroup] ASC);


GO
CREATE TRIGGER [dbo].[ResponsibilityType_AfterDelete]
   ON  [dbo].[ResponsibilityType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'ResponsibilityType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Removed', 'ResponsibilityType', ID from deleted

		DELETE	O
		FROM	cache.ObjectDetails O
				inner join deleted d
		ON		O.[Object] = 'ResponsibilityType' and O.ObjectID = d.ID

GO
CREATE TRIGGER [dbo].[ResponsibilityType_AfterInsert]
   ON  [dbo].[ResponsibilityType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'ResponsibilityType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'ResponsibilityType', ID from inserted

	declare @tbl table (RowID int identity, ID int)
	insert into @tbl 
		select ID from inserted

	declare @current int = 1,
			@max int,
			@thisID int
	select @max = max(RowID) from @tbl

	while @current <= @max
	begin
		select @thisID = ID from @tbl where RowID = @current
		exec [cache].[SynchronizeObjectDetails] 'ResponsibilityType', @thisID
		set @current = @current + 1
	end

GO
CREATE TRIGGER [dbo].[ResponsibilityType_AfterUpdate]
   ON  [dbo].[ResponsibilityType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'ResponsibilityType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'ResponsibilityType', ID from inserted

	declare @tbl table (RowID int identity, ID int)
	insert into @tbl 
		select ID from inserted

	declare @current int = 1,
			@max int,
			@thisID int
	select @max = max(RowID) from @tbl

	while @current <= @max
	begin
		select @thisID = ID from @tbl where RowID = @current
		exec [cache].[SynchronizeObjectDetails] 'ResponsibilityType', @thisID
		set @current = @current + 1
	end
