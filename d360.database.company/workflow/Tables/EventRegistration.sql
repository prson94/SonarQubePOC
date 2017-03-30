CREATE TABLE [workflow].[EventRegistration] (
    [ID]         INT          IDENTITY (1, 1) NOT NULL,
    [TypeID]     INT          NOT NULL,
    [Object]     VARCHAR (50) NOT NULL,
    [ObjectID]   INT          NOT NULL,
    [ChangeType] INT          NOT NULL,
    [Condition]  XML          NULL,
    [Settings] XML NULL, 
    CONSTRAINT [PK_WorkflowEventRegistration] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_WorkflowEventRegistration_WorkflowType] FOREIGN KEY ([TypeID]) REFERENCES [workflow].[Type] ([ID]) ON DELETE CASCADE
);
GO

