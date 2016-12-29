CREATE TABLE [workflow].[Item] (
    [ID]          BIGINT   IDENTITY (1, 1) NOT NULL,
    [VersionID]   INT      NOT NULL,
    [Active]      BIT      NOT NULL,
    [StartedBy]   INT      NOT NULL,
    [StartedOn]   DATETIME NOT NULL,
    [UpdatedBy]   INT      NOT NULL,
    [UpdatedOn]   DATETIME NOT NULL,
    [CompletedBy] INT      NOT NULL,
    [CompletedOn] DATETIME NOT NULL,
    [IsTest]      BIT      CONSTRAINT [DF_WorkflowItem_IsTest] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_WorkflowItem] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_WorkflowItem_WorkflowVersion] FOREIGN KEY ([VersionID]) REFERENCES [workflow].[Version] ([ID]) ON DELETE CASCADE
);
GO
