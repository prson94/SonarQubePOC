CREATE TABLE [dbo].[ArtifactTypeExportTemplateStyle] (
    [ID]                           INT      IDENTITY (1, 1) NOT NULL,
    [ArtifactTypeExportTemplateID] INT      NOT NULL,
    [Column]                       INT      NOT NULL,
    [Row]                          INT      NOT NULL,
    [Color]                        INT      NULL,
    [BackgroundColor]              INT      NULL,
    [IsBold]                       BIT      DEFAULT ((0)) NOT NULL,
    [UpdatedBy]                    INT      NULL,
    [UpdatedOn]                    DATETIME NULL,
    CONSTRAINT [PK_ArtifactTypeExportTemplateStyle] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_ArtifactTypeExportTemplateStyle_ArtifactTypeExportTemplate] FOREIGN KEY ([ArtifactTypeExportTemplateID]) REFERENCES [dbo].[ArtifactTypeExportTemplate] ([ID]) ON DELETE CASCADE
);

