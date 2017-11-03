CREATE TABLE [fusion].[RuleFilter] (
    [ID]     INT            IDENTITY (1, 1) NOT NULL,
    [RuleID] INT            NULL,
    [Name]   NVARCHAR (250) NOT NULL,
    [Fields] XML            NULL,
    [Sql]    NVARCHAR (MAX) NULL,
    [All]    BIT            CONSTRAINT [DF_FusionRuleFilter_All] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_RuleFilter] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionRuleFilter_FusionRule] FOREIGN KEY ([RuleID]) REFERENCES [fusion].[Rule] ([ID]) ON DELETE CASCADE
);

