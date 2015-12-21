CREATE TABLE [dbo].[IntersectMap] (
    [ID]                     INT IDENTITY (1, 1) NOT NULL,
    [MapID]                  INT NOT NULL,
    [SubjectIntersectNodeID] INT NOT NULL,
    [ObjectIntersectNodeID]  INT NOT NULL,
    [PredicatePhraseID]      INT NOT NULL,
    [Type]                   INT DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_IntersectMap] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_IntersectMap_ObjectIntersectNode] FOREIGN KEY ([ObjectIntersectNodeID]) REFERENCES [dbo].[IntersectNode] ([ID]),
    CONSTRAINT [FK_IntersectMap_PredicatePhrase] FOREIGN KEY ([PredicatePhraseID]) REFERENCES [dbo].[PredicatePhrase] ([ID]),
    CONSTRAINT [FK_IntersectMap_SubjectIntersectNode] FOREIGN KEY ([SubjectIntersectNodeID]) REFERENCES [dbo].[IntersectNode] ([ID])
);


GO

CREATE NONCLUSTERED INDEX [IX_IntersectMap_MapID]
    ON [dbo].[IntersectMap]([MapID] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_IntersectMap_PredicateID]
    ON [dbo].[IntersectMap]([PredicatePhraseID] ASC);


GO