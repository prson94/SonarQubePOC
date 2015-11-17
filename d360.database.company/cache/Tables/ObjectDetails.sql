CREATE TABLE [cache].[ObjectDetails] (
    [Object]         VARCHAR (50)    NOT NULL,
    [ObjectID]       INT             NOT NULL,
    [Name]           NVARCHAR (250)  NULL,
    [TextPath]       NVARCHAR (2500) NULL,
    [Description]    NVARCHAR (4000) NULL,
    [Parent]         VARCHAR (50)    NULL,
    [ParentID]       INT             NULL,
    [ParentName]     NVARCHAR (250)  NULL,
    [Url]            NVARCHAR (2500) NOT NULL,
    [ObjectType]     VARCHAR (25)    NOT NULL,
    [ObjectTypeID]   INT             NOT NULL,
    [ObjectTypeName] NVARCHAR (250)  NULL,
    [IconBackColor]  VARCHAR (15)    NULL,
    [IconForeColor]  VARCHAR (15)    NULL,
    [IconText]       VARCHAR (15)    NULL
);


GO
CREATE CLUSTERED INDEX [CIX_CacheObjectDetails]
    ON [cache].[ObjectDetails]([Object] ASC, [ObjectID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_CacheObjectDetails_ObjectType_ObjectTypeID]
    ON [cache].[ObjectDetails]([ObjectType] ASC, [ObjectTypeID] ASC);

