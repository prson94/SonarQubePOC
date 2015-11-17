CREATE TABLE [dbo].[ResponsibilityTypeHierarchy] (
    [ID]       INT NOT NULL,
    [ParentID] INT NULL,
    CONSTRAINT [PK_ResponsibilityTypeHierarchy] PRIMARY KEY CLUSTERED ([ID] ASC)
);

