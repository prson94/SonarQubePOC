CREATE TABLE [dbo].[AssetTypeStyle] (
    [ID]            INT          NOT NULL,
    [IconBackColor] VARCHAR (7)  NOT NULL,
    [IconForeColor] VARCHAR (7)  NOT NULL,
    [IconText]      VARCHAR (25) NULL,
    [Icon]          VARCHAR (50) NULL,
    CONSTRAINT [PK_AssetTypeStyle] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_AssetTypeStyle_AssetType] FOREIGN KEY ([ID]) REFERENCES [dbo].[AssetType] ([ID])
);

