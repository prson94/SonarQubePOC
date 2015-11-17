CREATE TABLE [fusion].[StagingRelation] (
    [ID]                         BIGINT         IDENTITY (1, 1) NOT NULL,
    [ExecutionID]                INT            NOT NULL,
    [StartID]                    NVARCHAR (500) NOT NULL,
    [EndID]                      NVARCHAR (500) NOT NULL,
    [StartFusionAttributeID]     INT            NULL,
    [EndFusionAttributeID]       INT            NULL,
    [StartFusionAttributeTypeID] INT            NULL,
    [EndFusionAttributeTypeID]   INT            NULL,
    [StartIntersectTypeNodeID]   INT            NULL,
    [EndIntersectTypeNodeID]     INT            NULL,
    [IntersectTypeID]            INT            NULL,
    [IntersectID]                INT            NULL,
    CONSTRAINT [PK_FusionStagingRelation] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);


GO
CREATE CLUSTERED INDEX [CIX_FusionStagingRelation]
    ON [fusion].[StagingRelation]([ExecutionID] DESC, [ID] ASC);

