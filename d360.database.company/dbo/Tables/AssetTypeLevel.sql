CREATE TABLE [dbo].[AssetTypeLevel] (
    [AssetTypeID] INT             NOT NULL,
    [Level]       INT             NOT NULL,
    [Name]        NVARCHAR (250)  NOT NULL,
    [Description] NVARCHAR (4000) NULL,
    CONSTRAINT [PK_AssetTypeLevel] PRIMARY KEY NONCLUSTERED ([AssetTypeID] ASC, [Level] ASC),
    CONSTRAINT [FK_AssetTypeLevel_AssetType] FOREIGN KEY ([AssetTypeID]) REFERENCES [dbo].[AssetType] ([ID])
);

