CREATE TABLE [dbo].[FieldTypeFusionLookupDisplayField] (
    [ID]                                INT            IDENTITY (1, 1) NOT NULL,
    [FieldTypeFusionLookupDefinitionID] INT            NOT NULL,
    [FieldTypeID]                       INT            NOT NULL,
    [FieldTypeName]                     NVARCHAR (250) NULL,
    [Show]                              BIT            CONSTRAINT [DF_FieldTypeFusionLookupDisplayField_Show] DEFAULT ((1)) NOT NULL,
    [SortOrder]                         INT            NULL,
    [FilterValue]                       NVARCHAR (250) NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FieldTypeFusionLookupDisplayField_FieldTypeFusionLookupDefinitionID] FOREIGN KEY ([FieldTypeFusionLookupDefinitionID]) REFERENCES [dbo].[FieldTypeFusionLookupDefinition] ([ID]) ON DELETE CASCADE
);



