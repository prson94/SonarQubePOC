CREATE TABLE [cache].[Object] (
    [Object]       VARCHAR (50) NOT NULL,
    [ObjectID]     INT          NOT NULL,
    [ObjectType]   VARCHAR (25) NOT NULL,
    [ObjectTypeID] INT          NOT NULL
);




GO



GO
CREATE NONCLUSTERED INDEX [IX_CacheObject_ObjectType_ObjectTypeID]
    ON [cache].[Object]([ObjectType] ASC, [ObjectTypeID] ASC);


GO
CREATE CLUSTERED INDEX [CIX_CacheObject]
    ON [cache].[Object]([Object] ASC, [ObjectID] ASC);

