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
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'ResponsibilityType', ID, coalesce(UpdatedBy, 0)), 'ResponsibilityType', ID from deleted

GO

CREATE TRIGGER [dbo].[ResponsibilityType_AfterInsert]
   ON  [dbo].[ResponsibilityType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Add', [queue].WriteIndexXml('', 'ResponsibilityType', ID, coalesce(UpdatedBy, 0)), 'ResponsibilityType', ID from inserted

GO

CREATE TRIGGER [dbo].[ResponsibilityType_AfterUpdate]
   ON  [dbo].[ResponsibilityType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Update', [queue].WriteIndexXml('', 'ResponsibilityType', ID, coalesce(UpdatedBy, 0)), 'ResponsibilityType', ID from inserted
