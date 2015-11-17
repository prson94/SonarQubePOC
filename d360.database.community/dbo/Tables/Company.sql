CREATE TABLE [dbo].[Company] (
    [ID]               INT              IDENTITY (1, 1) NOT NULL,
    [PublicID]         UNIQUEIDENTIFIER CONSTRAINT [DF_Company_ID] DEFAULT (newid()) NOT NULL,
    [Name]             NVARCHAR (250)   NOT NULL,
    [UrlPrefix]        VARCHAR (50)     NOT NULL,
    [DatabaseServer]   VARCHAR (250)    NULL,
    [DatabasePassword] VARCHAR (25)     NULL,
    [Status]           VARCHAR (50)     NULL,
    [DatabaseServerID] INT              NULL,
    [SynchAgentLog]    BIT              CONSTRAINT [DF_Company_SynchAgentLog] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_Company] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [UIX_Company-UrlPrefix] UNIQUE NONCLUSTERED ([UrlPrefix] ASC)
);

