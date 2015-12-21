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
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', ObjectType, ObjectID, coalesce(UpdatedBy, 0)), 'Attribute', ID from deleted

GO

CREATE TRIGGER [dbo].[Attribute_AfterInsert]
   ON  [dbo].[Attribute] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', ObjectType, ObjectID, coalesce(UpdatedBy, 0)), 'Attribute', ID from inserted

GO

CREATE TRIGGER [dbo].[Attribute_AfterUpdate]
   ON  [dbo].[Attribute] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', ObjectType, ObjectID, coalesce(UpdatedBy, 0)), 'Attribute', ID from inserted
