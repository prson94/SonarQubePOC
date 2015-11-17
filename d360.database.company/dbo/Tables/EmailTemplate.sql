CREATE TABLE [dbo].[EmailTemplate] (
    [ID]              INT            NOT NULL,
    [Name]            NVARCHAR (50)  NOT NULL,
    [Action]          VARCHAR (50)   NOT NULL,
    [Description]     NVARCHAR (MAX) NULL,
    [TemplateSubject] NVARCHAR (250) NOT NULL,
    [TemplateBody]    NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_EmailTemplate] PRIMARY KEY CLUSTERED ([ID] ASC)
);

