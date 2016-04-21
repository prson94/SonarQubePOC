CREATE TABLE [dbo].[BusinessTransformationRule](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[FocalObjectID] [int] NOT NULL,
	[FocalObject] [varchar](50) NOT NULL,
	[SourceObjectID] [int] NOT NULL,
	[SourceObject] [varchar](50) NOT NULL,
	[TargetObjectID] [int] NOT NULL,
	[TargetObject] [varchar](50) NOT NULL,
	[Transformation] [varchar](max) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)