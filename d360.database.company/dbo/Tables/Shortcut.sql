CREATE TABLE [dbo].[Shortcut] (
    [ID]              INT            IDENTITY (1, 1) NOT NULL,
    [Name]            VARCHAR (250)  NULL,
    [Icon]            VARCHAR (50)   NULL,
    [IconUrl]         VARCHAR (250)  NULL,
    [Url]             VARCHAR (250)  NULL,
    [Description]     NVARCHAR (500) NULL,
    [IconColor]       VARCHAR (100)  NULL,
    [TitleColor]      VARCHAR (100)  NULL,
    [BackgroundColor] VARCHAR (100)  NULL,
    [DisplayOrder]    INT            DEFAULT ((100)) NOT NULL,
    [LinkTarget]      INT            DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_Shortcut] PRIMARY KEY CLUSTERED ([ID] ASC)
);

