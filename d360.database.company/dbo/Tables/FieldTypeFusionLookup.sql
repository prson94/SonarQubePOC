CREATE TABLE [dbo].[FieldTypeFusionLookupDefinition]
(
	[Id] INT NOT NULL IDENTITY PRIMARY KEY,
	[FieldTypeID] INT NOT NULL,
	[SourceFusionAttributeTypeID] INT NOT NULL,
	[TargetFusionAttributeTypeID] INT NOT NULL,
	[Display] VARCHAR(25) NOT NULL,
	CONSTRAINT [FK_FieldTypeFusionLookupDefinition_FieldType] FOREIGN KEY ([FieldTypeID]) REFERENCES [dbo].[FieldType] ([ID]) ON DELETE CASCADE
)

