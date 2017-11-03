CREATE TABLE [dbo].[Organization] (
    [ID]                 INT            IDENTITY (1, 1) NOT NULL,
    [Name]               NVARCHAR (250) NOT NULL,
    [Accepted]           BIT            NULL,
    [AcceptedBy]         INT            NULL,
    [DateAccepted]       DATETIME       NULL,
    [AdministratorEmail] VARCHAR (250)  NULL,
    CONSTRAINT [PK_Organization] PRIMARY KEY CLUSTERED ([ID] ASC)
);

