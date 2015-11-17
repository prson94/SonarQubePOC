CREATE TABLE [dbo].[IntersectTypeNode] (
    [ID]              INT            IDENTITY (1, 1) NOT NULL,
    [IntersectTypeID] INT            NOT NULL,
    [ObjectType]      VARCHAR (50)   NOT NULL,
    [ObjectID]        INT            NOT NULL,
    [Order]           SMALLINT       NOT NULL,
    [MenuDisplayText] NVARCHAR (250) NULL,
    CONSTRAINT [PK_IntersectTypeNode] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_IntersectTypeNode_IntersectType] FOREIGN KEY ([IntersectTypeID]) REFERENCES [dbo].[IntersectType] ([ID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_IntersectTypeNode_ObjectType-ObjectID]
    ON [dbo].[IntersectTypeNode]([ObjectType] ASC, [ObjectID] ASC);

