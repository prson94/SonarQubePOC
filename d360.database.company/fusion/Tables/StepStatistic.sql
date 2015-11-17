CREATE TABLE [fusion].[StepStatistic] (
    [ExecutionID] INT NOT NULL,
    [Step]        INT NOT NULL,
    [Duration]    INT NOT NULL,
    CONSTRAINT [PK_FusionStepStatistic] PRIMARY KEY CLUSTERED ([ExecutionID] DESC, [Step] ASC)
);

