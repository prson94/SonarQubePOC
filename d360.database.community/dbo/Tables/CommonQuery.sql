CREATE TABLE [dbo].[CommonQuery] (
    [ID]   INT             IDENTITY (1, 1) NOT NULL,
    [Name] NVARCHAR (250)  NOT NULL,
    [Body] NVARCHAR (4000) NOT NULL,
    CONSTRAINT [PK_CommonQuery] PRIMARY KEY CLUSTERED ([ID] ASC)
);

