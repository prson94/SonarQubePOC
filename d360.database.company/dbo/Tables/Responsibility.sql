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
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', ObjectType, ObjectID, coalesce(UpdatedBy, 0)), 'Responsibility', ID from deleted

	delete	T
	from	cache.ResponsibilityItem T
			inner join deleted S on S.ID = T.ResponsibilityID

	declare @tbl table (RowID int identity, [ObjectType] varchar(50), ObjectID int)
	insert into @tbl 
		select [ObjectType], ObjectID from deleted
	declare @c int = 1,
			@m int,
			@o varchar(50),
			@oid int
	select @m = max(RowID) from @tbl

	while @c <= @m
	begin
		select	@o = ObjectType,
				@oid = ObjectID
		from	@tbl
		where	RowID = @c
		
		exec [cache].[SynchronizeResponsibilitiesForObject] @o, @oid

		set @c = @c + 1
	end

GO
CREATE TRIGGER [dbo].[Responsibility_AfterInsert]
   ON  [dbo].[Responsibility] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Add', [queue].WriteIndexXml('', ObjectType, ObjectID, coalesce(UpdatedBy, 0)), 'Responsibility', ID from inserted

	declare @tbl table (RowID int identity, [ObjectType] varchar(50), ObjectID int)
	insert into @tbl 
		select [ObjectType], ObjectID from inserted
	declare @c int = 1,
			@m int,
			@o varchar(50),
			@oid int
	select @m = max(RowID) from @tbl

	while @c <= @m
	begin
		select	@o = ObjectType,
				@oid = ObjectID
		from	@tbl
		where	RowID = @c
		
		exec [cache].[SynchronizeResponsibilitiesForObject] @o, @oid

		set @c = @c + 1
	end

GO
CREATE TRIGGER [dbo].[Responsibility_AfterUpdate]
   ON  [dbo].[Responsibility] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Update', [queue].WriteIndexXml('', ObjectType, ObjectID, coalesce(UpdatedBy, 0)), 'Responsibility', ID from inserted

	delete	T
	from	cache.[ResponsibilityItem] T
			inner join inserted S on S.ID = T.ResponsibilityID 

	declare @tbl table (RowID int identity, [ObjectType] varchar(50), ObjectID int)
	insert into @tbl 
		select [ObjectType], ObjectID from inserted
	declare @c int = 1,
			@m int,
			@o varchar(50),
			@oid int
	select @m = max(RowID) from @tbl

	while @c <= @m
	begin
		select	@o = ObjectType,
				@oid = ObjectID
		from	@tbl
		where	RowID = @c
		
		exec [cache].[SynchronizeResponsibilitiesForObject] @o, @oid

		set @c = @c + 1
	end
