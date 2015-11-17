CREATE TABLE [queue].[BulkLoad] (
    [ID]              UNIQUEIDENTIFIER CONSTRAINT [DF_QueueBulkLoad_ID] DEFAULT (newid()) NOT NULL,
    [LoadID]          INT              NOT NULL,
    [MachineAssigned] VARCHAR (250)    NULL,
    [HasError]        BIT              CONSTRAINT [DF_QueueBulkLoad_HasError] DEFAULT ((0)) NULL,
    [ErrorMessage]    NVARCHAR (MAX)   NULL,
    [NumberOfRetries] INT              CONSTRAINT [DF_QueueBulkLoad_NumberOfRetries] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_QueueBulkLoad] PRIMARY KEY CLUSTERED ([ID] ASC)
);

