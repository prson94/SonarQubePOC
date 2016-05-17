CREATE TABLE [dbo].[FusionAttributePromotion] (
    [ID]                             INT          IDENTITY (1, 1) NOT NULL,
    [FusionAttributeID]              INT          NOT NULL,
    [ObjectType]                     VARCHAR (25) NOT NULL,
    [ObjectID]                       INT          NOT NULL,
    [FusionAttributePromotionRuleID] INT          NOT NULL,
    [Step]                           INT          NULL,
    [ObjectTypeID]                   INT          CONSTRAINT [DF__FusionAtt__Objec__2A0CEAEA] DEFAULT ((-1)) NOT NULL,
    CONSTRAINT [PK_FusionAttributePromotion_1] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionAttributePromotion_FusionAttribute] FOREIGN KEY ([FusionAttributeID]) REFERENCES [dbo].[FusionAttribute] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_FusionAttributePromotion_FusionAttributePromotionRule] FOREIGN KEY ([FusionAttributePromotionRuleID]) REFERENCES [dbo].[FusionAttributePromotionRule] ([ID]) ON DELETE CASCADE
);


