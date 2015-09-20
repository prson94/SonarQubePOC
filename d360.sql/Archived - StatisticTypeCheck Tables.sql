CREATE TABLE [dbo].[StatisticTypeCheckAdvanced](
	[CompanyID] [int] NOT NULL,
	[StatisticTypeID] [int] NOT NULL,
	[SQL] [varchar](8000) NOT NULL,
 CONSTRAINT [PK_StatisticTypeCheckAdvanced] PRIMARY KEY CLUSTERED 
(
	[CompanyID] ASC,
	[StatisticTypeID] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF)
)FEDERATED ON ([company_distribution] = [CompanyID])
GO

ALTER TABLE [dbo].[StatisticTypeCheckAdvanced]  WITH CHECK ADD  CONSTRAINT [FK_StatisticTypeCheckAdvanced_StatisticType] FOREIGN KEY([CompanyID], [StatisticTypeID])
REFERENCES [dbo].[StatisticType] ([CompanyID], [ID])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[StatisticTypeCheckAdvanced] CHECK CONSTRAINT [FK_StatisticTypeCheckAdvanced_StatisticType]
GO


CREATE TABLE [dbo].[StatisticTypeCheckCount](
	[CompanyID] [int] NOT NULL,
	[StatisticTypeID] [int] NOT NULL,
	[ObjectType] [varchar](50) NOT NULL,
	[ObjectID] [int] NOT NULL,
 CONSTRAINT [PK_StatisticTypeCheckCount] PRIMARY KEY CLUSTERED 
(
	[CompanyID] ASC,
	[StatisticTypeID] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF)
)FEDERATED ON ([company_distribution] = [CompanyID])
GO

ALTER TABLE [dbo].[StatisticTypeCheckCount]  WITH CHECK ADD  CONSTRAINT [FK_StatisticTypeCheckCount_StatisticType] FOREIGN KEY([CompanyID], [StatisticTypeID])
REFERENCES [dbo].[StatisticType] ([CompanyID], [ID])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[StatisticTypeCheckCount] CHECK CONSTRAINT [FK_StatisticTypeCheckCount_StatisticType]
GO

CREATE TABLE [dbo].[StatisticTypeCheckExistence](
	[CompanyID] [int] NOT NULL,
	[StatisticTypeID] [int] NOT NULL,
	[ObjectType] [varchar](50) NOT NULL,
	[ObjectID] [int] NOT NULL,
 CONSTRAINT [PK_StatisticTypeCheckExistence] PRIMARY KEY CLUSTERED 
(
	[CompanyID] ASC,
	[StatisticTypeID] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF)
)FEDERATED ON ([company_distribution] = [CompanyID])
GO

ALTER TABLE [dbo].[StatisticTypeCheckExistence]  WITH CHECK ADD  CONSTRAINT [FK_StatisticTypeCheckExistence_StatisticType] FOREIGN KEY([CompanyID], [StatisticTypeID])
REFERENCES [dbo].[StatisticType] ([CompanyID], [ID])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[StatisticTypeCheckExistence] CHECK CONSTRAINT [FK_StatisticTypeCheckExistence_StatisticType]
GO

CREATE TABLE [dbo].[StatisticTypeCheckProperty](
	[CompanyID] [int] NOT NULL,
	[StatisticTypeID] [int] NOT NULL,
	[ObjectType] [varchar](50) NOT NULL,
	[ObjectID] [int] NOT NULL,
	[PropertyName] [varchar](250) NOT NULL,
	[Value] [nvarchar](4000) NOT NULL,
 CONSTRAINT [PK_StatisticTypeCheckProperty] PRIMARY KEY CLUSTERED 
(
	[CompanyID] ASC,
	[StatisticTypeID] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF)
)FEDERATED ON ([company_distribution] = [CompanyID])
GO

ALTER TABLE [dbo].[StatisticTypeCheckProperty]  WITH CHECK ADD  CONSTRAINT [FK_StatisticTypeCheckProperty_StatisticType] FOREIGN KEY([CompanyID], [StatisticTypeID])
REFERENCES [dbo].[StatisticType] ([CompanyID], [ID])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[StatisticTypeCheckProperty] CHECK CONSTRAINT [FK_StatisticTypeCheckProperty_StatisticType]
GO

CREATE TABLE [dbo].[StatisticTypeCheckRelationship](
	[CompanyID] [int] NOT NULL,
	[StatisticTypeID] [int] NOT NULL,
	[ObjectType] [varchar](50) NOT NULL,
	[ObjectID] [int] NOT NULL,
 CONSTRAINT [PK_StatisticTypeCheckRelationship] PRIMARY KEY CLUSTERED 
(
	[CompanyID] ASC,
	[StatisticTypeID] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF)
)FEDERATED ON ([company_distribution] = [CompanyID])
GO

ALTER TABLE [dbo].[StatisticTypeCheckRelationship]  WITH CHECK ADD  CONSTRAINT [FK_StatisticTypeCheckRelationship_StatisticType] FOREIGN KEY([CompanyID], [StatisticTypeID])
REFERENCES [dbo].[StatisticType] ([CompanyID], [ID])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[StatisticTypeCheckRelationship] CHECK CONSTRAINT [FK_StatisticTypeCheckRelationship_StatisticType]
GO

CREATE TABLE [dbo].[StatisticTypeCheckValue](
	[CompanyID] [int] NOT NULL,
	[StatisticTypeID] [int] NOT NULL,
	[ObjectType] [varchar](50) NOT NULL,
	[ObjectID] [int] NOT NULL,
	[Value] [nvarchar](4000) NOT NULL,
 CONSTRAINT [PK_StatisticTypeCheckValue] PRIMARY KEY CLUSTERED 
(
	[CompanyID] ASC,
	[StatisticTypeID] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF)
)FEDERATED ON ([company_distribution] = [CompanyID])
GO

ALTER TABLE [dbo].[StatisticTypeCheckValue]  WITH CHECK ADD  CONSTRAINT [FK_StatisticTypeCheckValue_StatisticType] FOREIGN KEY([CompanyID], [StatisticTypeID])
REFERENCES [dbo].[StatisticType] ([CompanyID], [ID])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[StatisticTypeCheckValue] CHECK CONSTRAINT [FK_StatisticTypeCheckValue_StatisticType]
GO


CREATE TABLE [dbo].[StatisticTypeCheckValueRange](
	[CompanyID] [int] NOT NULL,
	[StatisticTypeID] [int] NOT NULL,
	[ObjectType] [varchar](50) NOT NULL,
	[ObjectID] [int] NOT NULL,
	[ValueStart] [nvarchar](4000) NOT NULL,
	[ValueEnd] [nvarchar](4000) NOT NULL,
 CONSTRAINT [PK_StatisticTypeCheckValueRange] PRIMARY KEY CLUSTERED 
(
	[CompanyID] ASC,
	[StatisticTypeID] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF)
)FEDERATED ON ([company_distribution] = [CompanyID])
GO

ALTER TABLE [dbo].[StatisticTypeCheckValueRange]  WITH CHECK ADD  CONSTRAINT [FK_StatisticTypeCheckValueRange_StatisticType] FOREIGN KEY([CompanyID], [StatisticTypeID])
REFERENCES [dbo].[StatisticType] ([CompanyID], [ID])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[StatisticTypeCheckValueRange] CHECK CONSTRAINT [FK_StatisticTypeCheckValueRange_StatisticType]
GO

