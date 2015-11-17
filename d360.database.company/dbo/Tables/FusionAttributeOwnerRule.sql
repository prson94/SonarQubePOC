CREATE TABLE [dbo].[FusionAttributeOwnerRule] (
    [ID]                          INT          IDENTITY (1, 1) NOT NULL,
    [ObjectType]                  VARCHAR (25) NULL,
    [ObjectID]                    INT          NULL,
    [ParentObjectType]            VARCHAR (25) NULL,
    [ParentObjectID]              INT          NULL,
    [RelationshipOwnerObjectType] VARCHAR (25) NOT NULL,
    [RelationshipOwnerObjectID]   INT          NOT NULL,
    [FusionID]                    INT          NOT NULL,
    [UpdatedOn]                   DATETIME     NULL,
    [UpdatedBy]                   INT          NULL,
    CONSTRAINT [PK_FusionAttributeOwnerRule] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionAttributeOwnerRule_Fusion] FOREIGN KEY ([FusionID]) REFERENCES [dbo].[Fusion] ([ID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_FusionAttributeOwnerRule_FusionID]
    ON [dbo].[FusionAttributeOwnerRule]([FusionID] ASC);


GO
CREATE TRIGGER [dbo].[FusionAttributeOwnerRule_AfterDelete]
   ON  [dbo].[FusionAttributeOwnerRule] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	insert into [queue].ObjectVersion ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Fusion', FusionID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Removed', 'FusionAttributeOwnerRule', ID from deleted

GO
CREATE TRIGGER [dbo].[FusionAttributeOwnerRule_AfterInsert]
   ON  [dbo].[FusionAttributeOwnerRule] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into [queue].ObjectVersion ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Fusion', FusionID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'FusionAttributeOwnerRule', ID from inserted

GO
CREATE TRIGGER [dbo].[FusionAttributeOwnerRule_AfterUpdate]
   ON  [dbo].[FusionAttributeOwnerRule] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	insert into [queue].ObjectVersion ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Fusion', FusionID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'FusionAttributeOwnerRule', ID from inserted
