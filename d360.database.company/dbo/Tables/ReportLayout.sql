CREATE TABLE [dbo].[ReportLayout] (
    [ID]                   INT             IDENTITY (1, 1) NOT NULL,
    [Name]                 NVARCHAR (250)  NOT NULL,
    [Description]          NVARCHAR (1000) NULL,
    [Template]             NVARCHAR (1000) NOT NULL,
    [NumberOfContentAreas] INT             NOT NULL,
    CONSTRAINT [PK_ReportLayout] PRIMARY KEY CLUSTERED ([ID] ASC)
);

