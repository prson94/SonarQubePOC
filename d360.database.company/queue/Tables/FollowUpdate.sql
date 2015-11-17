CREATE TABLE [queue].[FollowUpdate] (
    [ID]              UNIQUEIDENTIFIER CONSTRAINT [DF_FollowUpdate] DEFAULT (newid()) NOT NULL,
    [ObjectID]        INT              NOT NULL,
    [ObjectType]      VARCHAR (50)     NOT NULL,
    [MachineAssigned] VARCHAR (250)    NULL,
    [HasError]        BIT              CONSTRAINT [DF_FollowUpdate_HasError] DEFAULT ((0)) NULL,
    [ErrorMessage]    NVARCHAR (MAX)   NULL,
    [NumberOfRetries] INT              CONSTRAINT [DF_FollowUpdate_NumberOfRetries] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_FollowUpdate] PRIMARY KEY CLUSTERED ([ID] ASC)
);

