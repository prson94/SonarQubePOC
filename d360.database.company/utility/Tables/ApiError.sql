CREATE TABLE [utility].[ApiError] (
    [ID]         UNIQUEIDENTIFIER CONSTRAINT [DF_ApiError_ID] DEFAULT (newid()) NOT NULL,
    [ObjectType] VARCHAR (25)     NOT NULL,
    [ObjectID]   INT              NOT NULL,
    [Date]       DATETIME         NOT NULL,
    [Message]    NVARCHAR (MAX)   NULL,
    CONSTRAINT [PK_ApiError] PRIMARY KEY CLUSTERED ([ID] ASC)
);

