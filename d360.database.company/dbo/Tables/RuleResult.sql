CREATE TABLE [dbo].[RuleResult] (
    [ID]                INT      IDENTITY (1, 1) NOT NULL,
    [RuleID]            INT      NULL,
    [EffectiveDate]     DATETIME NOT NULL,
    [RowsPassed]        INT      NOT NULL,
    [RowsFailed]        INT      NOT NULL,
    [PassFraction]      AS       (CONVERT([decimal](4,3),CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0))),
    [FailFraction]      AS       (CONVERT([decimal](4,3),CONVERT([decimal](3,3),CONVERT([decimal](18,3),(1),(0))-CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)),(0))),
    [Passed]            AS       ([utility].[CalculatePassedWrapper](CONVERT([decimal](4,3),CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)),[RuleID])),
    [CreatedOn]         DATETIME CONSTRAINT [DF_RuleResult_CreatedOn] DEFAULT (getutcdate()) NULL,
    [CreatedBy]         INT      CONSTRAINT [DF_RuleResult_CreatedBy] DEFAULT ((0)) NULL,
    [FusionAttributeID] INT      NULL,
    CONSTRAINT [PK_RuleResult] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_RuleResult_FusionAttribute] FOREIGN KEY ([FusionAttributeID]) REFERENCES [dbo].[FusionAttribute] ([ID]),
    CONSTRAINT [FK_RuleResult_Rule] FOREIGN KEY ([RuleID]) REFERENCES [dbo].[Rule] ([ID])
);

