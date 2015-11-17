CREATE TABLE [dbo].[FusionAttributeSynchronizationStatus] (
    [ID]      UNIQUEIDENTIFIER CONSTRAINT [DF_FusionAttributeSynchronizationStatus_ID] DEFAULT (newid()) NOT NULL,
    [QueueID] UNIQUEIDENTIFIER NULL,
    [Error]   NVARCHAR (MAX)   NULL,
    CONSTRAINT [PK_FusionAttributeSynchronizationStatus] PRIMARY KEY CLUSTERED ([ID] ASC)
);

