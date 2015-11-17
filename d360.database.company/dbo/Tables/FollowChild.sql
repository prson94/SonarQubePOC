CREATE TABLE [dbo].[FollowChild] (
    [ParentObjectType] VARCHAR (50) NULL,
    [ParentObjectID]   INT          NULL,
    [ObjectID]         INT          NOT NULL,
    [ObjectType]       VARCHAR (50) NOT NULL,
    [DateCreated]      DATETIME     NULL,
    [FollowTypeID]     INT          NULL
);

