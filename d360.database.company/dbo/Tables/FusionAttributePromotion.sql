CREATE TABLE [dbo].[FusionAttributePromotion] (
    [FusionAttributeID]              INT          NOT NULL,
    [ObjectType]                     VARCHAR (25) NOT NULL,
    [ObjectID]                       INT          NOT NULL,
    [FusionAttributePromotionRuleID] INT          NULL,
    CONSTRAINT [PK_FusionAttributePromotion] PRIMARY KEY CLUSTERED ([FusionAttributeID] ASC, [ObjectType] ASC, [ObjectID] ASC),
    CONSTRAINT [FK_FusionAttributePromotion_FusionAttribute] FOREIGN KEY ([FusionAttributeID]) REFERENCES [dbo].[FusionAttribute] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_FusionAttributePromotion_FusionAttributePromotionRule] FOREIGN KEY ([FusionAttributePromotionRuleID]) REFERENCES [dbo].[FusionAttributePromotionRule] ([ID]) ON DELETE CASCADE
);

