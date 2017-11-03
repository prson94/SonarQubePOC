CREATE TABLE [metrics].[MapResult] (
    [MapID]   BIGINT NOT NULL,
    [ScoreID] BIGINT NOT NULL,
    [Value]   BIT    NOT NULL,
    CONSTRAINT [PK_MetricMapResult] PRIMARY KEY NONCLUSTERED ([MapID] ASC, [ScoreID] ASC),
    CONSTRAINT [FK_MetricMapResult_MetricMap] FOREIGN KEY ([MapID]) REFERENCES [metrics].[Map] ([ID]),
    CONSTRAINT [FK_MetricMapResult_Score] FOREIGN KEY ([ScoreID]) REFERENCES [metrics].[Score] ([ID])
);

