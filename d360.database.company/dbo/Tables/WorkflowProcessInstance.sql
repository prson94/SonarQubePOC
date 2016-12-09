CREATE TABLE [dbo].[WorkflowProcessInstance] (
    [Id]                             UNIQUEIDENTIFIER NOT NULL,
    [StateName]                      NVARCHAR (MAX)   NOT NULL,
    [ActivityName]                   NVARCHAR (MAX)   NOT NULL,
    [SchemeId]                       UNIQUEIDENTIFIER NULL,
    [PreviousState]                  NVARCHAR (MAX)   NULL,
    [PreviousStateForDirect]         NVARCHAR (MAX)   NULL,
    [PreviousStateForReverse]        NVARCHAR (MAX)   NULL,
    [PreviousActivity]               NVARCHAR (MAX)   NULL,
    [PreviousActivityForDirect]      NVARCHAR (MAX)   NULL,
    [PreviousActivityForReverse]     NVARCHAR (MAX)   NULL,
    [ParentProcessId]                UNIQUEIDENTIFIER NULL,
    [RootProcessId]                  UNIQUEIDENTIFIER NOT NULL,
    [IsDeterminingParametersChanged] BIT              DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_WorkflowProcessInstance_1] PRIMARY KEY CLUSTERED ([Id] ASC)
);

