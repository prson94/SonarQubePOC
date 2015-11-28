CREATE TABLE [dbo].[Predicate] (
    [ID]     INT            IDENTITY (1, 1) NOT NULL,
    [Name]   NVARCHAR (100) NOT NULL,
    [Phrase] NVARCHAR (250) NOT NULL,
    CONSTRAINT [PK_Predicate] PRIMARY KEY CLUSTERED ([ID] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_Predicate_Name]
    ON [dbo].[Predicate]([Name] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Predicate_Phrase]
    ON [dbo].[Predicate]([Phrase] ASC);
GO