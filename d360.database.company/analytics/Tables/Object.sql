CREATE TABLE [analytics].[Object] (
    [ID]    INT          IDENTITY (1, 1) NOT NULL,
    [Value] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_Analytics_Object] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);


GO
CREATE CLUSTERED INDEX [CIX_Analytics_Object]
    ON [analytics].[Object]([Value] ASC);

