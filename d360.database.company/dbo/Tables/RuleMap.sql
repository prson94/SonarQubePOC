CREATE TABLE [dbo].[RuleMap] (
    [RuleID]     INT            NOT NULL,
    [SourceID]   VARCHAR (50)   NOT NULL,
    [SourceName] VARCHAR (250)  NULL,
    [SourceURI]  VARCHAR (1000) NULL,
    CONSTRAINT [PK_RuleMap] PRIMARY KEY CLUSTERED ([RuleID] ASC, [SourceID] ASC),
    CONSTRAINT [FK_RuleMap_Rule] FOREIGN KEY ([RuleID]) REFERENCES [dbo].[Rule] ([ID])
);

