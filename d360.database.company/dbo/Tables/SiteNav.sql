CREATE TABLE [dbo].[SiteNav](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ParentID] [int] NULL,
	[Name] [varchar](250) NULL,
	[Route] [varchar](250) NULL
)

GO