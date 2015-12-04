CREATE TABLE [plugin].[FusionLookupType]
(
	[ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[Name] nvarchar(500) NOT NULL,
	[Description] nvarchar(2000) NULL,
	[Provider] [nvarchar](500) NULL,
	CONSTRAINT [UC_FusionLookupTypes] UNIQUE([Name]) 
)
