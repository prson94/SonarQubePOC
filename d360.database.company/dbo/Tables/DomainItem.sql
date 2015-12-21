CREATE TABLE [dbo].[DomainItem] (
    [ID]          INT             IDENTITY (1, 1) NOT NULL,
    [Parents]     XML             NULL,
    [DomainID]    INT             NOT NULL,
    [Code]        NVARCHAR (50)   NOT NULL,
    [Name]        NVARCHAR (250)  NOT NULL,
    [Description] NVARCHAR (4000) NULL,
    [UpdatedOn]   DATETIME        NULL,
    [UpdatedBy]   INT             NULL,
    CONSTRAINT [PK_DomainItem] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_DomainItem_Domain] FOREIGN KEY ([DomainID]) REFERENCES [dbo].[Domain] ([ID]) ON DELETE CASCADE
);




GO
CREATE NONCLUSTERED INDEX [IX_DomainItem_DomainID]
    ON [dbo].[DomainItem]([DomainID] ASC);


GO

CREATE TRIGGER [dbo].[DomainItem_AfterDelete]
   ON  [dbo].[DomainItem] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'Doamin', DomainID, coalesce(UpdatedBy, 0)), 'DomainItem', ID from deleted

GO

CREATE TRIGGER [dbo].[DomainItem_AfterInsert]
   ON  [dbo].[DomainItem] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'Domain', DomainID, coalesce(UpdatedBy, 0)), 'DomainItem', ID from inserted

GO

CREATE TRIGGER [dbo].[DomainItem_AfterUpdate]
   ON  [dbo].[DomainItem] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'Domain', DomainID, coalesce(UpdatedBy, 0)), 'DomainItem', ID from inserted
