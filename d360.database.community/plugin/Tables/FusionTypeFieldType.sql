CREATE TABLE [plugin].[FusionTypeFieldType] (
    [FusionTypeID] INT NOT NULL,
    [FieldTypeID]  INT NOT NULL,
    CONSTRAINT [PK_Plugin_FusionTypeFieldType] PRIMARY KEY CLUSTERED ([FusionTypeID] ASC, [FieldTypeID] ASC)
);

