CREATE TABLE [dbo].[Lookup] (
    [ID]           INT      IDENTITY (1, 1) NOT NULL,
    [LookupTypeID] INT      NOT NULL,
    [UpdatedOn]    DATETIME NULL,
    [UpdatedBy]    INT      NULL,
    CONSTRAINT [PK_Lookup] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Lookup_LookupType] FOREIGN KEY ([LookupTypeID]) REFERENCES [dbo].[LookupType] ([ID]) ON DELETE CASCADE
);






GO
CREATE NONCLUSTERED INDEX [IX_Lookup_LookupTypeID]
    ON [dbo].[Lookup]([LookupTypeID] ASC);


GO

CREATE TRIGGER [dbo].[Lookup_AfterDelete]
   ON  [dbo].[Lookup] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'LookupType', LookupTypeID, coalesce(UpdatedBy, 0)), 'Lookup', ID from deleted

GO


GO

