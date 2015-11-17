CREATE TABLE [dbo].[ResourceType] (
    [ID]          INT            IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (250) NOT NULL,
    [DateCreated] DATETIME       CONSTRAINT [DF_ResourceType_DateCreated] DEFAULT (getutcdate()) NOT NULL,
    [DateUpdated] DATETIME       CONSTRAINT [DF_ResourceType_DateUpdated] DEFAULT (getutcdate()) NOT NULL,
    CONSTRAINT [PK_ResourceType] PRIMARY KEY CLUSTERED ([ID] ASC)
);

