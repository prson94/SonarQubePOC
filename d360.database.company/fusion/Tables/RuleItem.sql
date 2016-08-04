CREATE TABLE [fusion].[RuleItem] (
    [ID]                INT IDENTITY (1, 1) NOT NULL,
    [RuleID]            INT NULL,
    [FusionAttributeID] INT NULL,
    CONSTRAINT [PK_RuleItem] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionRuleItem_FusionRule] FOREIGN KEY ([RuleID]) REFERENCES [fusion].[Rule] ([ID])
);

