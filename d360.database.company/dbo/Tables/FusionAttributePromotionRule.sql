CREATE TABLE [dbo].[FusionAttributePromotionRule] (
    [ID]                        INT          IDENTITY (1, 1) NOT NULL,
    [ObjectType]                VARCHAR (25) NOT NULL,
    [ObjectID]                  INT          NOT NULL,
    [ParentObjectType]          VARCHAR (25) NULL,
    [ParentObjectID]            INT          NULL,
    [PromotionObjectType]       VARCHAR (25) NOT NULL,
    [PromotionObjectID]         INT          NOT NULL,
    [PromotionParentObjectType] VARCHAR (25) NULL,
    [PromotionParentObjectID]   INT          NULL,
    [FusionID]                  INT          NOT NULL,
    [UpdatedOn]                 DATETIME     NULL,
    [UpdatedBy]                 INT          NULL,
    [Enabled]                   BIT          CONSTRAINT [DF_FusionAttributePromotionRule_Enabled] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_FusionAttributePromotionRule] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionAttributePromotionRule_Fusion] FOREIGN KEY ([FusionID]) REFERENCES [dbo].[Fusion] ([ID])
);




GO
CREATE NONCLUSTERED INDEX [IX_FusionAttributePromotionRule_FusionID]
    ON [dbo].[FusionAttributePromotionRule]([FusionID] ASC);


GO

CREATE TRIGGER [dbo].[FusionAttributePromotionRule_AfterDelete]
   ON  [dbo].[FusionAttributePromotionRule] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'Fusion', FusionID, coalesce(UpdatedBy, 0)), 'FusionAttributePromotionRule', ID from deleted

GO

CREATE TRIGGER [dbo].[FusionAttributePromotionRule_AfterInsert]
   ON  [dbo].[FusionAttributePromotionRule] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'Fusion', FusionID, coalesce(UpdatedBy, 0)), 'FusionAttributePromotionRule', ID from inserted

GO

CREATE TRIGGER [dbo].[FusionAttributePromotionRule_AfterUpdate]
   ON  [dbo].[FusionAttributePromotionRule] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'Fusion', FusionID, coalesce(UpdatedBy, 0)), 'FusionAttributePromotionRule', ID from inserted
