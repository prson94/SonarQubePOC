CREATE TABLE [fusion].[AgentError] (
    [ID]          BIGINT        IDENTITY (1, 1) NOT NULL,
    [FusionID]    INT           NOT NULL,
    [MachineName] VARCHAR (250) NOT NULL,
    [Date]        DATETIME      CONSTRAINT [DF_FusionAgentError_Date] DEFAULT (getutcdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC)
);


