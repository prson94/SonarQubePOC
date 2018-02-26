CREATE TABLE [dbo].[Contract](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ContractType] [int] NOT NULL,
	[OrganizationID] [int] NULL,
	[Title] [nvarchar](250) NOT NULL,
	[Body] [nvarchar](max) NOT NULL,
	[PublishedOn] [datetime] NULL,
	[State] [int] NOT NULL,
	[UpdatedOn] [datetime] NULL,
	[UpdatedBy] [int] NULL,
	[CreatedOn] [datetime] NULL,
	[CreatedBy] [int] NULL,
 CONSTRAINT [PK_Contract] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)
GO

ALTER TABLE [dbo].[Contract] ADD  CONSTRAINT [DF_Contract_State]  DEFAULT ((1)) FOR [State]
GO

ALTER TABLE [dbo].[Contract]  WITH CHECK ADD  CONSTRAINT [FK_Contract_Organization] FOREIGN KEY([OrganizationID])
REFERENCES [dbo].[Organization] ([ID])
GO

ALTER TABLE [dbo].[Contract] CHECK CONSTRAINT [FK_Contract_Organization]
GO

