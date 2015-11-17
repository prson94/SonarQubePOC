CREATE TABLE [queue].[ObjectStyleCache] (
    [ID]              UNIQUEIDENTIFIER CONSTRAINT [DF_QueueObjectStyleCache] DEFAULT (newid()) NOT NULL,
    [Object]          VARCHAR (50)     NOT NULL,
    [ObjectID]        INT              NOT NULL,
    [ResourceID]      INT              NOT NULL,
    [Date]            DATETIME         NOT NULL,
    [Action]          VARCHAR (15)     NOT NULL,
    [MachineAssigned] VARCHAR (250)    NULL,
    [HasError]        BIT              CONSTRAINT [DF_QueueObjectStyleCache_HasError] DEFAULT ((0)) NULL,
    [ErrorMessage]    NVARCHAR (MAX)   NULL,
    [NumberOfRetries] INT              CONSTRAINT [DF_QueueObjectStyleCache_NumberOfRetries] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_QueueObjectStyleCache] PRIMARY KEY CLUSTERED ([ID] ASC)
);

