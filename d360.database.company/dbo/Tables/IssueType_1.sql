CREATE TABLE [dbo].[IssueType] (
    [ID]          INT            IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (250) NOT NULL,
    [Description] NVARCHAR (MAX) NULL,
    [IsSystem]    BIT            NOT NULL,
    [UpdatedOn]   DATETIME       NULL,
    [UpdatedBy]   INT            NULL,
    CONSTRAINT [PK_IssueType] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [CONST_IssueType_Name] UNIQUE NONCLUSTERED ([Name] ASC)
);




GO

GO
CREATE TRIGGER [dbo].[IssueType_AfterDelete]
   ON  [dbo].[IssueType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'IssueType', ID, coalesce(UpdatedBy, 0)), 'IssueType', ID from deleted
GO
