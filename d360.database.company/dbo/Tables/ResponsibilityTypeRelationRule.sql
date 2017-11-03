CREATE TABLE [dbo].[ResponsibilityTypeRelationRule] (
    [ID]                   INT            IDENTITY (1, 1) NOT NULL,
    [ResponsibilityTypeID] INT            NOT NULL,
    [Object]               VARCHAR (50)   NOT NULL,
    [ObjectID]             INT            NOT NULL,
    [Name]                 NVARCHAR (250) NOT NULL,
    [Definition]           NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_ResponsibilityTypeRelationRule] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);

