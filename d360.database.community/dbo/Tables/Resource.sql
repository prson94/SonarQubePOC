CREATE TABLE [dbo].[Resource] (
    [ID]                     INT            IDENTITY (1, 1) NOT NULL,
    [ResourceTypeID]         INT            NOT NULL,
    [Username]               NVARCHAR (250) NOT NULL,
    [Password]               NVARCHAR (50)  NOT NULL,
    [LastName]               NVARCHAR (250) NOT NULL,
    [FirstName]              NVARCHAR (250) NOT NULL,
    [Email]                  NVARCHAR (500) NOT NULL,
    [APIPublicKey]           VARCHAR (25)   CONSTRAINT [DF_Resource_APIPublicKey] DEFAULT ([dbo].[GenerateAPIKeyWrapper]((25))) NOT NULL,
    [APIPrivateKey]          VARCHAR (50)   CONSTRAINT [DF_Resource_APIPrivateKey] DEFAULT ([dbo].[GenerateAPIKeyWrapper]((50))) NOT NULL,
    [Status]                 VARCHAR (25)   NOT NULL,
    [DateLastLoggedIn]       DATETIME       NULL,
    [ApiReadOnlyAccessToken] VARCHAR (50)   CONSTRAINT [DF_Resource_ApiReadOnlyAccessCode] DEFAULT ([dbo].[GenerateReadOnlyAccessToken]()) NOT NULL,
    CONSTRAINT [PK_Resource] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Resource_ResourceType] FOREIGN KEY ([ResourceTypeID]) REFERENCES [dbo].[ResourceType] ([ID]) ON DELETE CASCADE
);

