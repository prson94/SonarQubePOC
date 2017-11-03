CREATE TABLE [dbo].[ScoreMetric] (
    [ScoreID]                  BIGINT         NOT NULL,
    [ScoreTypeMetricVersionID] INT            NOT NULL,
    [Value]                    DECIMAL (6, 3) NOT NULL,
    CONSTRAINT [PK_ScoreMetric] PRIMARY KEY CLUSTERED ([ScoreID] ASC, [ScoreTypeMetricVersionID] ASC),
    CONSTRAINT [FK_ScoreMetric_Score] FOREIGN KEY ([ScoreID]) REFERENCES [dbo].[Score] ([ID]),
    CONSTRAINT [FK_ScoreMetric_ScoreTypeMetricVersion] FOREIGN KEY ([ScoreTypeMetricVersionID]) REFERENCES [dbo].[ScoreTypeMetricVersion] ([ID])
);

