CREATE TABLE [dbo].[ContractAcceptanceHistory](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ResourceID] [int] NOT NULL,
	[Accepted] [bit] NOT NULL,
	[AcceptedOn] [datetime] NOT NULL,
	[ContractID] [int] NOT NULL,
	[OrganizationID] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)
GO

ALTER TABLE [dbo].[ContractAcceptanceHistory]  WITH CHECK ADD  CONSTRAINT [FK_ContractAcceptanceHistory_Contract] FOREIGN KEY([ContractID])
REFERENCES [dbo].[Contract] ([ID])
GO

ALTER TABLE [dbo].[ContractAcceptanceHistory] CHECK CONSTRAINT [FK_ContractAcceptanceHistory_Contract]
GO

ALTER TABLE [dbo].[ContractAcceptanceHistory]  WITH CHECK ADD  CONSTRAINT [FK_ContractAcceptanceHistory_Organization] FOREIGN KEY([OrganizationID])
REFERENCES [dbo].[Organization] ([ID])
GO

ALTER TABLE [dbo].[ContractAcceptanceHistory] CHECK CONSTRAINT [FK_ContractAcceptanceHistory_Organization]
GO