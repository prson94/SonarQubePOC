CREATE TYPE [dbo].[LineageTechnicalTable] AS TABLE (
    [ID]                      INT NULL,
    [MapItemID]               INT NULL,
    [SourceFusionAttributeID] INT NULL,
    [TargetFusionAttributeID] INT NULL,
    [Deleting]                BIT NULL,
    [Adding]                  BIT NULL);

