CREATE TABLE [dbo].[Organization](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[Accepted] [bit] NULL,
	[AcceptedBy] [int] NULL,
	[DateAccepted] [datetime] NULL,
	[AdministratorEmail] [varchar](250) NULL,
	[OrganizationTypeID] [int] NOT NULL,
	[State] [int] NOT NULL,
 CONSTRAINT [PK_Organization] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)
GO

ALTER TABLE [dbo].[Organization] ADD  CONSTRAINT [DF_Organization_OrganizationType]  DEFAULT ((1)) FOR [OrganizationTypeID]
GO

ALTER TABLE [dbo].[Organization] ADD  CONSTRAINT [DF_Organization_State]  DEFAULT ((1)) FOR [State]
GO

ALTER TABLE [dbo].[Organization]  WITH CHECK ADD  CONSTRAINT [FK_Organization_OrganizationType] FOREIGN KEY([OrganizationTypeID])
REFERENCES [dbo].[OrganizationType] ([ID])
GO

ALTER TABLE [dbo].[Organization] CHECK CONSTRAINT [FK_Organization_OrganizationType]
GO