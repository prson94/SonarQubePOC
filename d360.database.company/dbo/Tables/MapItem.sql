CREATE TABLE [dbo].[MapItem] (
    [ID]                INT      IDENTITY (1, 1) NOT NULL,
    [SourceIntersectID] INT      NOT NULL,
    [TargetIntersectID] INT      NULL,
    [CreatedBy]         INT      NOT NULL,
    [CreatedOn]         DATETIME NOT NULL,
    [UpdatedBy]         INT      NOT NULL,
    [UpdatedOn]         DATETIME NOT NULL,
	[Owner]		        VARCHAR (50) NULL,
    CONSTRAINT [PK_MapItem] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);




GO
CREATE NONCLUSTERED INDEX [IX_MapItem_TargetIntersectID]
    ON [dbo].[MapItem]([TargetIntersectID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_MapItem_SourceIntersectID]
    ON [dbo].[MapItem]([SourceIntersectID] ASC);

