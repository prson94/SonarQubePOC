CREATE TABLE [dbo].[CompanySetting] (
    [CompanyID] INT            NOT NULL,
    [SettingID] INT            NOT NULL,
    [Value]     VARCHAR (4000) NOT NULL,
    CONSTRAINT [PK_CompanySetting] PRIMARY KEY CLUSTERED ([CompanyID] ASC, [SettingID] ASC),
    CONSTRAINT [FK_CompanySetting_CompanySetting] FOREIGN KEY ([CompanyID], [SettingID]) REFERENCES [dbo].[CompanySetting] ([CompanyID], [SettingID])
);

