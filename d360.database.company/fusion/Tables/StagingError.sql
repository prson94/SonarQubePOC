CREATE TABLE [fusion].[StagingError] (
    [QueueID] UNIQUEIDENTIFIER NOT NULL,
    [Date]    DATETIME         NOT NULL,
    [Error]   NVARCHAR (MAX)   NULL,
    CONSTRAINT [PK_FusionStagingError] PRIMARY KEY CLUSTERED ([QueueID] ASC, [Date] ASC)
);

