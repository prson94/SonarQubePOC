CREATE TABLE [dbo].[MapRuleMap] (
    [MapRuleID] INT NOT NULL,
    [MapID]     INT NOT NULL,
    CONSTRAINT [PK_MapRuleMap] PRIMARY KEY CLUSTERED ([MapID] ASC, [MapRuleID] ASC),
    CONSTRAINT [FK_MapRuleMap_Map] FOREIGN KEY ([MapID]) REFERENCES [dbo].[Map] ([ID]),
    CONSTRAINT [FK_MapRuleMap_MapRule] FOREIGN KEY ([MapRuleID]) REFERENCES [dbo].[MapRule] ([ID])
);

