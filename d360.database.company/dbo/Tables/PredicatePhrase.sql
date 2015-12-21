CREATE TABLE [dbo].[PredicatePhrase] (
    [ID]          INT            IDENTITY (1, 1) NOT NULL,
    [PredicateID] INT            NOT NULL,
    [Phrase]      NVARCHAR (250) NOT NULL,
    CONSTRAINT [PK_PredicatePhrase] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_PredicatePhrase_Predicate] FOREIGN KEY ([PredicateID]) REFERENCES [dbo].[Predicate] ([ID])
);

