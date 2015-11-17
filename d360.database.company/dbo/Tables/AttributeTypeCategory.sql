CREATE TABLE [dbo].[AttributeTypeCategory] (
    [ID]          INT             IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (250)  NOT NULL,
    [Description] NVARCHAR (4000) NULL,
    CONSTRAINT [PK_AttributeTypeCategory] PRIMARY KEY CLUSTERED ([ID] ASC)
);

