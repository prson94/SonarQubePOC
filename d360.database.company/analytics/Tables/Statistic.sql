CREATE TABLE [analytics].[Statistic] (
    [ID]                UNIQUEIDENTIFIER CONSTRAINT [DF_Analytics_Statistic_ID] DEFAULT (newid()) NOT NULL,
    [Object]            INT              NOT NULL,
    [ObjectID]          INT              NOT NULL,
    [IpID]              INT              NOT NULL,
    [UserAgentID]       INT              NOT NULL,
    [HostID]            INT              NOT NULL,
    [BrowserLanguageID] INT              NOT NULL,
    [ActionID]          SMALLINT         NOT NULL,
    [ResourceID]        INT              CONSTRAINT [DF_Analytics_Statistic_ResourceID] DEFAULT ((0)) NOT NULL,
    [Timestamp]         DATETIME         NOT NULL,
    CONSTRAINT [PK_Analytics_Statistic] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_Analytics_Statistic_Timestamp]
    ON [analytics].[Statistic]([Timestamp] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Analytics_Statistic_Object]
    ON [analytics].[Statistic]([Object] ASC, [ObjectID] ASC);


GO
CREATE CLUSTERED INDEX [CIX_Analytics_Statistic]
    ON [analytics].[Statistic]([Object] ASC, [ObjectID] ASC);

