CREATE TABLE [dbo].[OrganizationRegistration] (
    [ID]                    UNIQUEIDENTIFIER CONSTRAINT [DF_OrganizationRegistration_ID] DEFAULT (newid()) NOT NULL,
    [OrganizationID]        INT              NOT NULL,
    [Email]                 NVARCHAR (500)   NOT NULL,
    [Step]                  INT              NOT NULL,
    [RegisteredStartedOn]   DATETIME         NOT NULL,
    [RegisteredCompletedOn] DATETIME         NULL,
    CONSTRAINT [PK_OrganizationRegistration] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_OrganizationRegistration_Organization] FOREIGN KEY ([OrganizationID]) REFERENCES [dbo].[Organization] ([ID])
);

