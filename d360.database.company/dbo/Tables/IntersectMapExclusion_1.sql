CREATE TABLE [dbo].[IntersectMapExclusion] (
    [IntersectMapIDToExclude] INT NOT NULL,
    [IntersectMapID]          INT NOT NULL,
    CONSTRAINT [PK_IntersectMapExclusion] PRIMARY KEY CLUSTERED ([IntersectMapID] ASC, [IntersectMapIDToExclude] ASC),
    CONSTRAINT [FK_IntersectMapExclusion_IntersectMap] FOREIGN KEY ([IntersectMapID]) REFERENCES [dbo].[IntersectMap] ([ID]),
    CONSTRAINT [FK_IntersectMapExclusion_IntersectMap_ToExclude] FOREIGN KEY ([IntersectMapIDToExclude]) REFERENCES [dbo].[IntersectMap] ([ID])
);



