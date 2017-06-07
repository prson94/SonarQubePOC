CREATE TABLE [dbo].[SiteNavPermission](
	[SiteNavID] [int] NOT NULL,
	[Object] [varchar](250) NOT NULL,
	[ObjectID] [int] NOT NULL,
 CONSTRAINT [PK_SiteNavPermission] PRIMARY KEY CLUSTERED 
(
	[SiteNavID] ASC,
	[Object] ASC,
	[ObjectID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO

ALTER TABLE [dbo].[SiteNavPermission]  WITH CHECK ADD  CONSTRAINT [FK_SiteNavPermission_SiteNavID] FOREIGN KEY([SiteNavID])
REFERENCES [dbo].[SiteNav] ([ID])
GO

ALTER TABLE [dbo].[SiteNavPermission] CHECK CONSTRAINT [FK_SiteNavPermission_SiteNavID]
GO


