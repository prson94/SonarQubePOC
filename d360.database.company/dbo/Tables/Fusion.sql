CREATE TABLE [dbo].[Fusion] (
    [ID]                INT             IDENTITY (1, 1) NOT NULL,
    [FusionTypeID]      INT             NOT NULL,
    [Name]              NVARCHAR (250)  NOT NULL,
    [Description]       NVARCHAR (4000) NULL,
    [Enabled]           BIT             CONSTRAINT [DF_Fusion_Enabled] DEFAULT ((1)) NOT NULL,
    [Manual]            BIT             NOT NULL,
    [LockPromotedItems] BIT             CONSTRAINT [DF_Fusion_LockPromotedItems] DEFAULT ((1)) NOT NULL,
    [IntervalType]      INT             NULL,
    [Interval]          INT             NULL,
    [ForceRefresh]      BIT             NULL,
    [UpdatedOn]         DATETIME        NULL,
    [UpdatedBy]         INT             NULL,
    CONSTRAINT [PK_Fusion] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Fusion_FusionType] FOREIGN KEY ([FusionTypeID]) REFERENCES [dbo].[FusionType] ([ID]) ON DELETE CASCADE
);




GO
CREATE NONCLUSTERED INDEX [IX_Fusion_FusionTypeID]
    ON [dbo].[Fusion]([FusionTypeID] ASC);


GO

CREATE TRIGGER [dbo].[Fusion_AfterDelete]
   ON  [dbo].[Fusion] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'Fusion', ID, coalesce(UpdatedBy, 0)), 'Fusion', ID from deleted

GO

CREATE TRIGGER [dbo].[Fusion_AfterInsert]
   ON  [dbo].[Fusion] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'Fusion', ID, coalesce(UpdatedBy, 0)), 'Fusion', ID from inserted

GO

CREATE TRIGGER [dbo].[Fusion_AfterUpdate]
   ON  [dbo].[Fusion] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'Fusion', ID, coalesce(UpdatedBy, 0)), 'Fusion', ID from inserted
