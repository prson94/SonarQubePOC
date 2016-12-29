CREATE TABLE [workflow].[VersionStep] (
    [ID]           INT            IDENTITY (1, 1) NOT NULL,
    [ParentID]     INT            NULL,
    [VersionID]    INT            NOT NULL,
    [Name]         NVARCHAR (500) NOT NULL,
    [StepType]     INT            NOT NULL,
    [ActivityType] INT            NOT NULL,
    [Settings]     XML            NULL,
    [Fields]       XML            NULL,
    [XPosition]    INT            NOT NULL,
    [YPosition]    INT            NOT NULL,
    CONSTRAINT [PK_WorkflowVersionStep] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_WorkflowVersionStep_Parent] FOREIGN KEY ([ParentID]) REFERENCES [workflow].[VersionStep] ([ID]),
    CONSTRAINT [FK_WorkflowVersionStep_WorkflowVersion] FOREIGN KEY ([VersionID]) REFERENCES [workflow].[Version] ([ID]) ON DELETE CASCADE
);
GO

