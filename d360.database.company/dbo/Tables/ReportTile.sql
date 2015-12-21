CREATE TABLE [dbo].[ReportTile] (
    [ID]                INT            IDENTITY (1, 1) NOT NULL,
    [ReportID]          INT            NOT NULL,
    [ReportTileType]    INT            NOT NULL,
    [ContentAreaNumber] INT            NOT NULL,
    [Name]              NVARCHAR (250) NOT NULL,
    [CommandText]       NVARCHAR (MAX) NOT NULL,
    [Settings]          XML            NULL,
    [UpdatedOn]         DATETIME       NULL,
    [UpdatedBy]         INT            NULL,
    CONSTRAINT [PK_ReportTile] PRIMARY KEY CLUSTERED ([ID] ASC)
);




GO

CREATE TRIGGER [dbo].[ReportTile_AfterDelete]
   ON  [dbo].[ReportTile] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'Report', ReportID, coalesce(UpdatedBy, 0)), 'ReportTile', ID from deleted

GO

CREATE TRIGGER [dbo].[ReportTile_AfterInsert]
   ON  [dbo].[ReportTile] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Add', [queue].WriteIndexXml('', 'Report', ReportID, coalesce(UpdatedBy, 0)), 'ReportTile', ID from inserted

GO

CREATE TRIGGER [dbo].[ReportTile_AfterUpdate]
   ON  [dbo].[ReportTile] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Update', [queue].WriteIndexXml('', 'Report', ReportID, coalesce(UpdatedBy, 0)), 'Report', ID from inserted
