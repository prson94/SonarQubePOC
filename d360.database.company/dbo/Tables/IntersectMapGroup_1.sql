CREATE TABLE [dbo].[IntersectMapGroup] (
    [IntersectMapID] INT NOT NULL,
    [GroupNumber]    INT NOT NULL,
    [ID]             INT IDENTITY (1, 1) NOT NULL,
    CONSTRAINT [FK_IntersectMapGroup_IntersectMap] FOREIGN KEY ([IntersectMapID]) REFERENCES [dbo].[IntersectMap] ([ID]) ON DELETE CASCADE
);




GO
CREATE CLUSTERED INDEX [CIX_IntersectMapGroup]
    ON [dbo].[IntersectMapGroup]([GroupNumber] ASC);

