CREATE TABLE [fusion].[RulePromotion] (
    [ID]                INT          IDENTITY (1, 1) NOT NULL,
    [FusionAttributeID] INT          NOT NULL,
    [ObjectType]        VARCHAR (25) NOT NULL,
    [ObjectID]          INT          NOT NULL,
    [RuleID]            INT          NOT NULL,
    [RuleStepID]        INT          NULL,
    [ObjectTypeID]      INT          DEFAULT ((-1)) NOT NULL,
    CONSTRAINT [PK_FusionRulePromotion] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionRulePromotion_FusionAttribute] FOREIGN KEY ([FusionAttributeID]) REFERENCES [dbo].[FusionAttribute] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_FusionRulePromotion_FusionRule] FOREIGN KEY ([RuleID]) REFERENCES [fusion].[Rule] ([ID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_FusionRulePromotion_FusionAttribute_Rule_RuleStep_Object]
    ON [fusion].[RulePromotion]([FusionAttributeID] ASC, [RuleID] ASC, [RuleStepID] ASC, [ObjectID] ASC, [ObjectType] ASC);

