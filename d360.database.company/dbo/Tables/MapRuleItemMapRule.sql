CREATE TABLE [dbo].[MapRuleItemMapRule] (
    [MapRuleID]     INT NOT NULL,
    [MapRuleItemID] INT NOT NULL,
    CONSTRAINT [PK_MapRuleItemMapRule] PRIMARY KEY CLUSTERED ([MapRuleID] ASC, [MapRuleItemID] ASC),
    CONSTRAINT [FK_MapRuleItemMap_MapRule] FOREIGN KEY ([MapRuleID]) REFERENCES [dbo].[MapRule] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_MapRuleItemMap_MapRuleItem] FOREIGN KEY ([MapRuleItemID]) REFERENCES [dbo].[MapRuleItem] ([ID])
);

