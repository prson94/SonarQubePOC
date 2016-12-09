CREATE TABLE [dbo].[WorkflowScheme] (
    [Code]   NVARCHAR (256) NOT NULL,
    [Scheme] NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_WorkflowScheme] PRIMARY KEY CLUSTERED ([Code] ASC)
);

