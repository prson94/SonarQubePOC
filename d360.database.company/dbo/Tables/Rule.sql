CREATE TABLE [dbo].[Rule] (
    [ID]          INT             IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (250)  NOT NULL,
    [Description] NVARCHAR (4000) NULL,
    [RuleType]    INT             NOT NULL,
    [UpdatedOn]   DATETIME        NULL,
    [UpdatedBy]   INT             NULL,
    CONSTRAINT [PK_Rule] PRIMARY KEY CLUSTERED ([ID] ASC)
);




GO

CREATE TRIGGER [dbo].[Rule_AfterUpdate]
   ON  [dbo].[Rule] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Update', [queue].WriteIndexXml('', 'Rule', ID, coalesce(UpdatedBy, 0)), 'Rule', ID from inserted

GO

CREATE TRIGGER [dbo].[Rule_AfterInsert]
   ON  [dbo].[Rule] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Add', [queue].WriteIndexXml('', 'Rule', ID, coalesce(UpdatedBy, 0)), 'Rule', ID from inserted

GO

CREATE TRIGGER [dbo].[Rule_AfterDelete]
   ON  [dbo].[Rule] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'Rule', ID, coalesce(UpdatedBy, 0)), 'Rule', ID from deleted
