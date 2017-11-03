CREATE TABLE [dbo].[AssetTypeExportTemplateStyle] (
    [ID]                        INT IDENTITY (1, 1) NOT NULL,
    [AssetTypeExportTemplateID] INT NOT NULL,
    [Column]                    INT NOT NULL,
    [Row]                       INT NOT NULL,
    [Color]                     INT NULL,
    [BackgroundColor]           INT NULL,
    [IsBold]                    BIT CONSTRAINT [DF_AssetTypeExportTemplateStyle_IsBold] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_AssetTypeExportTemplateStyle] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_AssetTypeExportTemplateStyle_AssetTypeExportTemplate] FOREIGN KEY ([AssetTypeExportTemplateID]) REFERENCES [dbo].[AssetTypeExportTemplate] ([ID])
);

