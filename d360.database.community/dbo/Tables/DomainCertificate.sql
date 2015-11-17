CREATE TABLE [dbo].[DomainCertificate] (
    [ID]       INT             IDENTITY (1, 1) NOT NULL,
    [Name]     NVARCHAR (250)  NOT NULL,
    [File]     VARBINARY (MAX) NOT NULL,
    [Password] NVARCHAR (250)  NULL,
    CONSTRAINT [PK_DomainCertificate] PRIMARY KEY CLUSTERED ([ID] ASC)
);

