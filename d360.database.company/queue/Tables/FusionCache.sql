CREATE TABLE [queue].[FusionCache] (
    [ID]              UNIQUEIDENTIFIER CONSTRAINT [DF_QueueFusionCache_ID] DEFAULT (newid()) NOT NULL,
    [FusionID]        INT              NOT NULL,
    [MachineAssigned] VARCHAR (250)    NULL,
    [HasError]        BIT              CONSTRAINT [DF_QueueFusionCache_HasError] DEFAULT ((0)) NULL,
    [ErrorMessage]    NVARCHAR (MAX)   NULL,
    [NumberOfRetries] INT              CONSTRAINT [DF_QueueFusionCache_NumberOfRetries] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_QueueFusionCache] PRIMARY KEY CLUSTERED ([ID] ASC)
);

