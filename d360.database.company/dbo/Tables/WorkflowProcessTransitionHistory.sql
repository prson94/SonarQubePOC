CREATE TABLE [dbo].[WorkflowProcessTransitionHistory] (
    [Id]                   UNIQUEIDENTIFIER NOT NULL,
    [ProcessId]            UNIQUEIDENTIFIER NOT NULL,
    [ExecutorIdentityId]   NVARCHAR (MAX)   NOT NULL,
    [ActorIdentityId]      NVARCHAR (MAX)   NOT NULL,
    [FromActivityName]     NVARCHAR (MAX)   NOT NULL,
    [ToActivityName]       NVARCHAR (MAX)   NOT NULL,
    [ToStateName]          NVARCHAR (MAX)   NULL,
    [TransitionTime]       DATETIME         NOT NULL,
    [TransitionClassifier] NVARCHAR (MAX)   NOT NULL,
    [IsFinalised]          BIT              NOT NULL,
    [FromStateName]        NVARCHAR (MAX)   NULL,
    [TriggerName]          NVARCHAR (MAX)   NULL,
    CONSTRAINT [PK_WorkflowProcessTransitionHistory] PRIMARY KEY CLUSTERED ([Id] ASC)
);

