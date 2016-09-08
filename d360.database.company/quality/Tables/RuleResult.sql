CREATE TABLE [quality].[RuleResult] (
    [ID]                INT      IDENTITY (1, 1) NOT NULL,
    [QualityRuleID]     INT      NULL,
    [EffectiveDate]     DATETIME NOT NULL,
    [RowsPassed]        INT      NOT NULL,
    [RowsFailed]        INT      NOT NULL,
    [PassFraction]      AS       (CONVERT([decimal](4,3),CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0))),
    [FailFraction]      AS       (CONVERT([decimal](4,3),CONVERT([decimal](3,3),CONVERT([decimal](18,3),(1),(0))-CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)),(0))),
    [Passed]            AS       ([quality].[CalculatePassedWrapper](CONVERT([decimal](4,3),CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)),[QualityRuleID])),
    [CreatedOn]         DATETIME CONSTRAINT [DF__RuleResul__Creat__5CF85D6A] DEFAULT (getutcdate()) NULL,
    [CreatedBy]         INT      NULL,
    [FusionAttributeID] INT      NULL,
    CONSTRAINT [PK_QualityRuleResult] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_QualityRuleResult_FusionAttribute] FOREIGN KEY ([FusionAttributeID]) REFERENCES [dbo].[FusionAttribute] ([ID]),
    CONSTRAINT [FK_QualityRuleResult_QualityRule] FOREIGN KEY ([QualityRuleID]) REFERENCES [quality].[Rule] ([ID])
);
GO

