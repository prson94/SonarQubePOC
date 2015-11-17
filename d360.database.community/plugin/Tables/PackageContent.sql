CREATE TABLE [plugin].[PackageContent] (
    [PackageID] INT           NOT NULL,
    [FileName]  VARCHAR (100) NOT NULL,
    CONSTRAINT [PK_PluginPackageContent] PRIMARY KEY CLUSTERED ([PackageID] ASC, [FileName] ASC)
);

