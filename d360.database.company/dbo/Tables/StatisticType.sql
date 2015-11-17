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
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'StatisticType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'StatisticType', ID from inserted

GO
CREATE TRIGGER [dbo].[StatisticType_AfterDelete]
   ON  [dbo].[StatisticType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'StatisticType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Removed', 'StatisticType', ID from deleted

GO
CREATE TRIGGER [dbo].[StatisticType_AfterInsert]
   ON  [dbo].[StatisticType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'StatisticType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'StatisticType', ID from inserted
