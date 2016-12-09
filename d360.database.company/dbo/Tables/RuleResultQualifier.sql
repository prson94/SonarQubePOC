CREATE TABLE [dbo].[RuleResultQualifier] (
    [RuleResultID]              INT             NOT NULL,
    [RuleResultQualifierTypeID] INT             NOT NULL,
    [Value]                     NVARCHAR (1000) NULL,
    [ResolvedObject]            VARCHAR (50)    NULL,
    [ResolvedObjectID]          INT             NULL,
    CONSTRAINT [PK_RuleResultQualifier] PRIMARY KEY NONCLUSTERED ([RuleResultID] ASC, [RuleResultQualifierTypeID] ASC),
    CONSTRAINT [FK_RuleResultQualifier_RuleResult] FOREIGN KEY ([RuleResultID]) REFERENCES [dbo].[RuleResult] ([ID]),
    CONSTRAINT [FK_RuleResultQualifier_RuleResultQualifierType] FOREIGN KEY ([RuleResultQualifierTypeID]) REFERENCES [dbo].[RuleResultQualifierType] ([ID])
);

