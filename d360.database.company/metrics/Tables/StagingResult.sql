CREATE TABLE [metrics].[StagingResult] (
    [MapID]         BIGINT         NOT NULL,
    [EffectiveDate] DATE           NOT NULL,
    [Object]        VARCHAR (50)   NOT NULL,
    [ObjectID]      INT            NOT NULL,
    [Value]         BIT            NOT NULL,
    [Score]         DECIMAL (5, 3) NOT NULL,
    CONSTRAINT [PK_MetricStagingResult] PRIMARY KEY NONCLUSTERED ([MapID] ASC, [EffectiveDate] DESC, [Object] ASC, [ObjectID] ASC),
    CONSTRAINT [FK_StagingResult_Map] FOREIGN KEY ([MapID]) REFERENCES [metrics].[Map] ([ID]) ON DELETE CASCADE
);

