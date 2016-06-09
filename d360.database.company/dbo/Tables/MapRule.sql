CREATE TABLE [dbo].[MapRule] (
    [ID]             INT             IDENTITY (1, 1) NOT NULL,
    [Name]           NVARCHAR (250)  NULL,
    [Transformation] NVARCHAR (4000) NULL,
    [CreatedBy]      INT             NOT NULL,
    [CreatedOn]      DATETIME        NOT NULL,
    [UpdatedBy]      INT             NOT NULL,
    [UpdatedOn]      DATETIME        NOT NULL,
    CONSTRAINT [PK_MapRule] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);

