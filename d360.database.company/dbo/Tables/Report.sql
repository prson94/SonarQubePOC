CREATE TABLE [dbo].[Report] (
    [ID]             INT             IDENTITY (1, 1) NOT NULL,
    [Name]           NVARCHAR (250)  NOT NULL,
    [Description]    NVARCHAR (4000) NULL,
    [ObjectType]     VARCHAR (25)    NOT NULL,
    [ObjectID]       INT             NOT NULL,
    [ReportLayoutID] INT             NOT NULL,
    [UpdatedOn]      DATETIME        NULL,
    [UpdatedBy]      INT             NULL,
    CONSTRAINT [PK_Report] PRIMARY KEY CLUSTERED ([ID] ASC)
);


GO
CREATE TRIGGER [dbo].[Report_AfterDelete]
   ON  [dbo].[Report] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Report', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Removed', 'Report', ID from deleted

GO
CREATE TRIGGER [dbo].[Report_AfterInsert]
   ON  [dbo].[Report] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Report', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'Report', ID from inserted

GO
CREATE TRIGGER [dbo].[Report_AfterUpdate]
   ON  [dbo].[Report] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Report', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'Report', ID from inserted
