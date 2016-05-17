CREATE TABLE [dbo].[DomainItemXref] (
    [ID]                INT IDENTITY (1, 1) NOT NULL,
    [HouseDomainItemID] INT NOT NULL,
    [DomainItemID]      INT NOT NULL,
    [LanguageID]        INT NULL,
    CONSTRAINT [PK_DomainItemXref] PRIMARY KEY CLUSTERED ([ID] ASC),
    FOREIGN KEY ([DomainItemID]) REFERENCES [dbo].[DomainItem] ([ID]),
    FOREIGN KEY ([DomainItemID]) REFERENCES [dbo].[DomainItem] ([ID]),
    FOREIGN KEY ([HouseDomainItemID]) REFERENCES [dbo].[DomainItem] ([ID]),
    FOREIGN KEY ([HouseDomainItemID]) REFERENCES [dbo].[DomainItem] ([ID])
);

