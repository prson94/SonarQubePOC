CREATE TABLE [dbo].[AssetScheduleItem] (
    [ID]              BIGINT         IDENTITY (1, 1) NOT NULL,
    [AssetID]         BIGINT         NOT NULL,
    [DateGenerated]   DATETIME       NOT NULL,
    [DateStarted]     DATETIME       NULL,
    [DateCompleted]   DATETIME       NULL,
    [MachineQueuedOn] VARCHAR (250)  NULL,
    [Success]         BIT            CONSTRAINT [DF_AssetScheduleItem_Success] DEFAULT ((0)) NOT NULL,
    [Message]         NVARCHAR (MAX) NULL,
    [FullRefresh]     BIT            CONSTRAINT [DF_AssetScheduleItem_FullRefresh] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_AssetScheduleItem] PRIMARY KEY CLUSTERED ([ID] DESC)
);

