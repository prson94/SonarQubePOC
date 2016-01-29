CREATE TABLE [dbo].[IntersectMapSourceRuleContext] (
    [IntersectMapSourceRuleID] INT          NOT NULL,
    [Object]                   VARCHAR (50) NOT NULL,
    [ObjectID]                 INT          NOT NULL,
    CONSTRAINT [PK_IntersectMapSourceRuleContext] PRIMARY KEY CLUSTERED ([IntersectMapSourceRuleID] ASC, [Object] ASC, [ObjectID] ASC),
    CONSTRAINT [FK_IntersectMapSourceRuleContext_IntersectMapSourceRule] FOREIGN KEY ([IntersectMapSourceRuleID]) REFERENCES [dbo].[IntersectMapSourceRule] ([ID])
);

