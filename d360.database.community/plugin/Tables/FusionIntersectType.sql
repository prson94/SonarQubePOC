CREATE TABLE [plugin].[FusionIntersectType] (
    [StartFusionAttributeTypeID] INT NOT NULL,
    [EndFusionAttributeTypeID]   INT NOT NULL,
    [ReadOnly]                   BIT NOT NULL,
    [FusionTypeID]               INT NOT NULL,
    CONSTRAINT [PK_Plugin_FusionIntersectType] PRIMARY KEY CLUSTERED ([StartFusionAttributeTypeID] ASC, [EndFusionAttributeTypeID] ASC)
);

