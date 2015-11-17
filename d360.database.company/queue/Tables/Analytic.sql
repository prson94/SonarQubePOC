CREATE TABLE [queue].[Analytic] (
    [ID]              UNIQUEIDENTIFIER CONSTRAINT [DF_QueueAnalytic] DEFAULT (newid()) NOT NULL,
    [MachineAssigned] VARCHAR (250)    NULL,
    [HasError]        BIT              CONSTRAINT [DF_QueueAnalytic_HasError] DEFAULT ((0)) NULL,
    [ErrorMessage]    NVARCHAR (MAX)   NULL,
    [NumberOfRetries] INT              CONSTRAINT [DF_QueueAnalytic_NumberOfRetries] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_QueueAnalytic] PRIMARY KEY CLUSTERED ([ID] ASC)
);

