CREATE TABLE [dbo].[WorkflowRuntime] (
    [RuntimeId] UNIQUEIDENTIFIER NOT NULL,
    [Timer]     NVARCHAR (MAX)   NOT NULL,
    CONSTRAINT [PK_WorkflowRuntime] PRIMARY KEY CLUSTERED ([RuntimeId] ASC)
);

