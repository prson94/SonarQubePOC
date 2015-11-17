CREATE TABLE [queue].[Notification] (
    [ID]               UNIQUEIDENTIFIER CONSTRAINT [DF_Notification] DEFAULT (newid()) NOT NULL,
    [NotificationType] INT              NOT NULL,
    [Object]           VARCHAR (50)     NOT NULL,
    [ObjectID]         INT              NOT NULL,
    [MachineAssigned]  VARCHAR (250)    NULL,
    [HasError]         BIT              CONSTRAINT [DF_Notification_HasError] DEFAULT ((0)) NULL,
    [ErrorMessage]     NVARCHAR (MAX)   NULL,
    [NumberOfRetries]  INT              CONSTRAINT [DF_QueueNotification_NumberOfRetries] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_QueueNotification] PRIMARY KEY CLUSTERED ([ID] ASC)
);

