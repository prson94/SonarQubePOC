CREATE TABLE [plugin].[FusionAttributeTypeFieldType] (
    [FusionAttributeTypeID] INT NOT NULL,
    [FieldTypeID]           INT NOT NULL,
    CONSTRAINT [PK_Plugin_FusionAttributeTypeFieldType] PRIMARY KEY CLUSTERED ([FusionAttributeTypeID] ASC, [FieldTypeID] ASC)
);

