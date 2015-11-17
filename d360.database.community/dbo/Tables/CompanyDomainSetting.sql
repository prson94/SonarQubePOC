CREATE TABLE [dbo].[CompanyDomainSetting] (
    [CompanyID]          INT           NOT NULL,
    [DomainSettingID]    INT           NOT NULL,
    [AuthenticationType] INT           NOT NULL,
    [AllowNewUserLogin]  BIT           NOT NULL,
    [UrlPrefix]          NVARCHAR (50) NOT NULL,
    [IsPrimary]          BIT           CONSTRAINT [DF_CompanyDomainSetting_IsPrimary] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_CompanyDomainSetting] PRIMARY KEY CLUSTERED ([CompanyID] ASC, [DomainSettingID] ASC),
    CONSTRAINT [FK_CompanyDomainSetting_Company] FOREIGN KEY ([CompanyID]) REFERENCES [dbo].[Company] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_CompanyDomainSetting_DomainSetting] FOREIGN KEY ([DomainSettingID]) REFERENCES [dbo].[DomainSetting] ([ID]) ON DELETE CASCADE
);


GO
CREATE TRIGGER [dbo].[CompanyDomainSetting_After]
   ON  [dbo].[CompanyDomainSetting] 
   AFTER INSERT, UPDATE, DELETE
AS 
	SET NOCOUNT ON;
	update	CacheStatus
	set		ShouldRecache = 1
	where	Name = 'CompanyPrefixes'
