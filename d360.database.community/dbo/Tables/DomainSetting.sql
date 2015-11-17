CREATE TABLE [dbo].[DomainSetting] (
    [ID]                     INT            IDENTITY (1, 1) NOT NULL,
    [IdpSsoEndpoint]         VARCHAR (1000) NOT NULL,
    [IdpSloEndpoint]         VARCHAR (1000) NOT NULL,
    [IdpDomainCertificateID] INT            NULL,
    [SpDomainCertificateID]  INT            NULL,
    [HashAlgorithmType]      INT            NOT NULL,
    CONSTRAINT [PK_DomainSetting] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_DomainSetting_IdpDomainCertificate] FOREIGN KEY ([IdpDomainCertificateID]) REFERENCES [dbo].[DomainCertificate] ([ID]),
    CONSTRAINT [FK_DomainSetting_SpDomainCertificate] FOREIGN KEY ([SpDomainCertificateID]) REFERENCES [dbo].[DomainCertificate] ([ID])
);

