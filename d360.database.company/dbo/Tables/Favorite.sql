CREATE TABLE [dbo].[Favorite](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ResourceID] [int] NOT NULL,
	[Route] [varchar](250) NULL,
	[Name] [varchar](250) NOT NULL,
	[SortOrder] [int] NULL,
	[IsOverride] [bit] NOT NULL,
 CONSTRAINT [PK_Favorite_ID] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO

ALTER TABLE [dbo].[Favorite] ADD  CONSTRAINT [DF_Favorite_IsOverride]  DEFAULT ((0)) FOR [IsOverride]
GO
