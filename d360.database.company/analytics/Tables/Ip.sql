CREATE TABLE [analytics].[Ip] (
    [ID]    INT           IDENTITY (1, 1) NOT NULL,
    [Value] VARCHAR (100) NOT NULL,
    CONSTRAINT [PK_Analytics_Ip] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);


GO
CREATE CLUSTERED INDEX [CIX_Analytics_Ip]
    ON [analytics].[Ip]([Value] ASC);

