CREATE TABLE [dbo].[FieldTypeFusionLookupDefinition] (
    [ID]                          INT IDENTITY (1, 1) NOT NULL,
    [FieldTypeID]                 INT NOT NULL,
    [SourceFusionAttributeTypeID] INT NOT NULL,
    [TargetFusionAttributeTypeID] INT NULL,
    [ReferenceType]               INT CONSTRAINT [DF_FieldTypeFusionLookupDefinition_ReferenceType] DEFAULT ((2)) NOT NULL,
    [HideHeader]                  BIT CONSTRAINT [DF_FieldTypeFusionLookupDefinition_HideHeader] DEFAULT ((1)) NOT NULL,
    [HideFooter]                  BIT CONSTRAINT [DF_FieldTypeFusionLookupDefinition_HideFooter] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_FieldTypeFusionLookupDefinition] PRIMARY KEY CLUSTERED ([ID] ASC)
);





