CREATE TYPE [dbo].[LineageTable] AS TABLE(
	[ID] [int] NULL,
	[SourceIntersectID] [int] NULL,
	[SourceSubject] [varchar](50) NULL,
	[SourceSubjectID] [int] NULL,
	[SourceObject] [varchar](50) NULL,
	[SourceObjectID] [int] NULL,
	[TargetIntersectID] [int] NULL,
	[TargetSubject] [varchar](50) NULL,
	[TargetSubjectID] [int] NULL,
	[TargetObject] [varchar](50) NULL,
	[TargetObjectID] [int] NULL,
	[Deleting] [bit] NULL,
	[Adding] [bit] NULL
)
GO
