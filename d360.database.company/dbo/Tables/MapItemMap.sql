CREATE TABLE [dbo].[MapItemMap] (
    [MapID]     INT NOT NULL,
    [MapItemID] INT NOT NULL,
    CONSTRAINT [PK_MapItemMap] PRIMARY KEY CLUSTERED ([MapID] ASC, [MapItemID] ASC),
    CONSTRAINT [FK_MapItemMap_Map] FOREIGN KEY ([MapID]) REFERENCES [dbo].[Map] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_MapItemMap_MapItem] FOREIGN KEY ([MapItemID]) REFERENCES [dbo].[MapItem] ([ID])
);

