CREATE TABLE [dbo].[IntersectTypePredicate] (
    [ID]              INT IDENTITY (1, 1) NOT NULL,
    [PredicateID]     INT NOT NULL,
    [IntersectTypeID] INT NOT NULL,
    CONSTRAINT [FK_IntersectTypePredicate_IntersectType] FOREIGN KEY ([IntersectTypeID]) REFERENCES [dbo].[IntersectType] ([ID]),
    CONSTRAINT [FK_IntersectTypePredicate_Predicate] FOREIGN KEY ([PredicateID]) REFERENCES [dbo].[Predicate] ([ID])
);

