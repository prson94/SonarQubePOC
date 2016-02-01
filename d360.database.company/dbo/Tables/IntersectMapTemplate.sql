CREATE TABLE [dbo].[IntersectMapTemplate]
(
	[ID] INT IDENTITY (1, 1) NOT NULL PRIMARY KEY,	
	[Query] VARCHAR(MAX) NOT NULL,
	[Object] VARCHAR(50) NOT NULL,
	[ObjectID] INT NOT NULL,
	[Enabled] BIT NOT NULL DEFAULT(1)
)
