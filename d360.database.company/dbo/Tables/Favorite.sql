CREATE TABLE [dbo].[Favorite](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ResourceID] [int] NOT NULL,
	[Route] [varchar](250) NULL,
	[Name] [varchar](250) NOT NULL,
	[SortOrder] [int] NULL
)

GO