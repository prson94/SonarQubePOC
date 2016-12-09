CREATE TABLE [dbo].[WorkflowProcessInstancePersistence] (
    [Id]            UNIQUEIDENTIFIER NOT NULL,
    [ProcessId]     UNIQUEIDENTIFIER NOT NULL,
    [ParameterName] NVARCHAR (MAX)   NOT NULL,
    [Value]         NTEXT            NOT NULL,
    CONSTRAINT [PK_WorkflowProcessInstancePersistence] PRIMARY KEY CLUSTERED ([Id] ASC)
);

