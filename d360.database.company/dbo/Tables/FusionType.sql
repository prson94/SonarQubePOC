CREATE TABLE [dbo].[FusionType] (
    [ID]          INT             IDENTITY (50000, 1) NOT NULL,
    [Name]        NVARCHAR (250)  NOT NULL,
    [Description] NVARCHAR (4000) NULL,
    [UpdatedOn]   DATETIME        NULL,
    [UpdatedBy]   INT             NULL,
    CONSTRAINT [PK_FusionType] PRIMARY KEY CLUSTERED ([ID] ASC)
);


GO
CREATE TRIGGER [dbo].[FusionType_AfterUpdate]
   ON  [dbo].[FusionType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'FusionType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'FusionType', ID from inserted

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
		exec [cache].[SynchronizeObjectDetails] 'FusionType', @thisID
		set @current = @current + 1
	end

GO
CREATE TRIGGER [dbo].[FusionType_AfterDelete]
   ON  [dbo].[FusionType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	declare @type varchar(50) = 'FusionType'

	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select @type, ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Removed', @type, ID from deleted

	DELETE	R
	FROM	AttributeTypeRelation R
			INNER JOIN deleted D on R.ObjectType = @type AND R.ObjectID = D.ID

	DELETE	R
	FROM	FieldType R
			INNER JOIN deleted D on R.[Object] = @type AND R.ObjectID = D.ID

	DELETE	R
	FROM	StatisticTypeRelation R
			INNER JOIN deleted D on R.ObjectType = @type AND R.ObjectID = D.ID

	DELETE	O
	FROM	cache.ObjectDetails O
			inner join deleted d
	ON		O.[Object] = @type and O.ObjectID = d.ID

GO
CREATE TRIGGER [dbo].[FusionType_AfterInsert]
   ON  [dbo].[FusionType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'FusionType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'FusionType', ID from inserted

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
		exec [cache].[SynchronizeObjectDetails] 'FusionType', @thisID
		set @current = @current + 1
	end
