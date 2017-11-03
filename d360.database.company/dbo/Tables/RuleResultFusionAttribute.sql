CREATE TABLE [dbo].[RuleResultFusionAttribute] (
    [ID]                BIGINT          IDENTITY (1, 1) NOT NULL,
    [RuleResultID]      INT             NOT NULL,
    [FusionAttribute]   NVARCHAR (2500) NOT NULL,
    [FusionAttributeID] INT             NULL,
    CONSTRAINT [PK_RuleResultFusionAttribute] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_RuleResultFusionAttribute_RuleResult] FOREIGN KEY ([RuleResultID]) REFERENCES [dbo].[RuleResult] ([ID])
);

