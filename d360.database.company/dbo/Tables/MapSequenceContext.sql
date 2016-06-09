CREATE TABLE [dbo].[MapSequenceContext] (
    [MapSequenceID] INT          NOT NULL,
    [Object]        VARCHAR (50) NOT NULL,
    [ObjectID]      INT          NOT NULL,
    CONSTRAINT [PK_MapSequenceContext] PRIMARY KEY NONCLUSTERED ([MapSequenceID] ASC, [Object] ASC, [ObjectID] ASC),
    CONSTRAINT [FK_MapSequenceContext_MapSequence] FOREIGN KEY ([MapSequenceID]) REFERENCES [dbo].[MapSequence] ([ID]) ON DELETE CASCADE
);

