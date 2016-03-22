CREATE TABLE [dbo].[FieldTypeRelationLookupDefinition] (
    [ID]                   INT IDENTITY (1, 1) NOT NULL,
    [FieldTypeID]          INT NOT NULL,
    [IntersectTypeID]      INT NOT NULL,
    [ReferenceType]        INT CONSTRAINT [DF_FieldTypeRelationLookupDefinition_ReferenceType] DEFAULT ((2)) NOT NULL,
    [ChildIntersectTypeID] INT NULL,
    [HideHeader]           BIT CONSTRAINT [DF_FieldTypeRelationLookupDefinition_HideHeader] DEFAULT ((1)) NOT NULL,
    [HideFooter]           BIT CONSTRAINT [DF_FieldTypeRelationLookupDefinition_HideFooter] DEFAULT ((1)) NOT NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FieldTypeRelationLookupDefinition_FieldType] FOREIGN KEY ([FieldTypeID]) REFERENCES [dbo].[FieldType] ([ID]) ON DELETE CASCADE
);

