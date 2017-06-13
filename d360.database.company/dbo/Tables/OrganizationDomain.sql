CREATE TABLE [dbo].[OrganizationDomain](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[OrganizationID] [int] NOT NULL,
	[Domain] [nvarchar](500) NOT NULL,
 CONSTRAINT [PK_OrganizationDomain] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO

ALTER TABLE [dbo].[OrganizationDomain]  WITH CHECK ADD  CONSTRAINT [FK_OrganizationDomain_Organization] FOREIGN KEY([OrganizationID])
REFERENCES [dbo].[Organization] ([ID])
GO

ALTER TABLE [dbo].[OrganizationDomain] CHECK CONSTRAINT [FK_OrganizationDomain_Organization]
GO