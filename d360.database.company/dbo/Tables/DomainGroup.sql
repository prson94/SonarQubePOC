CREATE TABLE [dbo].[DomainGroup] (
    [ID]           INT            IDENTITY (1, 1) NOT NULL,
    [Name]         NVARCHAR (250) NOT NULL,
    [DomainTypeID] INT            NOT NULL,
    [MasterListID] INT            NULL,
    [Description]  NVARCHAR (MAX) NULL,
    [UpdatedOn]    DATETIME       NULL,
    [UpdatedBy]    INT            NULL,
    CONSTRAINT [PK_DomainGroup] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_DomainGroup_Domain] FOREIGN KEY ([MasterListID]) REFERENCES [dbo].[Domain] ([ID]),
    CONSTRAINT [FK_DomainGroup_DomainType] FOREIGN KEY ([DomainTypeID]) REFERENCES [dbo].[DomainType] ([ID]) ON DELETE CASCADE
);






GO
CREATE NONCLUSTERED INDEX [IX_DomainGroup_DomainTypeID]
    ON [dbo].[DomainGroup]([DomainTypeID] ASC);


GO


CREATE TRIGGER [dbo].[DomainGroup_AfterDelete]
   ON  [dbo].[DomainGroup] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'DomainType', ID, coalesce(UpdatedBy, 0)), 'DomainGroup', ID from deleted

GO

CREATE TRIGGER [dbo].[DomainGroup_AfterInsert]
   ON  [dbo].[DomainGroup] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'DomainType', DomainTypeID, coalesce(UpdatedBy, 0)), 'DomainGroup', ID from inserted

GO

CREATE TRIGGER [dbo].[DomainGroup_AfterUpdate]
   ON  [dbo].[DomainGroup] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'DomainType', DomainTypeID, coalesce(UpdatedBy, 0)), 'DomainGroup', ID from inserted
