CREATE TABLE [dbo].[OrganizationResource](
	[OrganizationID] [int] NOT NULL,
	[ResourceID] [int] NOT NULL,
	[Accepted] [bit] NULL,
	[DateAccepted] [datetime] NULL,
 CONSTRAINT [PK_OrganizationResource] PRIMARY KEY CLUSTERED 
(
	[OrganizationID] ASC,
	[ResourceID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO

ALTER TABLE [dbo].[OrganizationResource]  WITH CHECK ADD  CONSTRAINT [FK_OrganizationResource_Organization] FOREIGN KEY([OrganizationID])
REFERENCES [dbo].[Organization] ([ID])
GO

ALTER TABLE [dbo].[OrganizationResource] CHECK CONSTRAINT [FK_OrganizationResource_Organization]
GO