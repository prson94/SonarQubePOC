CREATE TABLE [dbo].[Report] (
    [ID]               INT             IDENTITY (1, 1) NOT NULL,
    [Name]             NVARCHAR (250)  NOT NULL,
    [Description]      NVARCHAR (4000) NULL,
    [ObjectType]       VARCHAR (25)    NOT NULL,
    [ObjectID]         INT             NOT NULL,
    [ReportLayoutID]   INT             NOT NULL,
    [UpdatedOn]        DATETIME        NULL,
    [UpdatedBy]        INT             NULL,
    [ReportType]       VARCHAR (25)    CONSTRAINT [DF_Report_ReportType] DEFAULT ('legacy') NOT NULL,
    [PowerBIDatasetID] VARCHAR (50)    NULL,
    [PowerBIReportID]  VARCHAR (50)    NULL,
    [FileName] VARCHAR(260) NULL, 
    CONSTRAINT [PK_Report] PRIMARY KEY CLUSTERED ([ID] ASC)
);






GO

CREATE TRIGGER [dbo].[Report_AfterDelete]
   ON  [dbo].[Report] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'Report', ID, coalesce(UpdatedBy, 0)), 'Report', ID from deleted

GO

CREATE TRIGGER [dbo].[Report_AfterInsert]
   ON  [dbo].[Report] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Add', [queue].WriteIndexXml('', 'Report', ID, coalesce(UpdatedBy, 0)), 'Report', ID from inserted

GO

CREATE TRIGGER [dbo].[Report_AfterUpdate]
   ON  [dbo].[Report] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Update', [queue].WriteIndexXml('', 'Report', ID, coalesce(UpdatedBy, 0)), 'Report', ID from inserted
