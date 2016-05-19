CREATE TABLE [fusion].[Rule]
(	
	[ID] INT IDENTITY (1, 1) NOT NULL PRIMARY KEY,
	[Description] NVARCHAR(500) NULL,
	[Enabled] BIT NOT NULL,
	[FusionID] INT NOT NULL,
	[ObjectType] VARCHAR(25) NOT NULL,
	[ObjectID] INT NOT NULL,
	[UpdatedOn] DATETIME NOT NULL,
	[UpdatedBy] INT NOT NULL,
	CONSTRAINT [FK_FusionRule_Fusion] FOREIGN KEY([FusionID])
		REFERENCES [dbo].[fusion] ([ID])		
)