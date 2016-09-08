CREATE TABLE [quality].[RuleMap] (
    [QualityRuleID] INT          NOT NULL,
    [SourceID]      VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_QualityRuleMap] PRIMARY KEY CLUSTERED ([QualityRuleID] ASC, [SourceID] ASC),
    CONSTRAINT [FK_QualityRuleMap_QualityRule] FOREIGN KEY ([QualityRuleID]) REFERENCES [quality].[Rule] ([ID])
);

