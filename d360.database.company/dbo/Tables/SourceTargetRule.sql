CREATE TABLE [dbo].[SourceTargetRule](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[FocalObjectID] [int] NOT NULL,
	[FocalObject] [varchar](150) NOT NULL,
	[SourceObjectID] [int] NOT NULL,
	[SourceObject] [varchar](150) NOT NULL,
	[TargetObjectID] [int] NOT NULL,
	[TargetObject] [varchar](150) NOT NULL,
	[Transformation] [varchar](500) NULL,
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO

SET ANSI_PADDING OFF
GO
