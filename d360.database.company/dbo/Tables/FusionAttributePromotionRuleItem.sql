CREATE TABLE [dbo].[FusionAttributePromotionRuleItem] (
    [ID]                             INT IDENTITY (1, 1) NOT NULL,
    [FusionAttributePromotionRuleID] INT NOT NULL,
    [FusionAttributeID]              INT NULL,
    CONSTRAINT [PK_FusionAttributePromotionRuleItem] PRIMARY KEY CLUSTERED ([ID] ASC)
);

