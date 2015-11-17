CREATE TABLE [dbo].[WorkflowResourceNotification] (
    [WorkflowID] UNIQUEIDENTIFIER NOT NULL,
    [Activity]   SMALLINT         NOT NULL,
    [ResourceID] INT              NOT NULL,
    [Date]       DATETIME         NOT NULL,
    CONSTRAINT [PK_WorkflowResourceNotification] PRIMARY KEY CLUSTERED ([WorkflowID] ASC, [Activity] ASC, [ResourceID] ASC, [Date] ASC)
);

