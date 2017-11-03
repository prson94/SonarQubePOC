CREATE TABLE [dbo].[OrganizationInvitation] (
    [ID]             INT            IDENTITY (1, 1) NOT NULL,
    [OrganizationID] INT            NOT NULL,
    [Email]          NVARCHAR (500) NOT NULL,
    CONSTRAINT [PK_OrganizationInvitation] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_OrganizationInvitation_Organization] FOREIGN KEY ([OrganizationID]) REFERENCES [dbo].[Organization] ([ID])
);

