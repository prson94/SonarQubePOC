CREATE TABLE [dbo].[WorkflowProcessTimer] (
    [Id]                    UNIQUEIDENTIFIER NOT NULL,
    [ProcessId]             UNIQUEIDENTIFIER NOT NULL,
    [Name]                  NVARCHAR (MAX)   NOT NULL,
    [NextExecutionDateTime] DATETIME         NOT NULL,
    [Ignore]                BIT              NOT NULL,
    CONSTRAINT [PK_WorkflowProcessTimer] PRIMARY KEY CLUSTERED ([Id] ASC)
);

