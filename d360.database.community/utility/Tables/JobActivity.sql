CREATE TABLE [utility].[JobActivity] (
    [ID]          UNIQUEIDENTIFIER NOT NULL,
    [Name]        NVARCHAR (250)   NOT NULL,
    [DateStarted] DATETIME         NOT NULL,
    [DateStopped] DATETIME         NULL,
    [Status]      VARCHAR (8000)   NULL,
    CONSTRAINT [PK_JobActivity] PRIMARY KEY CLUSTERED ([ID] ASC)
);

