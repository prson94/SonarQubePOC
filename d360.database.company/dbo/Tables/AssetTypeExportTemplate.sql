CREATE TABLE [dbo].[AssetTypeExportTemplate] (
    [ID]             INT             IDENTITY (1, 1) NOT NULL,
    [AssetTypeID]    INT             NOT NULL,
    [Name]           NVARCHAR (250)  NOT NULL,
    [Description]    NVARCHAR (MAX)  NULL,
    [IncludeFields]  NVARCHAR (1000) NULL,
    [ExportViewType] SMALLINT        NOT NULL,
    [IncludeParent]  BIT             CONSTRAINT [DF_AssetTypeExportTemplate_IncludeParent] DEFAULT ((0)) NOT NULL,
    [IncludeUrl]     BIT             CONSTRAINT [DF_AssetTypeExportTemplate_IncludeUrl] DEFAULT ((0)) NOT NULL,
    [TemplateFile]   VARBINARY (MAX) NULL,
    [CreatedOn]      DATETIME        NULL,
    [CreatedBy]      INT             NULL,
    [UpdatedOn]      DATETIME        NULL,
    [UpdatedBy]      INT             NULL,
    CONSTRAINT [PK_AssetTypeExportTemplate] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_AssetTypeExportTemplate_AssetType] FOREIGN KEY ([AssetTypeID]) REFERENCES [dbo].[AssetType] ([ID])
);

