CREATE TABLE [dbo].[WorkflowStatus] (
    [ID]            BIGINT           IDENTITY (1, 1) NOT NULL,
    [WorkflowID]    UNIQUEIDENTIFIER NOT NULL,
    [TraceLevel]    SMALLINT         NOT NULL,
    [RecordNumber]  INT              NOT NULL,
    [ActivityName]  VARCHAR (250)    NOT NULL,
    [ActivityState] VARCHAR (125)    NOT NULL,
    [Data]          XML              NOT NULL,
    [Date]          DATETIME         NOT NULL,
    CONSTRAINT [PK_WorkflowStatus] PRIMARY KEY CLUSTERED ([ID] ASC)
);

