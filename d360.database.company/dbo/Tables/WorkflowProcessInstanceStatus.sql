CREATE TABLE [dbo].[WorkflowProcessInstanceStatus] (
    [Id]     UNIQUEIDENTIFIER NOT NULL,
    [Status] TINYINT          NOT NULL,
    [Lock]   UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT [PK_WorkflowProcessInstanceStatus] PRIMARY KEY CLUSTERED ([Id] ASC)
);

