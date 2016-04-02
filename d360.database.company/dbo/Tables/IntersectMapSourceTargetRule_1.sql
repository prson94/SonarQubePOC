CREATE TABLE [dbo].[IntersectMapSourceTargetRule] (
    [ID]             INT IDENTITY (1, 1) NOT NULL,
    [RuleID]         INT NOT NULL,
    [IntersectMapID] INT NOT NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_IntersectMap_ID] FOREIGN KEY ([IntersectMapID]) REFERENCES [dbo].[IntersectMap] ([ID]),
    CONSTRAINT [FK_SourceTargetRule_ID] FOREIGN KEY ([RuleID]) REFERENCES [dbo].[SourceTargetRule] ([ID])
);

