CREATE TABLE [fusion].[AgentErrorItem] (
	[ID]			   BIGINT IDENTITY (1, 1)	NOT NULL PRIMARY KEY,
    [AgentErrorID]     BIGINT  NOT NULL,
	[Date]				DATETIME         NOT NULL  DEFAULT(CURRENT_TIMESTAMP),        	
    [Message]			NVARCHAR(max)     NOT NULL,    	
    CONSTRAINT [FK_FusionAgentErrorItem_FusionAgentError] FOREIGN KEY ([AgentErrorID]) REFERENCES [fusion].[AgentError] ([ID]) ON DELETE CASCADE
);
