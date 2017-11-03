CREATE TABLE [dbo].[RuleResult] (
    [ID]                   INT      IDENTITY (1, 1) NOT NULL,
    [EffectiveDate]        DATETIME NOT NULL,
    [RowsPassed]           INT      NOT NULL,
    [RowsFailed]           INT      NOT NULL,
    [CreatedOn]            DATETIME CONSTRAINT [DF_RuleResult_CreatedOn] DEFAULT (getutcdate()) NULL,
    [CreatedBy]            INT      CONSTRAINT [DF_RuleResult_CreatedBy] DEFAULT ((0)) NULL,
    [RunDate]              DATETIME CONSTRAINT [DF_RuleResult_RunDate] DEFAULT (getutcdate()) NOT NULL,
    [RuleImplementationID] INT      CONSTRAINT [DF_RuleResult_RuleImplementationID] DEFAULT ((0)) NOT NULL,
    [PassFraction]         AS       (case when [RowsPassed]=(0) AND [RowsFailed]=(0) then (0) else CONVERT([decimal](4,3),CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)) end),
    [FailFraction]         AS       (case when [RowsPassed]=(0) AND [RowsFailed]=(0) then (0) when [RowsPassed]=(0) AND [RowsFailed]<>(0) then (1) else CONVERT([decimal](4,3),CONVERT([decimal](3,3),CONVERT([decimal](18,3),(1),(0))-CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)),(0)) end),
    [Passed]               AS       ([utility].[CalculatePassedWrapper](case when [RowsPassed]=(0) AND [RowsFailed]=(0) then (0) else CONVERT([decimal](4,3),CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)) end,[RuleImplementationID])),
    CONSTRAINT [PK_RuleResult] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_RuleResult_RuleImplementation] FOREIGN KEY ([RuleImplementationID]) REFERENCES [dbo].[RuleImplementation] ([ID])
);





