CREATE TABLE [dbo].[MapRuleItem] (
    [ID]                      INT IDENTITY (1, 1) NOT NULL,
    [MapRuleID]               INT NOT NULL,
    [SourceFusionAttributeID] INT NOT NULL,
    [TargetFusionAttributeID] INT NOT NULL,
    CONSTRAINT [PK_MapRuleItem] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_MapRuleItem_MapRule] FOREIGN KEY ([MapRuleID]) REFERENCES [dbo].[MapRule] ([ID]) ON DELETE CASCADE
);

