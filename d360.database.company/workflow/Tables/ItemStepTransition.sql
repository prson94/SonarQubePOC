CREATE TABLE [workflow].[ItemStepTransition] (
    [FromItemStepID] BIGINT   NOT NULL,
    [ToItemStepID]   BIGINT   NOT NULL,
    [Condition]      XML      NULL,
    [Date]           DATETIME NOT NULL,
    CONSTRAINT [PK_WorkflowItemStepTransition] PRIMARY KEY CLUSTERED ([FromItemStepID] ASC, [ToItemStepID] ASC),
    CONSTRAINT [FK_WorkflowItemStepTransition_FromItemStep] FOREIGN KEY ([FromItemStepID]) REFERENCES [workflow].[ItemStep] ([ID]),
    CONSTRAINT [FK_WorkflowItemStepTransition_ToItemStep] FOREIGN KEY ([ToItemStepID]) REFERENCES [workflow].[ItemStep] ([ID])
);
GO

