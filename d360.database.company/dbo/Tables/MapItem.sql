CREATE TABLE [dbo].[MapItem] (
    [ID]          INT      IDENTITY (1, 1) NOT NULL,
    [MapID]       INT      NOT NULL,
    [IntersectID] INT      NOT NULL,
    [IsSource]    BIT      NOT NULL,
    [CreatedBy]   INT      NOT NULL,
    [CreatedOn]   DATETIME NOT NULL,
    [UpdatedBy]   INT      NOT NULL,
    [UpdatedOn]   DATETIME NOT NULL,
    CONSTRAINT [PK_MapItem] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_MapItem_Map] FOREIGN KEY ([MapID]) REFERENCES [dbo].[Map] ([ID]) ON DELETE CASCADE
);

