CREATE TABLE [dbo].[FusionStatusLog] (
    [ID]              UNIQUEIDENTIFIER NOT NULL,
    [FusionID]        INT              NOT NULL,
    [DateStarted]     DATETIME         NOT NULL,
    [DateCompleted]   DATETIME         NULL,
    [MachineQueuedOn] VARCHAR (250)    NULL,
    [Success]         BIT              NOT NULL,
    [Message]         NVARCHAR (MAX)   NULL,
    CONSTRAINT [PK_FusionStatusLog] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionStatusLog_Fusion] FOREIGN KEY ([FusionID]) REFERENCES [dbo].[Fusion] ([ID]) ON DELETE CASCADE
);


GO
CREATE CLUSTERED INDEX [CIX_FusionStatusLog]
    ON [dbo].[FusionStatusLog]([FusionID] ASC, [DateCompleted] DESC);

