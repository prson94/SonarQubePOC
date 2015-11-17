CREATE TABLE [dbo].[WorkflowResource] (
    [WorkflowID] UNIQUEIDENTIFIER NOT NULL,
    [Activity]   SMALLINT         NOT NULL,
    [ResourceID] INT              NOT NULL,
    [IsComplete] BIT              NOT NULL,
    [ID]         UNIQUEIDENTIFIER CONSTRAINT [DF_WorkflowResource_ID] DEFAULT (newid()) NOT NULL,
    CONSTRAINT [PK_WorkflowResource] PRIMARY KEY CLUSTERED ([ID] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_WorkflowResource_WorkflowID]
    ON [dbo].[WorkflowResource]([WorkflowID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_WorkflowResource_ResourceID]
    ON [dbo].[WorkflowResource]([ResourceID] ASC);

