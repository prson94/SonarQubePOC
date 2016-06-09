CREATE TABLE [dbo].[IntersectGroup] (
    [ID]          INT      IDENTITY (1, 1) NOT NULL,
    [IntersectID] INT      NOT NULL,
    [GroupNumber] INT      NOT NULL,
    [CreatedBy]   INT      NOT NULL,
    [CreatedOn]   DATETIME NOT NULL,
    [UpdatedBy]   INT      NOT NULL,
    [UpdatedOn]   DATETIME NOT NULL,
    CONSTRAINT [PK_IntersectGroup] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_IntersectGroup_Intersect] FOREIGN KEY ([IntersectID]) REFERENCES [dbo].[Intersect] ([ID]) ON DELETE CASCADE
);

