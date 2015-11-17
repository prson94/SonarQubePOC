CREATE TABLE [dbo].[FusionJobSchedule] (
    [FusionID]      INT         NOT NULL,
    [IncrementType] VARCHAR (1) NOT NULL,
    [Increment]     INT         NOT NULL,
    [Enabled]       BIT         CONSTRAINT [DF_FusionJobSchedule_Enabled] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_FusionJobSchedule] PRIMARY KEY CLUSTERED ([FusionID] ASC),
    CONSTRAINT [FK_FusionJobSchedule_Fusion] FOREIGN KEY ([FusionID]) REFERENCES [dbo].[Fusion] ([ID]) ON DELETE CASCADE
);

