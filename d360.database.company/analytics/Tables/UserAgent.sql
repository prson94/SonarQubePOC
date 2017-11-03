CREATE TABLE [analytics].[UserAgent] (
    [ID]    INT            IDENTITY (1, 1) NOT NULL,
    [Value] NVARCHAR (250) NULL,
    CONSTRAINT [PK_Analytics_UserAgent] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);


GO
CREATE CLUSTERED INDEX [CIX_Analytics_UserAgent]
    ON [analytics].[UserAgent]([Value] ASC);

