CREATE TABLE [queue].[ObjectVersion] (
    [ID]              UNIQUEIDENTIFIER CONSTRAINT [DF_QueueObjectVersion] DEFAULT (newid()) NOT NULL,
    [Object]          VARCHAR (50)     NOT NULL,
    [ObjectID]        INT              NOT NULL,
    [ResourceID]      INT              NOT NULL,
    [Date]            DATETIME         NOT NULL,
    [Action]          VARCHAR (15)     NOT NULL,
    [ActionObject]    VARCHAR (50)     NOT NULL,
    [ActionObjectID]  INT              NOT NULL,
    [MachineAssigned] VARCHAR (250)    NULL,
    [HasError]        BIT              CONSTRAINT [DF_QueueObjectVersion_HasError] DEFAULT ((0)) NULL,
    [ErrorMessage]    NVARCHAR (MAX)   NULL,
    [NumberOfRetries] INT              CONSTRAINT [DF_QueueObjectVersion_NumberOfRetries] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_QueueObjectVersion] PRIMARY KEY CLUSTERED ([ID] ASC)
);

