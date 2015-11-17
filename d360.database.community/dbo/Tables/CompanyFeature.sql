CREATE TABLE [dbo].[CompanyFeature] (
    [CompanyID] INT NOT NULL,
    [Feature]   INT NOT NULL,
    CONSTRAINT [PK_CompanyFeature] PRIMARY KEY CLUSTERED ([CompanyID] ASC, [Feature] ASC)
);

