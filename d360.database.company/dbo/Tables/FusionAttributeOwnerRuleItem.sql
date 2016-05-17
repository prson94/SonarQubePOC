CREATE TABLE [dbo].[FusionAttributeOwnerRuleItem] (
    [ID]                         INT IDENTITY (1, 1) NOT NULL,
    [FusionAttributeOwnerRuleID] INT NOT NULL,
    [FusionAttributeID]          INT NULL,
    CONSTRAINT [PK_FusionAttributeOwnerRuleItem] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionAttributeOwnerRuleItem_FusionAttributeOwnerRule] FOREIGN KEY ([FusionAttributeOwnerRuleID]) REFERENCES [dbo].[FusionAttributeOwnerRule] ([ID]) ON DELETE CASCADE
);




GO
CREATE CLUSTERED INDEX [CIX_FusionAttributeOwnerRuleItem]
    ON [dbo].[FusionAttributeOwnerRuleItem]([FusionAttributeOwnerRuleID] ASC);

