CREATE TABLE [dbo].[Follow] (
    [ResourceID]   INT          NOT NULL,
    [ObjectType]   VARCHAR (50) NOT NULL,
    [ObjectID]     INT          NOT NULL,
    [DateCreated]  DATETIME     NOT NULL,
    [FollowTypeID] INT          NULL,
    [ID]           INT          IDENTITY (1, 1) NOT NULL,
    CONSTRAINT [PK_Follow] PRIMARY KEY CLUSTERED ([ID] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_Follow_ResourceID]
    ON [dbo].[Follow]([ResourceID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Follow_ObjectType-ObjectID]
    ON [dbo].[Follow]([ObjectType] ASC, [ObjectID] ASC);

