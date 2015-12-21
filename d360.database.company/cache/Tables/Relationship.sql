CREATE TABLE [cache].[Relationship] (
    [IntersectID]               INT          NOT NULL,
    [SourceIntersectTypeNodeID] INT          NOT NULL,
    [SourceObject]              VARCHAR (50) NOT NULL,
    [SourceObjectID]            INT          NOT NULL,
    [TargetIntersectTypeNodeID] INT          NOT NULL,
    [TargetObject]              VARCHAR (50) NOT NULL,
    [TargetObjectID]            INT          NOT NULL,
    CONSTRAINT [PK_CacheRelationship] PRIMARY KEY CLUSTERED ([IntersectID] ASC, [SourceObject] ASC, [SourceObjectID] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_Cache_Relationship_SourceObject]
    ON [cache].[Relationship]([SourceObject] ASC, [SourceObjectID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Cache_Relationship_TargetObject]
    ON [cache].[Relationship]([TargetObject] ASC, [TargetObjectID] ASC);

