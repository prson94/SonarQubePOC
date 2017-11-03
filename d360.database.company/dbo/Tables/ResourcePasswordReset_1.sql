CREATE TABLE [dbo].[ResourcePasswordReset] (
    [ID]         UNIQUEIDENTIFIER DEFAULT (newid()) NOT NULL,
    [ResourceID] INT              NOT NULL,
    [CreateDate] DATETIME         NOT NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC)
);

