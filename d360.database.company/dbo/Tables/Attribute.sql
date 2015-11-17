CREATE TABLE [dbo].[Attribute] (
    [ID]                    INT          IDENTITY (1, 1) NOT NULL,
    [ParentID]              INT          NULL,
    [AttributeTypeID]       INT          NOT NULL,
    [ObjectType]            VARCHAR (50) NOT NULL,
    [ObjectID]              INT          NOT NULL,
    [InheritanceObjectType] VARCHAR (50) NULL,
    [InheritanceObjectID]   INT          NULL,
    [UpdatedOn]             DATETIME     NULL,
    [UpdatedBy]             INT          NULL,
    CONSTRAINT [PK_Attribute] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Attribute_AttributeType] FOREIGN KEY ([AttributeTypeID]) REFERENCES [dbo].[AttributeType] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_Attribute_ParentAttribute] FOREIGN KEY ([ParentID]) REFERENCES [dbo].[Attribute] ([ID])
);


GO
CREATE NONCLUSTERED INDEX [IX_Attribute_ObjectType-ObjectID]
    ON [dbo].[Attribute]([ObjectType] ASC, [ObjectID] ASC);


GO
CREATE TRIGGER [dbo].[Attribute_AfterDelete]
   ON  [dbo].[Attribute] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;

	DELETE	F
	FROM	Field as F
			INNER JOIN deleted AS d
	ON		F.ObjectType = 'Attribute' and F.ObjectID = d.ID

	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select ObjectType, ObjectID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Removed', 'Attribute', ID from deleted

	insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Attribute', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'D', ObjectType, ObjectID from deleted

GO
CREATE TRIGGER [dbo].[Attribute_AfterInsert]
   ON  [dbo].[Attribute] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select ObjectType, ObjectID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'Attribute', ID from inserted
	insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Attribute', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'A', ObjectType, ObjectID from inserted

	declare @tbl table (RowID int identity, ObjectType varchar(50), ObjectID int)
	insert into @tbl 
		select ObjectType, ObjectID from inserted

	declare @current int = 1,
			@max int,
			@objectType varchar(50),
			@objectID int
	select @max = max(RowID) from @tbl

	while @current <= @max
	begin
		select @objectType = ObjectType, @objectID = ObjectID 
		from @tbl where RowID = @current
		exec utility.CalculateStatistics @objectType, @objectID
		set @current = @current + 1
	end

GO
CREATE TRIGGER [dbo].[Attribute_AfterUpdate]
   ON  [dbo].[Attribute] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select ObjectType, ObjectID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'Attribute', ID from inserted
	insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Attribute', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'U', ObjectType, ObjectID from inserted

	declare @tbl table (RowID int identity, ObjectType varchar(50), ObjectID int)
	insert into @tbl 
		select ObjectType, ObjectID from inserted

	declare @current int = 1,
			@max int,
			@objectType varchar(50),
			@objectID int
	select @max = max(RowID) from @tbl

	while @current <= @max
	begin
		select @objectType = ObjectType, @objectID = ObjectID 
		from @tbl where RowID = @current
		exec utility.CalculateStatistics @objectType, @objectID
		set @current = @current + 1
	end
