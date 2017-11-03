CREATE TABLE [dbo].[AssetSchedule] (
    [ID]          INT      IDENTITY (1, 1) NOT NULL,
    [AssetID]     BIGINT   NOT NULL,
    [Day]         INT      NOT NULL,
    [Time]        TIME (7) NOT NULL,
    [FullRefresh] BIT      CONSTRAINT [DF_AssetSchedule_FullRefresh] DEFAULT ((0)) NOT NULL,
    [CreatedOn]   DATETIME NULL,
    [CreatedBy]   INT      NULL,
    [UpdatedOn]   DATETIME NULL,
    [UpdatedBy]   INT      NULL,
    CONSTRAINT [PK_AssetSchedule] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);

