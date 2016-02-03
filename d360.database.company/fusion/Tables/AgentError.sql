CREATE TABLE [fusion].[AgentError] (
    [ID]					BIGINT IDENTITY (1, 1)	NOT NULL PRIMARY KEY,
	[FusionID]				INT						NOT NULL,
    [MachineName]			VARCHAR(250)			NOT NULL,
    [Date]					DATETIME				NOT NULL DEFAULT(CURRENT_TIMESTAMP),        
);
