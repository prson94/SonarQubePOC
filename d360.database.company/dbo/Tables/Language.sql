CREATE TABLE [dbo].[Language](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[Alpha2] [varchar](2) NOT NULL,
	[Alpha3b] [varchar](3) NOT NULL
)