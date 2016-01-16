CREATE TABLE [queue].[Task] (
    [ID]              UNIQUEIDENTIFIER CONSTRAINT [DF_QueueTask_ID] DEFAULT (newid()) NOT NULL,
    [Action]          VARCHAR (50)     NOT NULL,
    [Custom]          VARCHAR (500)    NULL,
    [Object]          VARCHAR (50)     NOT NULL,
    [ObjectID]        INT              NOT NULL,
    [Date]            DATETIME         CONSTRAINT [DF_QueueTask_Date] DEFAULT (getutcdate()) NOT NULL,
    [MachineAssigned] VARCHAR (250)    NULL,
    [HasError]        BIT              CONSTRAINT [DF_QueueTask_HasError] DEFAULT ((0)) NOT NULL,
    [ErrorMessage]    NVARCHAR (MAX)   NULL,
    [NumberOfRetries] INT              CONSTRAINT [DF_QueueTask_NumberOfRetries] DEFAULT ((0)) NOT NULL,
    [Priority]        SMALLINT         CONSTRAINT [DF_QueueTask_Priority] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_QueueTask] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);


GO
CREATE CLUSTERED INDEX [CIX_QueueTask] ON [queue].[Task] ( [Date] ASC )
GO