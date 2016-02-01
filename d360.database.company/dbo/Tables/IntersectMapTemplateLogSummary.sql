CREATE TABLE [dbo].[IntersectMapTemplateLogSummary]
(
	[ID] INT IDENTITY (1, 1) NOT NULL PRIMARY KEY,	
	[DateStarted] DATETIME NOT NULL ,
	[DateCompleted] DATETIME NULL,
	[NumberOfTemplatesProcessed] INT NULL,
	[NumberOfObjectsUpdated] INT NULL,
	[NumberOfObjectsConsidered] INT NULL,
	[NumberOfIntersectsAdded] INT NULL
)