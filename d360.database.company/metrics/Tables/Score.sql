CREATE TABLE [metrics].[Score] (
    [ID]                 BIGINT         IDENTITY (1, 1) NOT NULL,
    [Object]             VARCHAR (50)   NULL,
    [ObjectID]           INT            NULL,
    [EffectiveStartDate] DATE           CONSTRAINT [DF_MetricScore_EffectiveStartDate] DEFAULT (getutcdate()) NOT NULL,
    [EffectiveEndDate]   DATE           NULL,
    [Value]              DECIMAL (5, 3) NOT NULL,
    CONSTRAINT [PK_MetricScore] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);

