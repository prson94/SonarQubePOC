CREATE TABLE [dbo].[OrganizationDomain] (
    [ID]             INT            IDENTITY (1, 1) NOT NULL,
    [OrganizationID] INT            NOT NULL,
    [Domain]         NVARCHAR (500) NOT NULL,
    CONSTRAINT [PK_OrganizationDomain] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_OrganizationDomain_Organization] FOREIGN KEY ([OrganizationID]) REFERENCES [dbo].[Organization] ([ID])
);

