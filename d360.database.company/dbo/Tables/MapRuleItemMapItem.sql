CREATE TABLE [dbo].[MapRuleItemMapItem] (
    [MapRuleItemID] INT NOT NULL,
    [MapItemID]     INT NOT NULL,
    CONSTRAINT [PK_MapRuleItemMapItem] PRIMARY KEY CLUSTERED ([MapRuleItemID] ASC, [MapItemID] ASC)
);

