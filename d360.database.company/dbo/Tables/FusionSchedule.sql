CREATE TABLE [dbo].[FusionSchedule] (
    [FusionID]    INT      NOT NULL,
    [Day]         INT      NOT NULL,
    [Time]        TIME (7) NOT NULL,
    [FullRefresh] BIT      CONSTRAINT [DF_FusionSchedule_FullRefresh] DEFAULT ((0)) NOT NULL,
    [CreatedOn]   DATETIME NULL,
    [CreatedBy]   INT      NULL,
    [UpdatedOn]   DATETIME NULL,
    [UpdatedBy]   INT      NULL,
    [ID]          INT      IDENTITY (1, 1) NOT NULL,
    CONSTRAINT [PK_FusionSchedule] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionSchedule_Fusion] FOREIGN KEY ([FusionID]) REFERENCES [dbo].[Fusion] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [Con_FusionScheduleUniqueFusionIDDayTime] UNIQUE NONCLUSTERED ([FusionID] ASC, [Day] ASC, [Time] ASC)
);

