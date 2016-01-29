CREATE TABLE [dbo].[FieldTypeFusionLookupDefinition] (
    [ID]                          INT          IDENTITY (1, 1) NOT NULL,
    [FieldTypeID]                 INT          NOT NULL,
    [SourceFusionAttributeTypeID] INT          NOT NULL,
    [TargetFusionAttributeTypeID] INT          NOT NULL,
    [Display]                     VARCHAR (25) NOT NULL,
    [IsParentChild]               BIT          DEFAULT ((-1)) NOT NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FieldTypeFusionLookupDefinition_FieldType] FOREIGN KEY ([FieldTypeID]) REFERENCES [dbo].[FieldType] ([ID]) ON DELETE CASCADE
);



