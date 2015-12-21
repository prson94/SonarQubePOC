CREATE TABLE [dbo].[StatisticType] (
    [ID]            INT             IDENTITY (50000, 1) NOT NULL,
    [Name]          NVARCHAR (250)  NOT NULL,
    [Description]   NVARCHAR (4000) NULL,
    [CheckType]     INT             NOT NULL,
    [PartOfScore]   BIT             CONSTRAINT [DF__Statistic__PartO__1DE57479] DEFAULT ((1)) NOT NULL,
    [Configuration] XML             NULL,
    [UpdatedOn]     DATETIME        NULL,
    [UpdatedBy]     INT             NULL,
    CONSTRAINT [PK_StatisticType] PRIMARY KEY CLUSTERED ([ID] ASC)
);




GO

CREATE TRIGGER [dbo].[StatisticType_AfterUpdate]
   ON  [dbo].[StatisticType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Update', [queue].WriteIndexXml('', 'StatisticType', ID, coalesce(UpdatedBy, 0)), 'StatisticType', ID from inserted

GO

CREATE TRIGGER [dbo].[StatisticType_AfterDelete]
   ON  [dbo].[StatisticType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'StatisticType', ID, coalesce(UpdatedBy, 0)), 'StatisticType', ID from deleted

GO

CREATE TRIGGER [dbo].[StatisticType_AfterInsert]
   ON  [dbo].[StatisticType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Add', [queue].WriteIndexXml('', 'StatisticType', ID, coalesce(UpdatedBy, 0)), 'StatisticType', ID from inserted
