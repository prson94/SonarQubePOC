CREATE TABLE [fusion].[StagingStatistic] (
    [QueueID]  UNIQUEIDENTIFIER NOT NULL,
    [Step]     INT              NOT NULL,
    [Duration] INT              NOT NULL,
    CONSTRAINT [PK_FusionStagingStatistic] PRIMARY KEY CLUSTERED ([QueueID] ASC, [Step] ASC)
);

