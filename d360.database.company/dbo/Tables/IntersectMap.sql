CREATE TABLE [dbo].[IntersectMap] (
    [ID]                     INT IDENTITY (1, 1) NOT NULL,
    [MapID]                  INT NOT NULL,
    [SubjectIntersectNodeID] INT NOT NULL,
    [ObjectIntersectNodeID]  INT NOT NULL,
    [PredicateID]            INT NOT NULL,
    CONSTRAINT [PK_IntersectMap] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_IntersectMap_Map] FOREIGN KEY ([MapID]) REFERENCES [dbo].[Map] ([ID]),
    CONSTRAINT [FK_IntersectMap_ObjectIntersectNode] FOREIGN KEY ([ObjectIntersectNodeID]) REFERENCES [dbo].[IntersectNode] ([ID]),
    CONSTRAINT [FK_IntersectMap_Predicate] FOREIGN KEY ([PredicateID]) REFERENCES [dbo].[Predicate] ([ID]),
    CONSTRAINT [FK_IntersectMap_SubjectIntersectNode] FOREIGN KEY ([SubjectIntersectNodeID]) REFERENCES [dbo].[IntersectNode] ([ID])
);
GO

CREATE NONCLUSTERED INDEX [IX_IntersectMap_MapID]
    ON [dbo].[IntersectMap]([MapID] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_IntersectMap_PredicateID]
    ON [dbo].[IntersectMap]([PredicateID] ASC);
GO