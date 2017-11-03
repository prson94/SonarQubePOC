CREATE TABLE [dbo].[Contract] (
    [ID]             INT            IDENTITY (1, 1) NOT NULL,
    [ContractType]   INT            NOT NULL,
    [OrganizationID] INT            NULL,
    [Title]          NVARCHAR (250) NOT NULL,
    [Body]           NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_Contract] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Contract_Organization] FOREIGN KEY ([OrganizationID]) REFERENCES [dbo].[Organization] ([ID])
);

