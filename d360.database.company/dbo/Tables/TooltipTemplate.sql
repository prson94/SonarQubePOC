CREATE TABLE [dbo].[TooltipTemplate] (
    [ID]           INT             IDENTITY (500, 1) NOT NULL,
    [Name]         VARCHAR (50)    NOT NULL,
    [Action]       VARCHAR (50)    NOT NULL,
    [Description]  NVARCHAR (4000) NULL,
    [TemplateBody] NVARCHAR (4000) NOT NULL,
    CONSTRAINT [PK_TooltipTemplate] PRIMARY KEY CLUSTERED ([ID] ASC)
);

