CREATE TABLE [dbo].[WorkflowGlobalParameter] (
    [Id]    UNIQUEIDENTIFIER NOT NULL,
    [Type]  NVARCHAR (MAX)   NOT NULL,
    [Name]  NVARCHAR (MAX)   NOT NULL,
    [Value] NVARCHAR (MAX)   NOT NULL,
    CONSTRAINT [PK_WorkflowGlobalParameter] PRIMARY KEY CLUSTERED ([Id] ASC)
);

