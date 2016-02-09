CREATE TABLE [fusion].[Execution] (
    [ID]                  INT              IDENTITY (1, 1) NOT NULL,
    [QueueID]             UNIQUEIDENTIFIER NULL,
    [FusionID]            INT              NOT NULL,
    [RawLogFileName]      NVARCHAR (500)   NULL,
    [DateStarted]         DATETIME         NULL,
    [DateCompleted]       DATETIME         NULL,
    [Adds]                INT              NULL,
    [Updates]             INT              NULL,
    [Deletes]             INT              NULL,
    [LoadIsNew]           BIT              CONSTRAINT [DF_FusionExecution_LoadIsNew] DEFAULT ((0)) NULL,
    [DateToUseForHistory] DATETIME         CONSTRAINT [DF_FusionExecution_DateToUseForHistory] DEFAULT (getutcdate()) NOT NULL,
	[Version]			  VARCHAR(250)	   NOT NULL DEFAULT ('unknown'),
    CONSTRAINT [PK_FusionExecution] PRIMARY KEY CLUSTERED ([ID] ASC)
);

