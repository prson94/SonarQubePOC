CREATE TABLE [analytics].[Host] (
    [ID]    INT          IDENTITY (1, 1) NOT NULL,
    [Value] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_Analytics_Host] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);


GO
CREATE CLUSTERED INDEX [CIX_Analytics_Host]
    ON [analytics].[Host]([Value] ASC);

