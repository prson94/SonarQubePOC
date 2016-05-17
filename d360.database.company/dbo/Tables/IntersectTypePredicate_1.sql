CREATE TABLE [dbo].[IntersectTypePredicate] (
    [ID]              INT IDENTITY (1, 1) NOT NULL,
    [PredicateID]     INT CONSTRAINT [DF_IntersectTypePredicate_PredicateID] DEFAULT ((1)) NOT NULL,
    [IntersectTypeID] INT NOT NULL,
    [PredicateType]   INT NULL,
    CONSTRAINT [FK_IntersectTypePredicate_IntersectType] FOREIGN KEY ([IntersectTypeID]) REFERENCES [dbo].[IntersectType] ([ID]) ON DELETE CASCADE
);





