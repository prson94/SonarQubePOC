CREATE TABLE [dbo].[AssetTypeQuery] (
    [ID]    INT            NOT NULL,
    [Query] NVARCHAR (250) NOT NULL,
    CONSTRAINT [PK_AssetTypeQuery] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_AssetTypeQuery_AssetType] FOREIGN KEY ([ID]) REFERENCES [dbo].[AssetType] ([ID])
);

