CREATE TABLE [dbo].[LookupType] (
    [ID]        INT            IDENTITY (1, 1) NOT NULL,
    [Name]      NVARCHAR (250) NOT NULL,
    [UpdatedOn] DATETIME       NULL,
    [UpdatedBy] INT            NULL,
    CONSTRAINT [PK_LookupType] PRIMARY KEY CLUSTERED ([ID] ASC)
);








GO

CREATE TRIGGER [dbo].[LookupType_AfterDelete]
   ON  [dbo].[LookupType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'LookupType', ID, coalesce(UpdatedBy, 0)), 'LookupType', ID from deleted

GO


GO

