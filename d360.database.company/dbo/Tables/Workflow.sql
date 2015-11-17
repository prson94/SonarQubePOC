CREATE TABLE [dbo].[Workflow] (
    [ID]            UNIQUEIDENTIFIER NOT NULL,
    [WorkflowType]  INT              NOT NULL,
    [Data]          XML              NOT NULL,
    [DateStarted]   DATETIME         NOT NULL,
    [DateCompleted] DATETIME         NULL,
    [Step]          INT              CONSTRAINT [DF_Workflow_Step] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_Workflow] PRIMARY KEY CLUSTERED ([ID] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_Workflow_WorkflowType_DateCompleted]
    ON [dbo].[Workflow]([WorkflowType] ASC, [DateCompleted] DESC);


GO
CREATE PRIMARY XML INDEX [IXXML_Workflow_Data]
    ON [dbo].[Workflow]([Data])
    WITH (PAD_INDEX = OFF);

