CREATE TABLE [dbo].[IntersectNode] (
    [ID]                  INT          IDENTITY (1, 1) NOT NULL,
    [IntersectTypeNodeID] INT          NOT NULL,
    [IntersectID]         INT          NOT NULL,
    [ObjectType]          VARCHAR (50) NOT NULL,
    [ObjectID]            INT          NOT NULL,
    CONSTRAINT [PK_IntersectNode] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_IntersectNode_Intersect] FOREIGN KEY ([IntersectID]) REFERENCES [dbo].[Intersect] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_IntersectNode_IntersectTypeNode] FOREIGN KEY ([IntersectTypeNodeID]) REFERENCES [dbo].[IntersectTypeNode] ([ID])
);


GO
CREATE NONCLUSTERED INDEX [IX_IntersectNode_IntersectID]
    ON [dbo].[IntersectNode]([IntersectID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_IntersectNode_IntersectID-ObjectType-ObjectID]
    ON [dbo].[IntersectNode]([IntersectID] ASC, [ObjectType] ASC, [ObjectID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_IntersectNode_ObjectType-ObjectID]
    ON [dbo].[IntersectNode]([ObjectType] ASC, [ObjectID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_IntersectNode_IntersectTypeNodeID-ObjectID]
    ON [dbo].[IntersectNode]([IntersectTypeNodeID] ASC, [ObjectID] ASC)
    INCLUDE([ID]);

