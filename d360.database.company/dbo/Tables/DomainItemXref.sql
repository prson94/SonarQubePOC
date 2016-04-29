CREATE TABLE [dbo].[DomainItemXref](
	ID int identity primary key not null,
	[HouseDomainItemID] [int] NOT NULL,
	[DomainItemID] [int] NOT NULL,
	[LanguageID] [int] NULL,
)

GO

ALTER TABLE [dbo].[DomainItemXref]  WITH CHECK ADD FOREIGN KEY([DomainItemID])
REFERENCES [dbo].[DomainItem] ([ID])
GO

ALTER TABLE [dbo].[DomainItemXref]  WITH CHECK ADD FOREIGN KEY([HouseDomainItemID])
REFERENCES [dbo].[DomainItem] ([ID])
GO