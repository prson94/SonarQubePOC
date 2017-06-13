CREATE TABLE [dbo].[OrganizationRegistration](
	[ID] [uniqueidentifier] NOT NULL,
	[OrganizationID] [int] NOT NULL,
	[Email] [nvarchar](500) NOT NULL,
	[Step] [int] NOT NULL,
	[RegisteredStartedOn] [datetime] NOT NULL,
	[RegisteredCompletedOn] [datetime] NULL,
 CONSTRAINT [PK_OrganizationRegistration] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO

ALTER TABLE [dbo].[OrganizationRegistration] ADD  CONSTRAINT [DF_OrganizationRegistration_ID]  DEFAULT (newid()) FOR [ID]
GO

ALTER TABLE [dbo].[OrganizationRegistration]  WITH CHECK ADD  CONSTRAINT [FK_OrganizationRegistration_Organization] FOREIGN KEY([OrganizationID])
REFERENCES [dbo].[Organization] ([ID])
GO

ALTER TABLE [dbo].[OrganizationRegistration] CHECK CONSTRAINT [FK_OrganizationRegistration_Organization]
GO