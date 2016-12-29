CREATE TABLE [workflow].[ItemStep] (
    [ID]          BIGINT   IDENTITY (1, 1) NOT NULL,
    [ItemID]      BIGINT   NOT NULL,
    [StepID]      INT      NOT NULL,
    [Settings]    XML      NULL,
    [Fields]      XML      NULL,
    [StartedBy]   INT      NOT NULL,
    [StartedOn]   DATETIME NOT NULL,
    [CompletedBy] INT      NULL,
    [CompletedOn] DATETIME NULL,
    CONSTRAINT [PK_WorkflowItemStep] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_WorkflowItemStep_WorkflowItem] FOREIGN KEY ([ItemID]) REFERENCES [workflow].[Item] ([ID]),
    CONSTRAINT [FK_WorkflowItemStep_WorkflowVersionStep] FOREIGN KEY ([StepID]) REFERENCES [workflow].[VersionStep] ([ID]) ON DELETE CASCADE
);
GO

