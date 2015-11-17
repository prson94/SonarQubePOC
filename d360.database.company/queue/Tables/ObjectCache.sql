CREATE TABLE [queue].[ObjectCache] (
    [ID]              UNIQUEIDENTIFIER CONSTRAINT [DF_QueueObjectCache] DEFAULT (newid()) NOT NULL,
    [Object]          VARCHAR (50)     NOT NULL,
    [ObjectID]        INT              NOT NULL,
    [MachineAssigned] VARCHAR (250)    NULL,
    [HasError]        BIT              CONSTRAINT [DF_QueueObjectCache_HasError] DEFAULT ((0)) NULL,
    [ErrorMessage]    NVARCHAR (MAX)   NULL,
    [NumberOfRetries] INT              CONSTRAINT [DF_QueueObjectCache_NumberOfRetries] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_QueueObjectCache] PRIMARY KEY CLUSTERED ([ID] ASC)
);

