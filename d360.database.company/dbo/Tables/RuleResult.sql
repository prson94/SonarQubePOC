CREATE TABLE [dbo].[RuleResult] (
    [ID]                INT      IDENTITY (1, 1) NOT NULL,
    [RuleID]            INT      NULL,
    [EffectiveDate]     DATETIME NOT NULL,
    [RowsPassed]        INT      NOT NULL,
    [RowsFailed]        INT      NOT NULL,
    [PassFraction]      AS       (case when [RowsPassed]=(0) AND [RowsFailed]=(0) then (0) else CONVERT([decimal](4,3),CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)) end),
    [FailFraction]      AS       (case when [RowsPassed]=(0) AND [RowsFailed]=(0) then (0) else CONVERT([decimal](4,3),CONVERT([decimal](3,3),CONVERT([decimal](18,3),(1),(0))-CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)),(0)) end),
    [Passed]            AS       ([utility].[CalculatePassedWrapper](case when [RowsPassed]=(0) AND [RowsFailed]=(0) then (0) else CONVERT([decimal](4,3),CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)) end,[RuleID])),
    [CreatedOn]         DATETIME CONSTRAINT [DF_RuleResult_CreatedOn] DEFAULT (getutcdate()) NULL,
    [CreatedBy]         INT      CONSTRAINT [DF_RuleResult_CreatedBy] DEFAULT ((0)) NULL,
    [FusionAttributeID] INT      NULL,
    [RunDate]           DATETIME CONSTRAINT [DF_RuleResult_RunDate] DEFAULT (getutcdate()) NOT NULL,
    CONSTRAINT [PK_RuleResult] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_RuleResult_FusionAttribute] FOREIGN KEY ([FusionAttributeID]) REFERENCES [dbo].[FusionAttribute] ([ID]),
    CONSTRAINT [FK_RuleResult_Rule] FOREIGN KEY ([RuleID]) REFERENCES [dbo].[Rule] ([ID])
);



