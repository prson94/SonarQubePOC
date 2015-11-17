CREATE TABLE [cache].[Relationships] (
    [IntersectTypeID]           INT             NOT NULL,
    [IntersectID]               INT             NOT NULL,
    [Classification]            INT             NULL,
    [Description]               NVARCHAR (4000) NULL,
    [SourceIntersectTypeNodeID] INT             NOT NULL,
    [SourceObject]              VARCHAR (50)    NOT NULL,
    [SourceObjectID]            INT             NOT NULL,
    [SourceObjectName]          NVARCHAR (500)  NOT NULL,
    [SourceType]                VARCHAR (50)    NOT NULL,
    [SourceTypeID]              INT             NOT NULL,
    [SourceTypeName]            NVARCHAR (250)  NOT NULL,
    [TargetIntersectTypeNodeID] INT             NOT NULL,
    [TargetObject]              VARCHAR (50)    NOT NULL,
    [TargetObjectID]            INT             NOT NULL,
    [TargetObjectName]          NVARCHAR (500)  NOT NULL,
    [TargetType]                VARCHAR (50)    NOT NULL,
    [TargetTypeID]              INT             NOT NULL,
    [TargetTypeName]            NVARCHAR (250)  NOT NULL,
    [Role]                      NVARCHAR (250)  NULL,
    CONSTRAINT [PK_CacheRelationships] PRIMARY KEY CLUSTERED ([IntersectID] ASC, [SourceObject] ASC, [SourceObjectID] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_CacheRelationships_SourceObject_TargetType]
    ON [cache].[Relationships]([SourceObject] ASC, [SourceObjectID] ASC, [TargetType] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_CacheRelationships_SourceObject_TargetType_Classification]
    ON [cache].[Relationships]([SourceObject] ASC, [SourceObjectID] ASC, [TargetType] ASC, [Classification] ASC);

