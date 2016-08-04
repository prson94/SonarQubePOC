CREATE TABLE [dbo].[FieldTypeFilteredLookupDisplayField] (
    [ID]                                  INT            IDENTITY (1, 1) NOT NULL,
    [FieldTypeFilteredLookupDefinitionID] INT            NOT NULL,
    [FieldTypeID]                         INT            NOT NULL,
    [FieldTypeName]                       NVARCHAR (250) NULL,
    [Show]                                BIT            CONSTRAINT [DF_FieldTypeFilteredLookupDisplayField_Show] DEFAULT ((1)) NOT NULL,
    [SortOrder]                           INT            NULL,
    [Filter]                              BIT            NOT NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FieldTypeFilteredLookupDisplayField_FieldTypeFilteredLookupDefinition] FOREIGN KEY ([FieldTypeFilteredLookupDefinitionID]) REFERENCES [dbo].[FieldTypeFilteredLookupDefinition] ([ID]) ON DELETE CASCADE
);

