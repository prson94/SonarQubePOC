CREATE TABLE [dbo].[OrganizationInvitation](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[OrganizationID] [int] NOT NULL,
	[Email] [nvarchar](500) NOT NULL,
 CONSTRAINT [PK_OrganizationInvitation] PRIMARY KEY NONCLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO

ALTER TABLE [dbo].[OrganizationInvitation]  WITH CHECK ADD  CONSTRAINT [FK_OrganizationInvitation_Organization] FOREIGN KEY([OrganizationID])
REFERENCES [dbo].[Organization] ([ID])
GO

ALTER TABLE [dbo].[OrganizationInvitation] CHECK CONSTRAINT [FK_OrganizationInvitation_Organization]
GO