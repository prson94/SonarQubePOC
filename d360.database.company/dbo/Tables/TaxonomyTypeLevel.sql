CREATE TABLE [dbo].[TaxonomyTypeLevel] (
    [TaxonomyTypeID] INT             NOT NULL,
    [Level]          INT             NOT NULL,
    [Name]           NVARCHAR (250)  NOT NULL,
    [Description]    NVARCHAR (4000) NULL,
    CONSTRAINT [PK_TaxonomyTypeLevel] PRIMARY KEY CLUSTERED ([TaxonomyTypeID] ASC, [Level] ASC),
    CONSTRAINT [FK_TaxonomyTypeLevel_TaxonomyType] FOREIGN KEY ([TaxonomyTypeID]) REFERENCES [dbo].[TaxonomyType] ([ID]) ON DELETE CASCADE
);

