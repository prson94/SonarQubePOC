CREATE TABLE [analytics].[BrowserLanguage] (
    [ID]    INT           IDENTITY (1, 1) NOT NULL,
    [Value] VARCHAR (500) NOT NULL,
    CONSTRAINT [PK_Analytics_BrowserLanguage] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);


GO
CREATE CLUSTERED INDEX [CIX_Analytics_BrowserLanguage]
    ON [analytics].[BrowserLanguage]([Value] ASC);

