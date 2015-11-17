CREATE TABLE [dbo].[FusionAttributeType] (
    [ID]           INT            IDENTITY (50000, 1) NOT NULL,
    [ParentID]     INT            NULL,
    [FusionTypeID] INT            NOT NULL,
    [Name]         NVARCHAR (500) NOT NULL,
    [Path]         AS             ([utility].[GetBreadcrumbWrapper]('FusionAttributeType',[ID])),
    [TextPath]     AS             ([utility].[GetBreadcrumbStringWrapper]('FusionAttributeType',[ID],'.')),
    [Tab]          NVARCHAR (250) NULL,
    [Assignable]   BIT            CONSTRAINT [DF_FusionAttributeType_Assignable] DEFAULT ((0)) NOT NULL,
    [UpdatedOn]    DATETIME       NULL,
    [UpdatedBy]    INT            NULL,
    CONSTRAINT [PK_FusionAttributeType] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionAttributeType_Parent] FOREIGN KEY ([ParentID]) REFERENCES [dbo].[FusionAttributeType] ([ID])
);


GO
CREATE NONCLUSTERED INDEX [IX_FusionAttributeType_FusionTypeID]
    ON [dbo].[FusionAttributeType]([FusionTypeID] ASC);


GO
CREATE TRIGGER [dbo].[FusionAttributeType_AfterDelete]
   ON  [dbo].[FusionAttributeType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;

	declare @type varchar(50) = 'FusionAttributeType'

	insert into [queue].ObjectVersion ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'FusionType', FusionTypeID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Removed', 'FusionAttributeType', ID from deleted
		union
		select 'FusionAttributeType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Removed', 'FusionAttributeType', ID from deleted

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
CREATE TRIGGER [dbo].[FusionAttributeType_AfterInsert]
   ON  [dbo].[FusionAttributeType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into [queue].ObjectVersion ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'FusionType', FusionTypeID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'FusionAttributeType', ID from inserted
		union
		select 'FusionAttributeType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'FusionAttributeType', ID from inserted

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
		exec [cache].[SynchronizeObjectDetails] 'FusionAttributeType', @thisID
		set @current = @current + 1
	end

GO
CREATE TRIGGER [dbo].[FusionAttributeType_AfterUpdate]
   ON  [dbo].[FusionAttributeType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	insert into [queue].ObjectVersion ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'FusionType', FusionTypeID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'FusionAttributeType', ID from inserted
		union
		select 'FusionAttributeType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'FusionAttributeType', ID from inserted

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
		exec [cache].[SynchronizeObjectDetails] 'FusionAttributeType', @thisID
		set @current = @current + 1
	end
