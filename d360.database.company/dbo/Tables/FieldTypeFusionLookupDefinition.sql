CREATE TABLE [dbo].[FieldTypeFusionLookupDefinition] (
    [Id]                          INT          IDENTITY (1, 1) NOT NULL,
    [FieldTypeID]                 INT          NOT NULL,
    [SourceFusionAttributeTypeID] INT          NOT NULL,
    [TargetFusionAttributeTypeID] INT          NOT NULL,
    [Display]                     VARCHAR (25) NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_FieldTypeFusionLookupDefinition_FieldType] FOREIGN KEY ([FieldTypeID]) REFERENCES [dbo].[FieldType] ([ID]) ON DELETE CASCADE
);

