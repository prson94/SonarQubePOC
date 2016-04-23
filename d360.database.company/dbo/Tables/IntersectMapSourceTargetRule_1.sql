CREATE TABLE [dbo].[IntersectMapSourceTargetRule] (
    [ID]             INT IDENTITY (1, 1) NOT NULL,
    [RuleID]         INT NOT NULL,
    [IntersectMapID] INT NOT NULL,
    CONSTRAINT [PK_IntersectMapSourceTargetRule] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_IntersectMapSourceTargetRule_IntersectMap] FOREIGN KEY ([IntersectMapID]) REFERENCES [dbo].[IntersectMap] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_IntersectMapSourceTargetRule_SourceTargetRule] FOREIGN KEY ([RuleID]) REFERENCES [dbo].[SourceTargetRule] ([ID]) ON DELETE CASCADE
);



