CREATE TYPE [utility].[DiagramRelationshipTable] AS TABLE (
    [ItemID]                    INT          IDENTITY (1, 1) NOT NULL,
    [IntersectTypeID]           INT          NULL,
    [IntersectID]               INT          NULL,
    [ID]                        INT          NULL,
    [SourceObject]              VARCHAR (50) NULL,
    [SourceObjectID]            INT          NULL,
    [SourceIntersectTypeNodeID] INT          NULL,
    [TargetObject]              VARCHAR (50) NULL,
    [TargetObjectID]            INT          NULL,
    [TargetIntersectTypeNodeID] INT          NULL,
    [type]                      INT          NULL,
    [predicateid]               INT          NULL,
    [needsMapRecord]            INT          NULL);

