CREATE TYPE [dbo].[LineageTable] AS TABLE (
    [ID]                INT          NULL,
    [SourceIntersectID] INT          NULL,
    [SourceSubject]     VARCHAR (50) NULL,
    [SourceSubjectID]   INT          NULL,
    [SourceObject]      VARCHAR (50) NULL,
    [SourceObjectID]    INT          NULL,
    [TargetIntersectID] INT          NULL,
    [TargetSubject]     VARCHAR (50) NULL,
    [TargetSubjectID]   INT          NULL,
    [TargetObject]      VARCHAR (50) NULL,
    [TargetObjectID]    INT          NULL,
    [Deleting]          BIT          NULL,
    [Adding]            BIT          NULL);

