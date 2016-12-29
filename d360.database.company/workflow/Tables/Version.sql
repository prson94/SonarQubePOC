CREATE TABLE [workflow].[Version] (
    [ID]        INT      IDENTITY (1, 1) NOT NULL,
    [TypeID]    INT      NOT NULL,
    [CreatedBy] INT      NOT NULL,
    [CreatedOn] DATETIME NOT NULL,
    [UpdatedBy] INT      NOT NULL,
    [UpdatedOn] DATETIME NOT NULL,
    CONSTRAINT [PK_WorkflowVersion] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_WorkflowVersion_WorkflowType] FOREIGN KEY ([TypeID]) REFERENCES [workflow].[Type] ([ID]) ON DELETE CASCADE
);
GO