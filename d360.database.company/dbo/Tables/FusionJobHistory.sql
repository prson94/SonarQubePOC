CREATE TABLE [dbo].[FusionJobHistory] (
    [ID]                   INT            IDENTITY (1, 1) NOT NULL,
    [FusionID]             INT            NOT NULL,
    [Date]                 DATETIME       NOT NULL,
    [PercentComplete]      INT            CONSTRAINT [DF_FusionJobHistory_PercentComplete] DEFAULT ((0)) NOT NULL,
    [CurrentStatusMessage] NVARCHAR (500) NULL,
    CONSTRAINT [PK_FusionJobHistory] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionJobHistory_Fusion] FOREIGN KEY ([FusionID]) REFERENCES [dbo].[Fusion] ([ID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_FusionJobHistory_FusionID-Date]
    ON [dbo].[FusionJobHistory]([FusionID] ASC, [Date] DESC);

