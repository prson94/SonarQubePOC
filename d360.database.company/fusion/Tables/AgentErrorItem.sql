CREATE TABLE [fusion].[AgentErrorItem] (
    [ID]           BIGINT         IDENTITY (1, 1) NOT NULL,
    [AgentErrorID] BIGINT         NOT NULL,
    [Date]         DATETIME       CONSTRAINT [DF_FusionAgentErrorItem_Date] DEFAULT (getutcdate()) NOT NULL,
    [Message]      NVARCHAR (MAX) NOT NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionAgentErrorItem_FusionAgentError] FOREIGN KEY ([AgentErrorID]) REFERENCES [fusion].[AgentError] ([ID]) ON DELETE CASCADE
);


