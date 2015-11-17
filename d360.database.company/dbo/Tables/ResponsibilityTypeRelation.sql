CREATE TABLE [dbo].[ResponsibilityTypeRelation] (
    [ResponsibilityTypeID] INT          NOT NULL,
    [ObjectType]           VARCHAR (50) NOT NULL,
    [ObjectID]             INT          NOT NULL,
    CONSTRAINT [PK_ResponsibilityTypeRelation] PRIMARY KEY CLUSTERED ([ResponsibilityTypeID] ASC, [ObjectType] ASC, [ObjectID] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_ResponsibilityTypeRelation_Object]
    ON [dbo].[ResponsibilityTypeRelation]([ObjectType] ASC, [ObjectID] ASC)
    INCLUDE([ResponsibilityTypeID]);

