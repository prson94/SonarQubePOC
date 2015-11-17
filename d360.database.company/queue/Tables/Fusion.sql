CREATE TABLE [queue].[Fusion] (
    [ID]              UNIQUEIDENTIFIER CONSTRAINT [DF_QueueFusion_ID] DEFAULT (newid()) NOT NULL,
    [FusionID]        INT              NOT NULL,
    [Data]            XML              NOT NULL,
    [MachineAssigned] VARCHAR (250)    NULL,
    [HasError]        BIT              CONSTRAINT [DF_QueueFusion_HasError] DEFAULT ((0)) NULL,
    [ErrorMessage]    NVARCHAR (MAX)   NULL,
    [NumberOfRetries] INT              CONSTRAINT [DF_QueueFusion_NumberOfRetries] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_QueueFusion] PRIMARY KEY CLUSTERED ([ID] ASC)
);

