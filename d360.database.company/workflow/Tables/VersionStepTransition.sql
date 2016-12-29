CREATE TABLE [workflow].[VersionStepTransition] (
    [FromVersionStepID] INT            NOT NULL,
    [ToVersionStepID]   INT            NOT NULL,
    [Name]              NVARCHAR (500) NOT NULL,
    [TransitionType]    INT            NOT NULL,
    [Condition]         XML            NULL,
    [LinkType]          INT            NOT NULL,
    CONSTRAINT [PK_WorkflowVersionStepTransition] PRIMARY KEY CLUSTERED ([FromVersionStepID] ASC, [ToVersionStepID] ASC),
    CONSTRAINT [FK_WorkflowVersionStepTransition_FromVersionStep] FOREIGN KEY ([FromVersionStepID]) REFERENCES [workflow].[VersionStep] ([ID]),
    CONSTRAINT [FK_WorkflowVersionStepTransition_ToVersionStep] FOREIGN KEY ([ToVersionStepID]) REFERENCES [workflow].[VersionStep] ([ID])
);
GO

