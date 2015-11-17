CREATE TABLE [dbo].[IntersectType] (
    [ID]        INT      IDENTITY (1, 1) NOT NULL,
    [Name]      AS       ([utility].[DeriveIntersectTypeNameWrapper]([ID])),
    [UpdatedOn] DATETIME NULL,
    [UpdatedBy] INT      NULL,
    CONSTRAINT [PK_IntersectType] PRIMARY KEY CLUSTERED ([ID] ASC)
);


GO
CREATE TRIGGER [dbo].[IntersectType_AfterUpdate]
   ON  [dbo].[IntersectType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'IntersectType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'IntersectType', ID from inserted

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
		exec [cache].[SynchronizeObjectDetails] 'IntersectType', @thisID
		set @current = @current + 1
	end

GO
CREATE TRIGGER [dbo].[IntersectType_AfterDelete]
   ON  [dbo].[IntersectType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	declare @type varchar(50) = 'IntersectType'

	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select @type, ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Removed', @type, ID from deleted

	DELETE	O
	FROM	cache.Relationships O
			inner join deleted d
	ON		O.IntersectTypeID = d.ID

	DELETE	O
	FROM	cache.ObjectDetails O
			inner join deleted d
	ON		O.[Object] = @type and O.ObjectID = d.ID

GO
CREATE TRIGGER [dbo].[IntersectType_AfterInsert]
   ON  [dbo].[IntersectType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'IntersectType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'IntersectType', ID from inserted

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
		exec [cache].[SynchronizeObjectDetails] 'IntersectType', @thisID
		set @current = @current + 1
	end
