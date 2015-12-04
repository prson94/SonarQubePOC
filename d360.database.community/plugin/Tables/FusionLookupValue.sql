CREATE TABLE [plugin].[FusionLookupValue]
(
	[ID] INT  IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[FusionLookupTypeID] INT NOT NULL,
	[Key] NVARCHAR(500) NOT NULL,
	[Value] NVARCHAR(500) NOT NULL,
	[Description] NVARCHAR(1000) NULL,
	CONSTRAINT [UC_FusionLookupValue] UNIQUE([FusionFieldLookupTypeID],[Key]) 
)

GO

ALTER TABLE [plugin].[FusionLookupValue]  WITH CHECK ADD  CONSTRAINT [FK_FusionLookupValue_FusionLookupType] FOREIGN KEY([FusionLookupTypeID])
	REFERENCES [plugin].[FusionLookupType] ([ID])
	ON DELETE CASCADE
GO
