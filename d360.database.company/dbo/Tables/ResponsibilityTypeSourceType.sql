CREATE TABLE [dbo].[ResponsibilityTypeSourceType] (
    [ResponsibilityTypeID] INT          NOT NULL,
    [ObjectType]           VARCHAR (50) NOT NULL,
    [ObjectID]             INT          NOT NULL,
    CONSTRAINT [PK_ResponsibilityTypeSourceType] PRIMARY KEY CLUSTERED ([ResponsibilityTypeID] ASC, [ObjectType] ASC, [ObjectID] ASC)
);

