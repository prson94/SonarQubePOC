CREATE TABLE [dbo].[OrganizationResource] (
    [OrganizationID] INT      NOT NULL,
    [ResourceID]     INT      NOT NULL,
    [Accepted]       BIT      NULL,
    [DateAccepted]   DATETIME NULL,
    CONSTRAINT [PK_OrganizationResource] PRIMARY KEY CLUSTERED ([OrganizationID] ASC, [ResourceID] ASC),
    CONSTRAINT [FK_OrganizationResource_Organization] FOREIGN KEY ([OrganizationID]) REFERENCES [dbo].[Organization] ([ID])
);

