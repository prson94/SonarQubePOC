CREATE TABLE [dbo].[TaxonomyTypeClass] (
    [ID]        INT            IDENTITY (1, 1) NOT NULL,
    [Name]      NVARCHAR (250) NOT NULL,
    [UpdatedOn] DATETIME       NULL,
    [UpdatedBy] INT            NULL,
    CONSTRAINT [PK_TaxonomyTypeClass] PRIMARY KEY CLUSTERED ([ID] ASC)
);

