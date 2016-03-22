CREATE TABLE [dbo].[FieldTypeRelationLookupDisplayField] (
    [ID]                                  INT            IDENTITY (1, 1) NOT NULL,
    [FieldTypeRelationLookupDefinitionID] INT            NOT NULL,
    [FieldTypeID]                         INT            NOT NULL,
    [FieldTypeName]                       NVARCHAR (250) NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FieldTypeRelationLookupDisplayField_FieldTypeRelationLookupDefinitionID] FOREIGN KEY ([FieldTypeRelationLookupDefinitionID]) REFERENCES [dbo].[FieldTypeRelationLookupDefinition] ([ID]) ON DELETE CASCADE
);

