CREATE TABLE [cache].[Relationship] (
    [IntersectID]               INT          NOT NULL,
    [SourceIntersectTypeNodeID] INT          CONSTRAINT [DF_CacheRelationship_SourceIntersectTypeNodeID] DEFAULT ((0)) NOT NULL,
    [SourceObject]              VARCHAR (50) NOT NULL,
    [SourceObjectID]            INT          NOT NULL,
    [TargetIntersectTypeNodeID] INT          CONSTRAINT [DF_CacheRelationship_TargetIntersectTypeNodeID] DEFAULT ((0)) NOT NULL,
    [TargetObject]              VARCHAR (50) NOT NULL,
    [TargetObjectID]            INT          NOT NULL,
    [SourceIntersectNodeID]     INT          CONSTRAINT [DF_CacheRelationship_SourceIntersectNodeID] DEFAULT ((0)) NULL,
    [TargetIntersectNodeID]     INT          CONSTRAINT [DF_CacheRelationship_TargetIntersectNodeID] DEFAULT ((0)) NULL,
    CONSTRAINT [PK_CacheRelationship] PRIMARY KEY CLUSTERED ([IntersectID] ASC, [SourceObject] ASC, [SourceObjectID] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_Cache_Relationship_SourceObject]
    ON [cache].[Relationship]([SourceObject] ASC, [SourceObjectID] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Cache_Relationship_TargetObject]
    ON [cache].[Relationship]([TargetObject] ASC, [TargetObjectID] ASC);
GO

