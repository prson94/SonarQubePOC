CREATE TYPE [dbo].[LineageTechnicalTable] AS TABLE(
	[ID] [int] NULL,
	[MapItemID] [int] NULL,
	[SourceFusionAttributeID] [int] NULL,
	[TargetFusionAttributeID] [int] NULL,
	[Deleting] [bit] NULL,
	[Adding] [bit] NULL
)
GO
