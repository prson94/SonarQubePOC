CREATE TABLE [dbo].[SourceRuleContext] (
    [SourceRuleID] INT          NOT NULL,
    [Object]       VARCHAR (50) NOT NULL,
    [ObjectID]     INT          NOT NULL,
    CONSTRAINT [PK_SourceRuleContext] PRIMARY KEY CLUSTERED ([SourceRuleID] ASC, [Object] ASC, [ObjectID] ASC),
    CONSTRAINT [FK_SourceRuleContext_SourceRule] FOREIGN KEY ([SourceRuleID]) REFERENCES [dbo].[SourceRule] ([ID])
);

