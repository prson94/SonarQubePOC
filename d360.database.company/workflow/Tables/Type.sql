CREATE TABLE [workflow].[Type] (
    [ID]        INT            IDENTITY (1, 1) NOT NULL,
    [Name]      NVARCHAR (500) NOT NULL,
    [CreatedBy] INT            NOT NULL,
    [CreatedOn] DATETIME       NOT NULL,
    [UpdatedBy] INT            NOT NULL,
    [UpdatedOn] DATETIME       NOT NULL,
    CONSTRAINT [PK_WorkflowType] PRIMARY KEY CLUSTERED ([ID] ASC)
);
GO