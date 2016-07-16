CREATE TABLE [dbo].[FieldTypeFilteredLookupDefinition](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[FieldTypeID] [int] NOT NULL,
	[Object] [varchar](50) NOT NULL,
	[ObjectID] [int] NOT NULL,
	[HideHeader] [bit] NOT NULL,
	[HideFooter] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)
)

GO

ALTER TABLE [dbo].[FieldTypeFilteredLookupDefinition] ADD  CONSTRAINT [DF_FieldTypeFilteredLookupDefinition_HideHeader]  DEFAULT ((1)) FOR [HideHeader]
GO

ALTER TABLE [dbo].[FieldTypeFilteredLookupDefinition] ADD  CONSTRAINT [DF_FieldTypeFilteredLookupDefinition_HideFooter]  DEFAULT ((1)) FOR [HideFooter]
GO

ALTER TABLE [dbo].[FieldTypeFilteredLookupDefinition]  WITH CHECK ADD  CONSTRAINT [FK_FieldTypeFilteredLookupDefinition_FieldType] FOREIGN KEY([FieldTypeID])
REFERENCES [dbo].[FieldType] ([ID])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[FieldTypeFilteredLookupDefinition] CHECK CONSTRAINT [FK_FieldTypeFilteredLookupDefinition_FieldType]
GO

CREATE TABLE [dbo].[FieldTypeFilteredLookupDisplayField](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[FieldTypeFilteredLookupDefinitionID] [int] NOT NULL,
	[FieldTypeID] [int] NOT NULL,
	[FieldTypeName] [nvarchar](250) NULL,
	[Show] [bit] NOT NULL,
	[SortOrder] [int] NULL,
	[Filter] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)
)

GO

ALTER TABLE [dbo].[FieldTypeFilteredLookupDisplayField] ADD  CONSTRAINT [DF_FieldTypeFilteredLookupDisplayField_Show]  DEFAULT ((1)) FOR [Show]
GO

ALTER TABLE [dbo].[FieldTypeFilteredLookupDisplayField]  WITH CHECK ADD  CONSTRAINT [FK_FieldTypeFilteredLookupDisplayField_FieldTypeFilteredLookupDefinition] FOREIGN KEY([FieldTypeFilteredLookupDefinitionID])
REFERENCES [dbo].[FieldTypeFilteredLookupDefinition] ([ID])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[FieldTypeFilteredLookupDisplayField] CHECK CONSTRAINT [FK_FieldTypeFilteredLookupDisplayField_FieldTypeFilteredLookupDefinition]
GO


