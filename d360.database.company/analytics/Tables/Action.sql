CREATE TABLE [analytics].[Action] (
    [ID]    INT          IDENTITY (1, 1) NOT NULL,
    [Value] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_Analytics_Action] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);


GO
CREATE CLUSTERED INDEX [CIX_Analytics_Action]
    ON [analytics].[Action]([Value] ASC);

