CREATE TABLE [dbo].[IntersectMapExclusion] (
    [IntersectMapIDToExclude] INT NOT NULL,
    [MapID]                   INT NOT NULL,
    CONSTRAINT [PK_IntersectMapExclusion] PRIMARY KEY CLUSTERED ([MapID] ASC, [IntersectMapIDToExclude] ASC),
    CONSTRAINT [FK_IntersectMapExclusion_IntersectMap_ToExclude] FOREIGN KEY ([IntersectMapIDToExclude]) REFERENCES [dbo].[IntersectMap] ([ID]),
    CONSTRAINT [FK_IntersectMapExclusion_MapID] FOREIGN KEY ([MapID]) REFERENCES [dbo].[Map] ([ID])
);

