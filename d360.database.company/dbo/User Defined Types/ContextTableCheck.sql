CREATE TYPE [dbo].[ContextTableCheck] AS TABLE (
    [ID]                      BIGINT       NULL,
    [GroupID]                 INT          NULL,
    [ObjectType]              VARCHAR (25) NULL,
    [ObjectID]                INT          NULL,
    [IntersectRole]           INT          NULL,
    [IntersectClassification] INT          NULL);

