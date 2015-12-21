CREATE TABLE [dbo].[DomainType] (
    [ID]          INT             IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (250)  NOT NULL,
    [Description] NVARCHAR (4000) NULL,
    [UpdatedOn]   DATETIME        NULL,
    [UpdatedBy]   INT             NULL,
    CONSTRAINT [PK_DomainType] PRIMARY KEY CLUSTERED ([ID] ASC)
);




GO

CREATE TRIGGER [dbo].[DomainType_AfterDelete]
	ON [dbo].[DomainType]
	AFTER DELETE
	AS
BEGIN
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'DomainType', ID, coalesce(UpdatedBy, 0)), 'DomainType', ID from deleted
END

GO

CREATE TRIGGER [dbo].[DomainType_AfterInsert]
   ON  [dbo].[DomainType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'DomainType', ID, coalesce(UpdatedBy, 0)), 'DomainType', ID from inserted

GO

CREATE TRIGGER [dbo].[DomainType_AfterUpdate]
   ON  [dbo].[DomainType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'DomainType', ID, coalesce(UpdatedBy, 0)), 'DomainType', ID from inserted
