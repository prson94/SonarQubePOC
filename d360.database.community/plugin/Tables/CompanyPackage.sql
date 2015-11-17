CREATE TABLE [plugin].[CompanyPackage] (
    [CompanyID] INT NOT NULL,
    [PackageID] INT NOT NULL,
    CONSTRAINT [PK_PluginCompanyPackage] PRIMARY KEY CLUSTERED ([CompanyID] ASC, [PackageID] ASC)
);

