CREATE TABLE [dbo].[Group] (
    [ID]                       INT             IDENTITY (1, 1) NOT NULL,
    [Name]                     NVARCHAR (250)  NOT NULL,
    [Description]              NVARCHAR (4000) NULL,
    [PrimaryOwnerResourceID]   INT             NULL,
    [SecondaryOwnerResourceID] INT             NULL,
    [UpdatedOn]                DATETIME        NULL,
    [UpdatedBy]                INT             NULL,
    CONSTRAINT [PK_Group] PRIMARY KEY CLUSTERED ([ID] ASC)
);




GO

CREATE TRIGGER [dbo].[Group_AfterDelete]
   ON  [dbo].[Group] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'Group', ID, coalesce(UpdatedBy, 0)), 'Group', ID from deleted

GO

CREATE TRIGGER [dbo].[Group_AfterInsert]
   ON  [dbo].[Group] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'Group', ID, coalesce(UpdatedBy, 0)), 'Group', ID from inserted

GO

CREATE TRIGGER [dbo].[Group_AfterUpdate]
   ON  [dbo].[Group] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'Group', ID, coalesce(UpdatedBy, 0)), 'Group', ID from inserted
